import { Injectable, inject } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { catchError, map, of, switchMap } from 'rxjs';
import { ReportingApiService } from '../services/reporting-api.service';
import { ReportingActions } from './reporting.actions';


@Injectable()
export class ReportingEffects {
  private readonly actions$ = inject(Actions);
  private readonly api = inject(ReportingApiService);

  loadCostSummary$ = createEffect(() =>
    this.actions$.pipe(
      ofType(ReportingActions.loadCostSummary),
      switchMap(({ dateFrom, dateTo, projectId }) =>
        this.api.getCostSummary(dateFrom, dateTo, projectId).pipe(
          map(result => ReportingActions.loadCostSummarySuccess({ result })),
          catchError(err => of(ReportingActions.loadCostSummaryFailure({ error: err?.message ?? 'Lỗi tải cost summary.' })))
        )
      )
    )
  );

  loadCostBreakdown$ = createEffect(() =>
    this.actions$.pipe(
      ofType(ReportingActions.loadCostBreakdown),
      switchMap(({ groupBy, month, vendorId, projectId, resourceId, page, pageSize }) =>
        this.api.getCostBreakdown(groupBy, month, vendorId, projectId, resourceId, page, pageSize).pipe(
          map(result => ReportingActions.loadCostBreakdownSuccess({ result })),
          catchError(err => of(ReportingActions.loadCostBreakdownFailure({ error: err?.message ?? 'Lỗi tải breakdown.' })))
        )
      )
    )
  );

  loadResourceHeatmap$ = createEffect(() =>
    this.actions$.pipe(
      ofType(ReportingActions.loadResourceHeatmap),
      switchMap(({ from, to }) =>
        this.api.getResourceHeatmap(from, to).pipe(
          map(result => ReportingActions.loadResourceHeatmapSuccess({ result })),
          catchError(err => of(ReportingActions.loadResourceHeatmapFailure({ error: err?.message ?? 'Lỗi tải heatmap.' })))
        )
      )
    )
  );

  loadMilestones$ = createEffect(() =>
    this.actions$.pipe(
      ofType(ReportingActions.loadMilestones),
      switchMap(({ from, to }) =>
        this.api.getMilestones(from, to).pipe(
          map(milestones => ReportingActions.loadMilestonesSuccess({ milestones })),
          catchError(err => of(ReportingActions.loadMilestonesFailure({ error: err?.message ?? 'Lỗi tải milestones.' })))
        )
      )
    )
  );
}
