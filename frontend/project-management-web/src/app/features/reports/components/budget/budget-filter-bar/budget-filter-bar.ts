import {
  ChangeDetectionStrategy,
  Component,
  EventEmitter,
  Input,
  Output,
  inject,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { FeedbackDialogService } from '../../../../../shared/services/feedback-dialog.service';
import { ReportsFilters } from '../../../models/budget-report.model';

@Component({
  standalone: true,
  selector: 'app-budget-filter-bar',
  imports: [
    FormsModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
  ],
  templateUrl: './budget-filter-bar.html',
  styleUrl: './budget-filter-bar.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BudgetFilterBarComponent {
  private readonly feedbackDialog = inject(FeedbackDialogService);

  @Input() filters!: ReportsFilters;
  @Output() filtersChange = new EventEmitter<ReportsFilters>();
  @Output() exportPdf = new EventEmitter<void>();
  @Output() exportExcel = new EventEmitter<void>();

  onMonthChange(month: string): void {
    this.filtersChange.emit({ ...this.filters, month });
  }

  copyLink(): void {
    navigator.clipboard.writeText(window.location.href).then(() => {
      this.feedbackDialog.success('Đã sao chép liên kết!');
    });
  }
}
