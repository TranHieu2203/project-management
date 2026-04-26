import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { TimeEntry } from '../models/time-entry.model';
import { PagedResult } from '../models/paged-result.model';
import { BulkTimesheetRow, BulkValidationError } from '../models/bulk-timesheet.model';
import { ColumnMapping, ImportJobDto, ImportJobErrorDto } from '../models/import-job.model';
import { PeriodLockDto, PeriodReconcileDto } from '../models/period-lock.model';

export interface BulkCreateResponse {
  createdEntries?: TimeEntry[];
  errors?: BulkValidationError[];
}

export interface CreateTimeEntryRequest {
  resourceId: string;
  projectId: string;
  taskId?: string;
  date: string;
  hours: number;
  entryType: string;
  role: string;
  level: string;
  note?: string;
  supersedesEntryId?: string;
}

@Injectable({ providedIn: 'root' })
export class TimeTrackingApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/v1/time-entries';

  getTimeEntries(filter?: {
    dateFrom?: string;
    dateTo?: string;
    resourceId?: string;
    projectId?: string;
    entryType?: string;
    page?: number;
    pageSize?: number;
  }): Observable<PagedResult<TimeEntry>> {
    let params = new HttpParams();
    if (filter?.dateFrom) params = params.set('dateFrom', filter.dateFrom);
    if (filter?.dateTo) params = params.set('dateTo', filter.dateTo);
    if (filter?.resourceId) params = params.set('resourceId', filter.resourceId);
    if (filter?.projectId) params = params.set('projectId', filter.projectId);
    if (filter?.entryType) params = params.set('entryType', filter.entryType);
    if (filter?.page !== undefined) params = params.set('page', String(filter.page));
    if (filter?.pageSize !== undefined) params = params.set('pageSize', String(filter.pageSize));
    return this.http.get<PagedResult<TimeEntry>>(this.baseUrl, { params });
  }

  createTimeEntry(body: CreateTimeEntryRequest): Observable<TimeEntry> {
    return this.http.post<TimeEntry>(this.baseUrl, body);
  }

  getTimeEntryById(entryId: string): Observable<TimeEntry> {
    return this.http.get<TimeEntry>(`${this.baseUrl}/${entryId}`);
  }

  voidTimeEntry(entryId: string, reason: string): Observable<TimeEntry> {
    return this.http.post<TimeEntry>(`${this.baseUrl}/${entryId}/void`, { reason });
  }

  bulkCreateTimeEntries(rows: BulkTimesheetRow[]): Observable<TimeEntry[]> {
    return this.http.post<TimeEntry[]>(`${this.baseUrl}/bulk`, { rows });
  }

  startImportJob(file: File, vendorId: string, mapping: ColumnMapping): Observable<ImportJobDto> {
    const fd = new FormData();
    fd.append('file', file);
    fd.append('vendorId', vendorId);
    fd.append('resourceIdColumn', mapping.resourceIdColumn);
    fd.append('projectIdColumn', mapping.projectIdColumn);
    fd.append('dateColumn', mapping.dateColumn);
    fd.append('hoursColumn', mapping.hoursColumn);
    fd.append('roleColumn', mapping.roleColumn);
    fd.append('levelColumn', mapping.levelColumn);
    if (mapping.noteColumn) fd.append('noteColumn', mapping.noteColumn);
    if (mapping.taskIdColumn) fd.append('taskIdColumn', mapping.taskIdColumn);
    return this.http.post<ImportJobDto>('/api/v1/import-jobs', fd);
  }

  getImportJob(jobId: string): Observable<ImportJobDto> {
    return this.http.get<ImportJobDto>(`/api/v1/import-jobs/${jobId}`);
  }

  getImportJobErrors(jobId: string): Observable<ImportJobErrorDto[]> {
    return this.http.get<ImportJobErrorDto[]>(`/api/v1/import-jobs/${jobId}/errors`);
  }

  getPeriodReconcile(vendorId: string, year: number, month: number): Observable<PeriodReconcileDto> {
    const params = new HttpParams().set('vendorId', vendorId).set('year', String(year)).set('month', String(month));
    return this.http.get<PeriodReconcileDto>('/api/v1/period-locks/reconcile', { params });
  }

  getPeriodLocks(vendorId: string): Observable<PeriodLockDto[]> {
    return this.http.get<PeriodLockDto[]>('/api/v1/period-locks', { params: new HttpParams().set('vendorId', vendorId) });
  }

  lockPeriod(vendorId: string, year: number, month: number): Observable<PeriodLockDto> {
    return this.http.post<PeriodLockDto>('/api/v1/period-locks', { vendorId, year, month });
  }

  unlockPeriod(vendorId: string, year: number, month: number): Observable<void> {
    return this.http.delete<void>(`/api/v1/period-locks/${vendorId}/${year}/${month}`);
  }

  applyImportJob(jobId: string, mapping: ColumnMapping): Observable<ImportJobDto> {
    return this.http.post<ImportJobDto>(`/api/v1/import-jobs/${jobId}/apply`, {
      resourceIdColumn: mapping.resourceIdColumn,
      projectIdColumn: mapping.projectIdColumn,
      dateColumn: mapping.dateColumn,
      hoursColumn: mapping.hoursColumn,
      roleColumn: mapping.roleColumn,
      levelColumn: mapping.levelColumn,
      noteColumn: mapping.noteColumn ?? null,
      taskIdColumn: mapping.taskIdColumn ?? null,
    });
  }
}
