import { HttpInterceptorFn } from '@angular/common/http';
import { catchError, retry, timer, throwError, mergeMap } from 'rxjs';

const MAX_RETRIES = 3;
const BACKOFF_MS = [1000, 2000, 4000];

export const retryInterceptor: HttpInterceptorFn = (req, next) => {
  if (req.method !== 'GET') {
    return next(req);
  }

  let attempt = 0;

  return next(req).pipe(
    retry({
      count: MAX_RETRIES,
      delay: (error, retryCount) => {
        if (error.status >= 400) {
          return throwError(() => error);
        }
        attempt = retryCount - 1;
        return timer(BACKOFF_MS[attempt] ?? 4000);
      },
    })
  );
};
