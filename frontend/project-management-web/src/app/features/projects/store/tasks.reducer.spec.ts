import { describe, it, expect } from 'vitest';
import { tasksReducer, tasksAdapter, initialState, TasksState } from './tasks.reducer';
import { TasksActions } from './tasks.actions';
import { ProjectTask } from '../models/task.model';

const sampleTask: ProjectTask = {
  id: 'task-1',
  projectId: 'proj-1',
  parentId: null,
  type: 'Task',
  vbs: null,
  name: 'Sample Task',
  priority: 'Medium',
  status: 'NotStarted',
  notes: null,
  plannedStartDate: '2026-01-01',
  plannedEndDate: '2026-01-10',
  actualStartDate: null,
  actualEndDate: null,
  plannedEffortHours: null,
  actualEffortHours: null,
  percentComplete: null,
  assigneeUserId: null,
  sortOrder: 0,
  version: 1,
  predecessors: [],
};

describe('tasksReducer', () => {
  describe('clearTasks', () => {
    it('resets entities to empty when tasks exist', () => {
      const stateWithTasks = tasksAdapter.addOne(sampleTask, {
        ...initialState,
        currentProjectId: 'proj-1',
      });

      const result = tasksReducer(stateWithTasks as TasksState, TasksActions.clearTasks());

      expect(Object.keys(result.entities)).toHaveLength(0);
      expect(result.ids).toHaveLength(0);
    });

    it('resets currentProjectId to null', () => {
      const stateWithProject = tasksAdapter.addOne(sampleTask, {
        ...initialState,
        currentProjectId: 'proj-1',
      });

      const result = tasksReducer(stateWithProject as TasksState, TasksActions.clearTasks());

      expect(result.currentProjectId).toBeNull();
    });

    it('resets error to null', () => {
      const stateWithError = { ...initialState, error: 'some error' } as TasksState;
      const result = tasksReducer(stateWithError, TasksActions.clearTasks());
      expect(result.error).toBeNull();
    });

    it('resets loading to false', () => {
      const loadingState = { ...initialState, loading: true } as TasksState;
      const result = tasksReducer(loadingState, TasksActions.clearTasks());
      expect(result.loading).toBe(false);
    });

    it('resets conflict to null', () => {
      const conflictState = {
        ...initialState,
        conflict: { serverState: sampleTask, eTag: '"1"' },
      } as TasksState;
      const result = tasksReducer(conflictState, TasksActions.clearTasks());
      expect(result.conflict).toBeNull();
    });

    it('is idempotent when already empty', () => {
      const result1 = tasksReducer(initialState as TasksState, TasksActions.clearTasks());
      const result2 = tasksReducer(result1, TasksActions.clearTasks());
      expect(result2.ids).toHaveLength(0);
      expect(result2.currentProjectId).toBeNull();
    });
  });

  describe('loadTasks', () => {
    it('sets loading true and stores projectId', () => {
      const result = tasksReducer(
        initialState as TasksState,
        TasksActions.loadTasks({ projectId: 'proj-1' })
      );
      expect(result.loading).toBe(true);
      expect(result.currentProjectId).toBe('proj-1');
    });

    it('replaces all tasks on success', () => {
      const stateWithOldTask = tasksAdapter.addOne(
        { ...sampleTask, id: 'old-task', projectId: 'proj-0' },
        initialState
      );
      const result = tasksReducer(
        stateWithOldTask as TasksState,
        TasksActions.loadTasksSuccess({ tasks: [sampleTask] })
      );
      expect(result.ids).toEqual(['task-1']);
      expect(result.loading).toBe(false);
    });
  });
});
