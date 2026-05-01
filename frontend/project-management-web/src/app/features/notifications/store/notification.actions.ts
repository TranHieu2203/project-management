import { createActionGroup, emptyProps, props } from '@ngrx/store';
import { NotificationDto } from '../models/notification.model';

export type NotificationFilter = 'all' | 'assigned' | 'commented' | 'status-changed' | 'mentioned';

export const NotificationActions = createActionGroup({
  source: 'Notifications',
  events: {
    'Load Notifications': emptyProps(),
    'Load Notifications Success': props<{ notifications: NotificationDto[] }>(),
    'Load Notifications Failure': props<{ error: string }>(),
    'Mark Read': props<{ id: string; projectId: string | null; entityId: string | null }>(),
    'Mark Read Success': props<{ id: string }>(),
    'Mark All Read': emptyProps(),
    'Mark All Read Success': emptyProps(),
    'Toggle Panel': emptyProps(),
    'Close Panel': emptyProps(),
    'Set Filter': props<{ filter: NotificationFilter }>(),
  },
});
