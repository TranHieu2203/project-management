import { createEntityAdapter, EntityState } from '@ngrx/entity';
import { createReducer, on } from '@ngrx/store';
import { Resource } from '../models/resource.model';
import { ResourcesActions } from './resources.actions';

export interface ResourceConflictState {
  serverState: Resource;
  eTag: string;
  pendingName: string;
  pendingEmail?: string;
}

export interface ResourcesState extends EntityState<Resource> {
  selectedId: string | null;
  loading: boolean;
  creating: boolean;
  updating: boolean;
  inactivating: boolean;
  error: string | null;
  conflict: ResourceConflictState | null;
}

export const resourcesAdapter = createEntityAdapter<Resource>();

export const initialResourcesState: ResourcesState = resourcesAdapter.getInitialState({
  selectedId: null,
  loading: false,
  creating: false,
  updating: false,
  inactivating: false,
  error: null,
  conflict: null,
});

export const resourcesReducer = createReducer(
  initialResourcesState,

  on(ResourcesActions.loadResources, state => ({ ...state, loading: true, error: null })),
  on(ResourcesActions.loadResourcesSuccess, (state, { resources }) =>
    resourcesAdapter.setAll(resources, { ...state, loading: false })
  ),
  on(ResourcesActions.loadResourcesFailure, (state, { error }) => ({ ...state, loading: false, error })),
  on(ResourcesActions.selectResource, (state, { resourceId }) => ({ ...state, selectedId: resourceId })),

  on(ResourcesActions.createResource, state => ({ ...state, creating: true, error: null })),
  on(ResourcesActions.createResourceSuccess, (state, { resource }) =>
    resourcesAdapter.addOne(resource, { ...state, creating: false })
  ),
  on(ResourcesActions.createResourceFailure, (state, { error }) => ({ ...state, creating: false, error })),

  on(ResourcesActions.updateResource, state => ({ ...state, updating: true, error: null })),
  on(ResourcesActions.updateResourceSuccess, (state, { resource }) =>
    resourcesAdapter.updateOne({ id: resource.id, changes: resource }, { ...state, updating: false })
  ),
  on(ResourcesActions.updateResourceFailure, (state, { error }) => ({ ...state, updating: false, error })),
  on(ResourcesActions.updateResourceConflict, (state, { serverState, eTag, pendingName, pendingEmail }) => ({
    ...state,
    updating: false,
    conflict: { serverState, eTag, pendingName, pendingEmail },
  })),

  on(ResourcesActions.inactivateResource, state => ({ ...state, inactivating: true, error: null })),
  on(ResourcesActions.inactivateResourceSuccess, (state, { resource }) =>
    resourcesAdapter.updateOne({ id: resource.id, changes: resource }, { ...state, inactivating: false })
  ),
  on(ResourcesActions.inactivateResourceFailure, (state, { error }) => ({ ...state, inactivating: false, error })),

  on(ResourcesActions.clearConflict, state => ({ ...state, conflict: null })),
);
