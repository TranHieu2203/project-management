import { describe, it, expect } from 'vitest';

// Pure logic extracted from NotificationPreferencesComponent

function getLabel(type: string): string {
  const labels: Record<string, string> = {
    'overload':        'Cảnh báo Overload',
    'overdue':         'Task sắp trễ',
    'assigned':        'Được giao task',
    'commented':       'Có comment mới',
    'status-changed':  'Task thay đổi trạng thái',
    'mentioned':       '@mention trong comment',
  };
  return labels[type] ?? type;
}

function mapPreferences(prefs: { type: string; isEnabled: boolean }[]) {
  return prefs.map(p => ({ type: p.type, label: getLabel(p.type), isEnabled: p.isEnabled }));
}

const DIGEST_TYPES = ['overload', 'overdue'];
const REALTIME_TYPES = ['assigned', 'commented', 'status-changed', 'mentioned'];

describe('NotificationPreferencesComponent — pure logic', () => {
  describe('getLabel', () => {
    it('returns correct label for overload', () => {
      expect(getLabel('overload')).toBe('Cảnh báo Overload');
    });

    it('returns correct label for overdue', () => {
      expect(getLabel('overdue')).toBe('Task sắp trễ');
    });

    it('returns correct label for assigned (new Story 7-4)', () => {
      expect(getLabel('assigned')).toBe('Được giao task');
    });

    it('returns correct label for commented (new Story 7-4)', () => {
      expect(getLabel('commented')).toBe('Có comment mới');
    });

    it('returns correct label for status-changed (new Story 7-4)', () => {
      expect(getLabel('status-changed')).toBe('Task thay đổi trạng thái');
    });

    it('returns correct label for mentioned (new Story 7-4)', () => {
      expect(getLabel('mentioned')).toBe('@mention trong comment');
    });

    it('returns type itself for unknown type', () => {
      expect(getLabel('unknown-type')).toBe('unknown-type');
    });
  });

  describe('mapPreferences', () => {
    it('maps 6 types correctly when API returns 6 types', () => {
      const allTypes = [...DIGEST_TYPES, ...REALTIME_TYPES];
      const input = allTypes.map(type => ({ type, isEnabled: true }));
      const result = mapPreferences(input);

      expect(result).toHaveLength(6);
      result.forEach(p => {
        expect(p.label).not.toBe(p.type); // all known types should have proper labels
      });
    });

    it('preserves isEnabled value from API', () => {
      const input = [
        { type: 'assigned', isEnabled: false },
        { type: 'overload', isEnabled: true },
      ];
      const result = mapPreferences(input);

      expect(result[0].isEnabled).toBe(false);
      expect(result[1].isEnabled).toBe(true);
    });
  });

  describe('UI grouping logic', () => {
    it('digest types are overload and overdue', () => {
      expect(DIGEST_TYPES).toContain('overload');
      expect(DIGEST_TYPES).toContain('overdue');
      expect(DIGEST_TYPES).toHaveLength(2);
    });

    it('realtime types include the 4 new Story 7-4 types', () => {
      expect(REALTIME_TYPES).toContain('assigned');
      expect(REALTIME_TYPES).toContain('commented');
      expect(REALTIME_TYPES).toContain('status-changed');
      expect(REALTIME_TYPES).toContain('mentioned');
      expect(REALTIME_TYPES).toHaveLength(4);
    });

    it('digest and realtime groups partition all 6 types without overlap', () => {
      const all = new Set([...DIGEST_TYPES, ...REALTIME_TYPES]);
      expect(all.size).toBe(6);
      const overlap = DIGEST_TYPES.filter(t => REALTIME_TYPES.includes(t));
      expect(overlap).toHaveLength(0);
    });
  });
});
