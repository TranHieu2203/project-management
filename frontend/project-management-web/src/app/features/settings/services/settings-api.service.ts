import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface NotificationPreferenceDto {
  type: string;
  isEnabled: boolean;
}

@Injectable({ providedIn: 'root' })
export class SettingsApiService {
  private readonly http = inject(HttpClient);

  getNotificationPreferences(): Observable<NotificationPreferenceDto[]> {
    return this.http.get<NotificationPreferenceDto[]>('/api/v1/notification-preferences');
  }

  updateNotificationPreference(type: string, isEnabled: boolean): Observable<void> {
    return this.http.patch<void>(`/api/v1/notification-preferences/${type}`, { isEnabled });
  }
}
