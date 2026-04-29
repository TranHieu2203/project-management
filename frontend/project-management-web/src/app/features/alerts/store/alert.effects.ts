import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { catchError, map, of, switchMap, tap } from 'rxjs';
import { AlertsApiService } from '../services/alerts-api.service';
import { AlertActions } from './alert.actions';

@Injectable()
export class AlertsEffects {
  private readonly actions$ = inject(Actions);
  private readonly api = inject(AlertsApiService);
  private readonly router = inject(Router);

  loadAlerts$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AlertActions.loadAlerts),
      switchMap(() =>
        this.api.getAlerts().pipe(
          map(alerts => AlertActions.loadAlertsSuccess({ alerts })),
          catchError(err =>
            of(AlertActions.loadAlertsFailure({ error: err?.message ?? 'Lỗi tải alerts.' }))
          )
        )
      )
    )
  );

  markAlertRead$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AlertActions.markAlertRead),
      switchMap(({ id }) =>
        this.api.markRead(id).pipe(
          map(() => AlertActions.markAlertReadSuccess({ id })),
          catchError(err =>
            of(AlertActions.markAlertReadFailure({ error: err?.message ?? 'Lỗi mark read.' }))
          )
        )
      )
    )
  );

  navigateOnMarkRead$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(AlertActions.markAlertRead),
        tap(({ entityType, projectId, entityId }) => {
          if (entityType === 'Task' && projectId) {
            this.router.navigate(['/projects', projectId]);
          } else if (entityType === 'Project' && (projectId || entityId)) {
            this.router.navigate(['/projects', projectId ?? entityId]);
          }
        })
      ),
    { dispatch: false }
  );
}
