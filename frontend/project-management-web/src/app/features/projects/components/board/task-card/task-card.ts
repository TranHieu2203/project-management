import {
  ChangeDetectionStrategy, Component, EventEmitter, Input, Output,
} from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ProjectTask } from '../../../models/task.model';
import { ProjectMember } from '../../../models/project.model';

@Component({
  standalone: true,
  selector: 'app-task-card',
  imports: [MatIconModule, MatTooltipModule],
  templateUrl: './task-card.html',
  styleUrl: './task-card.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaskCardComponent {
  @Input({ required: true }) task!: ProjectTask;
  @Input() assigneeName: string | null = null;
  @Input() parentName: string | null = null;
  @Input() today: string = '';
  @Output() clickCard = new EventEmitter<ProjectTask>();

  get isOverdue(): boolean {
    if (!this.task.plannedEndDate) return false;
    if (['Completed', 'Cancelled'].includes(this.task.status)) return false;
    return this.task.plannedEndDate < this.today;
  }

  get initials(): string {
    if (!this.assigneeName) return '?';
    return this.assigneeName
      .split(' ')
      .map(w => w[0] ?? '')
      .join('')
      .toUpperCase()
      .slice(0, 2);
  }

  get formattedDate(): string {
    const d = this.task.plannedEndDate;
    if (!d) return '';
    const [y, m, day] = d.split('-');
    return `${day}/${m}/${y.slice(2)}`;
  }

  get effortLabel(): string {
    const planned = this.task.plannedEffortHours;
    if (!planned) return '';
    const actual = this.task.actualEffortHours;
    return actual !== null ? `${actual}/${planned}h` : `${planned}h`;
  }

  readonly priorityLabels: Record<string, string> = {
    Low: 'Thấp', Medium: 'TB', High: 'Cao', Critical: 'Khẩn',
  };

  readonly typeIcon: Record<string, string> = {
    Phase: 'folder', Milestone: 'flag', Task: 'task_alt',
  };

  onCardClick(event: MouseEvent): void {
    event.stopPropagation();
    this.clickCard.emit(this.task);
  }
}
