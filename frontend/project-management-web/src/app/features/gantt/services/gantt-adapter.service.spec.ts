import { TestBed } from '@angular/core/testing';
import { GanttAdapterService } from './gantt-adapter.service';
import { ProjectTask } from '../../projects/models/task.model';

const makeTask = (partial: Partial<ProjectTask>): ProjectTask => ({
  id: 'task-1',
  projectId: 'proj-1',
  parentId: null,
  type: 'Task',
  vbs: '1.1',
  name: 'Test Task',
  priority: 'Medium',
  status: 'NotStarted',
  notes: null,
  plannedStartDate: '2026-05-01',
  plannedEndDate: '2026-05-10',
  actualStartDate: null,
  actualEndDate: null,
  plannedEffortHours: null,
  actualEffortHours: null,
  percentComplete: 0,
  assigneeUserId: null,
  sortOrder: 0,
  version: 1,
  predecessors: [],
  ...partial,
});

describe('GanttAdapterService', () => {
  let service: GanttAdapterService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(GanttAdapterService);
  });

  it('should adapt a single task with depth 0', () => {
    const task = makeTask({ id: 'phase-1', type: 'Phase', parentId: null });
    const result = service.adapt([task]);

    expect(result.length).toBe(1);
    expect(result[0].depth).toBe(0);
    expect(result[0].type).toBe('Phase');
  });

  it('should calculate correct depth for nested hierarchy', () => {
    const phase = makeTask({ id: 'phase-1', type: 'Phase', parentId: null, sortOrder: 0 });
    const milestone = makeTask({ id: 'ms-1', type: 'Milestone', parentId: 'phase-1', sortOrder: 1 });
    const task = makeTask({ id: 'task-1', type: 'Task', parentId: 'ms-1', sortOrder: 2 });

    const result = service.adapt([phase, milestone, task]);
    const depthMap = new Map(result.map(t => [t.id, t.depth]));

    expect(depthMap.get('phase-1')).toBe(0);
    expect(depthMap.get('ms-1')).toBe(1);
    expect(depthMap.get('task-1')).toBe(2);
  });

  it('should parse plannedStartDate and plannedEndDate to Date objects', () => {
    const task = makeTask({ plannedStartDate: '2026-05-01', plannedEndDate: '2026-05-15' });
    const result = service.adapt([task]);

    expect(result[0].plannedStart).toBeInstanceOf(Date);
    expect(result[0].plannedEnd).toBeInstanceOf(Date);
    expect(result[0].plannedStart?.getFullYear()).toBe(2026);
    expect(result[0].plannedStart?.getMonth()).toBe(4); // May = 4 (0-indexed)
    expect(result[0].plannedStart?.getDate()).toBe(1);
  });

  it('should return null for missing dates', () => {
    const task = makeTask({ plannedStartDate: null, plannedEndDate: null });
    const result = service.adapt([task]);

    expect(result[0].plannedStart).toBeNull();
    expect(result[0].plannedEnd).toBeNull();
  });

  it('should sort tasks by sortOrder', () => {
    const t3 = makeTask({ id: 't3', sortOrder: 3, name: 'Third' });
    const t1 = makeTask({ id: 't1', sortOrder: 1, name: 'First' });
    const t2 = makeTask({ id: 't2', sortOrder: 2, name: 'Second' });

    const result = service.adapt([t3, t1, t2]);
    expect(result.map(t => t.id)).toEqual(['t1', 't2', 't3']);
  });

  it('should initialize collapsed to false', () => {
    const task = makeTask({});
    const result = service.adapt([task]);
    expect(result[0].collapsed).toBe(false);
  });

  it('should use 0 as default percentComplete when null', () => {
    const task = makeTask({ percentComplete: null });
    const result = service.adapt([task]);
    expect(result[0].percentComplete).toBe(0);
  });
});
