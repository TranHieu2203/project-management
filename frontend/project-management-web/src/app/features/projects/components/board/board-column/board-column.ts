import {
  ChangeDetectionStrategy, Component, EventEmitter, Input, Output, signal,
} from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { CdkDropList, CdkDrag, CdkDragDrop } from '@angular/cdk/drag-drop';
import { ProjectTask } from '../../../models/task.model';
import { ProjectMember } from '../../../models/project.model';
import { TaskCardComponent } from '../task-card/task-card';
import {
  TaskQuickCreateComponent, QuickCreateSubmitEvent, QuickCreateFullFormEvent,
} from '../task-quick-create/task-quick-create';

export const STATUS_META: Record<string, { label: string; colorClass: string }> = {
  NotStarted: { label: 'Chưa bắt đầu', colorClass: 'col-not-started' },
  InProgress:  { label: 'Đang thực hiện', colorClass: 'col-in-progress' },
  OnHold:      { label: 'Tạm dừng',       colorClass: 'col-on-hold' },
  Delayed:     { label: 'Bị trễ',          colorClass: 'col-delayed' },
  Completed:   { label: 'Hoàn thành',      colorClass: 'col-completed' },
  Cancelled:   { label: 'Đã hủy',          colorClass: 'col-cancelled' },
};

export interface ColumnQuickCreateEvent {
  status: ProjectTask['status'];
  name: string;
  phaseId: string;
}

export interface ColumnOpenFullFormEvent {
  status: ProjectTask['status'];
  name: string;
  phaseId: string | null;
}

@Component({
  standalone: true,
  selector: 'app-board-column',
  imports: [
    MatButtonModule, MatIconModule, MatTooltipModule,
    CdkDropList, CdkDrag,
    TaskCardComponent, TaskQuickCreateComponent,
  ],
  templateUrl: './board-column.html',
  styleUrl: './board-column.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BoardColumnComponent {
  @Input({ required: true }) status!: ProjectTask['status'];
  @Input({ required: true }) columnData!: ProjectTask[];
  @Input({ required: true }) displayTasks!: ProjectTask[];
  @Input({ required: true }) totalCount!: number;
  @Input() members: Map<string, ProjectMember> = new Map();
  @Input() parentNames: Map<string, string> = new Map();
  @Input() today: string = '';
  @Input() connectedTo: string[] = [];
  @Input() phases: ProjectTask[] = [];
  @Input() showQuickCreate: boolean = true;

  @Output() dropped = new EventEmitter<CdkDragDrop<ProjectTask[]>>();
  @Output() loadMore = new EventEmitter<void>();
  @Output() cardClick = new EventEmitter<ProjectTask>();
  @Output() quickCreate = new EventEmitter<ColumnQuickCreateEvent>();
  @Output() openFullForm = new EventEmitter<ColumnOpenFullFormEvent>();

  readonly isCreating = signal(false);

  get meta(): { label: string; colorClass: string } {
    return STATUS_META[this.status] ?? { label: this.status, colorClass: '' };
  }

  get showLoadMore(): boolean {
    return this.displayTasks.length < this.totalCount;
  }

  get remainingCount(): number {
    return this.totalCount - this.displayTasks.length;
  }

  get hasNoPhases(): boolean {
    return this.phases.length === 0;
  }

  assigneeName(task: ProjectTask): string | null {
    if (!task.assigneeUserId) return null;
    const m = this.members.get(task.assigneeUserId);
    return m?.displayName ?? m?.username ?? null;
  }

  parentNameFor(task: ProjectTask): string | null {
    return task.parentId ? (this.parentNames.get(task.parentId) ?? null) : null;
  }

  onDrop(event: CdkDragDrop<ProjectTask[]>): void {
    this.dropped.emit(event);
  }

  toggleCreate(): void {
    if (this.hasNoPhases) return;
    this.isCreating.set(!this.isCreating());
  }

  onQuickCreateSubmit(event: QuickCreateSubmitEvent): void {
    this.isCreating.set(false);
    this.quickCreate.emit({ status: this.status, name: event.name, phaseId: event.phaseId });
  }

  onQuickCreateCancel(): void {
    this.isCreating.set(false);
  }

  onOpenFullForm(event: QuickCreateFullFormEvent): void {
    this.isCreating.set(false);
    this.openFullForm.emit({ status: this.status, name: event.name, phaseId: event.phaseId });
  }
}
