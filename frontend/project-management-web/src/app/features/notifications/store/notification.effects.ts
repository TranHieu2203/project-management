import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { catchError, interval, map, of, switchMap, take, tap } from 'rxjs';
import { NotificationsApiService } from '../services/notifications-api.service';
import { NotificationActions } from './notification.actions';

@Injectable()
export class NotificationsEffects {
  private readonly actions$ = inject(Actions);
  private readonly api = inject(NotificationsApiService);
  private readonly router = inject(Router);

  loadNotifications$ = createEffect(() =>
    this.actions$.pipe(
      ofType(NotificationActions.loadNotifications),
      switchMap(() =>
        this.api.getNotifications().pipe(
          map(notifications => NotificationActions.loadNotificationsSuccess({ notifications })),
          catchError(err =>
            of(NotificationActions.loadNotificationsFailure({ error: err?.message ?? 'Lỗi tải thông báo.' }))
          )
        )
      )
    )
  );

  // Start polling after first response (success or failure)
  poll$ = createEffect(() =>
    this.actions$.pipe(
      ofType(
        NotificationActions.loadNotificationsSuccess,
        NotificationActions.loadNotificationsFailure
      ),
      take(1),
      switchMap(() =>
        interval(30_000).pipe(
          map(() => NotificationActions.loadNotifications())
        )
      )
    )
  );

  markRead$ = createEffect(() =>
    this.actions$.pipe(
      ofType(NotificationActions.markRead),
      switchMap(({ id }) =>
        this.api.markRead(id).pipe(
          map(() => NotificationActions.markReadSuccess({ id })),
          catchError(() => of(NotificationActions.markReadSuccess({ id }))) // optimistic
        )
      )
    )
  );

  markAllRead$ = createEffect(() =>
    this.actions$.pipe(
      ofType(NotificationActions.markAllRead),
      switchMap(() =>
        this.api.markAllRead().pipe(
          map(() => NotificationActions.markAllReadSuccess()),
          catchError(() => of(NotificationActions.markAllReadSuccess())) // optimistic
        )
      )
    )
  );

  navigate$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(NotificationActions.markRead),
        tap(({ projectId }) => {
          if (projectId) {
            this.router.navigate(['/projects', projectId]);
          } else {
            this.router.navigate(['/my-tasks']);
          }
        })
      ),
    { dispatch: false }
  );
}
