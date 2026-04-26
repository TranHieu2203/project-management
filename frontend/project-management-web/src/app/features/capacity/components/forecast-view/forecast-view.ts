import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { AsyncPipe, DatePipe, DecimalPipe, NgClass, NgFor, NgIf } from '@angular/common';
import { Store } from '@ngrx/store';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { CapacityActions } from '../../store/capacity.actions';
import { selectForecast, selectForecastComputing, selectForecastDelta, selectForecastDeltaLoading, selectForecastLoading } from '../../store/capacity.reducer';
import { ForecastDeltaItem, ForecastWeekCell } from '../../models/utilization.model';
import { MetricsApiService } from '../../../../core/services/metrics-api.service';
import { MetricEventType } from '../../../../core/models/metric-event-type';

@Component({
  selector: 'app-forecast-view',
  standalone: true,
  imports: [
    AsyncPipe,
    DatePipe,
    DecimalPipe,
    NgIf,
    NgFor,
    NgClass,
    MatButtonModule,
    MatCardModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
  ],
  templateUrl: './forecast-view.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ForecastViewComponent implements OnInit {
  private readonly store = inject(Store);
  private readonly metricsApi = inject(MetricsApiService);

  readonly forecast$ = this.store.select(selectForecast);
  readonly loading$ = this.store.select(selectForecastLoading);
  readonly computing$ = this.store.select(selectForecastComputing);
  readonly forecastDelta$ = this.store.select(selectForecastDelta);
  readonly forecastDeltaLoading$ = this.store.select(selectForecastDeltaLoading);

  readonly trafficLightIcon: Record<string, string> = {
    Green: '●', Yellow: '▲', Orange: '◆', Red: '✕',
  };

  ngOnInit(): void {
    this.store.dispatch(CapacityActions.loadForecast());
    this.store.dispatch(CapacityActions.loadForecastDelta());
  }

  compute(): void {
    this.store.dispatch(CapacityActions.triggerForecast());
  }

  cellClass(cell: ForecastWeekCell): string {
    return `cell-${cell.trafficLight.toLowerCase()}`;
  }

  cellTooltip(cell: ForecastWeekCell): string {
    return `Dự báo: ${cell.forecastedHours.toFixed(1)}h / ${cell.availableHours.toFixed(0)}h (${cell.forecastedUtilizationPct.toFixed(1)}%)`;
  }

  deltaSign(delta: number): string {
    return delta > 0 ? '+' : '';
  }

  deltaClass(item: ForecastDeltaItem): string {
    if (item.deltaPct > 10) return 'delta-up';
    if (item.deltaPct < -10) return 'delta-down';
    return 'delta-neutral';
  }

  onDeltaAction(item: ForecastDeltaItem): void {
    this.metricsApi.recordEvent(MetricEventType.ForecastProactive, {
      resourceId: item.resourceId,
      weekStart: item.weekStart,
      trafficLight: item.currentTrafficLight,
      deltaPct: item.deltaPct,
      hint: item.hint,
    });
  }
}
