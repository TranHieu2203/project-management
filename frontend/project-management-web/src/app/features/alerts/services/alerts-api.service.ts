import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { AlertDto } from '../models/alert.model';

interface AlertsResponse {
  items: AlertDto[];
  totalCount: number;
}

@Injectable({ providedIn: 'root' })
export class AlertsApiService {
  private readonly http = inject(HttpClient);

  getAlerts(unreadOnly?: boolean): Observable<AlertDto[]> {
    let params = new HttpParams();
    if (unreadOnly) params = params.set('unreadOnly', 'true');
    return this.http
      .get<AlertsResponse>('/api/v1/alerts', { params })
      .pipe(map(r => r.items));
  }

  markRead(id: string): Observable<void> {
    return this.http.patch<void>(`/api/v1/alerts/${id}/read`, {});
  }
}
