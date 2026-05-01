import { createSelector } from '@ngrx/store';
import {
  notificationsFeature,
  selectNotifications,
  selectFilter,
} from './notification.reducer';

export const {
  selectPanelOpen: selectNotifPanelOpen,
  selectUnreadCount: selectNotifUnreadCount,
  selectLoading: selectNotifLoading,
} = notificationsFeature;

export const selectFilteredNotifications = createSelector(
  selectNotifications,
  selectFilter,
  (notifications, filter) =>
    filter === 'all'
      ? notifications
      : notifications.filter(n => n.type === filter)
);
