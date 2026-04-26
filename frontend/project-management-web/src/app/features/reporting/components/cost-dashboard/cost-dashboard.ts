import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { AsyncPipe, DecimalPipe, NgFor, NgIf } from '@angular/common';
import { Store } from '@ngrx/store';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { ReportingActions } from '../../store/reporting.actions';
import { selectCostSummary, selectReportingLoading } from '../../store/reporting.reducer';

@Component({
  selector: 'app-cost-dashboard',
  standalone: true,
  imports: [
    AsyncPipe,
    DecimalPipe,
    NgIf,
    NgFor,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatProgressSpinnerModule,
    MatInputModule,
    MatFormFieldModule,
  ],
  templateUrl: './cost-dashboard.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CostDashboardComponent implements OnInit {
  private readonly store = inject(Store);
  private readonly fb = inject(FormBuilder);

  readonly costSummary$ = this.store.select(selectCostSummary);
  readonly loading$ = this.store.select(selectReportingLoading);

  readonly form = this.fb.nonNullable.group({
    dateFrom: [this.defaultDateFrom()],
    dateTo: [this.defaultDateTo()],
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    const { dateFrom, dateTo } = this.form.getRawValue();
    this.store.dispatch(ReportingActions.loadCostSummary({ dateFrom, dateTo }));
  }

  private defaultDateFrom(): string {
    const d = new Date();
    d.setMonth(d.getMonth() - 1);
    return d.toISOString().substring(0, 10);
  }

  private defaultDateTo(): string {
    return new Date().toISOString().substring(0, 10);
  }
}
