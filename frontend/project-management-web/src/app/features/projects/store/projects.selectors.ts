import { createSelector } from '@ngrx/store';
import { AppState } from '../../../core/store/app.state';
import { projectsAdapter, ProjectsState } from './projects.reducer';

export const selectProjectsState = (state: AppState) => state.projects;

export const {
  selectAll: selectAllProjects,
  selectEntities: selectProjectEntities,
  selectIds: selectProjectIds,
  selectTotal: selectProjectsTotal,
} = projectsAdapter.getSelectors(selectProjectsState);

export const selectProjectsLoading = createSelector(
  selectProjectsState,
  (state: ProjectsState) => state.loading
);

export const selectProjectsError = createSelector(
  selectProjectsState,
  (state: ProjectsState) => state.error
);

export const selectSelectedProjectId = createSelector(
  selectProjectsState,
  (state: ProjectsState) => state.selectedId
);

export const selectProjectsCreating = createSelector(
  selectProjectsState,
  (state: ProjectsState) => state.creating
);

export const selectProjectsUpdating = createSelector(
  selectProjectsState,
  (state: ProjectsState) => state.updating
);

export const selectProjectsDeleting = createSelector(
  selectProjectsState,
  (state: ProjectsState) => state.deleting
);

export const selectProjectsConflict = createSelector(
  selectProjectsState,
  (state: ProjectsState) => state.conflict
);
