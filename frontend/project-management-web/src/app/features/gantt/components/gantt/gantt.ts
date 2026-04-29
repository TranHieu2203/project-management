import {
  Component, OnInit, OnDestroy, inject, ChangeDetectionStrategy,
  DestroyRef, ElementRef, ViewChild, DOCUMENT, signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Store } from '@ngrx/store';
import { map, Observable } from 'rxjs';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatBadgeModule } from '@angular/material/badge';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AsyncPipe } from '@angular/common';
import { DeadlineAlertBannerComponent } from '../../../projects/components/deadline-alert-banner/deadline-alert-banner';
import { DeadlineAlertService } from '../../../projects/services/deadline-alert.service';
import { FilterBarComponent } from '../../../projects/components/filter-bar/filter-bar';
import { FilterCriteria } from '../../../projects/models/filter.model';
import { isEmpty, parseQueryParams, serializeFilter } from '../../../projects/models/filter.utils';
import { MembersApiService } from '../../../projects/services/members-api.service';
import { ProjectMember } from '../../../projects/models/project.model';
import { selectCurrentUser } from '../../../../features/auth/store/auth.selectors';

import { AppState } from '../../../../core/store/app.state';
import { GanttActions } from '../../store/gantt.actions';
import {
  selectGanttTasks,
  selectGanttLoading,
  selectGanttError,
  selectGranularity,
  selectDirtyTasksCount,
  selectSaving,
  selectConflict,
  selectProjectByGanttId,
} from '../../store/gantt.selectors';
import { GanttConflictState, GanttGranularity, GanttTask, GanttTaskEdit } from '../../models/gantt.model';
import { GanttLeftPanelComponent, GanttInlineEditEvent } from '../gantt-left-panel/gantt-left-panel';
import { GanttTimelineComponent } from '../gantt-timeline/gantt-timeline';
import { ConflictDialogComponent, ConflictDialogResult } from '../../../../shared/components/conflict-dialog/conflict-dialog';
import { ConfirmDialogComponent, ConfirmDialogData } from '../../../../shared/components/confirm-dialog/confirm-dialog';
import { TaskFormComponent, TaskFormData } from '../../../projects/components/task-form/task-form';
import { TasksApiService } from '../../../projects/services/tasks-api.service';
import { TasksActions } from '../../../projects/store/tasks.actions';
import { Project } from '../../../projects/models/project.model';

@Component({
  selector: 'app-gantt',
  standalone: true,
  imports: [
    AsyncPipe,
    RouterLink,
    MatButtonToggleModule,
    MatProgressSpinnerModule,
    MatIconModule,
    MatButtonModule,
    MatBadgeModule,
    MatTooltipModule,
    GanttLeftPanelComponent,
    GanttTimelineComponent,
    DeadlineAlertBannerComponent,
    FilterBarComponent,
  ],
  templateUrl: './gantt.html',
  styleUrl: './gantt.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GanttComponent implements OnInit, OnDestroy {
  private readonly store = inject(Store<AppState>);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  private readonly tasksApi = inject(TasksApiService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly deadlineService = inject(DeadlineAlertService);
  private readonly membersApi = inject(MembersApiService);

  readonly today = this.deadlineService.getLocalDateString();

  tasks$: Observable<GanttTask[]> = this.store.select(selectGanttTasks);
  readonly ganttDeadlineCounts$ = this.tasks$.pipe(
    map(tasks => {
      let overdue = 0, dueToday = 0, dueSoon = 0;
      for (const t of tasks) {
        const endStr = t.plannedEnd ? this.deadlineService.dateToLocalString(t.plannedEnd) : null;
        const s = this.deadlineService.getDeadlineStatusRaw(endStr, t.type, t.status, this.today);
        if (s === 'overdue') overdue++;
        else if (s === 'due-today') dueToday++;
        else if (s === 'due-soon') dueSoon++;
      }
      return { overdue, dueToday, dueSoon };
    }),
  );
  loading$: Observable<boolean> = this.store.select(selectGanttLoading);
  error$: Observable<string | null> = this.store.select(selectGanttError);
  granularity$: Observable<GanttGranularity> = this.store.select(selectGranularity);
  dirtyCount$: Observable<number> = this.store.select(selectDirtyTasksCount);
  saving$: Observable<boolean> = this.store.select(selectSaving);
  conflict$: Observable<GanttConflictState | null> = this.store.select(selectConflict);
  project$: Observable<Project | undefined> = this.store.select(selectProjectByGanttId);

  @ViewChild('leftContainer', { static: false }) leftContainer!: ElementRef<HTMLElement>;

  private readonly doc = inject(DOCUMENT);

  projectId = '';
  scrollTop = 0;

  // ── Filter ────────────────────────────────────────────────────────────────
  readonly ganttCriteria = signal<FilterCriteria>({});
  readonly ganttMembers = signal<ProjectMember[]>([]);
  readonly ganttVisibleMap = signal<Map<string, boolean> | null>(null);
  private ganttCurrentUserId = '';
  private ganttTasksSnapshot: GanttTask[] = [];

  // ── Resize split panel ────────────────────────────────────────────────────
  private readonly LEFT_MIN = 240;
  private readonly LEFT_MAX = 700;
  private readonly LEFT_DEFAULT = 380;
  leftPanelWidth = this.LEFT_DEFAULT;

  private resizeStartX = 0;
  private resizeStartWidth = 0;
  private readonly boundMouseMove = (e: MouseEvent) => this.onResizeMove(e);
  private readonly boundMouseUp = () => this.onResizeEnd();

  ngOnInit(): void {
    this.projectId = this.route.snapshot.paramMap.get('projectId') ?? '';
    if (this.projectId) {
      this.store.dispatch(GanttActions.loadGanttData({ projectId: this.projectId }));
      this.membersApi.getProjectMembers(this.projectId).subscribe({
        next: list => this.ganttMembers.set(list),
        error: () => {},
      });
    }

    // Restore filter from URL
    const initialCriteria = parseQueryParams(this.route.snapshot.queryParams);
    if (!isEmpty(initialCriteria)) {
      this.ganttCriteria.set(initialCriteria);
    }

    // Track current user for CURRENT_USER filter
    this.store.select(selectCurrentUser).pipe(takeUntilDestroyed(this.destroyRef)).subscribe(u => {
      this.ganttCurrentUserId = u?.id ?? '';
    });

    // Recompute visibleMap whenever gantt tasks change
    this.tasks$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(tasks => {
      this.ganttTasksSnapshot = tasks;
      this.recomputeGanttVisibleMap();
    });

    this.conflict$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(conflict => {
      if (conflict) this.openConflictDialog(conflict);
    });
  }

  ngOnDestroy(): void {
    this.store.dispatch(GanttActions.clearGantt());
    this.doc.removeEventListener('mousemove', this.boundMouseMove);
    this.doc.removeEventListener('mouseup', this.boundMouseUp);
    this.doc.body.style.cursor = '';
    this.doc.body.style.userSelect = '';
  }

  // ── Filter ────────────────────────────────────────────────────────────────

  onGanttCriteriaChange(criteria: FilterCriteria): void {
    this.ganttCriteria.set(criteria);
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: serializeFilter(criteria),
      replaceUrl: true,
    });
    this.recomputeGanttVisibleMap();
  }

  private recomputeGanttVisibleMap(): void {
    const criteria = this.ganttCriteria();
    if (isEmpty(criteria)) {
      this.ganttVisibleMap.set(null);
      return;
    }
    const today = this.today;
    const userId = this.ganttCurrentUserId;
    const tasks = this.ganttTasksSnapshot;

    const matchingIds = new Set<string>();
    for (const t of tasks) {
      if (this.ganttTaskMatches(t, criteria, userId, today)) {
        matchingIds.add(t.id);
      }
    }

    const taskMap = new Map(tasks.map(t => [t.id, t]));
    const vm = new Map<string, boolean>();
    for (const id of matchingIds) {
      vm.set(id, true);
      let cur = taskMap.get(id);
      while (cur?.parentId) {
        if (!vm.has(cur.parentId)) vm.set(cur.parentId, false);
        cur = taskMap.get(cur.parentId);
      }
    }
    this.ganttVisibleMap.set(vm);
  }

  countGanttMatches(vm: Map<string, boolean>): number {
    let n = 0;
    vm.forEach(isMatch => { if (isMatch) n++; });
    return n;
  }

  private ganttTaskMatches(t: GanttTask, c: FilterCriteria, userId: string, today: string): boolean {
    if (c.keyword) {
      const kw = c.keyword.toLowerCase();
      if (!t.name.toLowerCase().includes(kw) && !(t.vbs?.toLowerCase().includes(kw) ?? false))
        return false;
    }
    if (c.statuses?.length && !c.statuses.includes(t.status as any)) return false;
    if (c.priorities?.length && !c.priorities.includes(t.priority as any)) return false;
    if (c.nodeTypes?.length && !c.nodeTypes.includes(t.type as any)) return false;
    if (c.assigneeIds?.length) {
      const hasCurrentUser = c.assigneeIds.includes('CURRENT_USER');
      const hasUnassigned = c.assigneeIds.includes('UNASSIGNED');
      const otherIds = c.assigneeIds.filter(id => id !== 'CURRENT_USER' && id !== 'UNASSIGNED');
      let match = false;
      if (hasCurrentUser && t.assigneeUserId === userId) match = true;
      if (hasUnassigned && !t.assigneeUserId) match = true;
      if (otherIds.length && t.assigneeUserId && otherIds.includes(t.assigneeUserId)) match = true;
      if (!match) return false;
    }
    if (c.dueDateFrom && t.plannedEnd) {
      const endStr = this.deadlineService.dateToLocalString(t.plannedEnd);
      if (endStr < c.dueDateFrom) return false;
    }
    if (c.dueDateTo && t.plannedEnd) {
      const endStr = this.deadlineService.dateToLocalString(t.plannedEnd);
      if (endStr > c.dueDateTo) return false;
    }
    if (c.overdueOnly) {
      const endStr = t.plannedEnd ? this.deadlineService.dateToLocalString(t.plannedEnd) : null;
      const isOverdue = !!endStr && endStr < today
        && t.status !== 'Completed' && t.status !== 'Cancelled';
      if (!isOverdue) return false;
    }
    return true;
  }

  // ── Granularity / scroll ───────────────────────────────────────────────────

  switchView(view: string): void {
    if (view === 'grid') this.router.navigate(['/projects', this.projectId]);
  }

  onGranularityChange(granularity: GanttGranularity): void {
    this.store.dispatch(GanttActions.setGranularity({ granularity }));
  }

  onScrollChange(scrollTop: number): void {
    this.scrollTop = scrollTop;
  }

  // ── Add / Edit / Delete ───────────────────────────────────────────────────

  openAddTaskDialog(parentId: string | null = null): void {
    const data: TaskFormData = { mode: 'create', projectId: this.projectId, parentId };
    this.dialog.open(TaskFormComponent, { data, width: '620px' })
      .afterClosed()
      .subscribe(() => this.reloadGantt());
  }

  openEditTaskDialog(taskId: string): void {
    this.tasksApi.getTask(this.projectId, taskId).subscribe(task => {
      const data: TaskFormData = { mode: 'edit', projectId: this.projectId, task };
      this.dialog.open(TaskFormComponent, { data, width: '620px' })
        .afterClosed()
        .subscribe(() => this.reloadGantt());
    });
  }

  deleteGanttTask(task: GanttTask): void {
    this.dialog.open(ConfirmDialogComponent, {
      data: {
        message: `Xóa task "${task.name}"? Hành động này không thể hoàn tác.`,
        confirmLabel: 'Xóa',
      } as ConfirmDialogData,
      width: '360px',
    }).afterClosed().subscribe((confirmed: boolean) => {
      if (!confirmed) return;
      this.store.dispatch(TasksActions.deleteTask({
        projectId: this.projectId,
        taskId: task.id,
        version: task.version,
      }));
      setTimeout(() => this.reloadGantt(), 500);
    });
  }

  private reloadGantt(): void {
    if (this.projectId) {
      this.store.dispatch(GanttActions.loadGanttData({ projectId: this.projectId }));
    }
  }

  // ── Inline edit from left panel ────────────────────────────────────────────

  onInlineEdit(event: GanttInlineEditEvent): void {
    if (event.field !== 'status' && event.field !== 'percentComplete') {
      const edit: GanttTaskEdit = { taskId: event.taskId, originalVersion: event.version };
      if (event.field === 'name') edit.newName = event.value as string;
      if (event.field === 'plannedStart') edit.newPlannedStart = event.value ? new Date(event.value as string) : undefined;
      if (event.field === 'plannedEnd') edit.newPlannedEnd = event.value ? new Date(event.value as string) : undefined;
      this.store.dispatch(GanttActions.markTaskDirty({ edit }));
      return;
    }

    // status/percentComplete: cascade across tree
    this.store.select(selectGanttTasks).pipe(
      map(tasks => tasks),
    ).subscribe(allTasks => {
      const task = allTasks.find(t => t.id === event.taskId);
      if (!task) return;

      let newStatus = event.field === 'status' ? String(event.value ?? '') : task.status;
      let newPercent = event.field === 'percentComplete' ? (event.value as number) : task.percentComplete;
      if (event.field === 'percentComplete' && newPercent === 100) newStatus = 'Completed';
      if (event.field === 'status' && newStatus === 'Completed') newPercent = 100;

      const changes = new Map<string, { status: string; percent: number }>();
      changes.set(task.id, { status: newStatus, percent: newPercent });

      if (newStatus === 'Completed') {
        this.collectGanttDescendants(allTasks, task.id).forEach(d =>
          changes.set(d.id, { status: 'Completed', percent: 100 })
        );
      }
      this.propagateGanttUpward(allTasks, task.parentId, changes);

      for (const [taskId, change] of changes) {
        const t = allTasks.find(t => t.id === taskId)!;
        this.store.dispatch(GanttActions.markTaskDirty({
          edit: { taskId: t.id, originalVersion: t.version, newStatus: change.status, newPercentComplete: change.percent },
        }));
      }
    }).unsubscribe();
  }

  private collectGanttDescendants(allTasks: GanttTask[], parentId: string): GanttTask[] {
    const result: GanttTask[] = [];
    for (const t of allTasks) {
      if (t.parentId === parentId) {
        result.push(t, ...this.collectGanttDescendants(allTasks, t.id));
      }
    }
    return result;
  }

  private propagateGanttUpward(
    allTasks: GanttTask[],
    parentId: string | null,
    changes: Map<string, { status: string; percent: number }>,
  ): void {
    if (!parentId) return;
    const parent = allTasks.find(t => t.id === parentId);
    if (!parent) return;

    const children = allTasks.filter(t => t.parentId === parentId);
    const allCompleted = children.every(c => (changes.get(c.id)?.status ?? c.status) === 'Completed');
    const parentStatus = changes.get(parent.id)?.status ?? parent.status;

    if (allCompleted && parentStatus !== 'Completed') {
      changes.set(parent.id, { status: 'Completed', percent: 100 });
      this.propagateGanttUpward(allTasks, parent.parentId, changes);
    } else if (!allCompleted && parentStatus === 'Completed') {
      changes.set(parent.id, { status: 'InProgress', percent: parent.percentComplete });
      this.propagateGanttUpward(allTasks, parent.parentId, changes);
    }
  }

  // ── Gantt drag / dependency ────────────────────────────────────────────────

  onTaskEdited(edit: GanttTaskEdit): void {
    this.store.dispatch(GanttActions.markTaskDirty({ edit }));
  }

  onSave(): void {
    this.store.dispatch(GanttActions.saveGanttEdits());
  }

  onDiscard(): void {
    this.store.dispatch(GanttActions.discardGanttEdits());
  }

  // ── Resize split panel ────────────────────────────────────────────────────

  onResizeStart(event: MouseEvent): void {
    event.preventDefault();
    this.resizeStartX = event.clientX;
    this.resizeStartWidth = this.leftContainer.nativeElement.offsetWidth;
    this.doc.body.style.cursor = 'col-resize';
    this.doc.body.style.userSelect = 'none';
    this.doc.addEventListener('mousemove', this.boundMouseMove);
    this.doc.addEventListener('mouseup', this.boundMouseUp);
  }

  private onResizeMove(event: MouseEvent): void {
    const delta = event.clientX - this.resizeStartX;
    const newWidth = Math.min(this.LEFT_MAX, Math.max(this.LEFT_MIN, this.resizeStartWidth + delta));
    this.leftContainer.nativeElement.style.width = `${newWidth}px`;
  }

  private onResizeEnd(): void {
    this.leftPanelWidth = this.leftContainer.nativeElement.offsetWidth;
    this.doc.body.style.cursor = '';
    this.doc.body.style.userSelect = '';
    this.doc.removeEventListener('mousemove', this.boundMouseMove);
    this.doc.removeEventListener('mouseup', this.boundMouseUp);
  }

  // ── Conflict dialog ────────────────────────────────────────────────────────

  private openConflictDialog(conflict: GanttConflictState): void {
    const serverTask = JSON.parse(conflict.serverTaskJson);
    this.dialog.open(ConflictDialogComponent, {
      data: { serverState: serverTask, userChanges: conflict.localEdit, eTag: conflict.serverETag },
    }).afterClosed().subscribe((result: ConflictDialogResult | undefined) => {
      if (result === 'use-server') {
        this.store.dispatch(GanttActions.discardGanttEdits());
        this.reloadGantt();
      } else if (result === 'retry-mine') {
        const newEdit: GanttTaskEdit = {
          ...conflict.localEdit,
          originalVersion: serverTask.version ?? conflict.localEdit.originalVersion + 1,
        };
        this.store.dispatch(GanttActions.resolveConflict());
        this.store.dispatch(GanttActions.markTaskDirty({ edit: newEdit }));
        this.store.dispatch(GanttActions.saveGanttEdits());
      } else {
        this.store.dispatch(GanttActions.resolveConflict());
      }
    });
  }
}
