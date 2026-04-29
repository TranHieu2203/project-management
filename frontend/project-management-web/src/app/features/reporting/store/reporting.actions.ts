import { createActionGroup, props } from '@ngrx/store';
import { CostBreakdownResult, CostSummaryResult } from '../models/cost-report.model';
import { MilestoneDto, ResourceHeatmapResult } from '../models/resource-report.model';

export const ReportingActions = createActionGroup({
  source: 'Reporting',
  events: {
    'Load Cost Summary': props<{ dateFrom: string; dateTo: string; projectId?: string }>(),
    'Load Cost Summary Success': props<{ result: CostSummaryResult }>(),
    'Load Cost Summary Failure': props<{ error: string }>(),
    'Load Cost Breakdown': props<{ groupBy: string; month?: string; vendorId?: string; projectId?: string; resourceId?: string; page?: number; pageSize?: number }>(),
    'Load Cost Breakdown Success': props<{ result: CostBreakdownResult }>(),
    'Load Cost Breakdown Failure': props<{ error: string }>(),
    'Load Resource Heatmap': props<{ from: string; to: string }>(),
    'Load Resource Heatmap Success': props<{ result: ResourceHeatmapResult }>(),
    'Load Resource Heatmap Failure': props<{ error: string }>(),
    'Load Milestones': props<{ from?: string; to?: string }>(),
    'Load Milestones Success': props<{ milestones: MilestoneDto[] }>(),
    'Load Milestones Failure': props<{ error: string }>(),
  },
});
