import { Injectable, inject } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { catchError, map, switchMap } from 'rxjs/operators';
import { of } from 'rxjs';
import { TimeTrackingApiService } from '../services/time-tracking-api.service';
import { TimeTrackingActions } from './time-tracking.actions';

@Injectable()
export class TimeTrackingEffects {
  private readonly actions$ = inject(Actions);
  private readonly api = inject(TimeTrackingApiService);

  loadEntries$ = createEffect(() =>
    this.actions$.pipe(
      ofType(TimeTrackingActions.loadEntries),
      switchMap(action =>
        this.api.getTimeEntries({
          projectId: action.projectId,
          resourceId: action.resourceId,
        }).pipe(
          map(result => TimeTrackingActions.loadEntriesSuccess({ entries: result.items })),
          catchError(err =>
            of(TimeTrackingActions.loadEntriesFailure({
              error: err?.error?.detail ?? err?.message ?? 'Lỗi không xác định',
            }))
          )
        )
      )
    )
  );

  createEntry$ = createEffect(() =>
    this.actions$.pipe(
      ofType(TimeTrackingActions.createEntry),
      switchMap(action =>
        this.api.createTimeEntry({
          resourceId: action.resourceId,
          projectId: action.projectId,
          taskId: action.taskId,
          date: action.date,
          hours: action.hours,
          entryType: action.entryType,
          role: action.role,
          level: action.level,
          note: action.note,
          supersedesEntryId: action.supersededEntryId,
        }).pipe(
          map(entry => TimeTrackingActions.createEntrySuccess({ entry })),
          catchError(err =>
            of(TimeTrackingActions.createEntryFailure({
              error: err?.error?.detail ?? err?.message ?? 'Lỗi không xác định',
            }))
          )
        )
      )
    )
  );

  voidEntry$ = createEffect(() =>
    this.actions$.pipe(
      ofType(TimeTrackingActions.voidEntry),
      switchMap(action =>
        this.api.voidTimeEntry(action.entryId, action.reason).pipe(
          map(entry => TimeTrackingActions.voidEntrySuccess({ entry })),
          catchError(err =>
            of(TimeTrackingActions.voidEntryFailure({
              error: err?.error?.detail ?? err?.message ?? 'Lỗi không xác định',
            }))
          )
        )
      )
    )
  );

  submitBulk$ = createEffect(() =>
    this.actions$.pipe(
      ofType(TimeTrackingActions.submitBulk),
      switchMap(action =>
        this.api.bulkCreateTimeEntries(action.rows).pipe(
          map(entries => TimeTrackingActions.submitBulkSuccess({ entries })),
          catchError(err =>
            of(TimeTrackingActions.submitBulkFailure({
              error: err?.error?.detail ?? err?.message ?? 'Lỗi không xác định',
              validationErrors: err?.error?.errors,
            }))
          )
        )
      )
    )
  );
}
