import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { AsyncPipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MAT_DATE_FORMATS, MAT_DATE_LOCALE, provideNativeDateAdapter } from '@angular/material/core';

const VN_DATE_FORMATS = {
  parse: { dateInput: { day: 'numeric', month: 'numeric', year: 'numeric' } },
  display: {
    dateInput:           { day: '2-digit', month: '2-digit', year: 'numeric' },
    monthYearLabel:      { month: 'short', year: 'numeric' },
    dateA11yLabel:       { day: 'numeric', month: 'long', year: 'numeric' },
    monthYearA11yLabel:  { month: 'long', year: 'numeric' },
  },
};
import { Store } from '@ngrx/store';
import { TasksActions } from '../../store/tasks.actions';
import { selectTasksCreating, selectTasksUpdating, selectTasksByProject } from '../../store/tasks.selectors';
import { ProjectTask } from '../../models/task.model';
import { ProjectMember } from '../../models/project.model';
import { MembersApiService } from '../../services/members-api.service';

function dateRangeValidator(startKey: string, endKey: string, errorKey: string): ValidatorFn {
  return (group: AbstractControl): ValidationErrors | null => {
    const start = group.get(startKey)?.value as Date | null;
    const end = group.get(endKey)?.value as Date | null;
    return (start && end && end < start) ? { [errorKey]: true } : null;
  };
}

export interface TaskFormData {
  mode: 'create' | 'edit';
  projectId: string;
  parentId?: string | null;
  task?: ProjectTask;
}

@Component({
  selector: 'app-task-form',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    AsyncPipe,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatProgressSpinnerModule,
    MatDatepickerModule,
  ],
  providers: [
    provideNativeDateAdapter(),
    { provide: MAT_DATE_LOCALE, useValue: 'vi-VN' },
    { provide: MAT_DATE_FORMATS, useValue: VN_DATE_FORMATS },
  ],
  templateUrl: './task-form.html',
  styleUrl: './task-form.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaskFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly store = inject(Store);
  private readonly dialogRef = inject(MatDialogRef<TaskFormComponent>);
  private readonly membersApi = inject(MembersApiService);
  private readonly destroyRef = inject(DestroyRef);
  readonly data = inject<TaskFormData>(MAT_DIALOG_DATA);

  protected readonly creating$ = this.store.select(selectTasksCreating);
  protected readonly updating$ = this.store.select(selectTasksUpdating);
  protected readonly members = signal<ProjectMember[]>([]);
  protected readonly allTasks = signal<ProjectTask[]>([]);

  protected readonly parentOptions = computed(() => {
    const tasks = this.allTasks();
    const excluded = new Set<string>();
    if (this.isEdit && this.data.task) {
      const desc = this.collectDescendants(tasks, this.data.task.id);
      desc.forEach(id => excluded.add(id));
      excluded.add(this.data.task.id);
    }
    return this.buildTreeOptions(tasks, excluded);
  });

  private collectDescendants(tasks: ProjectTask[], rootId: string): Set<string> {
    const ids = new Set<string>();
    const queue = [rootId];
    while (queue.length) {
      const id = queue.shift()!;
      for (const t of tasks) {
        if (t.parentId === id) { ids.add(t.id); queue.push(t.id); }
      }
    }
    return ids;
  }

  private buildTreeOptions(
    tasks: ProjectTask[],
    disabled: Set<string>,
  ): { id: string; label: string; disabled: boolean }[] {
    const result: { id: string; label: string; disabled: boolean }[] = [];
    const byParent = new Map<string | null, ProjectTask[]>();
    for (const t of tasks) {
      const key = t.parentId ?? null;
      if (!byParent.has(key)) byParent.set(key, []);
      byParent.get(key)!.push(t);
    }
    const traverse = (parentId: string | null, depth: number) => {
      const children = (byParent.get(parentId) ?? []).sort((a, b) => a.sortOrder - b.sortOrder);
      for (const t of children) {
        const indent = '   '.repeat(depth);
        const connector = depth > 0 ? '└─ ' : '';
        const code = t.vbs ? `[${t.vbs}] ` : '';
        result.push({ id: t.id, label: indent + connector + code + t.name, disabled: disabled.has(t.id) });
        traverse(t.id, depth + 1);
      }
    };
    traverse(null, 0);
    return result;
  }

  readonly taskTypes = ['Phase', 'Milestone', 'Task'] as const;
  readonly priorities = ['Low', 'Medium', 'High', 'Critical'] as const;
  readonly statuses = ['NotStarted', 'InProgress', 'Completed', 'OnHold', 'Cancelled', 'Delayed'] as const;

  protected readonly form = this.fb.nonNullable.group(
    {
      parentId: [''],
      type: ['Task', Validators.required],
      vbs: ['', Validators.maxLength(50)],
      name: ['', [Validators.required, Validators.maxLength(500)]],
      priority: ['Medium', Validators.required],
      status: ['NotStarted', Validators.required],
      notes: ['', Validators.maxLength(4000)],
      plannedStartDate: [null as Date | null],
      plannedEndDate: [null as Date | null],
      actualStartDate: [null as Date | null],
      actualEndDate: [null as Date | null],
      plannedEffortHours: [null as number | null],
      percentComplete: [null as number | null],
      assigneeUserId: [''],
      sortOrder: [0],
    },
    {
      validators: [
        dateRangeValidator('plannedStartDate', 'plannedEndDate', 'plannedRange'),
        dateRangeValidator('actualStartDate', 'actualEndDate', 'actualRange'),
      ],
    },
  );

  get isEdit(): boolean {
    return this.data.mode === 'edit';
  }

  ngOnInit(): void {
    this.store.select(selectTasksByProject(this.data.projectId))
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(tasks => {
        this.allTasks.set(tasks);
      });

    // If TasksState is empty (e.g. opened from Gantt), trigger a load
    if (this.allTasks().length === 0) {
      this.store.dispatch(TasksActions.loadTasks({ projectId: this.data.projectId }));
    }

    this.membersApi.getProjectMembers(this.data.projectId).subscribe({
      next: (list) => this.members.set(list),
      error: () => this.members.set([]),
    });

    if (this.isEdit && this.data.task) {
      const t = this.data.task;
      this.form.patchValue({
        parentId: t.parentId ?? '',
        type: t.type,
        vbs: t.vbs ?? '',
        name: t.name,
        priority: t.priority,
        status: t.status,
        notes: t.notes ?? '',
        plannedStartDate: this.parseDate(t.plannedStartDate),
        plannedEndDate: this.parseDate(t.plannedEndDate),
        actualStartDate: this.parseDate(t.actualStartDate),
        actualEndDate: this.parseDate(t.actualEndDate),
        plannedEffortHours: t.plannedEffortHours,
        percentComplete: t.percentComplete,
        assigneeUserId: t.assigneeUserId ?? '',
        sortOrder: t.sortOrder,
      });
    } else {
      this.form.patchValue({ parentId: this.data.parentId ?? '' });
    }
  }

  memberLabel(m: ProjectMember): string {
    return m.displayName ? `${m.displayName} (${m.username})` : m.username;
  }

  submit(): void {
    if (this.form.invalid) return;

    const v = this.form.getRawValue();
    const request = {
      parentId: v.parentId || null,
      type: v.type,
      vbs: v.vbs || undefined,
      name: v.name,
      priority: v.priority,
      status: v.status,
      notes: v.notes || undefined,
      plannedStartDate: this.formatDate(v.plannedStartDate) || undefined,
      plannedEndDate: this.formatDate(v.plannedEndDate) || undefined,
      actualStartDate: this.formatDate(v.actualStartDate) || undefined,
      actualEndDate: this.formatDate(v.actualEndDate) || undefined,
      plannedEffortHours: v.plannedEffortHours ?? undefined,
      percentComplete: v.percentComplete ?? undefined,
      assigneeUserId: v.assigneeUserId || undefined,
      sortOrder: v.sortOrder,
      predecessors: [],
    };

    if (this.isEdit && this.data.task) {
      this.store.dispatch(TasksActions.updateTask({
        projectId: this.data.projectId,
        taskId: this.data.task.id,
        request,
        version: this.data.task.version,
      }));
    } else {
      this.store.dispatch(TasksActions.createTask({
        projectId: this.data.projectId,
        request,
      }));
    }

    this.dialogRef.close();
  }

  cancel(): void {
    this.dialogRef.close();
  }

  private parseDate(s: string | null | undefined): Date | null {
    if (!s) return null;
    const [y, m, d] = s.split('-').map(Number);
    return new Date(y, m - 1, d);
  }

  private formatDate(d: Date | null): string {
    if (!d) return '';
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${y}-${m}-${day}`;
  }
}
