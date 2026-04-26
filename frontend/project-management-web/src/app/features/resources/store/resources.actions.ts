import { createActionGroup, emptyProps, props } from '@ngrx/store';
import { Resource } from '../models/resource.model';

export const ResourcesActions = createActionGroup({
  source: 'Resources',
  events: {
    'Load Resources': props<{ resourceType?: string; vendorId?: string; activeOnly?: boolean }>(),
    'Load Resources Success': props<{ resources: Resource[] }>(),
    'Load Resources Failure': props<{ error: string }>(),
    'Select Resource': props<{ resourceId: string | null }>(),

    'Create Resource': props<{ code: string; name: string; email?: string; resourceType: string; vendorId?: string }>(),
    'Create Resource Success': props<{ resource: Resource }>(),
    'Create Resource Failure': props<{ error: string }>(),

    'Update Resource': props<{ resourceId: string; name: string; email?: string; version: number }>(),
    'Update Resource Success': props<{ resource: Resource }>(),
    'Update Resource Failure': props<{ error: string }>(),
    'Update Resource Conflict': props<{ serverState: Resource; eTag: string; pendingName: string; pendingEmail?: string }>(),

    'Inactivate Resource': props<{ resourceId: string; version: number }>(),
    'Inactivate Resource Success': props<{ resource: Resource }>(),
    'Inactivate Resource Failure': props<{ error: string }>(),

    'Clear Conflict': emptyProps(),
  },
});
