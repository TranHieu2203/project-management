import { createSelector } from '@ngrx/store';
import { AppState } from '../../../core/store/app.state';
import { tasksAdapter, TasksState } from './tasks.reducer';

export const selectTasksState = (state: AppState) => state.tasks;

export const {
  selectAll: selectAllTasks,
  selectEntities: selectTaskEntities,
  selectIds: selectTaskIds,
  selectTotal: selectTasksTotal,
} = tasksAdapter.getSelectors(selectTasksState);

export const selectTasksLoading = createSelector(
  selectTasksState,
  (s: TasksState) => s.loading
);

export const selectTasksError = createSelector(
  selectTasksState,
  (s: TasksState) => s.error
);

export const selectSelectedTaskId = createSelector(
  selectTasksState,
  (s: TasksState) => s.selectedTaskId
);

export const selectTasksConflict = createSelector(
  selectTasksState,
  (s: TasksState) => s.conflict
);

export const selectCurrentProjectId = createSelector(
  selectTasksState,
  (s: TasksState) => s.currentProjectId
);

export const selectTasksCreating = createSelector(
  selectTasksState,
  (s: TasksState) => s.creating
);

export const selectTasksUpdating = createSelector(
  selectTasksState,
  (s: TasksState) => s.updating
);

export const selectTasksDeleting = createSelector(
  selectTasksState,
  (s: TasksState) => s.deleting
);

// Selector: tasks theo projectId (filter từ store)
export const selectTasksByProject = (projectId: string) =>
  createSelector(selectAllTasks, tasks =>
    tasks.filter(t => t.projectId === projectId));
