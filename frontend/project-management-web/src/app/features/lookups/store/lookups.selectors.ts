import { createSelector } from '@ngrx/store';
import { AppState } from '../../../core/store/app.state';
import { LookupsState } from './lookups.reducer';

const selectLookupsState = (state: AppState) => state.lookups;

export const selectRoles = createSelector(selectLookupsState, (s: LookupsState) => s.roles);
export const selectLevels = createSelector(selectLookupsState, (s: LookupsState) => s.levels);
export const selectLookupsLoaded = createSelector(selectLookupsState, (s: LookupsState) => s.loaded);
export const selectLookupsError = createSelector(selectLookupsState, (s: LookupsState) => s.error);
