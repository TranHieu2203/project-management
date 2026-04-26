import { createActionGroup, emptyProps, props } from '@ngrx/store';
import { Project } from '../models/project.model';

export const ProjectsActions = createActionGroup({
  source: 'Projects',
  events: {
    // Load
    'Load Projects': emptyProps(),
    'Load Projects Success': props<{ projects: Project[] }>(),
    'Load Projects Failure': props<{ error: string }>(),
    'Select Project': props<{ projectId: string }>(),

    // Create
    'Create Project': props<{ code: string; name: string; description?: string }>(),
    'Create Project Success': props<{ project: Project }>(),
    'Create Project Failure': props<{ error: string }>(),

    // Update
    'Update Project': props<{ projectId: string; name: string; description?: string; version: number }>(),
    'Update Project Success': props<{ project: Project }>(),
    'Update Project Failure': props<{ error: string }>(),
    'Update Project Conflict': props<{ serverState: Project; eTag: string; pendingName: string; pendingDescription?: string }>(),

    // Delete
    'Delete Project': props<{ projectId: string; version: number }>(),
    'Delete Project Success': props<{ projectId: string }>(),
    'Delete Project Failure': props<{ error: string }>(),

    // Conflict resolved
    'Clear Conflict': emptyProps(),
  },
});
