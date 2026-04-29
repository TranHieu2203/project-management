import {
  ChangeDetectionStrategy, ChangeDetectorRef, Component, DestroyRef, DOCUMENT,
  ElementRef, inject, OnDestroy, OnInit, signal, ViewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AsyncPipe } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatBadgeModule } from '@angular/material/badge';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog } from '@angular/material/dialog';
import { Store } from '@ngrx/store';
import { combineLatest, map, Subject, take } from 'rxjs';

import { AppState } from '../../../../core/store/app.state';
import { TasksActions } from '../../store/tasks.actions';
import {
  selectActiveFilter, selectAllTasks, selectTasksError, selectTasksLoading,
} from '../../store/tasks.selectors';
import { selectCurrentUser } from '../../../auth/store/auth.selectors';
import { TaskTreeComponent, TaskReorderEvent, QuickUpdateEvent } from '../task-tree/task-tree';
import { FilterBarComponent } from '../filter-bar/filter-bar';
import { TaskFormComponent, TaskFormData } from '../task-form/task-form';
import { ProjectTask, UpdateTaskPayload } from '../../models/task.model';
import { ProjectMember } from '../../models/project.model';
import { MembersApiService } from '../../services/members-api.service';
import { TasksApiService } from '../../services/tasks-api.service';
import { DeadlineAlertBannerComponent } from '../deadline-alert-banner/deadline-alert-banner';
import { DeadlineAlertService, DeadlineStatus, DeadlineSummary } from '../../services/deadline-alert.service';
import { FilterCriteria } from '../../models/filter.model';
import { computeVisibleIds, isEmpty, parseQueryParams, serializeFilter } from '../../models/filter.utils';
import { BoardComponent } from '../board/board';

import { GanttActions } from '../../../gantt/store/gantt.actions';
import {
  selectGanttTasks, selectGanttLoading, selectGanttError, selectGranularity,
  selectDirtyTasksCount, selectSaving, selectConflict,
} from '../../../gantt/store/gantt.selectors';
import {
  GanttConflictState, GanttGranularity, GanttTask, GanttTaskEdit,
} from '../../../gantt/models/gantt.model';
import { GanttLeftPanelComponent, GanttInlineEditEvent } from '../../../gantt/components/gantt-left-panel/gantt-left-panel';
import { GanttTimelineComponent } from '../../../gantt/components/gantt-timeline/gantt-timeline';
import { ConflictDialogComponent, ConflictDialogResult } from '../../../../shared/components/conflict-dialog/conflict-dialog';
import { ConfirmDialogComponent, ConfirmDialogData } from '../../../../shared/components/confirm-dialog/confirm-dialog';

@Component({
  standalone: true,
  selector: 'app-project-detail',
  imports: [
    AsyncPipe,
    RouterLink,
    MatButtonModule,
    MatButtonToggleModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatBadgeModule,
    MatTooltipModule,
    TaskTreeComponent,
    FilterBarComponent,
    DeadlineAlertBannerComponent,
    BoardComponent,
    GanttLeftPanelComponent,
    GanttTimelineComponent,
  ],
  templateUrl: './project-detail.html',
  styleUrl: './project-detail.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProjectDetailComponent implements OnInit, OnDestroy {
  private readonly store = inject(Store<AppState>);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  private readonly membersApi = inject(MembersApiService);
  private readonly tasksApi = inject(TasksApiService);
  private readonly deadlineService = inject(DeadlineAlertService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly destroyRef = inject(DestroyRef);
  private readonly doc = inject(DOCUMENT);
  private readonly destroy$ = new Subject<void>();

  // ── Grid/Board state ──────────────────────────────────────────────────────

  readonly projectId = this.route.snapshot.paramMap.get('projectId')!;
  readonly tasks$ = this.store.select(selectAllTasks);
  readonly loading$ = this.store.select(selectTasksLoading);
  readonly error$ = this.store.select(selectTasksError);
  readonly criteria$ = this.store.select(selectActiveFilter);
  readonly members = signal<ProjectMember[]>([]);

  readonly today = this.deadlineService.getLocalDateString();
  readonly deadlineSummary$ = this.tasks$.pipe(
    map(tasks => this.deadlineService.computeDeadlineSummary(tasks, this.today)),
  );

  readonly visibleMap$ = combineLatest([
    this.store.select(selectAllTasks),
    this.store.select(selectActiveFilter),
    this.store.select(selectCurrentUser),
  ]).pipe(
    map(([tasks, criteria, user]) =>
      computeVisibleIds(tasks, criteria, user?.id ?? '', this.today)
    ),
  );

  readonly filteredCount$ = this.visibleMap$.pipe(
    map(vm => {
      let count = 0;
      vm.forEach(isMatch => { if (isMatch) count++; });
      return count;
    }),
  );

  activeDeadlineFilter = signal<DeadlineStatus | null>(null);
  highlightTaskId = signal<string | null>(null);
  currentView = signal<'grid' | 'board' | 'gantt'>('grid');

  // ── Gantt state ───────────────────────────────────────────────────────────

  readonly ganttTasks$ = this.store.select(selectGanttTasks);
  readonly ganttLoading$ = this.store.select(selectGanttLoading);
  readonly ganttError$ = this.store.select(selectGanttError);
  readonly ganttGranularity$ = this.store.select(selectGranularity);
  readonly ganttDirtyCount$ = this.store.select(selectDirtyTasksCount);
  readonly ganttSaving$ = this.store.select(selectSaving);
  readonly ganttConflict$ = this.store.select(selectConflict);

  readonly ganttDeadlineCounts$ = this.ganttTasks$.pipe(
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

  readonly ganttCriteria = signal<FilterCriteria>({});
  readonly ganttVisibleMap = signal<Map<string, boolean> | null>(null);
  ganttScrollTop = 0;

  private ganttCurrentUserId = '';
  private ganttTasksSnapshot: GanttTask[] = [];

  // ── Resize split panel ────────────────────────────────────────────────────

  @ViewChild('leftContainer', { static: false }) leftContainer!: ElementRef<HTMLElement>;

  private readonly LEFT_MIN = 240;
  private readonly LEFT_MAX = 700;
  private readonly LEFT_DEFAULT = 380;
  leftPanelWidth = this.LEFT_DEFAULT;

  private resizeStartX = 0;
  private resizeStartWidth = 0;
  private readonly boundMouseMove = (e: MouseEvent) => this.onResizeMove(e);
  private readonly boundMouseUp = () => this.onResizeEnd();

  // ── Lifecycle ─────────────────────────────────────────────────────────────

  ngOnInit(): void {
    this.store.dispatch(TasksActions.loadTasks({ projectId: this.projectId }));
    this.membersApi.getProjectMembers(this.projectId).subscribe({
      next: list => this.members.set(list),
      error: () => this.members.set([]),
    });

    // Restore filter + view from URL
    const qp = this.route.snapshot.queryParams;
    const initialCriteria = parseQueryParams(qp);
    if (!isEmpty(initialCriteria)) {
      this.store.dispatch(TasksActions.setFilter({ criteria: initialCriteria }));
    }
    if (qp['view'] === 'board') {
      this.currentView.set('board');
    } else if (qp['view'] === 'gantt') {
      this.currentView.set('gantt');
      this.store.dispatch(GanttActions.loadGanttData({ projectId: this.projectId }));
    }
    if (qp['highlight']) {
      this.highlightTaskId.set(qp['highlight']);
    }

    // Track current user for gantt filter
    this.store.select(selectCurrentUser)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(u => { this.ganttCurrentUserId = u?.id ?? ''; });

    // Recompute gantt visibleMap when tasks change
    this.ganttTasks$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(tasks => {
        this.ganttTasksSnapshot = tasks;
        this.recomputeGanttVisibleMap();
      });

    // Gantt conflict dialog — only when in gantt view
    this.ganttConflict$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(conflict => {
        if (conflict && this.currentView() === 'gantt') {
          this.openGanttConflictDialog(conflict);
        }
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.store.dispatch(TasksActions.clearTasks());
    this.store.dispatch(TasksActions.clearFilter());
    this.store.dispatch(GanttActions.clearGantt());
    this.doc.removeEventListener('mousemove', this.boundMouseMove);
    this.doc.removeEventListener('mouseup', this.boundMouseUp);
    this.doc.body.style.cursor = '';
    this.doc.body.style.userSelect = '';
  }

  // ── View switching ────────────────────────────────────────────────────────

  switchView(view: string): void {
    if (view === this.currentView()) return;

    if (this.currentView() === 'gantt') {
      this.store.dispatch(GanttActions.clearGantt());
    }

    if (view === 'gantt') {
      this.currentView.set('gantt');
      this.store.dispatch(GanttActions.loadGanttData({ projectId: this.projectId }));
      this.router.navigate([], {
        relativeTo: this.route,
        queryParams: { view: 'gantt' },
        queryParamsHandling: 'merge',
        replaceUrl: true,
      });
      return;
    }

    const v = view === 'board' ? 'board' : 'grid';
    this.currentView.set(v);
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { view: v === 'grid' ? null : v },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }

  // ── Grid/Board handlers ───────────────────────────────────────────────────

  onCriteriaChange(criteria: FilterCriteria): void {
    this.store.dispatch(TasksActions.setFilter({ criteria }));
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: serializeFilter(criteria),
      replaceUrl: true,
    });
  }

  onFilterChange(filter: DeadlineStatus | null, summary: DeadlineSummary): void {
    this.activeDeadlineFilter.set(filter);
    if (!filter) {
      this.highlightTaskId.set(null);
      return;
    }
    const group =
      filter === 'overdue' ? summary.overdue :
      filter === 'due-today' ? summary.dueToday :
      summary.dueSoon;
    if (!group.length) return;
    this.highlightTaskId.set(group[0].id);
    setTimeout(() => {
      document.querySelector(`[data-task-id="${group[0].id}"]`)
        ?.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }, 50);
    setTimeout(() => {
      this.highlightTaskId.set(null);
      this.cdr.markForCheck();
    }, 950);
  }

  openAddTaskDialog(parentId: string | null = null): void {
    const data: TaskFormData = { mode: 'create', projectId: this.projectId, parentId };
    const ref = this.dialog.open(TaskFormComponent, { data, width: '600px' });
    if (this.currentView() === 'gantt') {
      ref.afterClosed().subscribe(() => this.reloadGantt());
    }
  }

  openEditTaskDialog(task: ProjectTask): void {
    const data: TaskFormData = { mode: 'edit', projectId: this.projectId, task };
    this.dialog.open(TaskFormComponent, { data, width: '600px' });
  }

  deleteTask(task: ProjectTask): void {
    if (!confirm(`Xóa task "${task.name}"?`)) return;
    this.store.dispatch(TasksActions.deleteTask({
      projectId: this.projectId,
      taskId: task.id,
      version: task.version,
    }));
  }

  onQuickUpdateTask(event: QuickUpdateEvent): void {
    const { task, field, value } = event;

    if (field !== 'status' && field !== 'percentComplete') {
      this.buildAndDispatch(task, task.status, task.percentComplete,
        { [field]: (value === '' || value === null) ? undefined : value });
      return;
    }

    this.tasks$.pipe(take(1)).subscribe(allTasks => {
      let newStatus = field === 'status' ? String(value ?? '') : task.status;
      let newPercent: number | null = field === 'percentComplete' ? (value as number | null) : task.percentComplete;
      if (field === 'percentComplete' && newPercent === 100) newStatus = 'Completed';
      if (field === 'status' && newStatus === 'Completed') newPercent = 100;

      const changes = new Map<string, { status: string; percentComplete: number | null }>();
      changes.set(task.id, { status: newStatus, percentComplete: newPercent });

      if (newStatus === 'Completed') {
        this.collectDescendants(allTasks, task.id).forEach(d =>
          changes.set(d.id, { status: 'Completed', percentComplete: 100 })
        );
      }
      this.propagateUpward(allTasks, task.parentId, changes);

      for (const [taskId, change] of changes) {
        const t = allTasks.find(t => t.id === taskId)!;
        this.buildAndDispatch(t, change.status, change.percentComplete);
      }
    });
  }

  onReorderTask(event: TaskReorderEvent): void {
    this.tasks$.pipe(take(1)).subscribe(tasks => {
      const task = tasks.find(t => t.id === event.taskId);
      if (!task) return;
      const payload: UpdateTaskPayload = {
        parentId: event.newParentId,
        type: task.type,
        vbs: task.vbs ?? undefined,
        name: task.name,
        priority: task.priority,
        status: task.status,
        notes: task.notes ?? undefined,
        plannedStartDate: task.plannedStartDate ?? undefined,
        plannedEndDate: task.plannedEndDate ?? undefined,
        actualStartDate: task.actualStartDate ?? undefined,
        actualEndDate: task.actualEndDate ?? undefined,
        plannedEffortHours: task.plannedEffortHours ?? undefined,
        percentComplete: task.percentComplete ?? undefined,
        assigneeUserId: task.assigneeUserId ?? undefined,
        sortOrder: event.newSortOrder,
        predecessors: task.predecessors,
      };
      this.store.dispatch(TasksActions.updateTask({
        projectId: this.projectId,
        taskId: event.taskId,
        request: payload,
        version: event.version,
      }));
    });
  }

  // ── Gantt filter ──────────────────────────────────────────────────────────

  onGanttCriteriaChange(criteria: FilterCriteria): void {
    this.ganttCriteria.set(criteria);
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: serializeFilter(criteria),
      replaceUrl: true,
    });
    this.recomputeGanttVisibleMap();
  }

  countGanttMatches(vm: Map<string, boolean>): number {
    let n = 0;
    vm.forEach(isMatch => { if (isMatch) n++; });
    return n;
  }

  private recomputeGanttVisibleMap(): void {
    const criteria = this.ganttCriteria();
    if (isEmpty(criteria)) { this.ganttVisibleMap.set(null); return; }
    const tasks = this.ganttTasksSnapshot;
    const matchingIds = new Set<string>();
    for (const t of tasks) {
      if (this.ganttTaskMatches(t, criteria, this.ganttCurrentUserId, this.today)) {
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
      if (this.deadlineService.dateToLocalString(t.plannedEnd) < c.dueDateFrom) return false;
    }
    if (c.dueDateTo && t.plannedEnd) {
      if (this.deadlineService.dateToLocalString(t.plannedEnd) > c.dueDateTo) return false;
    }
    if (c.overdueOnly) {
      const endStr = t.plannedEnd ? this.deadlineService.dateToLocalString(t.plannedEnd) : null;
      if (!(endStr && endStr < today && t.status !== 'Completed' && t.status !== 'Cancelled'))
        return false;
    }
    return true;
  }

  // ── Gantt controls ────────────────────────────────────────────────────────

  onGranularityChange(granularity: GanttGranularity): void {
    this.store.dispatch(GanttActions.setGranularity({ granularity }));
  }

  onGanttScrollChange(scrollTop: number): void {
    this.ganttScrollTop = scrollTop;
  }

  onGanttSave(): void {
    this.store.dispatch(GanttActions.saveGanttEdits());
  }

  onGanttDiscard(): void {
    this.store.dispatch(GanttActions.discardGanttEdits());
  }

  // ── Gantt task CRUD ───────────────────────────────────────────────────────

  openGanttEditTaskDialog(taskId: string): void {
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
    this.store.dispatch(GanttActions.loadGanttData({ projectId: this.projectId }));
  }

  // ── Gantt inline edit ─────────────────────────────────────────────────────

  onInlineEdit(event: GanttInlineEditEvent): void {
    if (event.field !== 'status' && event.field !== 'percentComplete') {
      const edit: GanttTaskEdit = { taskId: event.taskId, originalVersion: event.version };
      if (event.field === 'name') edit.newName = event.value as string;
      if (event.field === 'plannedStart') edit.newPlannedStart = event.value ? new Date(event.value as string) : undefined;
      if (event.field === 'plannedEnd') edit.newPlannedEnd = event.value ? new Date(event.value as string) : undefined;
      this.store.dispatch(GanttActions.markTaskDirty({ edit }));
      return;
    }

    this.store.select(selectGanttTasks).pipe(take(1)).subscribe(allTasks => {
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
    });
  }

  onGanttTaskEdited(edit: GanttTaskEdit): void {
    this.store.dispatch(GanttActions.markTaskDirty({ edit }));
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

  // ── Gantt conflict dialog ─────────────────────────────────────────────────

  private openGanttConflictDialog(conflict: GanttConflictState): void {
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

  // ── Private helpers (grid/board) ──────────────────────────────────────────

  private collectDescendants(allTasks: ProjectTask[], parentId: string): ProjectTask[] {
    const result: ProjectTask[] = [];
    for (const t of allTasks) {
      if (t.parentId === parentId) result.push(t, ...this.collectDescendants(allTasks, t.id));
    }
    return result;
  }

  private propagateUpward(
    allTasks: ProjectTask[],
    parentId: string | null,
    changes: Map<string, { status: string; percentComplete: number | null }>,
  ): void {
    if (!parentId) return;
    const parent = allTasks.find(t => t.id === parentId);
    if (!parent) return;
    const children = allTasks.filter(t => t.parentId === parentId);
    const allCompleted = children.every(c => (changes.get(c.id)?.status ?? c.status) === 'Completed');
    const parentStatus = changes.get(parent.id)?.status ?? parent.status;
    if (allCompleted && parentStatus !== 'Completed') {
      changes.set(parent.id, { status: 'Completed', percentComplete: 100 });
      this.propagateUpward(allTasks, parent.parentId, changes);
    } else if (!allCompleted && parentStatus === 'Completed') {
      changes.set(parent.id, { status: 'InProgress', percentComplete: parent.percentComplete });
      this.propagateUpward(allTasks, parent.parentId, changes);
    }
  }

  private buildAndDispatch(
    task: ProjectTask,
    status: string,
    percentComplete: number | null,
    extra: Partial<UpdateTaskPayload> = {},
  ): void {
    const payload: UpdateTaskPayload = {
      parentId: task.parentId,
      type: task.type,
      vbs: task.vbs ?? undefined,
      name: task.name,
      priority: task.priority,
      status,
      notes: task.notes ?? undefined,
      plannedStartDate: task.plannedStartDate ?? undefined,
      plannedEndDate: task.plannedEndDate ?? undefined,
      actualStartDate: task.actualStartDate ?? undefined,
      actualEndDate: task.actualEndDate ?? undefined,
      plannedEffortHours: task.plannedEffortHours ?? undefined,
      percentComplete: percentComplete ?? undefined,
      assigneeUserId: task.assigneeUserId ?? undefined,
      sortOrder: task.sortOrder,
      predecessors: task.predecessors,
      ...extra,
    };
    this.store.dispatch(TasksActions.updateTask({
      projectId: this.projectId,
      taskId: task.id,
      request: payload,
      version: task.version,
    }));
  }

  // ── Private helpers (gantt) ───────────────────────────────────────────────

  private collectGanttDescendants(allTasks: GanttTask[], parentId: string): GanttTask[] {
    const result: GanttTask[] = [];
    for (const t of allTasks) {
      if (t.parentId === parentId) result.push(t, ...this.collectGanttDescendants(allTasks, t.id));
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
}
