import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { AsyncPipe, DecimalPipe, NgClass, NgFor, NgIf, SlicePipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Store } from '@ngrx/store';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ReportingActions } from '../../store/reporting.actions';
import { selectResourceHeatmap, selectHeatmapLoading } from '../../store/reporting.reducer';
import { ResourceHeatmapCell } from '../../models/resource-report.model';

@Component({
  selector: 'app-resource-report',
  standalone: true,
  imports: [
    AsyncPipe,
    DecimalPipe,
    NgIf,
    NgFor,
    NgClass,
    SlicePipe,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatProgressSpinnerModule,
    MatFormFieldModule,
    MatInputModule,
    MatTooltipModule,
  ],
  templateUrl: './resource-report.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ResourceReportComponent implements OnInit {
  private readonly store = inject(Store);
  private readonly fb = inject(FormBuilder);

  readonly heatmap$ = this.store.select(selectResourceHeatmap);
  readonly loading$ = this.store.select(selectHeatmapLoading);

  readonly form = this.fb.nonNullable.group({
    from: [this.defaultFrom()],
    to: [this.defaultTo()],
  });

  selectedCell: { resourceId: string; cell: ResourceHeatmapCell } | null = null;

  readonly trafficLightIcon: Record<string, string> = {
    Green: '●',
    Yellow: '▲',
    Orange: '◆',
    Red: '✕',
  };

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    if (this.form.invalid) return;
    const { from, to } = this.form.getRawValue();
    this.store.dispatch(ReportingActions.loadResourceHeatmap({ from, to }));
    this.selectedCell = null;
  }

  selectCell(resourceId: string, cell: ResourceHeatmapCell): void {
    this.selectedCell = { resourceId, cell };
  }

  cellClass(cell: ResourceHeatmapCell): string {
    return `cell-${cell.trafficLight.toLowerCase()}`;
  }

  cellTooltip(cell: ResourceHeatmapCell): string {
    return `${cell.actualHours.toFixed(1)}h / ${cell.availableHours.toFixed(0)}h (${cell.utilizationPct.toFixed(1)}%)`;
  }

  private defaultFrom(): string {
    const d = new Date();
    d.setDate(d.getDate() - 28);
    return d.toISOString().substring(0, 10);
  }

  private defaultTo(): string {
    const d = new Date();
    d.setDate(d.getDate() + 28);
    return d.toISOString().substring(0, 10);
  }
}
