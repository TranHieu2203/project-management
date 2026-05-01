import { describe, it, expect } from 'vitest';
import { MemoizedSelector } from '@ngrx/store';
import { selectFilteredNotifications } from './notification.selectors';
import { NotificationDto } from '../models/notification.model';

const makeNotif = (id: string, type: string): NotificationDto => ({
  id,
  type,
  title: `Notif ${id}`,
  body: 'body',
  entityType: 'task',
  entityId: null,
  projectId: null,
  isRead: false,
  createdAt: new Date().toISOString(),
  readAt: null,
});

describe('selectFilteredNotifications', () => {
  const projector = (selectFilteredNotifications as MemoizedSelector<any, any, any>).projector;

  it('returns all notifications when filter is "all"', () => {
    const notifs = [makeNotif('1', 'assigned'), makeNotif('2', 'commented')];
    const result = projector(notifs, 'all');
    expect(result).toHaveLength(2);
  });

  it('returns only assigned notifications when filter is "assigned"', () => {
    const notifs = [makeNotif('1', 'assigned'), makeNotif('2', 'commented'), makeNotif('3', 'assigned')];
    const result = projector(notifs, 'assigned');
    expect(result).toHaveLength(2);
    expect(result.every((n: NotificationDto) => n.type === 'assigned')).toBe(true);
  });

  it('returns empty array when filter matches nothing', () => {
    const notifs = [makeNotif('1', 'assigned'), makeNotif('2', 'commented')];
    const result = projector(notifs, 'mentioned');
    expect(result).toHaveLength(0);
  });

  it('filters status-changed correctly', () => {
    const notifs = [makeNotif('1', 'status-changed'), makeNotif('2', 'assigned')];
    const result = projector(notifs, 'status-changed');
    expect(result).toHaveLength(1);
    expect(result[0].id).toBe('1');
  });
});
