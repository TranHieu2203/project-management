import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { AsyncPipe, DecimalPipe, NgClass, NgFor, NgIf } from '@angular/common';
import { Store } from '@ngrx/store';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { CapacityActions } from '../../store/capacity.actions';
import { selectHeatmap, selectHeatmapLoading } from '../../store/capacity.reducer';
import { HeatmapCell } from '../../models/utilization.model';

@Component({
  selector: 'app-capacity-heatmap',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    AsyncPipe,
    DecimalPipe,
    NgIf,
    NgFor,
    NgClass,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
  ],
  templateUrl: './capacity-heatmap.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CapacityHeatmapComponent {
  private readonly store = inject(Store);
  private readonly fb = inject(FormBuilder);

  readonly heatmap$ = this.store.select(selectHeatmap);
  readonly loading$ = this.store.select(selectHeatmapLoading);

  readonly form = this.fb.nonNullable.group({
    dateFrom: ['', Validators.required],
    dateTo: ['', Validators.required],
  });

  selectedCell: { resourceId: string; cell: HeatmapCell } | null = null;

  readonly trafficLightIcon: Record<string, string> = {
    Green: '●',
    Yellow: '▲',
    Orange: '◆',
    Red: '✕',
  };

  load(): void {
    if (this.form.invalid) return;
    const { dateFrom, dateTo } = this.form.getRawValue();
    this.store.dispatch(CapacityActions.loadHeatmap({ dateFrom, dateTo }));
    this.selectedCell = null;
  }

  selectCell(resourceId: string, cell: HeatmapCell): void {
    this.selectedCell = { resourceId, cell };
  }

  cellTooltip(cell: HeatmapCell): string {
    return `${cell.actualHours.toFixed(1)}h / ${cell.availableHours.toFixed(0)}h available (${cell.utilizationPct.toFixed(1)}%)`;
  }

  cellClass(cell: HeatmapCell): string {
    return `cell-${cell.trafficLight.toLowerCase()}`;
  }
}
