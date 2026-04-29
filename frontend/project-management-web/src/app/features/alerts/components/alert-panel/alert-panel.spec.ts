import { describe, it, expect } from 'vitest';
import { AlertDto } from '../../models/alert.model';

// Pure logic extracted from AlertPanelComponent and alertsFeature reducer

function typeLabel(type: string): string {
  const labels: Record<string, string> = {
    deadline: 'Deadline',
    overload: 'Quá tải',
    budget: 'Budget',
  };
  return labels[type] ?? type;
}

function countUnread(alerts: Pick<AlertDto, 'isRead'>[]): number {
  return alerts.filter(a => !a.isRead).length;
}

function markReadInList(alerts: AlertDto[], id: string): AlertDto[] {
  return alerts.map(a =>
    a.id === id ? { ...a, isRead: true, readAt: new Date().toISOString() } : a
  );
}

describe('AlertPanelComponent — pure logic', () => {
  describe('typeLabel', () => {
    it('returns Deadline for deadline type', () => {
      expect(typeLabel('deadline')).toBe('Deadline');
    });

    it('returns Quá tải for overload type', () => {
      expect(typeLabel('overload')).toBe('Quá tải');
    });

    it('returns Budget for budget type', () => {
      expect(typeLabel('budget')).toBe('Budget');
    });

    it('returns type string itself for unknown type', () => {
      expect(typeLabel('custom')).toBe('custom');
    });
  });

  describe('unread count', () => {
    it('counts only unread alerts', () => {
      const alerts = [
        { isRead: false },
        { isRead: true },
        { isRead: false },
      ];
      expect(countUnread(alerts)).toBe(2);
    });

    it('returns 0 when all alerts are read', () => {
      const alerts = [{ isRead: true }, { isRead: true }];
      expect(countUnread(alerts)).toBe(0);
    });

    it('returns 0 for empty list', () => {
      expect(countUnread([])).toBe(0);
    });
  });

  describe('markReadInList', () => {
    const makeAlert = (id: string, isRead = false): AlertDto => ({
      id,
      projectId: null,
      type: 'deadline',
      entityType: null,
      entityId: null,
      title: `Alert ${id}`,
      description: null,
      isRead,
      createdAt: new Date().toISOString(),
      readAt: null,
    });

    it('marks the matching alert as read', () => {
      const alerts = [makeAlert('1'), makeAlert('2')];
      const updated = markReadInList(alerts, '1');
      expect(updated[0].isRead).toBe(true);
      expect(updated[1].isRead).toBe(false);
    });

    it('does not mutate original list', () => {
      const alerts = [makeAlert('1')];
      const updated = markReadInList(alerts, '1');
      expect(alerts[0].isRead).toBe(false);
      expect(updated[0].isRead).toBe(true);
    });

    it('leaves list unchanged if id not found', () => {
      const alerts = [makeAlert('1'), makeAlert('2')];
      const updated = markReadInList(alerts, '99');
      expect(updated.every(a => !a.isRead)).toBe(true);
    });

    it('sets readAt timestamp when marking read', () => {
      const alerts = [makeAlert('1')];
      const updated = markReadInList(alerts, '1');
      expect(updated[0].readAt).not.toBeNull();
    });
  });
});
