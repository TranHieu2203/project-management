import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { AsyncPipe, DecimalPipe } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { Store } from '@ngrx/store';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { ReportsActions } from '../../../store/reports.actions';
import {
  selectBudgetReport,
  selectReportsFilters,
  selectReportsLoading,
  selectReportsError,
} from '../../../store/reports.selectors';
import { ReportsApiService } from '../../../services/reports-api.service';
import { BudgetFilterBarComponent } from '../budget-filter-bar/budget-filter-bar';
import { BudgetTableComponent } from '../budget-table/budget-table';
import { ReportsFilters } from '../../../models/budget-report.model';

@Component({
  standalone: true,
  selector: 'app-budget-report',
  imports: [
    AsyncPipe,
    DecimalPipe,
    MatButtonModule,
    MatIconModule,
    MatSnackBarModule,
    BudgetFilterBarComponent,
    BudgetTableComponent,
  ],
  templateUrl: './budget-report.html',
  styleUrl: './budget-report.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BudgetReportComponent {
  private readonly store = inject(Store);
  private readonly api = inject(ReportsApiService);
  private readonly snackBar = inject(MatSnackBar);

  readonly filters$ = this.store.select(selectReportsFilters);
  readonly report$ = this.store.select(selectBudgetReport);
  readonly loading$ = this.store.select(selectReportsLoading);
  readonly error$ = this.store.select(selectReportsError);

  readonly emptyReport = {
    month: '', workingDaysInMonth: 0,
    grandTotalPlanned: 0, grandTotalActual: 0, projects: [],
  };

  onFiltersChange(filters: ReportsFilters): void {
    this.store.dispatch(ReportsActions.setFilters({ filters }));
  }

  async onExportPdf(filters: ReportsFilters): Promise<void> {
    this.api.exportBudgetPdf(filters.month, filters.projectIds).subscribe({
      next: blob => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `budget-${filters.month}.pdf`;
        a.click();
        URL.revokeObjectURL(url);
      },
      error: () => this.snackBar.open('Không thể xuất PDF.', undefined, { duration: 3000 }),
    });
  }

  async onExportExcelClick(filters: ReportsFilters): Promise<void> {
    const report = await firstValueFrom(this.report$);
    if (!report) return;
    try {
      await this.api.exportBudgetExcel(report);
    } catch {
      this.snackBar.open('Không thể xuất Excel.', undefined, { duration: 3000 });
    }
  }
}
