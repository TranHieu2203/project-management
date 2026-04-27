import { createActionGroup, emptyProps, props } from '@ngrx/store';
import { CreateTaskPayload, ProjectTask, UpdateTaskPayload } from '../models/task.model';
import { FilterCriteria } from '../models/filter.model';

export const TasksActions = createActionGroup({
  source: 'Tasks',
  events: {
    // Load tasks cho một project
    'Load Tasks': props<{ projectId: string }>(),
    'Load Tasks Success': props<{ tasks: ProjectTask[] }>(),
    'Load Tasks Failure': props<{ error: string }>(),

    // Create
    'Create Task': props<{ projectId: string; request: CreateTaskPayload }>(),
    'Create Task Success': props<{ task: ProjectTask }>(),
    'Create Task Failure': props<{ error: string }>(),

    // Update
    'Update Task': props<{ projectId: string; taskId: string; request: UpdateTaskPayload; version: number }>(),
    'Update Task Success': props<{ task: ProjectTask }>(),
    'Update Task Failure': props<{ error: string }>(),
    'Update Task Conflict': props<{ serverState: ProjectTask; eTag: string }>(),

    // Delete
    'Delete Task': props<{ projectId: string; taskId: string; version: number }>(),
    'Delete Task Success': props<{ taskId: string }>(),
    'Delete Task Failure': props<{ error: string }>(),

    // Filter
    'Set Filter': props<{ criteria: FilterCriteria }>(),
    'Clear Filter': emptyProps(),
    'Clear One Criterion': props<{ key: keyof FilterCriteria }>(),

    // Misc
    'Clear Task Conflict': emptyProps(),
    'Select Task': props<{ taskId: string | null }>(),
    'Clear Tasks': emptyProps(),
  },
});
