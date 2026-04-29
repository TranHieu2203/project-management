import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Deadline } from '../../../models/dashboard.model';

@Component({
  standalone: true,
  selector: 'app-upcoming-deadlines',
  imports: [DatePipe, MatIconModule, MatTooltipModule],
  templateUrl: './upcoming-deadlines.html',
  styleUrl: './upcoming-deadlines.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UpcomingDeadlinesComponent {
  @Input({ required: true }) deadlines: Deadline[] = [];
  @Input() loading = false;
  @Input() error: string | null = null;
  @Output() deadlineClick = new EventEmitter<Deadline>();

  formatDaysRemaining(days: number): string {
    if (days === 0) return 'Hôm nay';
    if (days === 1) return 'Còn 1 ngày';
    return `Còn ${days} ngày`;
  }

  urgencyClass(days: number): string {
    if (days === 0) return 'urgent';
    if (days <= 2) return 'warning';
    return 'normal';
  }
}
