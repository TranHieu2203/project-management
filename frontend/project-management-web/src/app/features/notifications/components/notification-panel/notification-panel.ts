import { ChangeDetectionStrategy, Component, EventEmitter, Output, inject } from '@angular/core';
import { AsyncPipe, DatePipe, NgClass, NgFor, NgIf } from '@angular/common';
import { Store } from '@ngrx/store';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDividerModule } from '@angular/material/divider';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { NotificationActions, NotificationFilter } from '../../store/notification.actions';
import {
  selectFilteredNotifications,
  selectNotifLoading,
  selectNotifUnreadCount,
} from '../../store/notification.selectors';
import { selectFilter } from '../../store/notification.reducer';
import { NotificationDto } from '../../models/notification.model';

export const FILTER_OPTIONS: { value: NotificationFilter; label: string }[] = [
  { value: 'all', label: 'Tất cả' },
  { value: 'assigned', label: 'Được giao' },
  { value: 'status-changed', label: 'Trạng thái' },
  { value: 'commented', label: 'Bình luận' },
  { value: 'mentioned', label: '@Mention' },
];

@Component({
  selector: 'app-notification-panel',
  standalone: true,
  imports: [
    AsyncPipe, DatePipe, NgFor, NgIf, NgClass,
    MatButtonModule, MatIconModule, MatDividerModule, MatProgressSpinnerModule,
  ],
  templateUrl: './notification-panel.html',
  styleUrl: './notification-panel.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotificationPanelComponent {
  private readonly store = inject(Store);

  readonly notifications$ = this.store.select(selectFilteredNotifications);
  readonly loading$ = this.store.select(selectNotifLoading);
  readonly unreadCount$ = this.store.select(selectNotifUnreadCount);
  readonly filter$ = this.store.select(selectFilter);

  readonly FILTER_OPTIONS = FILTER_OPTIONS;

  @Output() closed = new EventEmitter<void>();

  typeLabel(type: string): string {
    const labels: Record<string, string> = {
      assigned: 'Được giao',
      'status-changed': 'Trạng thái',
      commented: 'Bình luận',
      mentioned: '@Mention',
    };
    return labels[type] ?? type;
  }

  onNotificationClick(n: NotificationDto): void {
    this.store.dispatch(NotificationActions.markRead({
      id: n.id,
      projectId: n.projectId,
      entityId: n.entityId,
    }));
    this.store.dispatch(NotificationActions.closePanel());
    this.closed.emit();
  }

  onMarkAllRead(): void {
    this.store.dispatch(NotificationActions.markAllRead());
  }

  onFilterChange(filter: NotificationFilter): void {
    this.store.dispatch(NotificationActions.setFilter({ filter }));
  }

  trackNotification(_: number, n: NotificationDto): string { return n.id; }
}
