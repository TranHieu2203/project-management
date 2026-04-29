import { inject, Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { Store } from '@ngrx/store';
import { routerNavigatedAction } from '@ngrx/router-store';
import { catchError, distinctUntilChanged, map, of, skip, switchMap, tap, withLatestFrom } from 'rxjs';
import { ReportsActions } from './reports.actions';
import { selectReportsFilters } from './reports.selectors';
import { ReportsApiService } from '../services/reports-api.service';
import { ReportsFilters } from '../models/budget-report.model';

const filtersEqual = (a: ReportsFilters, b: ReportsFilters) => JSON.stringify(a) === JSON.stringify(b);

@Injectable()
export class ReportsEffects {
  private readonly actions$ = inject(Actions);
  private readonly store = inject(Store);
  private readonly router = inject(Router);
  private readonly api = inject(ReportsApiService);

  syncFiltersFromUrl$ = createEffect(() =>
    this.actions$.pipe(
      ofType(routerNavigatedAction),
      map(action => {
        const qp = (action as any).payload.routerState.root.queryParams;
        const month: string = qp['month'] ?? '';
        const projectIds: string[] = qp['projectIds']
          ? (Array.isArray(qp['projectIds']) ? qp['projectIds'] : [qp['projectIds']])
          : [];
        return ReportsActions.setFilters({ filters: { month, projectIds } });
      }),
    )
  );

  updateUrl$ = createEffect(() =>
    this.store.select(selectReportsFilters).pipe(
      distinctUntilChanged(filtersEqual),
      skip(1),
      tap(filters => {
        this.router.navigate([], {
          queryParamsHandling: 'merge',
          queryParams: {
            month: filters.month || null,
            projectIds: filters.projectIds.length > 0 ? filters.projectIds : null,
          },
        });
      }),
    ),
  { dispatch: false });

  loadBudgetReport$ = createEffect(() =>
    this.actions$.pipe(
      ofType(ReportsActions.loadBudgetReport, ReportsActions.setFilters),
      withLatestFrom(this.store.select(selectReportsFilters)),
      switchMap(([, filters]) =>
        this.api.getBudgetReport(filters.month, filters.projectIds).pipe(
          map(report => ReportsActions.loadBudgetReportSuccess({ report })),
          catchError(err => of(ReportsActions.loadBudgetReportFailure({
            error: err?.message ?? 'Không thể tải báo cáo ngân sách.',
          }))),
        )
      ),
    )
  );
}
