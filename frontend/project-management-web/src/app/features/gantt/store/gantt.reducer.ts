import { createReducer, on } from '@ngrx/store';
import { GanttState } from '../models/gantt.model';
import { GanttActions } from './gantt.actions';

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

export const ganttReducer = createReducer(
  initialState,

  // Load
  on(GanttActions.loadGanttData, (s, { projectId }) =>
    ({ ...s, loading: true, error: null, projectId, dirtyTasks: {}, conflict: null })),
  on(GanttActions.loadGanttDataSuccess, (s, { tasks }) =>
    ({ ...s, loading: false, tasks })),
  on(GanttActions.loadGanttDataFailure, (s, { error }) =>
    ({ ...s, loading: false, error })),

  // Granularity
  on(GanttActions.setGranularity, (s, { granularity }) =>
    ({ ...s, granularity })),

  // Dirty edits
  on(GanttActions.markTaskDirty, (s, { edit }) => ({
    ...s,
    dirtyTasks: { ...s.dirtyTasks, [edit.taskId]: edit },
    tasks: s.tasks.map(t => t.id === edit.taskId
      ? {
          ...t,
          dirty: true,
          plannedStart: edit.newPlannedStart ?? t.plannedStart,
          plannedEnd: edit.newPlannedEnd ?? t.plannedEnd,
          name: edit.newName ?? t.name,
          status: edit.newStatus ?? t.status,
          percentComplete: edit.newPercentComplete !== undefined
            ? edit.newPercentComplete
            : t.percentComplete,
        }
      : t
    ),
  })),

  on(GanttActions.discardGanttEdits, (s) => ({
    ...s,
    dirtyTasks: {},
    conflict: null,
  })),

  // Save
  on(GanttActions.saveGanttEdits, (s) => ({ ...s, saving: true, error: null })),
  on(GanttActions.saveGanttEditsSuccess, (s, { updatedTasks }) => ({
    ...s,
    saving: false,
    dirtyTasks: {},
    conflict: null,
    tasks: s.tasks.map(t => {
      const updated = updatedTasks.find(u => u.id === t.id);
      return updated
        ? { ...t, version: updated.version, dirty: false,
            plannedStart: updated.plannedStart, plannedEnd: updated.plannedEnd,
            name: updated.name ?? t.name,
            status: updated.status ?? t.status,
            percentComplete: updated.percentComplete !== undefined
              ? updated.percentComplete : t.percentComplete,
          }
        : t;
    }),
  })),
  on(GanttActions.saveGanttEditsFailure, (s, { error }) =>
    ({ ...s, saving: false, error })),

  // Conflict
  on(GanttActions.ganttConflict, (s, { conflict }) =>
    ({ ...s, saving: false, conflict })),
  on(GanttActions.resolveConflict, (s) =>
    ({ ...s, conflict: null })),

  // Clear
  on(GanttActions.clearGantt, () => initialState),
);
