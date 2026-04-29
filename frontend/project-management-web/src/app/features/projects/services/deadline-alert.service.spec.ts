import { TestBed } from '@angular/core/testing';
import { DeadlineAlertService, DeadlineStatus } from './deadline-alert.service';
import { ProjectTask } from '../models/task.model';

function makeTask(overrides: Partial<ProjectTask> = {}): ProjectTask {
  return {
    id: 'task-1',
    projectId: 'proj-1',
    parentId: null,
    type: 'Task',
    vbs: null,
    name: 'Test Task',
    priority: 'Medium',
    status: 'InProgress',
    notes: null,
    plannedStartDate: null,
    plannedEndDate: null,
    actualStartDate: null,
    actualEndDate: null,
    plannedEffortHours: null,
    actualEffortHours: null,
    percentComplete: null,
    assigneeUserId: null,
    sortOrder: 1000,
    version: 1,
    predecessors: [],
    ...overrides,
  };
}

describe('DeadlineAlertService', () => {
  let service: DeadlineAlertService;
  const TODAY = '2026-04-26';

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(DeadlineAlertService);
  });

  // ── getDeadlineStatus ────────────────────────────────────────────────────────

  describe('getDeadlineStatus', () => {
    it('returns none for Phase type', () => {
      const task = makeTask({ type: 'Phase', plannedEndDate: '2026-04-01' });
      expect(service.getDeadlineStatus(task, TODAY)).toBe('none');
    });

    it('returns none for Milestone type', () => {
      const task = makeTask({ type: 'Milestone', plannedEndDate: '2026-04-01' });
      expect(service.getDeadlineStatus(task, TODAY)).toBe('none');
    });

    it('returns none when plannedEndDate is null', () => {
      const task = makeTask({ plannedEndDate: null });
      expect(service.getDeadlineStatus(task, TODAY)).toBe('none');
    });

    it('returns none when status is Completed', () => {
      const task = makeTask({ status: 'Completed', plannedEndDate: '2026-04-01' });
      expect(service.getDeadlineStatus(task, TODAY)).toBe('none');
    });

    it('returns none when status is Cancelled', () => {
      const task = makeTask({ status: 'Cancelled', plannedEndDate: '2026-04-01' });
      expect(service.getDeadlineStatus(task, TODAY)).toBe('none');
    });

    it('returns overdue when plannedEndDate < today', () => {
      const task = makeTask({ plannedEndDate: '2026-04-25' });
      expect(service.getDeadlineStatus(task, TODAY)).toBe('overdue');
    });

    it('returns due-today when plannedEndDate === today (boundary)', () => {
      const task = makeTask({ plannedEndDate: TODAY });
      expect(service.getDeadlineStatus(task, TODAY)).toBe('due-today');
    });

    it('returns due-soon when plannedEndDate = today+1', () => {
      const task = makeTask({ plannedEndDate: '2026-04-27' });
      expect(service.getDeadlineStatus(task, TODAY)).toBe('due-soon');
    });

    it('returns due-soon when plannedEndDate = today+7 (boundary)', () => {
      const task = makeTask({ plannedEndDate: '2026-05-03' });
      expect(service.getDeadlineStatus(task, TODAY)).toBe('due-soon');
    });

    it('returns none when plannedEndDate = today+8 (outside due-soon window)', () => {
      const task = makeTask({ plannedEndDate: '2026-05-04' });
      expect(service.getDeadlineStatus(task, TODAY)).toBe('none');
    });
  });

  // ── getLocalDateString ───────────────────────────────────────────────────────

  describe('getLocalDateString', () => {
    it('returns a string in YYYY-MM-DD format', () => {
      const result = service.getLocalDateString();
      expect(result).toMatch(/^\d{4}-\d{2}-\d{2}$/);
    });

    it('returns local date, not UTC shifted date', () => {
      // Mock Date at local midnight to prove no UTC offset
      const mockDate = new Date(2026, 3, 26, 0, 30, 0); // April 26 local 00:30
      vi.spyOn(globalThis, 'Date').mockImplementation(() => mockDate as unknown as string);
      const result = service.getLocalDateString();
      expect(result).toBe('2026-04-26');
      vi.restoreAllMocks();
    });
  });

  // ── computeDeadlineSummary ───────────────────────────────────────────────────

  describe('computeDeadlineSummary', () => {
    it('returns empty summary for empty task array', () => {
      const summary = service.computeDeadlineSummary([], TODAY);
      expect(summary.overdue).toHaveLength(0);
      expect(summary.dueToday).toHaveLength(0);
      expect(summary.dueSoon).toHaveLength(0);
    });

    it('ignores Phase and Milestone types', () => {
      const tasks = [
        makeTask({ id: '1', type: 'Phase', plannedEndDate: '2026-04-01' }),
        makeTask({ id: '2', type: 'Milestone', plannedEndDate: '2026-04-01' }),
      ];
      const summary = service.computeDeadlineSummary(tasks, TODAY);
      expect(summary.overdue).toHaveLength(0);
    });

    it('correctly groups tasks into overdue, dueToday, dueSoon', () => {
      const tasks = [
        makeTask({ id: '1', plannedEndDate: '2026-04-20' }),  // overdue
        makeTask({ id: '2', plannedEndDate: TODAY }),          // due-today
        makeTask({ id: '3', plannedEndDate: '2026-04-28' }),  // due-soon
        makeTask({ id: '4', plannedEndDate: '2026-05-10' }),  // none
        makeTask({ id: '5', status: 'Completed', plannedEndDate: '2026-04-01' }),  // ignored
      ];
      const summary = service.computeDeadlineSummary(tasks, TODAY);
      expect(summary.overdue.map(t => t.id)).toEqual(['1']);
      expect(summary.dueToday.map(t => t.id)).toEqual(['2']);
      expect(summary.dueSoon.map(t => t.id)).toEqual(['3']);
    });
  });

  // ── dateToLocalString ────────────────────────────────────────────────────────

  describe('dateToLocalString', () => {
    it('converts Date to YYYY-MM-DD using local date', () => {
      const date = new Date(2026, 3, 26, 12, 0, 0); // April 26 local noon
      expect(service.dateToLocalString(date)).toBe('2026-04-26');
    });
  });
});
