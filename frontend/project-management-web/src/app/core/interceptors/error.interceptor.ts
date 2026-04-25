import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError, Subject } from 'rxjs';
import { TokenService } from '../auth/token.service';

export const conflictError$ = new Subject<{ body: unknown; eTag: string }>();

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const tokenService = inject(TokenService);

  return next(req).pipe(
    catchError(err => {
      switch (err.status) {
        case 401:
          tokenService.clearToken();
          router.navigate(['/login']);
          break;
        case 409:
          conflictError$.next({
            body: err.error,
            eTag: err.headers.get('ETag') ?? '',
          });
          break;
      }
      return throwError(() => err);
    })
  );
};
