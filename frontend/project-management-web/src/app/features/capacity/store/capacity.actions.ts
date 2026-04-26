import { createActionGroup, emptyProps, props } from '@ngrx/store';

import { ResourceOverloadResult } from '../models/overload.model';
import { CapacityHeatmapResult, CapacityUtilizationResult, CrossProjectOverloadResult, ForecastComputeResult, ForecastDeltaResult, ForecastResult, LogOverrideRequest } from '../models/utilization.model';

export const CapacityActions = createActionGroup({
  source: 'Capacity',
  events: {
    'Load Overload': props<{ resourceId: string; dateFrom: string; dateTo: string }>(),
    'Load Overload Success': props<{ result: ResourceOverloadResult }>(),
    'Load Overload Failure': props<{ error: string }>(),
    'Start Polling': props<{ resourceId: string; dateFrom: string; dateTo: string }>(),
    'Stop Polling': emptyProps(),
    'Load Utilization': props<{ resourceId: string; dateFrom: string; dateTo: string }>(),
    'Load Utilization Success': props<{ utilization: CapacityUtilizationResult }>(),
    'Load Utilization Failure': props<{ error: string }>(),
    'Log Override': props<{ request: LogOverrideRequest }>(),
    'Log Override Success': emptyProps(),
    'Log Override Failure': props<{ error: string }>(),
    'Load Cross Project': props<{ dateFrom: string; dateTo: string }>(),
    'Load Cross Project Success': props<{ result: CrossProjectOverloadResult }>(),
    'Load Cross Project Failure': props<{ error: string }>(),
    'Load Heatmap': props<{ dateFrom: string; dateTo: string }>(),
    'Load Heatmap Success': props<{ result: CapacityHeatmapResult }>(),
    'Load Heatmap Failure': props<{ error: string }>(),
    'Trigger Forecast': emptyProps(),
    'Trigger Forecast Success': props<{ result: ForecastComputeResult }>(),
    'Trigger Forecast Failure': props<{ error: string }>(),
    'Load Forecast': emptyProps(),
    'Load Forecast Success': props<{ result: ForecastResult }>(),
    'Load Forecast Failure': props<{ error: string }>(),
    'Load Forecast Delta': emptyProps(),
    'Load Forecast Delta Success': props<{ result: ForecastDeltaResult }>(),
    'Load Forecast Delta Failure': props<{ error: string }>(),
  },
});
