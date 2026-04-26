import { createReducer, on } from '@ngrx/store';
import { LookupItem } from '../models/lookup.model';
import { LookupsActions } from './lookups.actions';

export interface LookupsState {
  roles: LookupItem[];
  levels: LookupItem[];
  loaded: boolean;
  error: string | null;
}

export const initialLookupsState: LookupsState = {
  roles: [],
  levels: [],
  loaded: false,
  error: null,
};

export const lookupsReducer = createReducer(
  initialLookupsState,
  on(LookupsActions.loadCatalog, state => ({ ...state, error: null })),
  on(LookupsActions.loadCatalogSuccess, (state, { roles, levels }) =>
    ({ ...state, roles, levels, loaded: true })
  ),
  on(LookupsActions.loadCatalogFailure, (state, { error }) =>
    ({ ...state, error })
  ),
);
