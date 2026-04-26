import { createFeature, createReducer, on } from '@ngrx/store';
import { ResourceOverloadResult } from '../models/overload.model';
import { CapacityHeatmapResult, CapacityUtilizationResult, CrossProjectOverloadResult, ForecastDeltaResult, ForecastResult } from '../models/utilization.model';
import { CapacityActions } from './capacity.actions';

export interface CapacityState {
  result: ResourceOverloadResult | null;
  loading: boolean;
  error: string | null;
  lastUpdated: string | null;
  utilization: CapacityUtilizationResult | null;
  utilizationLoading: boolean;
  crossProject: CrossProjectOverloadResult | null;
  crossProjectLoading: boolean;
  heatmap: CapacityHeatmapResult | null;
  heatmapLoading: boolean;
  forecast: ForecastResult | null;
  forecastLoading: boolean;
  forecastComputing: boolean;
  forecastDelta: ForecastDeltaResult | null;
  forecastDeltaLoading: boolean;
}

const initialState: CapacityState = {
  result: null,
  loading: false,
  error: null,
  lastUpdated: null,
  utilization: null,
  utilizationLoading: false,
  crossProject: null,
  crossProjectLoading: false,
  heatmap: null,
  heatmapLoading: false,
  forecast: null,
  forecastLoading: false,
  forecastComputing: false,
  forecastDelta: null,
  forecastDeltaLoading: false,
};

export const capacityFeature = createFeature({
  name: 'capacity',
  reducer: createReducer(
    initialState,
    on(CapacityActions.loadOverload, state => ({
      ...state,
      loading: state.result === null,  // spinner only on first load, not background refresh
      error: null,
    })),
    on(CapacityActions.startPolling, state => ({ ...state, error: null })),
    on(CapacityActions.loadOverloadSuccess, (state, { result }) => ({
      ...state, loading: false, result, lastUpdated: new Date().toISOString(), error: null,
    })),
    on(CapacityActions.loadOverloadFailure, (state, { error }) => ({ ...state, loading: false, error })),
    on(CapacityActions.loadUtilization, state => ({ ...state, utilizationLoading: true })),
    on(CapacityActions.loadUtilizationSuccess, (state, { utilization }) => ({
      ...state, utilizationLoading: false, utilization,
    })),
    on(CapacityActions.loadUtilizationFailure, state => ({ ...state, utilizationLoading: false })),
    on(CapacityActions.logOverride, state => state),
    on(CapacityActions.logOverrideSuccess, state => state),
    on(CapacityActions.logOverrideFailure, state => state),
    on(CapacityActions.loadCrossProject, state => ({ ...state, crossProjectLoading: true })),
    on(CapacityActions.loadCrossProjectSuccess, (state, { result }) => ({
      ...state, crossProjectLoading: false, crossProject: result,
    })),
    on(CapacityActions.loadCrossProjectFailure, state => ({ ...state, crossProjectLoading: false })),
    on(CapacityActions.loadHeatmap, state => ({ ...state, heatmapLoading: true })),
    on(CapacityActions.loadHeatmapSuccess, (state, { result }) => ({
      ...state, heatmapLoading: false, heatmap: result,
    })),
    on(CapacityActions.loadHeatmapFailure, state => ({ ...state, heatmapLoading: false })),
    on(CapacityActions.triggerForecast, state => ({ ...state, forecastComputing: true })),
    on(CapacityActions.triggerForecastSuccess, state => ({ ...state, forecastComputing: false })),
    on(CapacityActions.triggerForecastFailure, state => ({ ...state, forecastComputing: false })),
    on(CapacityActions.loadForecast, state => ({ ...state, forecastLoading: true })),
    on(CapacityActions.loadForecastSuccess, (state, { result }) => ({
      ...state, forecastLoading: false, forecast: result,
    })),
    on(CapacityActions.loadForecastFailure, state => ({ ...state, forecastLoading: false })),
    on(CapacityActions.loadForecastDelta, state => ({ ...state, forecastDeltaLoading: true })),
    on(CapacityActions.loadForecastDeltaSuccess, (state, { result }) => ({
      ...state, forecastDeltaLoading: false, forecastDelta: result,
    })),
    on(CapacityActions.loadForecastDeltaFailure, state => ({ ...state, forecastDeltaLoading: false })),
  ),
});

export const {
  selectResult: selectOverloadResult,
  selectLoading: selectCapacityLoading,
  selectError: selectCapacityError,
  selectLastUpdated,
  selectUtilization,
  selectUtilizationLoading,
  selectCrossProject,
  selectCrossProjectLoading,
  selectHeatmap,
  selectHeatmapLoading,
  selectForecast,
  selectForecastLoading,
  selectForecastComputing,
  selectForecastDelta,
  selectForecastDeltaLoading,
} = capacityFeature;
