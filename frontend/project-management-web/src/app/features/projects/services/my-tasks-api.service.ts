import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface MyTask {
  id: string;
  projectId: string;
  projectName: string;
  projectCode: string;
  parentId: string | null;
  type: 'Phase' | 'Milestone' | 'Task';
  vbs: string | null;
  name: string;
  priority: 'Low' | 'Medium' | 'High' | 'Critical';
  status: 'NotStarted' | 'InProgress' | 'OnHold' | 'Delayed' | 'Completed';
  plannedEndDate: string | null;
  percentComplete: number | null;
  assigneeUserId: string | null;
  sortOrder: number;
  version: number;
}

@Injectable({ providedIn: 'root' })
export class MyTasksApiService {
  private readonly http = inject(HttpClient);

  getMyTasks(params: { overdue?: boolean; q?: string } = {}): Observable<MyTask[]> {
    const query: Record<string, string> = {};
    if (params.overdue) query['overdue'] = 'true';
    if (params.q) query['q'] = params.q;
    return this.http.get<MyTask[]>('/api/v1/my-tasks', { params: query });
  }
}
