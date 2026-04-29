import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Deadline, ProjectSummary, StatCards } from '../models/dashboard.model';

export interface DashboardMyTaskDto {
  id: string;
  projectId: string;
  projectName: string;
  projectCode: string | null;
  vbs: string | null;
  name: string;
  status: string;
  priority: string;
  plannedEndDate: string | null;
  percentComplete: number | null;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

@Injectable({ providedIn: 'root' })
export class DashboardApiService {
  private readonly http = inject(HttpClient);

  getSummary(projectIds?: string[]): Observable<ProjectSummary[]> {
    let params = new HttpParams();
    if (projectIds?.length) {
      projectIds.forEach(id => { params = params.append('projectIds', id); });
    }
    return this.http.get<ProjectSummary[]>('/api/v1/dashboard/summary', { params });
  }

  getStatCards(projectIds?: string[]): Observable<StatCards> {
    let params = new HttpParams();
    if (projectIds?.length) {
      projectIds.forEach(id => { params = params.append('projectIds', id); });
    }
    return this.http.get<StatCards>('/api/v1/dashboard/stat-cards', { params });
  }

  getDeadlines(daysAhead = 7, projectIds?: string[]): Observable<Deadline[]> {
    let params = new HttpParams().set('daysAhead', String(daysAhead));
    if (projectIds?.length) {
      projectIds.forEach(id => { params = params.append('projectIds', id); });
    }
    return this.http.get<Deadline[]>('/api/v1/dashboard/deadlines', { params });
  }

  getMyTasks(p: {
    page?: number;
    pageSize?: number;
    status?: string | null;
    projectIds?: string[];
  }): Observable<PagedResult<DashboardMyTaskDto>> {
    let params = new HttpParams()
      .set('page', String(p.page ?? 1))
      .set('pageSize', String(p.pageSize ?? 20));
    if (p.status) params = params.set('status', p.status);
    if (p.projectIds?.length) {
      p.projectIds.forEach(id => { params = params.append('projectIds', id); });
    }
    return this.http.get<PagedResult<DashboardMyTaskDto>>(
      '/api/v1/dashboard/my-tasks', { params }
    );
  }
}
