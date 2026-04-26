import { Injectable, inject } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { catchError, map, of, switchMap, takeUntil, timer } from 'rxjs';
import { CapacityApiService } from '../services/capacity-api.service';
import { CapacityActions } from './capacity.actions';

@Injectable()
export class CapacityEffects {
  private readonly actions$ = inject(Actions);
  private readonly api = inject(CapacityApiService);

  loadOverload$ = createEffect(() =>
    this.actions$.pipe(
      ofType(CapacityActions.loadOverload),
      switchMap(({ resourceId, dateFrom, dateTo }) =>
        this.api.getResourceOverload(resourceId, dateFrom, dateTo).pipe(
          map(result => CapacityActions.loadOverloadSuccess({ result })),
          catchError(err => of(CapacityActions.loadOverloadFailure({ error: err?.message ?? 'Lỗi tải overload.' })))
        )
      )
    )
  );

  startPolling$ = createEffect(() =>
    this.actions$.pipe(
      ofType(CapacityActions.startPolling),
      switchMap(({ resourceId, dateFrom, dateTo }) =>
        timer(0, 30000).pipe(
          takeUntil(this.actions$.pipe(ofType(CapacityActions.stopPolling))),
          switchMap(() =>
            this.api.getResourceOverload(resourceId, dateFrom, dateTo).pipe(
              map(result => CapacityActions.loadOverloadSuccess({ result })),
              catchError(err => of(CapacityActions.loadOverloadFailure({ error: err?.message ?? 'Lỗi tải overload.' })))
            )
          )
        )
      )
    )
  );

  loadUtilization$ = createEffect(() =>
    this.actions$.pipe(
      ofType(CapacityActions.loadUtilization),
      switchMap(({ resourceId, dateFrom, dateTo }) =>
        this.api.getCapacityUtilization(resourceId, dateFrom, dateTo).pipe(
          map(utilization => CapacityActions.loadUtilizationSuccess({ utilization })),
          catchError(err => of(CapacityActions.loadUtilizationFailure({ error: err?.message ?? 'Lỗi tải utilization.' })))
        )
      )
    )
  );

  logOverride$ = createEffect(() =>
    this.actions$.pipe(
      ofType(CapacityActions.logOverride),
      switchMap(({ request }) =>
        this.api.logCapacityOverride(request).pipe(
          map(() => CapacityActions.logOverrideSuccess()),
          catchError(err => of(CapacityActions.logOverrideFailure({ error: err?.message ?? 'Lỗi ghi override.' })))
        )
      )
    )
  );

  loadCrossProject$ = createEffect(() =>
    this.actions$.pipe(
      ofType(CapacityActions.loadCrossProject),
      switchMap(({ dateFrom, dateTo }) =>
        this.api.getCrossProjectOverload(dateFrom, dateTo).pipe(
          map(result => CapacityActions.loadCrossProjectSuccess({ result })),
          catchError(err => of(CapacityActions.loadCrossProjectFailure({ error: err?.message ?? 'Lỗi tải cross-project.' })))
        )
      )
    )
  );

  loadHeatmap$ = createEffect(() =>
    this.actions$.pipe(
      ofType(CapacityActions.loadHeatmap),
      switchMap(({ dateFrom, dateTo }) =>
        this.api.getCapacityHeatmap(dateFrom, dateTo).pipe(
          map(result => CapacityActions.loadHeatmapSuccess({ result })),
          catchError(err => of(CapacityActions.loadHeatmapFailure({ error: err?.message ?? 'Lỗi tải heatmap.' })))
        )
      )
    )
  );

  triggerForecast$ = createEffect(() =>
    this.actions$.pipe(
      ofType(CapacityActions.triggerForecast),
      switchMap(() =>
        this.api.triggerForecastCompute().pipe(
          switchMap(result => [
            CapacityActions.triggerForecastSuccess({ result }),
            CapacityActions.loadForecast(),
            CapacityActions.loadForecastDelta(),
          ]),
          catchError(err => of(CapacityActions.triggerForecastFailure({ error: err?.message ?? 'Lỗi tính forecast.' })))
        )
      )
    )
  );

  loadForecast$ = createEffect(() =>
    this.actions$.pipe(
      ofType(CapacityActions.loadForecast),
      switchMap(() =>
        this.api.getLatestForecast().pipe(
          map(result => CapacityActions.loadForecastSuccess({ result })),
          catchError(err => of(CapacityActions.loadForecastFailure({ error: err?.message ?? 'Lỗi tải forecast.' })))
        )
      )
    )
  );

  loadForecastDelta$ = createEffect(() =>
    this.actions$.pipe(
      ofType(CapacityActions.loadForecastDelta),
      switchMap(() =>
        this.api.getForecastDelta().pipe(
          map(result => CapacityActions.loadForecastDeltaSuccess({ result })),
          catchError(err => of(CapacityActions.loadForecastDeltaFailure({ error: err?.message ?? 'Lỗi tải delta.' })))
        )
      )
    )
  );
}
