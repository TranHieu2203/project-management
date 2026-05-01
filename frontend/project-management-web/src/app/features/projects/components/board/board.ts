import {
  ChangeDetectionStrategy, ChangeDetectorRef, Component, DestroyRef,
  Input, OnInit, inject,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatDialog } from '@angular/material/dialog';
import { FeedbackDialogService } from '../../../../shared/services/feedback-dialog.service';
import { CdkDragDrop, transferArrayItem } from '@angular/cdk/drag-drop';
import { Store } from '@ngrx/store';
import { combineLatest, filter, take } from 'rxjs';
import { pairwise } from 'rxjs/operators';
import { TasksActions } from '../../store/tasks.actions';
import {
  selectActiveFilter, selectAllTasks, selectTasksConflict,
  selectCurrentProjectPhases, selectTasksCreating, selectTasksError,
} from '../../store/tasks.selectors';
import { selectCurrentUser } from '../../../auth/store/auth.selectors';
import { ProjectTask, CreateTaskPayload, UpdateTaskPayload } from '../../models/task.model';
import { ProjectMember } from '../../models/project.model';
import { FilterCriteria } from '../../models/filter.model';
import { applyFilter, isEmpty } from '../../models/filter.utils';
import { DeadlineAlertService } from '../../services/deadline-alert.service';
import { FilterBarComponent } from '../filter-bar/filter-bar';
import { BoardColumnComponent, ColumnQuickCreateEvent, ColumnOpenFullFormEvent } from './board-column/board-column';
import { TaskFormComponent, TaskFormData } from '../task-form/task-form';

const TASK_STATUS_COLUMNS: ProjectTask['status'][] = [
  'NotStarted', 'InProgress', 'OnHold', 'Delayed', 'Completed', 'Cancelled',
];

const PAGE_SIZE = 20;

@Component({
  standalone: true,
  selector: 'app-board',
  imports: [FilterBarComponent, BoardColumnComponent],
  templateUrl: './board.html',
  styleUrl: './board.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BoardComponent implements OnInit {
  private readonly store = inject(Store);
  private readonly dialog = inject(MatDialog);
  private readonly feedbackDialog = inject(FeedbackDialogService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly deadlineService = inject(DeadlineAlertService);
  private readonly destroyRef = inject(DestroyRef);

  @Input({ required: true }) projectId!: string;
  @Input() members: ProjectMember[] = [];

  readonly today = this.deadlineService.getLocalDateString();
  readonly STATUSES = TASK_STATUS_COLUMNS;

  // IDs used by CDK to connect all drop lists
  readonly connectedDropIds = TASK_STATUS_COLUMNS.map(s => `col-${s}`);

  // Local mutable columns (CDK DragDrop mutates these arrays)
  columns: Map<ProjectTask['status'], ProjectTask[]> = new Map(
    TASK_STATUS_COLUMNS.map(s => [s, []])
  );

  // How many tasks are shown per column (pagination)
  limits: Map<ProjectTask['status'], number> = new Map(
    TASK_STATUS_COLUMNS.map(s => [s, PAGE_SIZE])
  );

  // Members map for avatar/tooltip
  membersMap: Map<string, ProjectMember> = new Map();

  // Parent name lookup
  parentNames: Map<string, string> = new Map();

  // Filter state (for sharing with filter bar)
  criteria: FilterCriteria = {};

  // All tasks (from store — used for rollback and filter bar)
  allTasks: ProjectTask[] = [];
  private currentUserId = '';

  // Phases for quick-create chips
  phases: ProjectTask[] = [];

  // True while a temp (optimistic) task is pending API confirmation
  private hasPendingCreate = false;

  ngOnInit(): void {
    // Build members map
    this.membersMap = new Map(this.members.map(m => [m.userId, m]));

    combineLatest([
      this.store.select(selectAllTasks),
      this.store.select(selectActiveFilter),
      this.store.select(selectCurrentUser),
    ]).pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(([tasks, criteria, user]) => {
        // When tasks arrive, a pending create succeeded — clear flag
        if (this.hasPendingCreate) {
          this.hasPendingCreate = false;
        }
        this.allTasks = tasks;
        this.criteria = criteria;
        this.currentUserId = user?.id ?? '';
        this.buildParentNames(tasks);
        this.refreshColumns(tasks, criteria, this.currentUserId);
        this.cdr.markForCheck();
      });

    // Load phases for quick-create (phases = tasks with type 'Phase')
    this.store.select(selectCurrentProjectPhases)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(phases => {
        this.phases = phases;
        this.cdr.markForCheck();
      });

    // Watch for 409 conflict — rollback + toast
    this.store.select(selectTasksConflict)
      .pipe(
        filter(Boolean),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(() => {
        this.refreshColumns(this.allTasks, this.criteria, this.currentUserId);
        this.cdr.markForCheck();
        this.feedbackDialog.error('Conflict: dữ liệu đã thay đổi, refresh để xem mới nhất');
        this.store.dispatch(TasksActions.clearTaskConflict());
      });

    // Watch for create failure — rollback optimistic card + toast
    this.store.select(selectTasksCreating).pipe(
      pairwise(),
      filter(([prev, curr]) => prev === true && curr === false),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe(() => {
      if (!this.hasPendingCreate) return;
      // creating finished but hasPendingCreate still set → success was not yet
      // handled; check error next tick
      this.store.select(selectTasksError).pipe(take(1)).subscribe(error => {
        if (error && this.hasPendingCreate) {
          this.hasPendingCreate = false;
          this.refreshColumns(this.allTasks, this.criteria, this.currentUserId);
          this.cdr.markForCheck();
          this.feedbackDialog.error('Không thể tạo task. Thử lại?');
        }
      });
    });
  }

  private buildParentNames(tasks: ProjectTask[]): void {
    const nameMap = new Map(tasks.map(t => [t.id, t.name]));
    const newParents = new Map<string, string>();
    for (const t of tasks) {
      if (t.parentId && nameMap.has(t.parentId)) {
        newParents.set(t.id, nameMap.get(t.parentId)!);
      }
    }
    this.parentNames = newParents;
  }

  private refreshColumns(
    tasks: ProjectTask[],
    criteria: FilterCriteria,
    userId: string,
  ): void {
    const matchedIds = isEmpty(criteria)
      ? null
      : new Set(applyFilter(tasks, criteria, userId, this.today));

    const filtered = matchedIds ? tasks.filter(t => matchedIds.has(t.id)) : tasks;

    for (const status of TASK_STATUS_COLUMNS) {
      const col = filtered.filter(t => t.status === status);
      this.columns.set(status, col);
    }
  }

  getColumnData(status: ProjectTask['status']): ProjectTask[] {
    return this.columns.get(status) ?? [];
  }

  getDisplayTasks(status: ProjectTask['status']): ProjectTask[] {
    const limit = this.limits.get(status) ?? PAGE_SIZE;
    return (this.columns.get(status) ?? []).slice(0, limit);
  }

  getTotalCount(status: ProjectTask['status']): number {
    return (this.columns.get(status) ?? []).length;
  }

  onLoadMore(status: ProjectTask['status']): void {
    const current = this.limits.get(status) ?? PAGE_SIZE;
    this.limits.set(status, current + PAGE_SIZE);
    this.cdr.markForCheck();
  }

  onDrop(event: CdkDragDrop<ProjectTask[]>, newStatus: ProjectTask['status']): void {
    if (event.previousContainer === event.container) return;

    const task = event.item.data as ProjectTask;
    if (task.status === newStatus) return;

    // Optimistic: move card in local arrays immediately
    transferArrayItem(
      event.previousContainer.data,
      event.container.data,
      event.previousIndex,
      event.currentIndex,
    );
    this.cdr.markForCheck();

    // Dispatch actual status change
    this.dispatchStatusChange(task, newStatus);
  }

  private dispatchStatusChange(task: ProjectTask, newStatus: ProjectTask['status']): void {
    const payload: UpdateTaskPayload = {
      parentId: task.parentId,
      type: task.type,
      vbs: task.vbs ?? undefined,
      name: task.name,
      priority: task.priority,
      status: newStatus,
      notes: task.notes ?? undefined,
      plannedStartDate: task.plannedStartDate ?? undefined,
      plannedEndDate: task.plannedEndDate ?? undefined,
      actualStartDate: task.actualStartDate ?? undefined,
      actualEndDate: task.actualEndDate ?? undefined,
      plannedEffortHours: task.plannedEffortHours ?? undefined,
      percentComplete: newStatus === 'Completed' ? 100 : (task.percentComplete ?? undefined),
      assigneeUserId: task.assigneeUserId ?? undefined,
      sortOrder: task.sortOrder,
      predecessors: task.predecessors,
    };

    this.store.dispatch(TasksActions.updateTask({
      projectId: this.projectId,
      taskId: task.id,
      request: payload,
      version: task.version,
    }));
  }

  onCriteriaChange(criteria: FilterCriteria): void {
    this.store.dispatch(TasksActions.setFilter({ criteria }));
  }

  openQuickEdit(task: ProjectTask): void {
    const data: TaskFormData = { mode: 'edit', projectId: this.projectId, task };
    this.dialog.open(TaskFormComponent, { data, width: '600px', maxHeight: '90vh' });
  }

  // --- Quick Create ---

  onQuickCreate(event: ColumnQuickCreateEvent): void {
    const { status, name, phaseId } = event;

    // 1. Optimistic: prepend temp card to column
    const tempId = `temp-${Date.now()}`;
    const tempTask: ProjectTask = {
      id: tempId,
      projectId: this.projectId,
      parentId: phaseId,
      type: 'Task',
      vbs: null,
      name,
      priority: 'Medium',
      status,
      notes: null,
      plannedStartDate: null,
      plannedEndDate: null,
      actualStartDate: null,
      actualEndDate: null,
      plannedEffortHours: null,
      actualEffortHours: null,
      percentComplete: null,
      assigneeUserId: null,
      sortOrder: 0,
      version: 0,
      predecessors: [],
    };

    const col = this.columns.get(status) ?? [];
    this.columns.set(status, [tempTask, ...col]);
    this.hasPendingCreate = true;
    this.cdr.markForCheck();

    // 2. Dispatch create to API
    const payload: CreateTaskPayload = {
      parentId: phaseId,
      type: 'Task',
      name,
      priority: 'Medium',
      status,
      sortOrder: 0,
    };

    this.store.dispatch(TasksActions.createTask({
      projectId: this.projectId,
      request: payload,
    }));
  }

  onOpenFullForm(event: ColumnOpenFullFormEvent): void {
    const data: TaskFormData = {
      mode: 'create',
      projectId: this.projectId,
      parentId: event.phaseId,
      initialStatus: event.status,
    };
    this.dialog.open(TaskFormComponent, { data, width: '600px', maxHeight: '90vh' });
  }

  // Tasks list for filter bar
  get allTasksForFilter(): ProjectTask[] {
    return this.allTasks;
  }

  // Filtered count (tasks that match filter, excluding ancestors)
  get filteredCount(): number {
    if (isEmpty(this.criteria)) return this.allTasks.length;
    const ids = applyFilter(this.allTasks, this.criteria, this.currentUserId, this.today);
    return ids.length;
  }
}
