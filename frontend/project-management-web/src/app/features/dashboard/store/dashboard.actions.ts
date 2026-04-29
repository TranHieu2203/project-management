import { createActionGroup, emptyProps, props } from '@ngrx/store';
import { DashboardFilters, Deadline, ProjectSummary, StatCards } from '../models/dashboard.model';

export const DashboardActions = createActionGroup({
  source: 'Dashboard',
  events: {
    'Start Polling': emptyProps(),
    'Stop Polling': emptyProps(),
    'Load Portfolio': emptyProps(),
    'Load Summary Success': props<{ data: ProjectSummary[] }>(),
    'Load Summary Failure': props<{ error: string }>(),
    'Load Stat Cards Success': props<{ data: StatCards }>(),
    'Load Stat Cards Failure': props<{ error: string }>(),
    'Load Deadlines Success': props<{ data: Deadline[] }>(),
    'Load Deadlines Failure': props<{ error: string }>(),
    'Set Filters': props<{ filters: DashboardFilters }>(),
    'Clear Filters': emptyProps(),
  },
});
