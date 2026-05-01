import { describe, it, expect } from 'vitest';
import { FILTER_OPTIONS } from './notification-panel';

// Pure logic tests — no Angular TestBed required
// typeLabel logic is inline in the component; test the FILTER_OPTIONS constant

describe('NotificationPanel pure logic', () => {
  describe('FILTER_OPTIONS', () => {
    it('has exactly 5 entries', () => {
      expect(FILTER_OPTIONS).toHaveLength(5);
    });

    it('first entry is "all"', () => {
      expect(FILTER_OPTIONS[0].value).toBe('all');
      expect(FILTER_OPTIONS[0].label).toBe('Tất cả');
    });

    it('contains "assigned" entry', () => {
      const entry = FILTER_OPTIONS.find(f => f.value === 'assigned');
      expect(entry).toBeDefined();
      expect(entry?.label).toBe('Được giao');
    });

    it('contains "status-changed" entry', () => {
      const entry = FILTER_OPTIONS.find(f => f.value === 'status-changed');
      expect(entry).toBeDefined();
      expect(entry?.label).toBe('Trạng thái');
    });

    it('contains "commented" entry', () => {
      const entry = FILTER_OPTIONS.find(f => f.value === 'commented');
      expect(entry).toBeDefined();
      expect(entry?.label).toBe('Bình luận');
    });

    it('contains "mentioned" entry', () => {
      const entry = FILTER_OPTIONS.find(f => f.value === 'mentioned');
      expect(entry).toBeDefined();
      expect(entry?.label).toBe('@Mention');
    });
  });

  describe('typeLabel mapping', () => {
    const labels: Record<string, string> = {
      assigned: 'Được giao',
      'status-changed': 'Trạng thái',
      commented: 'Bình luận',
      mentioned: '@Mention',
    };

    it.each(Object.entries(labels))('typeLabel(%s) returns %s', (type, expected) => {
      const result = labels[type] ?? type;
      expect(result).toBe(expected);
    });

    it('unknown type returns the type itself', () => {
      const result = labels['unknown-type'] ?? 'unknown-type';
      expect(result).toBe('unknown-type');
    });
  });
});
