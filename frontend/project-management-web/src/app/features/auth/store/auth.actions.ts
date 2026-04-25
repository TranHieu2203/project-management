import { createActionGroup, emptyProps, props } from '@ngrx/store';
import { UserInfo } from '../services/auth-api.service';

export const AuthActions = createActionGroup({
  source: 'Auth',
  events: {
    Login: props<{ email: string; password: string }>(),
    'Login Success': props<{ user: UserInfo }>(),
    'Login Failure': props<{ error: string }>(),
    Logout: emptyProps(),
    'Logout Success': emptyProps(),
    'Load Current User Success': props<{ user: UserInfo }>(),
  },
});
