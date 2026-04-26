export type TrafficLightStatus = 'Green' | 'Yellow' | 'Orange' | 'Red';

export interface TopContribution {
  date: string;
  hours: number;
}

export interface CapacityUtilizationResult {
  resourceId: string;
  utilizationPct: number;
  availableHours: number;
  actualHours: number;
  trafficLight: TrafficLightStatus;
  topContributions: TopContribution[];
}

export interface LogOverrideRequest {
  resourceId: string;
  dateFrom: string;
  dateTo: string;
  trafficLight: TrafficLightStatus;
}

export interface ResourceOverloadSummary {
  resourceId: string;
  totalHours: number;
  overloadedDays: number;
  overloadedWeeks: number;
  hasOverload: boolean;
}

export interface CrossProjectOverloadResult {
  resources: ResourceOverloadSummary[];
  dateFrom: string;
  dateTo: string;
  projectCount: number;
}

export interface HeatmapCell {
  weekStart: string;
  utilizationPct: number;
  trafficLight: TrafficLightStatus;
  actualHours: number;
  availableHours: number;
}

export interface HeatmapRow {
  resourceId: string;
  cells: HeatmapCell[];
}

export interface CapacityHeatmapResult {
  weeks: string[];
  rows: HeatmapRow[];
  dateFrom: string;
  dateTo: string;
  projectCount: number;
}

export interface ForecastWeekCell {
  weekStart: string;
  forecastedHours: number;
  availableHours: number;
  forecastedUtilizationPct: number;
  trafficLight: TrafficLightStatus;
}

export interface ForecastResourceRow {
  resourceId: string;
  cells: ForecastWeekCell[];
}

export interface ForecastResult {
  version: number;
  computedAt: string | null;
  weeks: string[];
  rows: ForecastResourceRow[];
}

export interface ForecastComputeResult {
  version: number;
  computedAt: string;
  status: string;
}

export interface ForecastDeltaItem {
  resourceId: string;
  weekStart: string;
  previousUtilizationPct: number;
  currentUtilizationPct: number;
  deltaPct: number;
  currentTrafficLight: TrafficLightStatus;
  hint: string;
}

export interface ForecastDeltaResult {
  currentVersion: number;
  previousVersion: number;
  topChanges: ForecastDeltaItem[];
  hasData: boolean;
}
