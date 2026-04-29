import { describe, it, expect } from 'vitest';
import {
  isEmpty,
  criteriaEquals,
  applyFilter,
  computeVisibleIds,
  getMilestoneSubtreeIds,
  serializeFilter,
  parseQueryParams,
  countActiveFilters,
  removeCriterion,
  getChipLabel,
} from './filter.utils';
import { FilterCriteria } from './filter.model';
import { ProjectTask } from './task.model';

// ─── Helpers ───────────────────────────────────────────────────────────────────

function makeTask(overrides: Partial<ProjectTask> & { id: string }): ProjectTask {
  return {
    id: overrides.id,
    projectId: 'proj-1',
    parentId: overrides.parentId ?? null,
    type: overrides.type ?? 'Task',
    vbs: overrides.vbs ?? null,
    name: overrides.name ?? 'Sample Task',
    priority: overrides.priority ?? 'Medium',
    status: overrides.status ?? 'NotStarted',
    notes: null,
    plannedStartDate: overrides.plannedStartDate ?? null,
    plannedEndDate: overrides.plannedEndDate ?? null,
    actualStartDate: null,
    actualEndDate: null,
    plannedEffortHours: null,
    actualEffortHours: null,
    percentComplete: null,
    assigneeUserId: overrides.assigneeUserId ?? null,
    sortOrder: 0,
    version: 1,
    predecessors: [],
  };
}

const TODAY = '2026-04-29';
const USER_ID = 'user-abc';

// ─── isEmpty ──────────────────────────────────────────────────────────────────

describe('isEmpty', () => {
  it('returns true for empty object', () => {
    expect(isEmpty({})).toBe(true);
  });

  it('returns true for null/undefined', () => {
    expect(isEmpty(null)).toBe(true);
    expect(isEmpty(undefined)).toBe(true);
  });

  it('returns false when keyword is set', () => {
    expect(isEmpty({ keyword: 'abc' })).toBe(false);
  });

  it('returns false when statuses is non-empty', () => {
    expect(isEmpty({ statuses: ['InProgress'] })).toBe(false);
  });

  it('returns true when statuses is empty array', () => {
    expect(isEmpty({ statuses: [] })).toBe(true);
  });

  it('returns false when overdueOnly is true', () => {
    expect(isEmpty({ overdueOnly: true })).toBe(false);
  });

  it('returns false when milestoneId is set', () => {
    expect(isEmpty({ milestoneId: 'ms-1' })).toBe(false);
  });
});

// ─── criteriaEquals ───────────────────────────────────────────────────────────

describe('criteriaEquals', () => {
  it('returns true for identical criteria', () => {
    const a: FilterCriteria = { keyword: 'foo', statuses: ['InProgress'] };
    const b: FilterCriteria = { keyword: 'foo', statuses: ['InProgress'] };
    expect(criteriaEquals(a, b)).toBe(true);
  });

  it('returns false for different criteria', () => {
    expect(criteriaEquals({ keyword: 'foo' }, { keyword: 'bar' })).toBe(false);
  });

  it('returns true for two empty objects', () => {
    expect(criteriaEquals({}, {})).toBe(true);
  });
});

// ─── serializeFilter / parseQueryParams round-trip ────────────────────────────

describe('serializeFilter + parseQueryParams', () => {
  it('round-trips keyword', () => {
    const c: FilterCriteria = { keyword: 'hello' };
    expect(parseQueryParams(serializeFilter(c))).toEqual(c);
  });

  it('round-trips statuses', () => {
    const c: FilterCriteria = { statuses: ['InProgress', 'OnHold'] };
    expect(parseQueryParams(serializeFilter(c))).toEqual(c);
  });

  it('round-trips assigneeIds', () => {
    const c: FilterCriteria = { assigneeIds: ['CURRENT_USER', 'UNASSIGNED'] };
    expect(parseQueryParams(serializeFilter(c))).toEqual(c);
  });

  it('round-trips overdueOnly', () => {
    const c: FilterCriteria = { overdueOnly: true };
    expect(parseQueryParams(serializeFilter(c))).toEqual(c);
  });

  it('round-trips date range', () => {
    const c: FilterCriteria = { dueDateFrom: '2026-01-01', dueDateTo: '2026-03-31' };
    expect(parseQueryParams(serializeFilter(c))).toEqual(c);
  });

  it('returns empty object for empty params', () => {
    expect(parseQueryParams({})).toEqual({});
  });
});

// ─── applyFilter ──────────────────────────────────────────────────────────────

describe('applyFilter', () => {
  const tasks = [
    makeTask({ id: 't1', name: 'Alpha task', status: 'InProgress', priority: 'High', assigneeUserId: USER_ID, plannedEndDate: '2026-04-01' }),
    makeTask({ id: 't2', name: 'Beta task',  status: 'NotStarted', priority: 'Low',  assigneeUserId: null,    plannedEndDate: '2026-05-10' }),
    makeTask({ id: 't3', name: 'Gamma task', status: 'Completed',  priority: 'Medium',assigneeUserId: null,   plannedEndDate: null }),
  ];

  it('returns all when filter is empty', () => {
    const ids = applyFilter(tasks, {}, USER_ID, TODAY);
    expect(ids).toHaveLength(3);
  });

  it('filters by keyword (name)', () => {
    const ids = applyFilter(tasks, { keyword: 'alpha' }, USER_ID, TODAY);
    expect(ids).toEqual(['t1']);
  });

  it('filters by status', () => {
    const ids = applyFilter(tasks, { statuses: ['InProgress'] }, USER_ID, TODAY);
    expect(ids).toEqual(['t1']);
  });

  it('filters by multiple statuses', () => {
    const ids = applyFilter(tasks, { statuses: ['InProgress', 'NotStarted'] }, USER_ID, TODAY);
    expect(ids).toHaveLength(2);
  });

  it('filters by priority', () => {
    const ids = applyFilter(tasks, { priorities: ['High'] }, USER_ID, TODAY);
    expect(ids).toEqual(['t1']);
  });

  it('filters by CURRENT_USER assignee', () => {
    const ids = applyFilter(tasks, { assigneeIds: ['CURRENT_USER'] }, USER_ID, TODAY);
    expect(ids).toEqual(['t1']);
  });

  it('filters by UNASSIGNED', () => {
    const ids = applyFilter(tasks, { assigneeIds: ['UNASSIGNED'] }, USER_ID, TODAY);
    expect(ids).toHaveLength(2);
    expect(ids).toContain('t2');
    expect(ids).toContain('t3');
  });

  it('filters by overdueOnly', () => {
    // t1 has plannedEndDate 2026-04-01 < TODAY 2026-04-29 and status InProgress
    const ids = applyFilter(tasks, { overdueOnly: true }, USER_ID, TODAY);
    expect(ids).toEqual(['t1']);
  });

  it('does not include completed tasks in overdueOnly', () => {
    const completedOverdue = makeTask({ id: 'tx', plannedEndDate: '2026-01-01', status: 'Completed' });
    const ids = applyFilter([completedOverdue], { overdueOnly: true }, USER_ID, TODAY);
    expect(ids).toHaveLength(0);
  });

  it('filters by dueDateFrom: excludes tasks with earlier date, keeps null-date tasks', () => {
    // t1 ends 2026-04-01 < from 2026-04-29 → filtered out
    // t2 ends 2026-05-10 ≥ from → included
    // t3 has no date → included (no date = not filtered by date range)
    const ids = applyFilter(tasks, { dueDateFrom: '2026-04-29' }, USER_ID, TODAY);
    expect(ids).toContain('t2');
    expect(ids).toContain('t3');
    expect(ids).not.toContain('t1');
  });

  it('filters by dueDateTo: excludes tasks with later date, keeps null-date tasks', () => {
    // t1 ends 2026-04-01 ≤ to 2026-04-28 → included
    // t2 ends 2026-05-10 > to → filtered out
    // t3 has no date → included
    const ids = applyFilter(tasks, { dueDateTo: '2026-04-28' }, USER_ID, TODAY);
    expect(ids).toContain('t1');
    expect(ids).toContain('t3');
    expect(ids).not.toContain('t2');
  });
});

// ─── getMilestoneSubtreeIds ───────────────────────────────────────────────────

describe('getMilestoneSubtreeIds', () => {
  const tasks = [
    makeTask({ id: 'ms1', type: 'Milestone', parentId: null }),
    makeTask({ id: 'c1',  type: 'Task',      parentId: 'ms1' }),
    makeTask({ id: 'c2',  type: 'Task',      parentId: 'ms1' }),
    makeTask({ id: 'gc1', type: 'Task',      parentId: 'c1' }),
    makeTask({ id: 'other', type: 'Task',    parentId: null }),
  ];

  it('includes the milestone itself', () => {
    const ids = getMilestoneSubtreeIds(tasks, 'ms1');
    expect(ids.has('ms1')).toBe(true);
  });

  it('includes direct children', () => {
    const ids = getMilestoneSubtreeIds(tasks, 'ms1');
    expect(ids.has('c1')).toBe(true);
    expect(ids.has('c2')).toBe(true);
  });

  it('includes grandchildren', () => {
    const ids = getMilestoneSubtreeIds(tasks, 'ms1');
    expect(ids.has('gc1')).toBe(true);
  });

  it('excludes tasks outside subtree', () => {
    const ids = getMilestoneSubtreeIds(tasks, 'ms1');
    expect(ids.has('other')).toBe(false);
  });

  it('returns single-item set for leaf node', () => {
    const ids = getMilestoneSubtreeIds(tasks, 'other');
    expect(ids.size).toBe(1);
    expect(ids.has('other')).toBe(true);
  });
});

// ─── milestoneId in applyFilter / computeVisibleIds ──────────────────────────

describe('milestoneId filter', () => {
  const tasks = [
    makeTask({ id: 'ms1', type: 'Milestone', parentId: null,  name: 'Milestone 1' }),
    makeTask({ id: 't1',  type: 'Task',      parentId: 'ms1', name: 'Task 1', status: 'InProgress' }),
    makeTask({ id: 't2',  type: 'Task',      parentId: 'ms1', name: 'Task 2', status: 'NotStarted' }),
    makeTask({ id: 'ms2', type: 'Milestone', parentId: null,  name: 'Milestone 2' }),
    makeTask({ id: 't3',  type: 'Task',      parentId: 'ms2', name: 'Task 3', status: 'InProgress' }),
  ];

  it('applyFilter with milestoneId returns only tasks in subtree', () => {
    const ids = applyFilter(tasks, { milestoneId: 'ms1' }, USER_ID, TODAY);
    expect(ids).toContain('ms1');
    expect(ids).toContain('t1');
    expect(ids).toContain('t2');
    expect(ids).not.toContain('ms2');
    expect(ids).not.toContain('t3');
  });

  it('milestoneId + status combo filters correctly', () => {
    const ids = applyFilter(tasks, { milestoneId: 'ms1', statuses: ['InProgress'] }, USER_ID, TODAY);
    expect(ids).toEqual(['t1']);
  });
});

// ─── computeVisibleIds ────────────────────────────────────────────────────────

describe('computeVisibleIds', () => {
  // Phase → Milestone → Task hierarchy
  const tasks = [
    makeTask({ id: 'phase1', type: 'Phase',     parentId: null,     name: 'Phase 1' }),
    makeTask({ id: 'ms1',    type: 'Milestone', parentId: 'phase1', name: 'Milestone 1' }),
    makeTask({ id: 't1',     type: 'Task',      parentId: 'ms1',    name: 'Task 1', status: 'InProgress', priority: 'High' }),
    makeTask({ id: 't2',     type: 'Task',      parentId: 'ms1',    name: 'Task 2', status: 'NotStarted', priority: 'Low' }),
    makeTask({ id: 'phase2', type: 'Phase',     parentId: null,     name: 'Phase 2' }),
    makeTask({ id: 't3',     type: 'Task',      parentId: 'phase2', name: 'Task 3', status: 'InProgress', priority: 'Low' }),
  ];

  it('returns all tasks as true when no filter', () => {
    const vm = computeVisibleIds(tasks, {}, USER_ID, TODAY);
    expect(vm.size).toBe(tasks.length);
    vm.forEach(v => expect(v).toBe(true));
  });

  it('marks matching tasks as true and ancestors as false', () => {
    // Filter: High priority → only t1 matches
    // Ancestors of t1: ms1, phase1 should be ancestor context (false)
    const vm = computeVisibleIds(tasks, { priorities: ['High'] }, USER_ID, TODAY);

    expect(vm.get('t1')).toBe(true);
    expect(vm.get('ms1')).toBe(false);   // ancestor context
    expect(vm.get('phase1')).toBe(false); // ancestor context
    expect(vm.has('t2')).toBe(false);    // not in map
    expect(vm.has('phase2')).toBe(false);
    expect(vm.has('t3')).toBe(false);
  });

  it('does not include unrelated ancestors when only some tasks match', () => {
    // Filter status=InProgress → t1, t3 match
    const vm = computeVisibleIds(tasks, { statuses: ['InProgress'] }, USER_ID, TODAY);

    expect(vm.get('t1')).toBe(true);
    expect(vm.get('t3')).toBe(true);
    expect(vm.get('ms1')).toBe(false);   // ancestor of t1
    expect(vm.get('phase1')).toBe(false);
    expect(vm.get('phase2')).toBe(false); // ancestor of t3
    expect(vm.has('t2')).toBe(false);
  });
});

// ─── countActiveFilters ───────────────────────────────────────────────────────

describe('countActiveFilters', () => {
  it('returns 0 for empty criteria', () => {
    expect(countActiveFilters({})).toBe(0);
  });

  it('counts each criterion once', () => {
    expect(countActiveFilters({
      keyword: 'foo',
      statuses: ['InProgress'],
      assigneeIds: ['CURRENT_USER'],
      priorities: ['High'],
      nodeTypes: ['Task'],
      milestoneId: 'ms-1',
      dueDateFrom: '2026-01-01',
      overdueOnly: true,
    })).toBe(8);
  });

  it('counts dueDateFrom and dueDateTo as 1', () => {
    expect(countActiveFilters({ dueDateFrom: '2026-01-01', dueDateTo: '2026-03-31' })).toBe(1);
  });
});

// ─── removeCriterion ──────────────────────────────────────────────────────────

describe('removeCriterion', () => {
  it('removes the specified key', () => {
    const c: FilterCriteria = { keyword: 'foo', statuses: ['InProgress'] };
    const result = removeCriterion(c, 'keyword');
    expect(result.keyword).toBeUndefined();
    expect(result.statuses).toEqual(['InProgress']);
  });

  it('does not mutate the original', () => {
    const c: FilterCriteria = { keyword: 'foo' };
    removeCriterion(c, 'keyword');
    expect(c.keyword).toBe('foo');
  });
});

// ─── getChipLabel ─────────────────────────────────────────────────────────────

describe('getChipLabel', () => {
  it('formats keyword chip', () => {
    expect(getChipLabel('keyword', { keyword: 'test' })).toContain('test');
  });

  it('formats overdueOnly chip', () => {
    expect(getChipLabel('overdueOnly', { overdueOnly: true })).toBeTruthy();
  });
});
