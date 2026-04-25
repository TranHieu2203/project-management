import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { catchError, finalize, map, of, switchMap, tap } from 'rxjs';
import { TokenService } from '../../../core/auth/token.service';
import { AuthApiService } from '../services/auth-api.service';
import { AuthActions } from './auth.actions';

@Injectable()
export class AuthEffects {
  private readonly actions$ = inject(Actions);
  private readonly authApiService = inject(AuthApiService);
  private readonly tokenService = inject(TokenService);
  private readonly router = inject(Router);

  login$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthActions.login),
      switchMap(({ email, password }) =>
        this.authApiService.login(email, password).pipe(
          tap(res => this.tokenService.setToken(res.accessToken)),
          map(res => AuthActions.loginSuccess({ user: res.user })),
          catchError(err =>
            of(
              AuthActions.loginFailure({
                error: err.error?.detail ?? err.error?.title ?? 'Đăng nhập thất bại. Vui lòng thử lại.',
              })
            )
          )
        )
      )
    )
  );

  loginSuccess$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(AuthActions.loginSuccess),
        tap(() => {
          const returnUrl =
            this.router.parseUrl(this.router.url).queryParams['returnUrl'];
          this.router.navigateByUrl(returnUrl ?? '/projects');
        })
      ),
    { dispatch: false }
  );

  logout$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthActions.logout),
      switchMap(() =>
        this.authApiService.logout().pipe(
          finalize(() => {
            this.tokenService.clearToken();
            this.router.navigate(['/login']);
          }),
          map(() => AuthActions.logoutSuccess()),
          catchError(() => of(AuthActions.logoutSuccess()))
        )
      )
    )
  );
}
