import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { AsyncPipe, DecimalPipe, NgFor, NgIf } from '@angular/common';
import { Store } from '@ngrx/store';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { ReportingActions } from '../../store/reporting.actions';
import { selectBreakdownLoading, selectCostBreakdown } from '../../store/reporting.reducer';

@Component({
  selector: 'app-cost-breakdown',
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
    MatSelectModule,
  ],
  templateUrl: './cost-breakdown.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CostBreakdownComponent implements OnInit {
  private readonly store = inject(Store);
  private readonly fb = inject(FormBuilder);

  readonly breakdown$ = this.store.select(selectCostBreakdown);
  readonly loading$ = this.store.select(selectBreakdownLoading);

  readonly dimensions = [
    { value: 'vendor',   label: 'Theo Vendor' },
    { value: 'project',  label: 'Theo Project' },
    { value: 'resource', label: 'Theo Nhân sự' },
    { value: 'month',    label: 'Theo Tháng' },
  ];

  readonly form = this.fb.nonNullable.group({
    groupBy: ['vendor'],
    month: [''],
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    const { groupBy, month } = this.form.getRawValue();
    this.store.dispatch(ReportingActions.loadCostBreakdown({
      groupBy,
      month: month || undefined,
    }));
  }

  loadPage(page: number): void {
    const { groupBy, month } = this.form.getRawValue();
    this.store.dispatch(ReportingActions.loadCostBreakdown({
      groupBy,
      month: month || undefined,
      page,
    }));
  }

  totalPages(breakdown: { totalCount: number; pageSize: number }): number {
    return Math.ceil(breakdown.totalCount / breakdown.pageSize);
  }
}
