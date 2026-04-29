export type GanttTaskType = 'Phase' | 'Milestone' | 'Task';
export type GanttGranularity = 'week' | 'day';

export interface GanttTask {
  id: string;
  parentId: string | null;
  type: GanttTaskType;
  vbs: string | null;
  name: string;
  status: string;
  priority: string;
  plannedStart: Date | null;
  plannedEnd: Date | null;
  percentComplete: number;
  depth: number;
  sortOrder: number;
  collapsed: boolean;
  version: number;
  dirty: boolean;
  assigneeUserId: string | null;
}

export interface GanttTaskEdit {
  taskId: string;
  originalVersion: number;
  newPlannedStart?: Date;
  newPlannedEnd?: Date;
  newName?: string;
  newStatus?: string;
  newPercentComplete?: number;
}

export interface GanttConflictState {
  taskId: string;
  serverTaskJson: string;  // serialized ProjectTask from 409
  localEdit: GanttTaskEdit;
  serverETag: string;
}

export interface GanttState {
  projectId: string | null;
  tasks: GanttTask[];
  loading: boolean;
  error: string | null;
  granularity: GanttGranularity;
  dirtyTasks: Record<string, GanttTaskEdit>;
  saving: boolean;
  conflict: GanttConflictState | null;
}

export interface GanttConfig {
  pixelsPerWeek: number;
  pixelsPerDay: number;
  rowHeight: number;
  headerHeight: number;
}

export const DEFAULT_GANTT_CONFIG: GanttConfig = {
  pixelsPerWeek: 120,
  pixelsPerDay: 24,
  rowHeight: 36,
  headerHeight: 56,
};
