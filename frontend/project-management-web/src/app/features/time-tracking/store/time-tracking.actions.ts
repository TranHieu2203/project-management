import { createActionGroup, props } from '@ngrx/store';
import { TimeEntry } from '../models/time-entry.model';
import { BulkTimesheetRow, BulkValidationError } from '../models/bulk-timesheet.model';

export const TimeTrackingActions = createActionGroup({
  source: 'TimeTracking',
  events: {
    'Load Entries': props<{ projectId?: string; resourceId?: string }>(),
    'Load Entries Success': props<{ entries: TimeEntry[] }>(),
    'Load Entries Failure': props<{ error: string }>(),
    'Create Entry': props<{
      resourceId: string;
      projectId: string;
      taskId?: string;
      date: string;
      hours: number;
      entryType: string;
      role: string;
      level: string;
      note?: string;
      supersededEntryId?: string;
    }>(),
    'Create Entry Success': props<{ entry: TimeEntry }>(),
    'Create Entry Failure': props<{ error: string }>(),
    'Void Entry': props<{ entryId: string; reason: string }>(),
    'Void Entry Success': props<{ entry: TimeEntry }>(),
    'Void Entry Failure': props<{ error: string }>(),
    'Submit Bulk': props<{ rows: BulkTimesheetRow[] }>(),
    'Submit Bulk Success': props<{ entries: TimeEntry[] }>(),
    'Submit Bulk Failure': props<{ error: string; validationErrors?: BulkValidationError[] }>(),
  },
});
