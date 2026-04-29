export interface DashboardFilters {
  selectedProjectIds: string[];
  dateRange: { start: string; end: string } | null;
  quickChips: string[];
}

export const DEFAULT_FILTERS: DashboardFilters = {
  selectedProjectIds: [],
  dateRange: null,
  quickChips: [],
};

export interface ProjectSummary {
  projectId: string;
  name: string;
  healthStatus: 'OnTrack' | 'AtRisk' | 'Delayed';
  percentComplete: number;
  percentTimeElapsed: number;
  remainingTaskCount: number;
  overdueTaskCount: number;
}

export interface StatCards {
  overdueTaskCount: number;
  atRiskProjectCount: number;
  overloadedResourceCount: number;
}

export interface Deadline {
  taskId: string;
  projectId: string;
  projectName: string;
  entityType: 'Task' | 'Milestone';
  name: string;
  dueDate: string;
  daysRemaining: number;
}

export interface DashboardState {
  projects: ProjectSummary[];
  loadingProjects: boolean;
  errorProjects: string | null;
  lastUpdatedAt: number | null;
  statCards: StatCards | null;
  loadingStatCards: boolean;
  errorStatCards: string | null;
  deadlines: Deadline[];
  loadingDeadlines: boolean;
  errorDeadlines: string | null;
}
