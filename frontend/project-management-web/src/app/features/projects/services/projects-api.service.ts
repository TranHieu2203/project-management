import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Project } from '../models/project.model';

@Injectable({ providedIn: 'root' })
export class ProjectsApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/v1/projects';

  getProjects(): Observable<Project[]> {
    return this.http.get<Project[]>(this.baseUrl);
  }

  getProjectById(projectId: string): Observable<Project> {
    return this.http.get<Project>(`${this.baseUrl}/${projectId}`);
  }

  createProject(code: string, name: string, description?: string): Observable<Project> {
    return this.http.post<Project>(this.baseUrl, { code, name, description });
  }

  updateProject(projectId: string, name: string, description: string | undefined, version: number): Observable<Project> {
    const headers = new HttpHeaders({ 'If-Match': `"${version}"` });
    return this.http.put<Project>(`${this.baseUrl}/${projectId}`, { name, description }, { headers });
  }

  deleteProject(projectId: string, version: number): Observable<void> {
    const headers = new HttpHeaders({ 'If-Match': `"${version}"` });
    return this.http.delete<void>(`${this.baseUrl}/${projectId}`, { headers });
  }
}
