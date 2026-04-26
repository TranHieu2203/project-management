import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { AsyncPipe, DecimalPipe } from '@angular/common';
import { Store } from '@ngrx/store';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { TimeTrackingActions } from '../../store/time-tracking.actions';
import { selectAllEntries, selectTimeTrackingLoading } from '../../store/time-tracking.selectors';
import { TimeEntryFormComponent, TimeEntryFormData } from '../time-entry-form/time-entry-form';
import { TimeEntry } from '../../models/time-entry.model';

@Component({
  selector: 'app-time-entry-list',
  standalone: true,
  imports: [
    AsyncPipe,
    DecimalPipe,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTableModule,
    MatTooltipModule,
  ],
  templateUrl: './time-entry-list.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TimeEntryListComponent implements OnInit {
  private readonly store = inject(Store);
  private readonly dialog = inject(MatDialog);

  readonly entries$ = this.store.select(selectAllEntries);
  readonly loading$ = this.store.select(selectTimeTrackingLoading);

  readonly displayedColumns = ['date', 'resourceId', 'projectId', 'hours', 'entryType', 'rateAtTime', 'costAtTime', 'enteredBy', 'actions'];

  ngOnInit(): void {
    this.store.dispatch(TimeTrackingActions.loadEntries({}));
  }

  openCreateDialog(): void {
    const ref = this.dialog.open<TimeEntryFormComponent, TimeEntryFormData>(TimeEntryFormComponent, {
      width: '540px',
      data: {},
    });
    ref.afterClosed().subscribe(result => {
      if (result) {
        this.store.dispatch(TimeTrackingActions.createEntry(result));
      }
    });
  }

  voidEntry(entry: TimeEntry): void {
    const reason = prompt(`Lý do void entry ngày ${entry.date} (${entry.hours}h):`);
    if (reason && reason.trim()) {
      this.store.dispatch(TimeTrackingActions.voidEntry({ entryId: entry.id, reason: reason.trim() }));
    }
  }
}
