import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { AsyncPipe, DatePipe, NgFor, NgIf } from '@angular/common';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Store } from '@ngrx/store';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { ReportingActions } from '../../store/reporting.actions';
import { selectMilestones, selectMilestonesLoading } from '../../store/reporting.reducer';

@Component({
  selector: 'app-milestone-report',
  standalone: true,
  imports: [
    AsyncPipe,
    DatePipe,
    NgIf,
    NgFor,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatProgressSpinnerModule,
    MatFormFieldModule,
    MatInputModule,
  ],
  templateUrl: './milestone-report.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MilestoneReportComponent implements OnInit {
  private readonly store = inject(Store);
  private readonly fb = inject(FormBuilder);

  readonly milestones$ = this.store.select(selectMilestones);
  readonly loading$ = this.store.select(selectMilestonesLoading);

  readonly form = this.fb.nonNullable.group({
    from: [''],
    to: [''],
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    const { from, to } = this.form.getRawValue();
    this.store.dispatch(ReportingActions.loadMilestones({
      from: from || undefined,
      to: to || undefined,
    }));
  }
}
