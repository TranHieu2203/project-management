import { createSelector } from '@ngrx/store';
import { AppState } from '../../../core/store/app.state';
import { tasksAdapter, TasksState } from './tasks.reducer';
import { computeVisibleIds } from '../models/filter.utils';
import { FilterCriteria } from '../models/filter.model';

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

export const selectActiveFilter = createSelector(
  selectTasksState,
  (s: TasksState) => s.activeFilter
);

// Selector: tasks theo projectId (filter từ store)
export const selectTasksByProject = (projectId: string) =>
  createSelector(selectAllTasks, tasks =>
    tasks.filter(t => t.projectId === projectId));

// Phases = tasks với type 'Phase' trong project hiện tại (store chỉ chứa tasks của 1 project)
export const selectCurrentProjectPhases = createSelector(
  selectAllTasks,
  tasks => tasks.filter(t => t.type === 'Phase')
);

/**
 * Memoized selector: trả Map<taskId, isMatch> cho filter hiện tại.
 * false = ancestor context node (hiển thị mờ), true = match thực sự.
 * currentUserId và today được inject qua factory vì chúng không có trong store.
 */
export const selectVisibleTaskIds = (currentUserId: string, today: string) =>
  createSelector(
    selectAllTasks,
    selectActiveFilter,
    (tasks, criteria: FilterCriteria) =>
      computeVisibleIds(tasks, criteria, currentUserId, today)
  );
