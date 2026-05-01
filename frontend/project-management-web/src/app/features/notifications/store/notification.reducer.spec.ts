import { describe, it, expect } from 'vitest';
import { notificationsFeature, initialState, NotificationsState } from './notification.reducer';
import { NotificationActions } from './notification.actions';
import { NotificationDto } from '../models/notification.model';

const reducer = notificationsFeature.reducer;

const makeNotif = (id: string, isRead = false): NotificationDto => ({
  id,
  type: 'assigned',
  title: 'Task X',
  body: 'body',
  entityType: 'task',
  entityId: 'entity-1',
  projectId: 'proj-1',
  isRead,
  createdAt: new Date().toISOString(),
  readAt: null,
});

describe('notificationsFeature reducer', () => {
  it('loadNotifications sets loading=true', () => {
    const state = reducer(initialState, NotificationActions.loadNotifications());
    expect(state.loading).toBe(true);
  });

  it('loadNotificationsSuccess stores notifications and calculates unreadCount', () => {
    const notifs = [makeNotif('1', false), makeNotif('2', true), makeNotif('3', false)];
    const state = reducer(initialState, NotificationActions.loadNotificationsSuccess({ notifications: notifs }));
    expect(state.loading).toBe(false);
    expect(state.notifications).toHaveLength(3);
    expect(state.unreadCount).toBe(2);
  });

  it('loadNotificationsFailure sets loading=false', () => {
    const loading = reducer({ ...initialState, loading: true }, NotificationActions.loadNotificationsFailure({ error: 'err' }));
    expect(loading.loading).toBe(false);
  });

  it('markReadSuccess marks single notification as read and decrements unreadCount', () => {
    const withTwo: NotificationsState = {
      ...initialState,
      notifications: [makeNotif('a', false), makeNotif('b', false)],
      unreadCount: 2,
    };
    const state = reducer(withTwo, NotificationActions.markReadSuccess({ id: 'a' }));
    expect(state.notifications.find(n => n.id === 'a')?.isRead).toBe(true);
    expect(state.notifications.find(n => n.id === 'b')?.isRead).toBe(false);
    expect(state.unreadCount).toBe(1);
  });

  it('markAllReadSuccess marks all as read and sets unreadCount to 0', () => {
    const withTwo: NotificationsState = {
      ...initialState,
      notifications: [makeNotif('a', false), makeNotif('b', false)],
      unreadCount: 2,
    };
    const state = reducer(withTwo, NotificationActions.markAllReadSuccess());
    expect(state.notifications.every(n => n.isRead)).toBe(true);
    expect(state.unreadCount).toBe(0);
  });

  it('togglePanel flips panelOpen', () => {
    const opened = reducer(initialState, NotificationActions.togglePanel());
    expect(opened.panelOpen).toBe(true);
    const closed = reducer(opened, NotificationActions.togglePanel());
    expect(closed.panelOpen).toBe(false);
  });

  it('closePanel sets panelOpen to false', () => {
    const opened = { ...initialState, panelOpen: true };
    const state = reducer(opened, NotificationActions.closePanel());
    expect(state.panelOpen).toBe(false);
  });

  it('setFilter updates filter', () => {
    const state = reducer(initialState, NotificationActions.setFilter({ filter: 'assigned' }));
    expect(state.filter).toBe('assigned');
  });
});
