import { Injectable, inject } from '@angular/core';
import { Store, select } from '@ngrx/store';
import { Observable } from 'rxjs';
import { take } from 'rxjs/operators';
import { DashboardFilters, Deadline, ProjectSummary, StatCards } from '../models/dashboard.model';
import { DashboardActions } from './dashboard.actions';
import {
  selectDateRange,
  selectDeadlines,
  selectDashboardFilters,
  selectErrorDeadlines,
  selectErrorProjects,
  selectErrorStatCards,
  selectHasActiveFilters,
  selectLastUpdatedAt,
  selectLoadingDeadlines,
  selectLoadingProjects,
  selectLoadingStatCards,
  selectProjects,
  selectQuickChips,
  selectSelectedProjectIds,
  selectStatCards,
} from './dashboard.selectors';

@Injectable({ providedIn: 'root' })
export class DashboardFacade {
  private readonly store = inject(Store);

  readonly projects$: Observable<ProjectSummary[]> = this.store.select(selectProjects);
  readonly loadingProjects$: Observable<boolean> = this.store.select(selectLoadingProjects);
  readonly errorProjects$: Observable<string | null> = this.store.select(selectErrorProjects);
  readonly lastUpdatedAt$: Observable<number | null> = this.store.select(selectLastUpdatedAt);

  readonly statCards$: Observable<StatCards | null> = this.store.select(selectStatCards);
  readonly loadingStatCards$: Observable<boolean> = this.store.select(selectLoadingStatCards);
  readonly errorStatCards$: Observable<string | null> = this.store.select(selectErrorStatCards);

  readonly deadlines$: Observable<Deadline[]> = this.store.select(selectDeadlines);
  readonly loadingDeadlines$: Observable<boolean> = this.store.select(selectLoadingDeadlines);
  readonly errorDeadlines$: Observable<string | null> = this.store.select(selectErrorDeadlines);

  readonly filters$: Observable<DashboardFilters> = this.store.select(selectDashboardFilters);
  readonly selectedProjectIds$: Observable<string[]> = this.store.select(selectSelectedProjectIds);
  readonly dateRange$: Observable<{ start: string; end: string } | null> = this.store.select(selectDateRange);
  readonly quickChips$: Observable<string[]> = this.store.select(selectQuickChips);
  readonly hasActiveFilters$: Observable<boolean> = this.store.select(selectHasActiveFilters);

  startPolling(): void {
    this.store.dispatch(DashboardActions.startPolling());
  }

  stopPolling(): void {
    this.store.dispatch(DashboardActions.stopPolling());
  }

  setProjectFilter(projectIds: string[]): void {
    this.store.pipe(select(selectDashboardFilters), take(1)).subscribe(current => {
      this.store.dispatch(DashboardActions.setFilters({
        filters: { ...current, selectedProjectIds: projectIds }
      }));
    });
  }

  setDateRange(range: { start: string; end: string } | null): void {
    this.store.pipe(select(selectDashboardFilters), take(1)).subscribe(current => {
      this.store.dispatch(DashboardActions.setFilters({ filters: { ...current, dateRange: range } }));
    });
  }

  toggleChip(chip: string): void {
    this.store.pipe(select(selectDashboardFilters), take(1)).subscribe(current => {
      const chips = current.quickChips.includes(chip)
        ? current.quickChips.filter(c => c !== chip)
        : [...current.quickChips, chip];
      this.store.dispatch(DashboardActions.setFilters({ filters: { ...current, quickChips: chips } }));
    });
  }

  clearFilters(): void {
    this.store.dispatch(DashboardActions.clearFilters());
  }
}
