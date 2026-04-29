import { describe, it, expect } from 'vitest';

// Pure logic extracted from DashboardMyTasksComponent

const STATUS_OPTIONS: { value: string | null; label: string }[] = [
  { value: null, label: 'Tất cả' },
  { value: 'NotStarted', label: 'Chưa bắt đầu' },
  { value: 'InProgress', label: 'Đang thực hiện' },
  { value: 'OnHold', label: 'Tạm dừng' },
  { value: 'Delayed', label: 'Bị trễ' },
  { value: 'Completed', label: 'Hoàn thành' },
];

function statusLabel(s: string): string {
  return STATUS_OPTIONS.find(o => o.value === s)?.label ?? s;
}

function formatDate(d: string | null): string {
  if (!d) return '—';
  const [y, m, day] = d.split('-');
  return `${day}/${m}/${y}`;
}

function totalPages(totalCount: number, pageSize: number): number {
  return Math.ceil(totalCount / pageSize);
}

describe('DashboardMyTasksComponent — pure logic', () => {
  describe('statusLabel', () => {
    it('returns label for known status', () => {
      expect(statusLabel('InProgress')).toBe('Đang thực hiện');
      expect(statusLabel('NotStarted')).toBe('Chưa bắt đầu');
      expect(statusLabel('OnHold')).toBe('Tạm dừng');
      expect(statusLabel('Delayed')).toBe('Bị trễ');
      expect(statusLabel('Completed')).toBe('Hoàn thành');
    });

    it('returns raw value for unknown status', () => {
      expect(statusLabel('SomeOther')).toBe('SomeOther');
    });
  });

  describe('formatDate', () => {
    it('formats ISO date to dd/MM/yyyy', () => {
      expect(formatDate('2026-04-15')).toBe('15/04/2026');
    });

    it('returns em dash for null', () => {
      expect(formatDate(null)).toBe('—');
    });

    it('returns em dash for empty string', () => {
      expect(formatDate('')).toBe('—');
    });
  });

  describe('totalPages', () => {
    it('returns 1 for exactly pageSize items', () => {
      expect(totalPages(20, 20)).toBe(1);
    });

    it('rounds up when remainder exists', () => {
      expect(totalPages(21, 20)).toBe(2);
      expect(totalPages(41, 20)).toBe(3);
    });

    it('returns 0 for 0 items', () => {
      expect(totalPages(0, 20)).toBe(0);
    });
  });
});
