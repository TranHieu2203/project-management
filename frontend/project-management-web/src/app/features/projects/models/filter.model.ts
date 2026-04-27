export type TaskStatus = 'NotStarted' | 'InProgress' | 'Completed' | 'OnHold' | 'Cancelled' | 'Delayed';
export type TaskPriority = 'Low' | 'Medium' | 'High' | 'Critical';
export type TaskNodeType = 'Phase' | 'Milestone' | 'Task';

/** Serializable — phải dùng JSON.stringify được để lưu Saved Presets */
export interface FilterCriteria {
  keyword?: string;
  statuses?: TaskStatus[];
  /** uuid[] or 'UNASSIGNED' special value */
  assigneeIds?: string[];
  priorities?: TaskPriority[];
  nodeTypes?: TaskNodeType[];
  /** uuid của node Milestone — filter tasks thuộc subtree này */
  milestoneId?: string;
  /** ISO date 'YYYY-MM-DD' */
  dueDateFrom?: string;
  /** ISO date 'YYYY-MM-DD' */
  dueDateTo?: string;
  overdueOnly?: boolean;
}

export interface FilterPreset {
  id: string;
  name: string;
  criteria: FilterCriteria;
  /** System defaults không xóa được */
  isSystem?: boolean;
}

export const SYSTEM_PRESETS: FilterPreset[] = [
  {
    id: 'sys-my-tasks',
    name: 'My Tasks',
    criteria: { assigneeIds: ['CURRENT_USER'] },
    isSystem: true,
  },
  {
    id: 'sys-overdue',
    name: 'Overdue Tasks',
    criteria: { overdueOnly: true },
    isSystem: true,
  },
  {
    id: 'sys-high-priority',
    name: 'Unfinished High Priority',
    criteria: {
      priorities: ['High', 'Critical'],
      statuses: ['NotStarted', 'InProgress', 'OnHold', 'Delayed'],
    },
    isSystem: true,
  },
];

export const FILTER_PRESETS_LS_KEY = 'task-filter-presets-v1';
export const GANTT_FILTER_MODE_LS_KEY = 'gantt-filter-mode';
