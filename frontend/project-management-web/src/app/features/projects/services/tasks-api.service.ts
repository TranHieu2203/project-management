import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CreateTaskPayload, ProjectTask, UpdateTaskPayload } from '../models/task.model';

@Injectable({ providedIn: 'root' })
export class TasksApiService {
  private readonly http = inject(HttpClient);

  private url(projectId: string): string {
    return `/api/v1/projects/${projectId}/tasks`;
  }

  getTasks(projectId: string): Observable<ProjectTask[]> {
    return this.http.get<ProjectTask[]>(this.url(projectId));
  }

  getTask(projectId: string, taskId: string): Observable<ProjectTask> {
    return this.http.get<ProjectTask>(`${this.url(projectId)}/${taskId}`);
  }

  createTask(projectId: string, request: CreateTaskPayload): Observable<ProjectTask> {
    return this.http.post<ProjectTask>(this.url(projectId), request);
  }

  updateTask(
    projectId: string,
    taskId: string,
    request: UpdateTaskPayload,
    version: number
  ): Observable<ProjectTask> {
    const headers = new HttpHeaders({ 'If-Match': `"${version}"` });
    return this.http.put<ProjectTask>(
      `${this.url(projectId)}/${taskId}`, request, { headers }
    );
  }

  deleteTask(projectId: string, taskId: string, version: number): Observable<void> {
    const headers = new HttpHeaders({ 'If-Match': `"${version}"` });
    return this.http.delete<void>(
      `${this.url(projectId)}/${taskId}`, { headers }
    );
  }
}
