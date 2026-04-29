import { describe, it, expect } from 'vitest';
import { ganttReducer } from './gantt.reducer';
import { GanttActions } from './gantt.actions';
import { GanttState, GanttTask, GanttTaskEdit } from '../models/gantt.model';

const initialState: GanttState = {
  projectId: null,
  tasks: [],
  loading: false,
  error: null,
  granularity: 'week',
  dirtyTasks: {},
  saving: false,
  conflict: null,
};

const sampleTask: GanttTask = {
  id: 'task-1',
  parentId: null,
  type: 'Task',
  vbs: null,
  name: 'Test Task',
  status: 'NotStarted',
  priority: 'Medium',
  plannedStart: new Date(2026, 0, 1),
  plannedEnd: new Date(2026, 0, 10),
  percentComplete: 0,
  depth: 0,
  sortOrder: 0,
  collapsed: false,
  version: 1,
  dirty: false,
  assigneeUserId: null,
};

describe('gantt reducer', () => {
  describe('mark task dirty', () => {
    it('sets dirty flag and stores edit', () => {
      const state: GanttState = { ...initialState, tasks: [sampleTask] };
      const edit: GanttTaskEdit = {
        taskId: 'task-1',
        originalVersion: 1,
        newPlannedEnd: new Date(2026, 0, 20),
      };
      const result = ganttReducer(state, GanttActions.markTaskDirty({ edit }));

      expect(result.dirtyTasks['task-1']).toEqual(edit);
      expect(result.tasks[0].dirty).toBe(true);
      expect(result.tasks[0].plannedEnd).toEqual(new Date(2026, 0, 20));
    });

    it('preserves original start when only end changes', () => {
      const state: GanttState = { ...initialState, tasks: [sampleTask] };
      const edit: GanttTaskEdit = {
        taskId: 'task-1',
        originalVersion: 1,
        newPlannedEnd: new Date(2026, 0, 15),
      };
      const result = ganttReducer(state, GanttActions.markTaskDirty({ edit }));
      expect(result.tasks[0].plannedStart).toEqual(sampleTask.plannedStart);
    });

    it('updates both dates when moving task', () => {
      const state: GanttState = { ...initialState, tasks: [sampleTask] };
      const edit: GanttTaskEdit = {
        taskId: 'task-1',
        originalVersion: 1,
        newPlannedStart: new Date(2026, 1, 1),
        newPlannedEnd: new Date(2026, 1, 10),
      };
      const result = ganttReducer(state, GanttActions.markTaskDirty({ edit }));
      expect(result.tasks[0].plannedStart).toEqual(new Date(2026, 1, 1));
      expect(result.tasks[0].plannedEnd).toEqual(new Date(2026, 1, 10));
    });

    it('overwrites previous dirty edit for same task', () => {
      const firstEdit: GanttTaskEdit = {
        taskId: 'task-1',
        originalVersion: 1,
        newPlannedEnd: new Date(2026, 0, 15),
      };
      const state1 = ganttReducer({ ...initialState, tasks: [sampleTask] }, GanttActions.markTaskDirty({ edit: firstEdit }));

      const secondEdit: GanttTaskEdit = {
        taskId: 'task-1',
        originalVersion: 1,
        newPlannedEnd: new Date(2026, 0, 20),
      };
      const state2 = ganttReducer(state1, GanttActions.markTaskDirty({ edit: secondEdit }));

      expect(state2.dirtyTasks['task-1'].newPlannedEnd).toEqual(new Date(2026, 0, 20));
    });
  });

  describe('discard gantt edits', () => {
    it('clears all dirty tasks', () => {
      const edit: GanttTaskEdit = { taskId: 'task-1', originalVersion: 1 };
      const state = ganttReducer(
        { ...initialState, tasks: [{ ...sampleTask, dirty: true }], dirtyTasks: { 'task-1': edit } },
        GanttActions.discardGanttEdits()
      );
      expect(state.dirtyTasks).toEqual({});
      expect(state.conflict).toBeNull();
    });
  });

  describe('save gantt edits', () => {
    it('sets saving to true on save start', () => {
      const result = ganttReducer(initialState, GanttActions.saveGanttEdits());
      expect(result.saving).toBe(true);
      expect(result.error).toBeNull();
    });

    it('clears dirty state on success', () => {
      const dirtyState: GanttState = {
        ...initialState,
        tasks: [{ ...sampleTask, dirty: true }],
        dirtyTasks: { 'task-1': { taskId: 'task-1', originalVersion: 1 } },
        saving: true,
      };
      const updatedTask = {
        id: 'task-1',
        version: 2,
        plannedStart: new Date(2026, 0, 1),
        plannedEnd: new Date(2026, 0, 10),
      };
      const result = ganttReducer(dirtyState, GanttActions.saveGanttEditsSuccess({ updatedTasks: [updatedTask] }));

      expect(result.saving).toBe(false);
      expect(result.dirtyTasks).toEqual({});
      expect(result.tasks[0].dirty).toBe(false);
      expect(result.tasks[0].version).toBe(2);
    });

    it('sets error on failure', () => {
      const result = ganttReducer(
        { ...initialState, saving: true },
        GanttActions.saveGanttEditsFailure({ error: 'Lỗi lưu' })
      );
      expect(result.saving).toBe(false);
      expect(result.error).toBe('Lỗi lưu');
    });
  });

  describe('conflict state', () => {
    it('sets conflict state and stops saving on 409', () => {
      const conflict = {
        taskId: 'task-1',
        serverTaskJson: '{}',
        localEdit: { taskId: 'task-1', originalVersion: 1 },
        serverETag: '"2"',
      };
      const result = ganttReducer(
        { ...initialState, saving: true },
        GanttActions.ganttConflict({ conflict })
      );
      expect(result.saving).toBe(false);
      expect(result.conflict).toEqual(conflict);
    });

    it('clears conflict on resolve', () => {
      const state: GanttState = {
        ...initialState,
        conflict: {
          taskId: 'task-1',
          serverTaskJson: '{}',
          localEdit: { taskId: 'task-1', originalVersion: 1 },
          serverETag: '"2"',
        },
      };
      const result = ganttReducer(state, GanttActions.resolveConflict());
      expect(result.conflict).toBeNull();
    });
  });

  describe('load gantt data', () => {
    it('sets loading and clears dirty state on load', () => {
      const state: GanttState = {
        ...initialState,
        dirtyTasks: { 'task-1': { taskId: 'task-1', originalVersion: 1 } },
      };
      const result = ganttReducer(state, GanttActions.loadGanttData({ projectId: 'proj-1' }));
      expect(result.loading).toBe(true);
      expect(result.dirtyTasks).toEqual({});
      expect(result.projectId).toBe('proj-1');
    });

    it('sets tasks on success', () => {
      const result = ganttReducer(
        { ...initialState, loading: true },
        GanttActions.loadGanttDataSuccess({ tasks: [sampleTask] })
      );
      expect(result.loading).toBe(false);
      expect(result.tasks).toHaveLength(1);
    });
  });
});
