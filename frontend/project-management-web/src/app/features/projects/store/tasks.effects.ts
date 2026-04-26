import { Injectable, inject } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { catchError, map, mergeMap, of, switchMap } from 'rxjs';
import { TasksApiService } from '../services/tasks-api.service';
import { TasksActions } from './tasks.actions';

@Injectable()
export class TasksEffects {
  private readonly actions$ = inject(Actions);
  private readonly tasksApi = inject(TasksApiService);

  loadTasks$ = createEffect(() =>
    this.actions$.pipe(
      ofType(TasksActions.loadTasks),
      switchMap(({ projectId }) =>
        this.tasksApi.getTasks(projectId).pipe(
          map(tasks => TasksActions.loadTasksSuccess({ tasks })),
          catchError(err =>
            of(TasksActions.loadTasksFailure({
              error: err.error?.detail ?? err.error?.title ?? 'Không thể tải tasks.',
            }))
          )
        )
      )
    )
  );

  createTask$ = createEffect(() =>
    this.actions$.pipe(
      ofType(TasksActions.createTask),
      switchMap(({ projectId, request }) =>
        this.tasksApi.createTask(projectId, request).pipe(
          map(task => TasksActions.createTaskSuccess({ task })),
          catchError(err =>
            of(TasksActions.createTaskFailure({
              error: err.error?.detail ?? err.error?.title ?? 'Không thể tạo task.',
            }))
          )
        )
      )
    )
  );

  updateTask$ = createEffect(() =>
    this.actions$.pipe(
      ofType(TasksActions.updateTask),
      mergeMap(({ projectId, taskId, request, version }) =>
        this.tasksApi.updateTask(projectId, taskId, request, version).pipe(
          map(task => TasksActions.updateTaskSuccess({ task })),
          catchError(err => {
            if (err.status === 409) {
              // current và eTag ở ROOT level của body (không phải extensions.current)
              return of(TasksActions.updateTaskConflict({
                serverState: err.error.current,
                eTag: err.error.eTag,
              }));
            }
            return of(TasksActions.updateTaskFailure({
              error: err.error?.detail ?? err.error?.title ?? 'Không thể cập nhật task.',
            }));
          })
        )
      )
    )
  );

  deleteTask$ = createEffect(() =>
    this.actions$.pipe(
      ofType(TasksActions.deleteTask),
      switchMap(({ projectId, taskId, version }) =>
        this.tasksApi.deleteTask(projectId, taskId, version).pipe(
          map(() => TasksActions.deleteTaskSuccess({ taskId })),
          catchError(err =>
            of(TasksActions.deleteTaskFailure({
              error: err.error?.detail ?? err.error?.title ?? 'Không thể xóa task.',
            }))
          )
        )
      )
    )
  );
}
