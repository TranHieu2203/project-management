import { createSelector } from '@ngrx/store';
import { AppState } from '../../../core/store/app.state';

export const selectAuthState = (state: AppState) => state.auth;

export const selectCurrentUser = createSelector(selectAuthState, s => s.user);
export const selectAuthLoading = createSelector(selectAuthState, s => s.isLoading);
export const selectAuthError = createSelector(selectAuthState, s => s.error);
export const selectIsAuthenticated = createSelector(
  selectCurrentUser,
  user => user !== null
);
