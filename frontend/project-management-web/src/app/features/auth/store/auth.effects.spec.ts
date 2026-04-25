import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { provideMockActions } from '@ngrx/effects/testing';
import { Action } from '@ngrx/store';
import { firstValueFrom, Observable, of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { TokenService } from '../../../core/auth/token.service';
import { AuthApiService, LoginResponse, UserInfo } from '../services/auth-api.service';
import { AuthActions } from './auth.actions';
import { AuthEffects } from './auth.effects';

const mockUser: UserInfo = { id: 'uid-1', email: 'pm1@local.test', displayName: 'PM One' };
const mockLoginResponse: LoginResponse = {
  accessToken: 'test.jwt.token',
  tokenType: 'Bearer',
  expiresInSeconds: 28800,
  user: mockUser,
};

describe('AuthEffects', () => {
  let actions$: Observable<Action>;
  let effects: AuthEffects;

  const authApiService = {
    login: vi.fn(),
    logout: vi.fn(),
    me: vi.fn(),
  };
  const tokenService = {
    setToken: vi.fn(),
    clearToken: vi.fn(),
    getToken: vi.fn(),
    isTokenValid: vi.fn(),
  };
  const router = {
    navigate: vi.fn(),
    navigateByUrl: vi.fn(),
    parseUrl: vi.fn().mockReturnValue({ queryParams: {} }),
    url: '/login',
  };

  beforeEach(() => {
    vi.clearAllMocks();
    router.parseUrl.mockReturnValue({ queryParams: {} });

    TestBed.configureTestingModule({
      providers: [
        AuthEffects,
        provideMockActions(() => actions$),
        { provide: AuthApiService, useValue: authApiService },
        { provide: TokenService, useValue: tokenService },
        { provide: Router, useValue: router },
      ],
    });

    effects = TestBed.inject(AuthEffects);
  });

  describe('login$', () => {
    it('dispatches loginSuccess and stores token on success', async () => {
      authApiService.login.mockReturnValue(of(mockLoginResponse));
      actions$ = of(AuthActions.login({ email: 'pm1@local.test', password: 'P@ssw0rd!123' }));

      const action = await firstValueFrom(effects.login$);

      expect(tokenService.setToken).toHaveBeenCalledWith('test.jwt.token');
      expect(action).toEqual(AuthActions.loginSuccess({ user: mockUser }));
    });

    it('dispatches loginFailure with API error title', async () => {
      authApiService.login.mockReturnValue(
        throwError(() => ({ status: 401, error: { title: 'Tên đăng nhập hoặc mật khẩu không đúng' } }))
      );
      actions$ = of(AuthActions.login({ email: 'wrong@test.com', password: 'bad' }));

      const action = await firstValueFrom(effects.login$);

      expect(action).toEqual(
        AuthActions.loginFailure({ error: 'Tên đăng nhập hoặc mật khẩu không đúng' })
      );
    });

    it('dispatches loginFailure with fallback message on network error', async () => {
      authApiService.login.mockReturnValue(throwError(() => ({ status: 0, error: null })));
      actions$ = of(AuthActions.login({ email: 'pm1@local.test', password: 'pass' }));

      const action = await firstValueFrom(effects.login$);

      expect(action).toEqual(
        AuthActions.loginFailure({ error: 'Đăng nhập thất bại. Vui lòng thử lại.' })
      );
    });
  });

  describe('loginSuccess$', () => {
    it('navigates to /projects when no returnUrl', async () => {
      router.parseUrl.mockReturnValue({ queryParams: {} });
      actions$ = of(AuthActions.loginSuccess({ user: mockUser }));

      await firstValueFrom(effects.loginSuccess$);

      expect(router.navigateByUrl).toHaveBeenCalledWith('/projects');
    });

    it('navigates to returnUrl when present', async () => {
      router.parseUrl.mockReturnValue({ queryParams: { returnUrl: '/projects/42' } });
      actions$ = of(AuthActions.loginSuccess({ user: mockUser }));

      await firstValueFrom(effects.loginSuccess$);

      expect(router.navigateByUrl).toHaveBeenCalledWith('/projects/42');
    });
  });

  describe('logout$', () => {
    it('clears token and navigates to /login on logout', async () => {
      authApiService.logout.mockReturnValue(of(undefined));
      actions$ = of(AuthActions.logout());

      const action = await firstValueFrom(effects.logout$);

      expect(tokenService.clearToken).toHaveBeenCalled();
      expect(router.navigate).toHaveBeenCalledWith(['/login']);
      expect(action).toEqual(AuthActions.logoutSuccess());
    });

    it('still clears token and dispatches logoutSuccess even if API fails', async () => {
      authApiService.logout.mockReturnValue(throwError(() => new Error('Network error')));
      actions$ = of(AuthActions.logout());

      const action = await firstValueFrom(effects.logout$);

      expect(tokenService.clearToken).toHaveBeenCalled();
      expect(action).toEqual(AuthActions.logoutSuccess());
    });
  });
});
