import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ResourceOverloadResult } from '../models/overload.model';
import { CapacityHeatmapResult, CapacityUtilizationResult, CrossProjectOverloadResult, ForecastComputeResult, ForecastDeltaResult, ForecastResult, LogOverrideRequest } from '../models/utilization.model';

@Injectable({ providedIn: 'root' })
export class CapacityApiService {
  private readonly http = inject(HttpClient);

  getResourceOverload(resourceId: string, dateFrom: string, dateTo: string): Observable<ResourceOverloadResult> {
    const params = new HttpParams()
      .set('resourceId', resourceId)
      .set('dateFrom', dateFrom)
      .set('dateTo', dateTo);
    return this.http.get<ResourceOverloadResult>('/api/v1/capacity/overload', { params });
  }

  getCapacityUtilization(resourceId: string, dateFrom: string, dateTo: string): Observable<CapacityUtilizationResult> {
    const params = new HttpParams()
      .set('resourceId', resourceId)
      .set('dateFrom', dateFrom)
      .set('dateTo', dateTo);
    return this.http.get<CapacityUtilizationResult>('/api/v1/capacity/utilization', { params });
  }

  logCapacityOverride(request: LogOverrideRequest): Observable<void> {
    return this.http.post<void>('/api/v1/capacity/overrides', request);
  }

  getCrossProjectOverload(dateFrom: string, dateTo: string): Observable<CrossProjectOverloadResult> {
    const params = new HttpParams().set('dateFrom', dateFrom).set('dateTo', dateTo);
    return this.http.get<CrossProjectOverloadResult>('/api/v1/capacity/cross-project', { params });
  }

  getCapacityHeatmap(dateFrom: string, dateTo: string): Observable<CapacityHeatmapResult> {
    const params = new HttpParams().set('dateFrom', dateFrom).set('dateTo', dateTo);
    return this.http.get<CapacityHeatmapResult>('/api/v1/capacity/heatmap', { params });
  }

  triggerForecastCompute(): Observable<ForecastComputeResult> {
    return this.http.post<ForecastComputeResult>('/api/v1/capacity/forecast/compute', {});
  }

  getLatestForecast(): Observable<ForecastResult> {
    return this.http.get<ForecastResult>('/api/v1/capacity/forecast');
  }

  getForecastDelta(): Observable<ForecastDeltaResult> {
    return this.http.get<ForecastDeltaResult>('/api/v1/capacity/forecast/delta');
  }
}
