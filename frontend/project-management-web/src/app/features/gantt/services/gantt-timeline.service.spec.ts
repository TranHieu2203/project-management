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
  version: 1,
  dirty: false,
  assigneeUserId: null,
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

  describe('getBarColor — priority matrix', () => {
    // Tầng 1: Phase và Milestone có màu cố định bất kể status
    it('Phase → #2196F3 (xanh dương)', () => {
      expect(service.getBarColor(makeGanttTask({ type: 'Phase' }))).toBe('#2196F3');
    });

    it('Phase + status Delayed → #2196F3 (Phase trumps Delayed)', () => {
      expect(service.getBarColor(makeGanttTask({ type: 'Phase', status: 'Delayed' }))).toBe('#2196F3');
    });

    it('Phase + status Completed → #2196F3 (Phase trumps Completed)', () => {
      expect(service.getBarColor(makeGanttTask({ type: 'Phase', status: 'Completed' }))).toBe('#2196F3');
    });

    it('Milestone → #FF9800 (cam)', () => {
      expect(service.getBarColor(makeGanttTask({ type: 'Milestone' }))).toBe('#FF9800');
    });

    it('Milestone + status Completed → #FF9800 (Milestone trumps Completed)', () => {
      expect(service.getBarColor(makeGanttTask({ type: 'Milestone', status: 'Completed' }))).toBe('#FF9800');
    });

    it('Milestone + status Delayed → #FF9800 (Milestone trumps Delayed)', () => {
      expect(service.getBarColor(makeGanttTask({ type: 'Milestone', status: 'Delayed' }))).toBe('#FF9800');
    });

    // Tầng 2: Task thường phân theo status — Completed trước Delayed
    it('Task + status Completed → #9E9E9E (xám)', () => {
      expect(service.getBarColor(makeGanttTask({ type: 'Task', status: 'Completed' }))).toBe('#9E9E9E');
    });

    it('Task + status Delayed → #F44336 (đỏ)', () => {
      expect(service.getBarColor(makeGanttTask({ type: 'Task', status: 'Delayed' }))).toBe('#F44336');
    });

    it('Task + status NotStarted → #4CAF50 (xanh lá)', () => {
      expect(service.getBarColor(makeGanttTask({ type: 'Task', status: 'NotStarted' }))).toBe('#4CAF50');
    });

    it('Task + status InProgress → #4CAF50 (xanh lá)', () => {
      expect(service.getBarColor(makeGanttTask({ type: 'Task', status: 'InProgress' }))).toBe('#4CAF50');
    });

    it('Task + status OnHold → #4CAF50 (xanh lá)', () => {
      expect(service.getBarColor(makeGanttTask({ type: 'Task', status: 'OnHold' }))).toBe('#4CAF50');
    });

    it('Task + status Cancelled → #4CAF50 (xanh lá)', () => {
      expect(service.getBarColor(makeGanttTask({ type: 'Task', status: 'Cancelled' }))).toBe('#4CAF50');
    });

    it('Task + status null/undefined → #4CAF50 (fallback, no error)', () => {
      expect(service.getBarColor(makeGanttTask({ type: 'Task', status: null as any }))).toBe('#4CAF50');
    });

    it('Task + unknown status → #4CAF50 (fallback)', () => {
      expect(service.getBarColor(makeGanttTask({ type: 'Task', status: 'Unknown' }))).toBe('#4CAF50');
    });
  });
});
