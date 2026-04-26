import { createFeatureSelector, createSelector } from '@ngrx/store';
import { TimeTrackingState, selectAllTimeEntries } from './time-tracking.reducer';

const selectTimeTrackingState = createFeatureSelector<TimeTrackingState>('timeTracking');

export const selectAllEntries = createSelector(selectTimeTrackingState, selectAllTimeEntries);
export const selectTimeTrackingLoading = createSelector(
  selectTimeTrackingState, s => s.loading);
export const selectTimeTrackingError = createSelector(
  selectTimeTrackingState, s => s.error);
