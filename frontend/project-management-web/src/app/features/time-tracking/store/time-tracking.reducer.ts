import { createEntityAdapter, EntityState } from '@ngrx/entity';
import { createReducer, on } from '@ngrx/store';
import { TimeEntry } from '../models/time-entry.model';
import { TimeTrackingActions } from './time-tracking.actions';

export interface TimeTrackingState extends EntityState<TimeEntry> {
  loading: boolean;
  error: string | null;
}

const adapter = createEntityAdapter<TimeEntry>();

const initialState: TimeTrackingState = adapter.getInitialState({
  loading: false,
  error: null,
});

export const timeTrackingReducer = createReducer(
  initialState,
  on(TimeTrackingActions.loadEntries, state => ({ ...state, loading: true, error: null })),
  on(TimeTrackingActions.loadEntriesSuccess, (state, { entries }) =>
    adapter.setAll(entries, { ...state, loading: false })),
  on(TimeTrackingActions.loadEntriesFailure, (state, { error }) =>
    ({ ...state, loading: false, error })),

  on(TimeTrackingActions.createEntry, state => ({ ...state, loading: true, error: null })),
  on(TimeTrackingActions.createEntrySuccess, (state, { entry }) =>
    adapter.addOne(entry, { ...state, loading: false })),
  on(TimeTrackingActions.createEntryFailure, (state, { error }) =>
    ({ ...state, loading: false, error })),

  on(TimeTrackingActions.voidEntry, state => ({ ...state, loading: true, error: null })),
  on(TimeTrackingActions.voidEntrySuccess, (state, { entry }) =>
    adapter.updateOne({ id: entry.id, changes: entry }, { ...state, loading: false })),
  on(TimeTrackingActions.voidEntryFailure, (state, { error }) =>
    ({ ...state, loading: false, error })),

  on(TimeTrackingActions.submitBulk, state => ({ ...state, loading: true, error: null })),
  on(TimeTrackingActions.submitBulkSuccess, (state, { entries }) =>
    adapter.addMany(entries, { ...state, loading: false })),
  on(TimeTrackingActions.submitBulkFailure, (state, { error }) =>
    ({ ...state, loading: false, error })),
);

export const { selectAll: selectAllTimeEntries, selectEntities } = adapter.getSelectors();
