import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { Store } from '@ngrx/store';
import { routerNavigatedAction } from '@ngrx/router-store';
import { catchError, distinctUntilChanged, filter, map, merge, of, skip, switchMap, takeUntil, tap, timer, withLatestFrom } from 'rxjs';
import { DashboardApiService } from '../services/dashboard-api.service';
import { DashboardActions } from './dashboard.actions';
import { selectDashboardFilters } from './dashboard.selectors';
import { DashboardFilters } from '../models/dashboard.model';

const filtersEqual = (a: DashboardFilters, b: DashboardFilters) =>
  JSON.stringify(a) === JSON.stringify(b);

@Injectable()
export class DashboardEffects {
  private readonly actions$ = inject(Actions);
  private readonly api = inject(DashboardApiService);
  private readonly store = inject(Store);
  private readonly router = inject(Router);

  pollDashboard$ = createEffect(() =>
    this.actions$.pipe(
      ofType(DashboardActions.startPolling),
      switchMap(() =>
        timer(0, 30_000).pipe(
          takeUntil(this.actions$.pipe(ofType(DashboardActions.stopPolling))),
          map(() => DashboardActions.loadPortfolio())
        )
      )
    )
  );

  loadPortfolio$ = createEffect(() =>
    this.actions$.pipe(
      ofType(DashboardActions.loadPortfolio),
      withLatestFrom(this.store.select(selectDashboardFilters)),
      switchMap(([, filters]) =>
        merge(
          this.api.getSummary(filters.selectedProjectIds).pipe(
            map(data => DashboardActions.loadSummarySuccess({ data })),
            catchError(err =>
              of(DashboardActions.loadSummaryFailure({
                error: err?.message ?? 'Lỗi tải portfolio summary.'
              }))
            )
          ),
          this.api.getStatCards(filters.selectedProjectIds).pipe(
            map(data => DashboardActions.loadStatCardsSuccess({ data })),
            catchError(err =>
              of(DashboardActions.loadStatCardsFailure({
                error: err?.message ?? 'Lỗi tải stat cards.'
              }))
            )
          ),
          this.api.getDeadlines(7, filters.selectedProjectIds).pipe(
            map(data => DashboardActions.loadDeadlinesSuccess({ data })),
            catchError(err =>
              of(DashboardActions.loadDeadlinesFailure({
                error: err?.message ?? 'Lỗi tải deadlines.'
              }))
            )
          ),
        )
      )
    )
  );

  syncFiltersFromUrl$ = createEffect(() =>
    this.actions$.pipe(
      ofType(routerNavigatedAction),
      filter(action => action.payload.routerState.url.includes('/dashboard')),
      map(action => {
        const params = action.payload.routerState.root.queryParams;
        return DashboardActions.setFilters({
          filters: {
            selectedProjectIds: params['projects'] ? params['projects'].split(',') : [],
            dateRange: params['from'] && params['to']
              ? { start: params['from'], end: params['to'] }
              : null,
            quickChips: params['chips'] ? params['chips'].split(',') : [],
          }
        });
      })
    )
  );

  updateUrl$ = createEffect(() =>
    this.store.select(selectDashboardFilters).pipe(
      distinctUntilChanged(filtersEqual),
      skip(1),
      tap(filters => this.router.navigate([], {
        queryParams: {
          projects: filters.selectedProjectIds.length ? filters.selectedProjectIds.join(',') : null,
          from: filters.dateRange?.start ?? null,
          to: filters.dateRange?.end ?? null,
          chips: filters.quickChips.length ? filters.quickChips.join(',') : null,
        },
        queryParamsHandling: 'merge',
      }))
    ),
    { dispatch: false }
  );
}
