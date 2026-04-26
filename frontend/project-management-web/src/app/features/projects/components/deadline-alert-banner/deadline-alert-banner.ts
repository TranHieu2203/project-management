import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { DeadlineStatus } from '../../services/deadline-alert.service';

@Component({
  selector: 'app-deadline-alert-banner',
  standalone: true,
  imports: [MatIconModule],
  templateUrl: './deadline-alert-banner.html',
  styleUrl: './deadline-alert-banner.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DeadlineAlertBannerComponent {
  @Input() overdueCnt = 0;
  @Input() dueTodayCnt = 0;
  @Input() dueSoonCnt = 0;
  @Input() activeFilter: DeadlineStatus | null = null;
  @Output() filterChange = new EventEmitter<DeadlineStatus | null>();

  onBadgeClick(group: DeadlineStatus): void {
    // Toggle: click active group → clear filter
    const next = this.activeFilter === group ? null : group;
    this.filterChange.emit(next);
  }
}
