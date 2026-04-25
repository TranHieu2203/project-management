import { Component, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { JsonPipe } from '@angular/common';

type HealthResponse = {
  status: string;
  service: string;
  environment: string;
};

@Component({
  selector: 'app-home-page',
  imports: [JsonPipe],
  templateUrl: './home-page.html',
  styleUrl: './home-page.scss',
})
export class HomePage {
  private readonly http = inject(HttpClient);

  protected loading = signal(true);
  protected ok = signal(false);
  protected error = signal<string | null>(null);
  protected health = signal<HealthResponse | null>(null);

  constructor() {
    this.refresh();
  }

  protected refresh() {
    this.loading.set(true);
    this.error.set(null);

    this.http.get<HealthResponse>('/api/v1/health').subscribe({
      next: (res) => {
        this.health.set(res);
        this.ok.set(res.status === 'ok');
        this.loading.set(false);
      },
      error: () => {
        this.ok.set(false);
        this.loading.set(false);
        this.error.set('Không thể kết nối API. Vui lòng thử lại.');
      },
    });
  }
}

