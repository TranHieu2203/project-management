import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { NotificationDto } from '../models/notification.model';

@Injectable({ providedIn: 'root' })
export class NotificationsApiService {
  private readonly http = inject(HttpClient);

  getNotifications(unreadOnly = false): Observable<NotificationDto[]> {
    const params = unreadOnly
      ? new HttpParams().set('unreadOnly', 'true')
      : new HttpParams();
    return this.http.get<NotificationDto[]>('/api/v1/notifications', { params });
  }

  markRead(id: string): Observable<void> {
    return this.http.patch<void>(`/api/v1/notifications/${id}/read`, {});
  }

  markAllRead(): Observable<void> {
    return this.http.patch<void>('/api/v1/notifications/read-all', {});
  }
}
