import { createSelector } from '@ngrx/store';
import { dashboardFeature } from './dashboard.reducer';

export const {
  selectDashboardState,
  selectProjects,
  selectLoadingProjects,
  selectErrorProjects,
  selectLastUpdatedAt,
  selectStatCards,
  selectLoadingStatCards,
  selectErrorStatCards,
  selectDeadlines,
  selectLoadingDeadlines,
  selectErrorDeadlines,
  selectFilters,
} = dashboardFeature;

export const selectDashboardFilters = selectFilters;
export const selectSelectedProjectIds = createSelector(selectFilters, f => f.selectedProjectIds);
export const selectDateRange = createSelector(selectFilters, f => f.dateRange);
export const selectQuickChips = createSelector(selectFilters, f => f.quickChips);
export const selectHasActiveFilters = createSelector(
  selectFilters,
  f => f.selectedProjectIds.length > 0 || f.dateRange !== null || f.quickChips.length > 0
);
