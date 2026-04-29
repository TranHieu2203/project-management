import { createFeature, createReducer, on } from '@ngrx/store';
import { ReportsState } from '../models/budget-report.model';
import { ReportsActions } from './reports.actions';

const today = new Date();
const defaultMonth = `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, '0')}`;

const initialState: ReportsState = {
  filters: { month: defaultMonth, projectIds: [] },
  report: null,
  loading: false,
  error: null,
};

export const reportsFeature = createFeature({
  name: 'reports',
  reducer: createReducer(
    initialState,
    on(ReportsActions.setFilters, (state, { filters }) => ({
      ...state,
      filters,
    })),
    on(ReportsActions.loadBudgetReport, state => ({
      ...state,
      loading: true,
      error: null,
    })),
    on(ReportsActions.loadBudgetReportSuccess, (state, { report }) => ({
      ...state,
      loading: false,
      report,
    })),
    on(ReportsActions.loadBudgetReportFailure, (state, { error }) => ({
      ...state,
      loading: false,
      error,
    })),
  ),
});

export const {
  selectReportsState,
  selectFilters,
  selectReport,
  selectLoading,
  selectError,
} = reportsFeature;
