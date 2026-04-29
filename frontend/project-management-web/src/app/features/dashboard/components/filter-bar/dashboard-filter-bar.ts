import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatChipsModule } from '@angular/material/chips';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { DEFAULT_FILTERS, DashboardFilters, ProjectSummary } from '../../models/dashboard.model';

@Component({
  standalone: true,
  selector: 'app-dashboard-filter-bar',
  imports: [
    CommonModule,
    MatSelectModule,
    MatFormFieldModule,
    MatInputModule,
    MatChipsModule,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule,
  ],
  templateUrl: './dashboard-filter-bar.html',
  styleUrl: './dashboard-filter-bar.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardFilterBarComponent {
  @Input({ required: true }) projects: ProjectSummary[] = [];
  @Input() filters: DashboardFilters = DEFAULT_FILTERS;
  @Output() filtersChange = new EventEmitter<DashboardFilters>();

  readonly QUICK_CHIPS = [
    { key: 'overdue', label: 'Quá hạn' },
    { key: 'atRisk', label: 'Nguy cơ' },
    { key: 'overloaded', label: 'Quá tải' },
  ];

  get hasActiveFilters(): boolean {
    return this.filters.selectedProjectIds.length > 0
      || this.filters.dateRange !== null
      || this.filters.quickChips.length > 0;
  }

  isChipActive(chip: string): boolean {
    return this.filters.quickChips.includes(chip);
  }

  onProjectChange(projectIds: string[]): void {
    this.filtersChange.emit({ ...this.filters, selectedProjectIds: projectIds });
  }

  onFromDateChange(event: Event): void {
    const value = (event.target as HTMLInputElement).value || null;
    const dateRange = value
      ? { start: value, end: this.filters.dateRange?.end ?? value }
      : null;
    this.filtersChange.emit({ ...this.filters, dateRange });
  }

  onToDateChange(event: Event): void {
    if (!this.filters.dateRange) return;
    const value = (event.target as HTMLInputElement).value || null;
    const dateRange = value ? { ...this.filters.dateRange, end: value } : null;
    this.filtersChange.emit({ ...this.filters, dateRange });
  }

  onChipToggle(chip: string): void {
    const chips = this.isChipActive(chip)
      ? this.filters.quickChips.filter(c => c !== chip)
      : [...this.filters.quickChips, chip];
    this.filtersChange.emit({ ...this.filters, quickChips: chips });
  }

  onClearAll(): void {
    this.filtersChange.emit({ ...DEFAULT_FILTERS });
  }
}
