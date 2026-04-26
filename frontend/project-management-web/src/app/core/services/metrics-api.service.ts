import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Injectable({ providedIn: 'root' })
export class MetricsApiService {
  private readonly http = inject(HttpClient);

  recordEvent(eventType: string, context: Record<string, unknown>, correlationId?: string): void {
    this.http.post('/api/v1/metrics/events', {
      eventType,
      contextJson: JSON.stringify(context),
      correlationId: correlationId ?? crypto.randomUUID(),
    }).subscribe({ error: () => {} });
  }
}
