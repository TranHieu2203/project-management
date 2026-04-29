import { createActionGroup, emptyProps, props } from '@ngrx/store';
import { AlertDto } from '../models/alert.model';

export const AlertActions = createActionGroup({
  source: 'Alerts',
  events: {
    'Load Alerts': emptyProps(),
    'Load Alerts Success': props<{ alerts: AlertDto[] }>(),
    'Load Alerts Failure': props<{ error: string }>(),
    'Mark Alert Read': props<{ id: string; projectId: string | null; entityType: string | null; entityId: string | null }>(),
    'Mark Alert Read Success': props<{ id: string }>(),
    'Mark Alert Read Failure': props<{ error: string }>(),
    'Toggle Panel': emptyProps(),
    'Close Panel': emptyProps(),
  },
});
