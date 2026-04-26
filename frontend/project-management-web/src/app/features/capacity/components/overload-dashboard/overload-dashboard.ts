import { ChangeDetectionStrategy, Component, OnDestroy, inject } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { AsyncPipe, DecimalPipe, NgClass, NgIf } from '@angular/common';
import { Store } from '@ngrx/store';
import { take } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { CapacityActions } from '../../store/capacity.actions';
import {
  selectCapacityLoading, selectCapacityError, selectOverloadResult, selectLastUpdated,
  selectUtilization, selectUtilizationLoading,
} from '../../store/capacity.reducer';
import { OverloadWarningBannerComponent } from '../overload-warning-banner/overload-warning-banner';
import { TrafficLightWidgetComponent } from '../traffic-light-widget/traffic-light-widget';
import { MetricsApiService } from '../../../../core/services/metrics-api.service';
import { MetricEventType } from '../../../../core/models/metric-event-type';

@Component({
  selector: 'app-overload-dashboard',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    AsyncPipe,
    DecimalPipe,
    NgIf,
    NgClass,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatTableModule,
    OverloadWarningBannerComponent,
    TrafficLightWidgetComponent,
  ],
  templateUrl: './overload-dashboard.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OverloadDashboardComponent implements OnDestroy {
  private readonly store = inject(Store);
  private readonly fb = inject(FormBuilder);
  private readonly metricsApi = inject(MetricsApiService);

  readonly result$ = this.store.select(selectOverloadResult);
  readonly loading$ = this.store.select(selectCapacityLoading);
  readonly error$ = this.store.select(selectCapacityError);
  readonly lastUpdated$ = this.store.select(selectLastUpdated);
  readonly utilization$ = this.store.select(selectUtilization);
  readonly utilizationLoading$ = this.store.select(selectUtilizationLoading);

  readonly form = this.fb.nonNullable.group({
    resourceId: ['', Validators.required],
    dateFrom: ['', Validators.required],
    dateTo: ['', Validators.required],
  });

  readonly dayColumns = ['date', 'hours', 'status'];
  readonly weekColumns = ['weekStart', 'totalHours', 'status'];

  startPolling(): void {
    if (this.form.invalid) return;
    const { resourceId, dateFrom, dateTo } = this.form.getRawValue();
    this.store.dispatch(CapacityActions.startPolling({ resourceId, dateFrom, dateTo }));
    this.store.dispatch(CapacityActions.loadUtilization({ resourceId, dateFrom, dateTo }));
  }

  retry(): void {
    if (this.form.invalid) return;
    const { resourceId, dateFrom, dateTo } = this.form.getRawValue();
    this.store.dispatch(CapacityActions.loadOverload({ resourceId, dateFrom, dateTo }));
  }

  onOverride(): void {
    const { resourceId, dateFrom, dateTo } = this.form.getRawValue();
    this.utilization$.pipe(take(1)).subscribe(u => {
      if (!u) return;
      this.store.dispatch(CapacityActions.logOverride({
        request: { resourceId, dateFrom, dateTo, trafficLight: u.trafficLight },
      }));
      this.metricsApi.recordEvent(MetricEventType.PredictiveOverride, {
        resourceId,
        dateFrom,
        dateTo,
        trafficLight: u.trafficLight,
      });
    });
  }

  ngOnDestroy(): void {
    this.store.dispatch(CapacityActions.stopPolling());
  }
}
