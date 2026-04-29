import { createSelector } from '@ngrx/store';
import { selectFilters, selectReport, selectLoading, selectError } from './reports.reducer';

export const selectReportsFilters = selectFilters;
export const selectBudgetReport = selectReport;
export const selectReportsLoading = selectLoading;
export const selectReportsError = selectError;

export const selectReportsMonth = createSelector(selectFilters, f => f.month);
export const selectReportsProjectIds = createSelector(selectFilters, f => f.projectIds);
export const selectHasReport = createSelector(selectReport, r => r !== null && r.projects.length > 0);
