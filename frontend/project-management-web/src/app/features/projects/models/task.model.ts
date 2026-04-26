export interface TaskDependency {
  predecessorId: string;
  dependencyType: 'FS' | 'SS' | 'FF' | 'SF';
}

export interface ProjectTask {
  id: string;
  projectId: string;
  parentId: string | null;
  type: 'Phase' | 'Milestone' | 'Task';
  vbs: string | null;
  name: string;
  priority: 'Low' | 'Medium' | 'High' | 'Critical';
  status: 'NotStarted' | 'InProgress' | 'Completed' | 'OnHold' | 'Cancelled' | 'Delayed';
  notes: string | null;
  plannedStartDate: string | null;   // "2026-04-25" format (DateOnly)
  plannedEndDate: string | null;
  actualStartDate: string | null;
  actualEndDate: string | null;
  plannedEffortHours: number | null;
  actualEffortHours: number | null;  // luôn null cho đến Epic 3
  percentComplete: number | null;
  assigneeUserId: string | null;
  sortOrder: number;
  version: number;
  predecessors: TaskDependency[];
}

export interface CreateTaskPayload {
  parentId: string | null;
  type: string;
  vbs?: string;
  name: string;
  priority: string;
  status: string;
  notes?: string;
  plannedStartDate?: string;
  plannedEndDate?: string;
  actualStartDate?: string;
  actualEndDate?: string;
  plannedEffortHours?: number;
  percentComplete?: number;
  assigneeUserId?: string;
  sortOrder: number;
  predecessors?: { predecessorId: string; dependencyType: string }[];
}

export interface UpdateTaskPayload extends CreateTaskPayload {}
