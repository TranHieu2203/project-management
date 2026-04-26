import { TestBed } from '@angular/core/testing';
import { GanttTimelineService } from './gantt-timeline.service';
import { DEFAULT_GANTT_CONFIG, GanttTask } from '../models/gantt.model';

const makeGanttTask = (partial: Partial<GanttTask>): GanttTask => ({
  id: 'task-1',
  parentId: null,
  type: 'Task',
  vbs: '1.1',
  name: 'Test',
  status: 'NotStarted',
  priority: 'Medium',
  plannedStart: new Date(2026, 4, 1), // May 1
  plannedEnd: new Date(2026, 4, 15),  // May 15
  percentComplete: 0,
  depth: 0,
  sortOrder: 0,
  collapsed: false,
  predecessors: [],
  ...partial,
});

describe('GanttTimelineService', () => {
  let service: GanttTimelineService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(GanttTimelineService);
  });

  describe('dateToX', () => {
    it('should return 0 for the timeline start date', () => {
      const start = new Date(2026, 4, 1);
      const x = service.dateToX(new Date(2026, 4, 1), start, 24);
      expect(x).toBe(0);
    });

    it('should return 24 for 1 day after start with 24 pixelsPerDay', () => {
      const start = new Date(2026, 4, 1);
      const date = new Date(2026, 4, 2);
      const x = service.dateToX(date, start, 24);
      expect(x).toBe(24);
    });

    it('should return 168 for 7 days (1 week) with 24 pixelsPerDay', () => {
      const start = new Date(2026, 4, 1);
      const date = new Date(2026, 4, 8);
      const x = service.dateToX(date, start, 24);
      expect(x).toBe(168);
    });

    it('should handle week granularity (120/7 ppd)', () => {
      const ppd = 120 / 7;
      const start = new Date(2026, 4, 4); // Monday
      const date = new Date(2026, 4, 11); // Next Monday = 7 days
      const x = service.dateToX(date, start, ppd);
      expect(x).toBeCloseTo(120, 0);
    });
  });

  describe('getTimelineRange', () => {
    it('should return a default range when no tasks have dates', () => {
      const tasks = [makeGanttTask({ plannedStart: null, plannedEnd: null })];
      const range = service.getTimelineRange(tasks, DEFAULT_GANTT_CONFIG);
      expect(range.totalDays).toBeGreaterThan(0);
      expect(range.totalWidth).toBeGreaterThan(0);
    });

    it('should set start to Monday before earliest date', () => {
      const tasks = [makeGanttTask({
        plannedStart: new Date(2026, 4, 6), // Wednesday
        plannedEnd: new Date(2026, 4, 20),
      })];
      const range = service.getTimelineRange(tasks, DEFAULT_GANTT_CONFIG);
      expect(range.start.getDay()).toBe(1); // Monday
      expect(range.start <= new Date(2026, 4, 6)).toBe(true);
    });

    it('should include buffer weeks after last date', () => {
      const tasks = [makeGanttTask({
        plannedStart: new Date(2026, 4, 1),
        plannedEnd: new Date(2026, 4, 15),
      })];
      const range = service.getTimelineRange(tasks, DEFAULT_GANTT_CONFIG);
      const lastDate = new Date(2026, 4, 15);
      expect(range.end > lastDate).toBe(true);
    });
  });

  describe('getBarColor', () => {
    it('should return blue for Phase', () => {
      const task = makeGanttTask({ type: 'Phase' });
      expect(service.getBarColor(task)).toBe('#2196F3');
    });

    it('should return orange for Milestone', () => {
      const task = makeGanttTask({ type: 'Milestone' });
      expect(service.getBarColor(task)).toBe('#FF9800');
    });

    it('should return red for Delayed task', () => {
      const task = makeGanttTask({ type: 'Task', status: 'Delayed' });
      expect(service.getBarColor(task)).toBe('#F44336');
    });

    it('should return grey for Completed task', () => {
      const task = makeGanttTask({ type: 'Task', status: 'Completed' });
      expect(service.getBarColor(task)).toBe('#9E9E9E');
    });

    it('should return green for normal Task', () => {
      const task = makeGanttTask({ type: 'Task', status: 'NotStarted' });
      expect(service.getBarColor(task)).toBe('#4CAF50');
    });
  });

  describe('calculateArrowPath', () => {
    const from = makeGanttTask({
      id: 'from',
      plannedStart: new Date(2026, 4, 1),
      plannedEnd: new Date(2026, 4, 10),
    });
    const to = makeGanttTask({
      id: 'to',
      plannedStart: new Date(2026, 4, 11),
      plannedEnd: new Date(2026, 4, 20),
    });
    const timelineStart = new Date(2026, 4, 1);
    const ppd = 24;
    const rowHeight = 36;

    it('should return an SVG path string for FS dependency', () => {
      const path = service.calculateArrowPath(from, to, 'FS', 0, 1, timelineStart, ppd, rowHeight);
      expect(typeof path).toBe('string');
      expect(path.startsWith('M ')).toBe(true);
    });

    it('should return different paths for different dependency types', () => {
      const fs = service.calculateArrowPath(from, to, 'FS', 0, 1, timelineStart, ppd, rowHeight);
      const ss = service.calculateArrowPath(from, to, 'SS', 0, 1, timelineStart, ppd, rowHeight);
      expect(fs).not.toBe(ss);
    });

    it('should use right edge of predecessor for FS type', () => {
      // FS: x1 = dateToX(from.plannedEnd) = dateToX(May 10) = 9 days * 24 = 216
      const path = service.calculateArrowPath(from, to, 'FS', 0, 1, timelineStart, ppd, rowHeight);
      expect(path).toContain('M 216'); // right edge of predecessor
    });

    it('should use left edge of predecessor for SS type', () => {
      // SS: x1 = dateToX(from.plannedStart) = 0
      const path = service.calculateArrowPath(from, to, 'SS', 0, 1, timelineStart, ppd, rowHeight);
      expect(path).toContain('M 0'); // left edge of predecessor
    });
  });
});
