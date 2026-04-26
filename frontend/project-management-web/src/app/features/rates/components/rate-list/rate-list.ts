import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { AsyncPipe, DecimalPipe } from '@angular/common';
import { Store } from '@ngrx/store';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { RatesActions } from '../../store/rates.actions';
import { selectAllRates, selectRatesLoading } from '../../store/rates.selectors';
import { RateFormComponent, RateFormData } from '../rate-form/rate-form';
import { MonthlyRate } from '../../models/monthly-rate.model';

@Component({
  selector: 'app-rate-list',
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
  templateUrl: './rate-list.html',
  styleUrl: './rate-list.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RateListComponent implements OnInit {
  private readonly store = inject(Store);
  private readonly dialog = inject(MatDialog);

  readonly rates$ = this.store.select(selectAllRates);
  readonly loading$ = this.store.select(selectRatesLoading);

  readonly displayedColumns = ['vendor', 'role', 'level', 'period', 'monthlyAmount', 'hourlyRate', 'actions'];

  ngOnInit(): void {
    this.store.dispatch(RatesActions.loadRates({}));
  }

  openCreateDialog(): void {
    const ref = this.dialog.open<RateFormComponent, RateFormData>(RateFormComponent, {
      width: '540px',
      data: {},
    });
    ref.afterClosed().subscribe(result => {
      if (result) {
        this.store.dispatch(RatesActions.createRate({
          vendorId: result.vendorId,
          role: result.role,
          level: result.level,
          year: result.year,
          month: result.month,
          monthlyAmount: result.monthlyAmount,
        }));
      }
    });
  }

  deleteRate(rate: MonthlyRate): void {
    if (!confirm(`Xóa rate ${rate.role}/${rate.level} tháng ${rate.month}/${rate.year}?`)) return;
    this.store.dispatch(RatesActions.deleteRate({ rateId: rate.id }));
  }

  formatPeriod(rate: MonthlyRate): string {
    return `${String(rate.month).padStart(2, '0')}/${rate.year}`;
  }
}
