import {
  ChangeDetectionStrategy, Component, inject, OnInit, signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AsyncPipe } from '@angular/common';
import { Store } from '@ngrx/store';
import { TasksActions } from '../../../store/tasks.actions';
import { selectTasksUpdating } from '../../../store/tasks.selectors';
import { ProjectTask, CreateTaskPayload, UpdateTaskPayload } from '../../../models/task.model';
import { ProjectMember } from '../../../models/project.model';
import { MembersApiService } from '../../../services/members-api.service';

export interface TaskQuickEditData {
  /** null = create mode */
  task: ProjectTask | null;
  projectId: string;
  /** Create mode: initial values */
  initialStatus?: ProjectTask['status'];
  initialPhaseId?: string | null;
  initialName?: string;
  /** Available phases for create mode (pre-loaded) */
  phases?: ProjectTask[];
}

@Component({
  standalone: true,
  selector: 'app-task-quick-edit',
  imports: [
    FormsModule,
    AsyncPipe,
    RouterLink,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './task-quick-edit.html',
  styleUrl: './task-quick-edit.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaskQuickEditComponent implements OnInit {
  private readonly store = inject(Store);
  private readonly dialogRef = inject(MatDialogRef<TaskQuickEditComponent>);
  private readonly membersApi = inject(MembersApiService);
  readonly data = inject<TaskQuickEditData>(MAT_DIALOG_DATA);

  readonly updating$ = this.store.select(selectTasksUpdating);
  readonly members = signal<ProjectMember[]>([]);

  get isCreateMode(): boolean { return this.data.task === null; }

  // Editable form state
  name = '';
  status: ProjectTask['status'] = 'NotStarted';
  priority: ProjectTask['priority'] = 'Medium';
  assigneeUserId: string | null = null;
  plannedStartDate = '';
  plannedEndDate = '';
  notes = '';
  selectedPhaseId: string | null = null;

  readonly statuses: ProjectTask['status'][] = [
    'NotStarted', 'InProgress', 'OnHold', 'Delayed', 'Completed', 'Cancelled',
  ];

  readonly priorities: ProjectTask['priority'][] = ['Low', 'Medium', 'High', 'Critical'];

  readonly statusLabels: Record<string, string> = {
    NotStarted: 'Chưa bắt đầu', InProgress: 'Đang thực hiện',
    OnHold: 'Tạm dừng', Delayed: 'Bị trễ',
    Completed: 'Hoàn thành', Cancelled: 'Đã hủy',
  };

  readonly priorityLabels: Record<string, string> = {
    Low: 'Thấp', Medium: 'Trung bình', High: 'Cao', Critical: 'Khẩn cấp',
  };

  ngOnInit(): void {
    if (this.data.task) {
      const t = this.data.task;
      this.name = t.name;
      this.status = t.status;
      this.priority = t.priority;
      this.assigneeUserId = t.assigneeUserId;
      this.plannedStartDate = t.plannedStartDate ?? '';
      this.plannedEndDate = t.plannedEndDate ?? '';
      this.notes = t.notes ?? '';
    } else {
      // Create mode — pre-fill from data
      this.name = this.data.initialName ?? '';
      this.status = this.data.initialStatus ?? 'NotStarted';
      this.selectedPhaseId = this.data.initialPhaseId ?? null;
    }

    this.membersApi.getProjectMembers(this.data.projectId).subscribe({
      next: list => this.members.set(list),
      error: () => {},
    });
  }

  save(): void {
    if (this.isCreateMode) {
      this.createTask();
    } else {
      this.updateTask();
    }
  }

  private createTask(): void {
    if (!this.name.trim() || !this.selectedPhaseId) return;
    const payload: CreateTaskPayload = {
      parentId: this.selectedPhaseId,
      type: 'Task',
      name: this.name.trim(),
      priority: this.priority,
      status: this.status,
      notes: this.notes || undefined,
      plannedStartDate: this.plannedStartDate || undefined,
      plannedEndDate: this.plannedEndDate || undefined,
      assigneeUserId: this.assigneeUserId ?? undefined,
      sortOrder: 0,
    };
    this.store.dispatch(TasksActions.createTask({
      projectId: this.data.projectId,
      request: payload,
    }));
    this.dialogRef.close();
  }

  private updateTask(): void {
    const t = this.data.task!;
    const payload: UpdateTaskPayload = {
      parentId: t.parentId,
      type: t.type,
      vbs: t.vbs ?? undefined,
      name: this.name.trim() || t.name,
      priority: this.priority,
      status: this.status,
      notes: this.notes || undefined,
      plannedStartDate: this.plannedStartDate || undefined,
      plannedEndDate: this.plannedEndDate || undefined,
      actualStartDate: t.actualStartDate ?? undefined,
      actualEndDate: t.actualEndDate ?? undefined,
      plannedEffortHours: t.plannedEffortHours ?? undefined,
      percentComplete: this.status === 'Completed' ? 100 : (t.percentComplete ?? undefined),
      assigneeUserId: this.assigneeUserId ?? undefined,
      sortOrder: t.sortOrder,
      predecessors: t.predecessors,
    };

    this.store.dispatch(TasksActions.updateTask({
      projectId: this.data.projectId,
      taskId: t.id,
      request: payload,
      version: t.version,
    }));

    this.dialogRef.close();
  }

  cancel(): void {
    this.dialogRef.close();
  }

  get fullDetailLink(): string[] {
    return ['/projects', this.data.projectId];
  }

  get fullDetailQueryParams(): Record<string, string> {
    return { view: 'grid', highlight: this.data.task?.id ?? '' };
  }
}
