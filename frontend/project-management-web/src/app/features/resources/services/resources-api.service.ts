import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Resource } from '../models/resource.model';

@Injectable({ providedIn: 'root' })
export class ResourcesApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/v1/resources';

  getResources(type?: string, vendorId?: string, activeOnly?: boolean): Observable<Resource[]> {
    const params: Record<string, string> = {};
    if (type) params['type'] = type;
    if (vendorId) params['vendorId'] = vendorId;
    if (activeOnly !== undefined) params['activeOnly'] = String(activeOnly);
    return this.http.get<Resource[]>(this.baseUrl, { params });
  }

  getResourceById(resourceId: string): Observable<Resource> {
    return this.http.get<Resource>(`${this.baseUrl}/${resourceId}`);
  }

  createResource(code: string, name: string, email: string | undefined, type: string, vendorId?: string): Observable<Resource> {
    return this.http.post<Resource>(this.baseUrl, { code, name, email, type, vendorId });
  }

  updateResource(resourceId: string, name: string, email: string | undefined, version: number): Observable<Resource> {
    const headers = new HttpHeaders({ 'If-Match': `"${version}"` });
    return this.http.put<Resource>(`${this.baseUrl}/${resourceId}`, { name, email }, { headers });
  }

  inactivateResource(resourceId: string, version: number): Observable<void> {
    const headers = new HttpHeaders({ 'If-Match': `"${version}"` });
    return this.http.delete<void>(`${this.baseUrl}/${resourceId}`, { headers });
  }
}
