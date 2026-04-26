import { createSelector } from '@ngrx/store';
import { AppState } from '../../../core/store/app.state';
import { resourcesAdapter, ResourcesState } from './resources.reducer';

const selectResourcesState = (state: AppState) => state.resources;

const { selectAll, selectEntities } = resourcesAdapter.getSelectors(selectResourcesState);

export const selectAllResources = selectAll;
export const selectResourceEntities = selectEntities;
export const selectResourcesLoading = createSelector(selectResourcesState, (s: ResourcesState) => s.loading);
export const selectResourcesCreating = createSelector(selectResourcesState, (s: ResourcesState) => s.creating);
export const selectResourcesUpdating = createSelector(selectResourcesState, (s: ResourcesState) => s.updating);
export const selectResourcesInactivating = createSelector(selectResourcesState, (s: ResourcesState) => s.inactivating);
export const selectResourceConflict = createSelector(selectResourcesState, (s: ResourcesState) => s.conflict);
export const selectResourcesError = createSelector(selectResourcesState, (s: ResourcesState) => s.error);
export const selectSelectedResourceId = createSelector(selectResourcesState, (s: ResourcesState) => s.selectedId);
export const selectSelectedResource = createSelector(
  selectResourceEntities,
  selectSelectedResourceId,
  (entities, id) => (id ? entities[id] : null)
);
