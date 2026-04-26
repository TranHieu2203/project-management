import { createFeature, createReducer, on } from '@ngrx/store';
import { CostBreakdownResult, CostSummaryResult } from '../models/cost-report.model';
import { ReportingActions } from './reporting.actions';

export interface ReportingState {
  costSummary: CostSummaryResult | null;
  loading: boolean;
  error: string | null;
  costBreakdown: CostBreakdownResult | null;
  breakdownLoading: boolean;
}

const initialState: ReportingState = {
  costSummary: null,
  loading: false,
  error: null,
  costBreakdown: null,
  breakdownLoading: false,
};

export const reportingFeature = createFeature({
  name: 'reporting',
  reducer: createReducer(
    initialState,
    on(ReportingActions.loadCostSummary, state => ({ ...state, loading: true, error: null })),
    on(ReportingActions.loadCostSummarySuccess, (state, { result }) => ({
      ...state, loading: false, costSummary: result,
    })),
    on(ReportingActions.loadCostSummaryFailure, (state, { error }) => ({
      ...state, loading: false, error,
    })),
    on(ReportingActions.loadCostBreakdown, state => ({ ...state, breakdownLoading: true })),
    on(ReportingActions.loadCostBreakdownSuccess, (state, { result }) => ({
      ...state, breakdownLoading: false, costBreakdown: result,
    })),
    on(ReportingActions.loadCostBreakdownFailure, state => ({ ...state, breakdownLoading: false })),
  ),
});

export const {
  selectCostSummary,
  selectLoading: selectReportingLoading,
  selectError: selectReportingError,
  selectCostBreakdown,
  selectBreakdownLoading,
} = reportingFeature;
