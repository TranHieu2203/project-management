import { createSelector } from '@ngrx/store';
import { AppState } from '../../../core/store/app.state';
import { GanttState } from '../models/gantt.model';
import { selectProjectEntities } from '../../projects/store/projects.selectors';

export const selectGanttState = (state: AppState) => state.gantt;

export const selectGanttTasks = createSelector(
  selectGanttState,
  (s: GanttState) => s.tasks
);

export const selectGanttLoading = createSelector(
  selectGanttState,
  (s: GanttState) => s.loading
);

export const selectGanttError = createSelector(
  selectGanttState,
  (s: GanttState) => s.error
);

export const selectGranularity = createSelector(
  selectGanttState,
  (s: GanttState) => s.granularity
);

export const selectGanttProjectId = createSelector(
  selectGanttState,
  (s: GanttState) => s.projectId
);

export const selectDirtyTasks = createSelector(
  selectGanttState,
  (s: GanttState) => s.dirtyTasks
);

export const selectDirtyTasksCount = createSelector(
  selectDirtyTasks,
  (dirty) => Object.keys(dirty).length
);

export const selectSaving = createSelector(
  selectGanttState,
  (s: GanttState) => s.saving
);

export const selectConflict = createSelector(
  selectGanttState,
  (s: GanttState) => s.conflict
);

export const selectProjectByGanttId = createSelector(
  selectProjectEntities,
  selectGanttProjectId,
  (entities, projectId) => projectId ? entities[projectId] : undefined
);
