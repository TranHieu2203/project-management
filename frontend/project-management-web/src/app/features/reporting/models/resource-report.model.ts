export interface ResourceHeatmapCell {
  weekStart: string;
  utilizationPct: number;
  trafficLight: 'Green' | 'Yellow' | 'Orange' | 'Red';
  actualHours: number;
  availableHours: number;
}

export interface ResourceHeatmapRow {
  resourceId: string;
  cells: ResourceHeatmapCell[];
}

export interface ResourceHeatmapResult {
  weeks: string[];
  rows: ResourceHeatmapRow[];
  dateFrom: string;
  dateTo: string;
  projectCount: number;
}

export interface MilestoneDto {
  taskId: string;
  name: string;
  projectId: string;
  projectName: string;
  dueDate: string | null;
  status: string;
}
