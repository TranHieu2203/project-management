import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ProjectMember } from '../models/project.model';

@Injectable({ providedIn: 'root' })
export class MembersApiService {
  private readonly http = inject(HttpClient);

  getProjectMembers(projectId: string): Observable<ProjectMember[]> {
    return this.http.get<ProjectMember[]>(`/api/v1/projects/${projectId}/members`);
  }
}
