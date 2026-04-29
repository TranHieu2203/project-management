import { createFeature, createReducer, on } from '@ngrx/store';
import { CostBreakdownResult, CostSummaryResult } from '../models/cost-report.model';
import { MilestoneDto, ResourceHeatmapResult } from '../models/resource-report.model';
import { ReportingActions } from './reporting.actions';

export interface ReportingState {
  costSummary: CostSummaryResult | null;
  loading: boolean;
  error: string | null;
  costBreakdown: CostBreakdownResult | null;
  breakdownLoading: boolean;
  resourceHeatmap: ResourceHeatmapResult | null;
  heatmapLoading: boolean;
  milestones: MilestoneDto[];
  milestonesLoading: boolean;
}

const initialState: ReportingState = {
  costSummary: null,
  loading: false,
  error: null,
  costBreakdown: null,
  breakdownLoading: false,
  resourceHeatmap: null,
  heatmapLoading: false,
  milestones: [],
  milestonesLoading: false,
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
    on(ReportingActions.loadResourceHeatmap, state => ({ ...state, heatmapLoading: true })),
    on(ReportingActions.loadResourceHeatmapSuccess, (state, { result }) => ({
      ...state, heatmapLoading: false, resourceHeatmap: result,
    })),
    on(ReportingActions.loadResourceHeatmapFailure, state => ({ ...state, heatmapLoading: false })),
    on(ReportingActions.loadMilestones, state => ({ ...state, milestonesLoading: true })),
    on(ReportingActions.loadMilestonesSuccess, (state, { milestones }) => ({
      ...state, milestonesLoading: false, milestones,
    })),
    on(ReportingActions.loadMilestonesFailure, state => ({ ...state, milestonesLoading: false })),
  ),
});

export const {
  selectCostSummary,
  selectLoading: selectReportingLoading,
  selectError: selectReportingError,
  selectCostBreakdown,
  selectBreakdownLoading,
  selectResourceHeatmap,
  selectHeatmapLoading,
  selectMilestones,
  selectMilestonesLoading,
} = reportingFeature;
