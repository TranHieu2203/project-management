import {
  ChangeDetectionStrategy, ChangeDetectorRef, Component, inject, OnDestroy, OnInit, signal,
} from '@angular/core';
import { AsyncPipe } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog } from '@angular/material/dialog';
import { Store } from '@ngrx/store';
import { combineLatest, map, Subject, take } from 'rxjs';
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
import { DeadlineAlertBannerComponent } from '../deadline-alert-banner/deadline-alert-banner';
import { DeadlineAlertService, DeadlineStatus, DeadlineSummary } from '../../services/deadline-alert.service';
import { FilterCriteria } from '../../models/filter.model';
import { computeVisibleIds, isEmpty, parseQueryParams, serializeFilter } from '../../models/filter.utils';
import { BoardComponent } from '../board/board';

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
    TaskTreeComponent,
    FilterBarComponent,
    DeadlineAlertBannerComponent,
    BoardComponent,
  ],
  templateUrl: './project-detail.html',
  styleUrl: './project-detail.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProjectDetailComponent implements OnInit, OnDestroy {
  private readonly store = inject(Store);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  private readonly membersApi = inject(MembersApiService);
  private readonly deadlineService = inject(DeadlineAlertService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly destroy$ = new Subject<void>();

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
  currentView = signal<'grid' | 'board'>('grid');

  ngOnInit(): void {
    this.store.dispatch(TasksActions.loadTasks({ projectId: this.projectId }));
    this.membersApi.getProjectMembers(this.projectId).subscribe({
      next: (list) => this.members.set(list),
      error: () => this.members.set([]),
    });

    // Restore filter + view from URL query params on init
    const qp = this.route.snapshot.queryParams;
    const initialCriteria = parseQueryParams(qp);
    if (!isEmpty(initialCriteria)) {
      this.store.dispatch(TasksActions.setFilter({ criteria: initialCriteria }));
    }
    if (qp['view'] === 'board') {
      this.currentView.set('board');
    }
    if (qp['highlight']) {
      this.highlightTaskId.set(qp['highlight']);
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.store.dispatch(TasksActions.clearTasks());
    this.store.dispatch(TasksActions.clearFilter());
  }

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

  switchView(view: string): void {
    if (view === 'gantt') {
      this.router.navigate(['/projects', this.projectId, 'gantt']);
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

  openAddTaskDialog(parentId: string | null = null): void {
    const data: TaskFormData = { mode: 'create', projectId: this.projectId, parentId };
    this.dialog.open(TaskFormComponent, { data, width: '600px' });
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

  private collectDescendants(allTasks: ProjectTask[], parentId: string): ProjectTask[] {
    const result: ProjectTask[] = [];
    for (const t of allTasks) {
      if (t.parentId === parentId) {
        result.push(t, ...this.collectDescendants(allTasks, t.id));
      }
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
}
