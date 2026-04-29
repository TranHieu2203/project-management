import { Injectable, inject } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { Store } from '@ngrx/store';
import { of, switchMap, map, catchError, forkJoin } from 'rxjs';
import { withLatestFrom } from 'rxjs/operators';
import { GanttActions } from './gantt.actions';
import { TasksApiService } from '../../projects/services/tasks-api.service';
import { GanttAdapterService } from '../services/gantt-adapter.service';
import { AppState } from '../../../core/store/app.state';
import { GanttTaskEdit } from '../models/gantt.model';
import { ProjectTask, UpdateTaskPayload } from '../../projects/models/task.model';
import { selectGanttState } from './gantt.selectors';
import { selectTaskEntities } from '../../projects/store/tasks.selectors';

function formatDateOnly(date: Date): string {
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, '0');
  const d = String(date.getDate()).padStart(2, '0');
  return `${y}-${m}-${d}`;
}

function buildUpdatePayload(edit: GanttTaskEdit, original: ProjectTask): UpdateTaskPayload {
  return {
    parentId: original.parentId,
    type: original.type,
    vbs: original.vbs ?? undefined,
    name: edit.newName ?? original.name,
    priority: original.priority,
    status: edit.newStatus ?? original.status,
    notes: original.notes ?? undefined,
    plannedStartDate: edit.newPlannedStart
      ? formatDateOnly(edit.newPlannedStart)
      : original.plannedStartDate ?? undefined,
    plannedEndDate: edit.newPlannedEnd
      ? formatDateOnly(edit.newPlannedEnd)
      : original.plannedEndDate ?? undefined,
    actualStartDate: original.actualStartDate ?? undefined,
    actualEndDate: original.actualEndDate ?? undefined,
    plannedEffortHours: original.plannedEffortHours ?? undefined,
    percentComplete: edit.newPercentComplete !== undefined
      ? edit.newPercentComplete
      : original.percentComplete ?? undefined,
    assigneeUserId: original.assigneeUserId ?? undefined,
    sortOrder: original.sortOrder,
    predecessors: original.predecessors.map(p => ({
      predecessorId: p.predecessorId,
      dependencyType: p.dependencyType,
    })),
  };
}

function parseDate(dateStr: string): Date {
  const [year, month, day] = dateStr.split('-').map(Number);
  return new Date(year, month - 1, day);
}

@Injectable()
export class GanttEffects {
  private readonly actions$ = inject(Actions);
  private readonly store = inject(Store<AppState>);
  private readonly tasksApiService = inject(TasksApiService);
  private readonly adapter = inject(GanttAdapterService);

  discardGanttEdits$ = createEffect(() =>
    this.actions$.pipe(
      ofType(GanttActions.discardGanttEdits),
      withLatestFrom(this.store.select(selectGanttState)),
      switchMap(([_, ganttState]) => {
        const projectId = ganttState.projectId;
        if (!projectId) return of();
        return of(GanttActions.loadGanttData({ projectId }));
      })
    )
  );

  loadGanttData$ = createEffect(() =>
    this.actions$.pipe(
      ofType(GanttActions.loadGanttData),
      switchMap(({ projectId }) =>
        this.tasksApiService.getTasks(projectId).pipe(
          map(tasks => GanttActions.loadGanttDataSuccess({ tasks: this.adapter.adapt(tasks) })),
          catchError(err => {
            const msg = err?.status === 404
              ? 'Dự án không tồn tại hoặc bạn không có quyền truy cập.'
              : (err?.error?.detail ?? err?.message ?? 'Lỗi tải dữ liệu Gantt');
            return of(GanttActions.loadGanttDataFailure({ error: msg }));
          })
        )
      )
    )
  );

  saveGanttEdits$ = createEffect(() =>
    this.actions$.pipe(
      ofType(GanttActions.saveGanttEdits),
      withLatestFrom(
        this.store.select(selectGanttState),
        this.store.select(selectTaskEntities)
      ),
      switchMap(([_, ganttState, taskEntities]) => {
        const dirtyEdits = Object.values(ganttState.dirtyTasks);
        const projectId = ganttState.projectId!;

        if (dirtyEdits.length === 0) {
          return of(GanttActions.saveGanttEditsSuccess({ updatedTasks: [] }));
        }

        const saveRequests = dirtyEdits.map(edit => {
          const original = taskEntities[edit.taskId] as ProjectTask | undefined;
          if (!original) {
            return of({ ok: false as const, edit, err: { status: 404, error: { detail: 'Task not found' } } });
          }
          const payload = buildUpdatePayload(edit, original);
          return this.tasksApiService.updateTask(projectId, edit.taskId, payload, edit.originalVersion).pipe(
            map(updated => ({ ok: true as const, updated })),
            catchError(err => of({ ok: false as const, edit, err }))
          );
        });

        return forkJoin(saveRequests).pipe(
          switchMap(results => {
            const conflictResult = results.find(r => !r.ok && (r as any).err?.status === 409);
            if (conflictResult) {
              const { edit, err } = conflictResult as { ok: false; edit: GanttTaskEdit; err: any };
              return of(GanttActions.ganttConflict({
                conflict: {
                  taskId: edit.taskId,
                  serverTaskJson: JSON.stringify(
                    err.error?.extensions?.current ?? err.error?.current ?? {}
                  ),
                  localEdit: edit,
                  serverETag: err.error?.extensions?.eTag ?? '',
                },
              }));
            }

            const preconditionResult = results.find(r => !r.ok && (r as any).err?.status === 412);
            if (preconditionResult) {
              return of(GanttActions.loadGanttData({ projectId }));
            }

            const notFoundResult = results.find(r => !r.ok && (r as any).err?.status === 404);
            if (notFoundResult) {
              return of(GanttActions.saveGanttEditsFailure({ error: 'Không có quyền truy cập dự án' }));
            }

            const otherFailure = results.find(r => !r.ok);
            if (otherFailure) {
              const err = (otherFailure as any).err;
              return of(GanttActions.saveGanttEditsFailure({
                error: err?.error?.detail ?? 'Lỗi lưu Gantt',
              }));
            }

            const updatedTasks = results
              .filter(r => r.ok)
              .map(r => {
                const { updated } = r as { ok: true; updated: ProjectTask };
                return {
                  id: updated.id,
                  version: updated.version,
                  plannedStart: updated.plannedStartDate ? parseDate(updated.plannedStartDate) : null,
                  plannedEnd: updated.plannedEndDate ? parseDate(updated.plannedEndDate) : null,
                  name: updated.name,
                  status: updated.status,
                  percentComplete: updated.percentComplete ?? undefined,
                };
              });

            return of(GanttActions.saveGanttEditsSuccess({ updatedTasks }));
          })
        );
      })
    )
  );
}
