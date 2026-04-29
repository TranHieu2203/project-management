import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { DecimalPipe, NgClass } from '@angular/common';
import { MatTooltipModule } from '@angular/material/tooltip';
import { BudgetReport } from '../../../models/budget-report.model';

@Component({
  standalone: true,
  selector: 'app-budget-table',
  imports: [DecimalPipe, NgClass, MatTooltipModule],
  templateUrl: './budget-table.html',
  styleUrl: './budget-table.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BudgetTableComponent {
  @Input() report!: BudgetReport;
  @Input() loading = false;

  formatCost(n: number): string {
    return n.toLocaleString('vi-VN');
  }
}
