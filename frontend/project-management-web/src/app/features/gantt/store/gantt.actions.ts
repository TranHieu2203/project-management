import { createActionGroup, emptyProps, props } from '@ngrx/store';
import { GanttConflictState, GanttDependency, GanttGranularity, GanttTask, GanttTaskEdit } from '../models/gantt.model';

export const GanttActions = createActionGroup({
  source: 'Gantt',
  events: {
    // Load
    'Load Gantt Data': props<{ projectId: string }>(),
    'Load Gantt Data Success': props<{ tasks: GanttTask[] }>(),
    'Load Gantt Data Failure': props<{ error: string }>(),

    // Granularity
    'Set Granularity': props<{ granularity: GanttGranularity }>(),

    // Dirty edits
    'Mark Task Dirty': props<{ edit: GanttTaskEdit }>(),
    'Discard Gantt Edits': emptyProps(),

    // Save
    'Save Gantt Edits': emptyProps(),
    'Save Gantt Edits Success': props<{
      updatedTasks: Array<{
        id: string;
        version: number;
        plannedStart: Date | null;
        plannedEnd: Date | null;
        predecessors: GanttDependency[];
        name?: string;
        status?: string;
        percentComplete?: number;
      }>;
    }>(),
    'Save Gantt Edits Failure': props<{ error: string }>(),

    // Conflict
    'Gantt Conflict': props<{ conflict: GanttConflictState }>(),
    'Resolve Conflict': emptyProps(),

    // Clear
    'Clear Gantt': emptyProps(),
  },
});
