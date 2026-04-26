import { describe, it, expect } from 'vitest';

// Utility functions extracted for unit testing (mirrors what the component does)
function addDays(date: Date, days: number): Date {
  const d = new Date(date);
  d.setDate(d.getDate() + days);
  return d;
}

function deltaXToDays(deltaX: number, pixelsPerDay: number): number {
  return Math.round(deltaX / pixelsPerDay);
}

function computeNewDates(
  originalStart: Date,
  originalEnd: Date,
  deltaX: number,
  pixelsPerDay: number,
  mode: 'move' | 'resize'
): { plannedStart?: Date; plannedEnd?: Date } {
  const deltaDays = deltaXToDays(deltaX, pixelsPerDay);
  if (mode === 'move') {
    return {
      plannedStart: addDays(originalStart, deltaDays),
      plannedEnd: addDays(originalEnd, deltaDays),
    };
  } else {
    const minEnd = addDays(originalStart, 1);
    const newEnd = addDays(originalEnd, deltaDays);
    return { plannedEnd: newEnd > minEnd ? newEnd : minEnd };
  }
}

describe('drag calculation: deltaX → daysDelta → newDate', () => {
  const ppd = 24; // 24px per day (day mode)
  const ppdWeek = 120 / 7; // ~17.14px per day (week mode)

  it('converts positive deltaX to positive day delta (day mode)', () => {
    expect(deltaXToDays(48, ppd)).toBe(2);
    expect(deltaXToDays(24, ppd)).toBe(1);
  });

  it('converts negative deltaX to negative day delta', () => {
    expect(deltaXToDays(-48, ppd)).toBe(-2);
    expect(deltaXToDays(-24, ppd)).toBe(-1);
  });

  it('rounds partial pixels correctly', () => {
    expect(deltaXToDays(35, ppd)).toBe(1);  // 35/24 = 1.46 → rounds to 1
    expect(deltaXToDays(37, ppd)).toBe(2);  // 37/24 = 1.54 → rounds to 2
  });

  it('handles week mode pixels correctly', () => {
    // ~34.3px = ~2 days in week mode
    const days = deltaXToDays(34, ppdWeek);
    expect(days).toBe(2);
  });

  it('move mode shifts both dates by same delta', () => {
    const start = new Date(2026, 0, 10);
    const end = new Date(2026, 0, 20);
    const { plannedStart, plannedEnd } = computeNewDates(start, end, 48, ppd, 'move');
    expect(plannedStart).toEqual(new Date(2026, 0, 12));
    expect(plannedEnd).toEqual(new Date(2026, 0, 22));
  });

  it('move mode with negative delta moves backward', () => {
    const start = new Date(2026, 0, 10);
    const end = new Date(2026, 0, 20);
    const { plannedStart, plannedEnd } = computeNewDates(start, end, -24, ppd, 'move');
    expect(plannedStart).toEqual(new Date(2026, 0, 9));
    expect(plannedEnd).toEqual(new Date(2026, 0, 19));
  });

  it('resize mode only changes end date', () => {
    const start = new Date(2026, 0, 10);
    const end = new Date(2026, 0, 20);
    const result = computeNewDates(start, end, 48, ppd, 'resize');
    expect(result.plannedStart).toBeUndefined();
    expect(result.plannedEnd).toEqual(new Date(2026, 0, 22));
  });

  it('resize mode enforces minimum 1-day duration', () => {
    const start = new Date(2026, 0, 10);
    const end = new Date(2026, 0, 11); // already 1 day
    // Try to resize to 0 days (negative delta)
    const result = computeNewDates(start, end, -100, ppd, 'resize');
    expect(result.plannedEnd).toEqual(new Date(2026, 0, 11)); // clamped to start+1
  });

  it('resize to negative days is clamped to start+1', () => {
    const start = new Date(2026, 0, 10);
    const end = new Date(2026, 0, 20);
    const result = computeNewDates(start, end, -300, ppd, 'resize');
    // -300/24 = -12.5 → -13 days → 20-13=7 which is before start+1 (jan 11), so clamp
    expect(result.plannedEnd).toEqual(new Date(2026, 0, 11));
  });
});
