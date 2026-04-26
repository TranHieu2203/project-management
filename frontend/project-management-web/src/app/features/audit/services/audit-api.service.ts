import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AuditEvent } from '../models/audit-event.model';

@Injectable({ providedIn: 'root' })
export class AuditApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/v1/audit';

  getAuditEvents(entityType?: string, entityId?: string, pageSize = 50): Observable<AuditEvent[]> {
    const params: Record<string, string> = { pageSize: String(pageSize) };
    if (entityType) params['entityType'] = entityType;
    if (entityId) params['entityId'] = entityId;
    return this.http.get<AuditEvent[]>(this.baseUrl, { params });
  }
}
