import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CostBreakdownResult, CostSummaryResult, ExportJobDto } from '../models/cost-report.model';
import { MilestoneDto, ResourceHeatmapResult } from '../models/resource-report.model';

@Injectable({ providedIn: 'root' })
export class ReportingApiService {
  private readonly http = inject(HttpClient);

  getCostSummary(dateFrom: string, dateTo: string, projectId?: string): Observable<CostSummaryResult> {
    let params = new HttpParams().set('dateFrom', dateFrom).set('dateTo', dateTo);
    if (projectId) params = params.set('projectId', projectId);
    return this.http.get<CostSummaryResult>('/api/v1/reports/cost', { params });
  }

  getCostBreakdown(
    groupBy: string,
    month?: string,
    vendorId?: string,
    projectId?: string,
    resourceId?: string,
    page = 1,
    pageSize = 50,
  ): Observable<CostBreakdownResult> {
    let params = new HttpParams()
      .set('groupBy', groupBy)
      .set('page', page)
      .set('pageSize', pageSize);
    if (month)      params = params.set('month', month);
    if (vendorId)   params = params.set('vendorId', vendorId);
    if (projectId)  params = params.set('projectId', projectId);
    if (resourceId) params = params.set('resourceId', resourceId);
    return this.http.get<CostBreakdownResult>('/api/v1/reports/cost/breakdown', { params });
  }

  triggerExport(
    format: string,
    groupBy: string,
    month?: string,
    vendorId?: string,
    projectId?: string,
    resourceId?: string,
  ): Observable<{ jobId: string; status: string }> {
    return this.http.post<{ jobId: string; status: string }>(
      '/api/v1/reports/export-jobs',
      { format, groupBy, month: month ?? null, vendorId: vendorId ?? null, projectId: projectId ?? null, resourceId: resourceId ?? null },
    );
  }

  getExportJobStatus(jobId: string): Observable<ExportJobDto> {
    return this.http.get<ExportJobDto>(`/api/v1/reports/export-jobs/${jobId}`);
  }

  downloadExport(jobId: string): Observable<Blob> {
    return this.http.get(`/api/v1/reports/export-jobs/${jobId}/download`, { responseType: 'blob' });
  }

  getResourceHeatmap(from: string, to: string): Observable<ResourceHeatmapResult> {
    const params = new HttpParams().set('from', from).set('to', to);
    return this.http.get<ResourceHeatmapResult>('/api/v1/reports/resources', { params });
  }

  getMilestones(from?: string, to?: string): Observable<MilestoneDto[]> {
    let params = new HttpParams();
    if (from) params = params.set('from', from);
    if (to) params = params.set('to', to);
    return this.http.get<MilestoneDto[]>('/api/v1/reports/milestones', { params });
  }
}
