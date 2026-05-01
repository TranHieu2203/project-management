import { createFeature, createReducer, on } from '@ngrx/store';
import { NotificationDto } from '../models/notification.model';
import { NotificationActions, NotificationFilter } from './notification.actions';

export interface NotificationsState {
  notifications: NotificationDto[];
  loading: boolean;
  panelOpen: boolean;
  unreadCount: number;
  filter: NotificationFilter;
}

export const initialState: NotificationsState = {
  notifications: [],
  loading: false,
  panelOpen: false,
  unreadCount: 0,
  filter: 'all',
};

export const notificationsFeature = createFeature({
  name: 'notifications',
  reducer: createReducer(
    initialState,
    on(NotificationActions.loadNotifications, state => ({ ...state, loading: true })),
    on(NotificationActions.loadNotificationsSuccess, (state, { notifications }) => ({
      ...state,
      loading: false,
      notifications,
      unreadCount: notifications.filter(n => !n.isRead).length,
    })),
    on(NotificationActions.loadNotificationsFailure, state => ({ ...state, loading: false })),
    on(NotificationActions.markReadSuccess, (state, { id }) => {
      const notifications = state.notifications.map(n =>
        n.id === id ? { ...n, isRead: true } : n
      );
      return { ...state, notifications, unreadCount: notifications.filter(n => !n.isRead).length };
    }),
    on(NotificationActions.markAllReadSuccess, state => ({
      ...state,
      notifications: state.notifications.map(n => ({ ...n, isRead: true })),
      unreadCount: 0,
    })),
    on(NotificationActions.togglePanel, state => ({ ...state, panelOpen: !state.panelOpen })),
    on(NotificationActions.closePanel, state => ({ ...state, panelOpen: false })),
    on(NotificationActions.setFilter, (state, { filter }) => ({ ...state, filter })),
  ),
});

export const {
  selectNotifications,
  selectLoading,
  selectPanelOpen,
  selectUnreadCount,
  selectFilter,
} = notificationsFeature;
