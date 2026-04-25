import { createReducer, on } from '@ngrx/store';
import { UserInfo } from '../services/auth-api.service';
import { AuthActions } from './auth.actions';

export interface AuthState {
  user: UserInfo | null;
  isLoading: boolean;
  error: string | null;
}

const initialState: AuthState = {
  user: null,
  isLoading: false,
  error: null,
};

export const authReducer = createReducer(
  initialState,
  on(AuthActions.login, state => ({ ...state, isLoading: true, error: null })),
  on(AuthActions.loginSuccess, (state, { user }) => ({
    ...state,
    user,
    isLoading: false,
    error: null,
  })),
  on(AuthActions.loginFailure, (state, { error }) => ({
    ...state,
    isLoading: false,
    error,
  })),
  on(AuthActions.logout, state => ({ ...state, isLoading: true })),
  on(AuthActions.logoutSuccess, () => initialState),
  on(AuthActions.loadCurrentUserSuccess, (state, { user }) => ({
    ...state,
    user,
  }))
);
