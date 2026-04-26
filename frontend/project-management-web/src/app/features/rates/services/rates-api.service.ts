import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { MonthlyRate } from '../models/monthly-rate.model';

export interface CreateRateRequest {
  vendorId: string;
  role: string;
  level: string;
  year: number;
  month: number;
  monthlyAmount: number;
}

@Injectable({ providedIn: 'root' })
export class RatesApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/v1/rates';

  getRates(vendorId?: string, year?: number, month?: number): Observable<MonthlyRate[]> {
    const params: Record<string, string> = {};
    if (vendorId) params['vendorId'] = vendorId;
    if (year !== undefined) params['year'] = String(year);
    if (month !== undefined) params['month'] = String(month);
    return this.http.get<MonthlyRate[]>(this.baseUrl, { params });
  }

  getRateById(rateId: string): Observable<MonthlyRate> {
    return this.http.get<MonthlyRate>(`${this.baseUrl}/${rateId}`);
  }

  createRate(body: CreateRateRequest): Observable<MonthlyRate> {
    return this.http.post<MonthlyRate>(this.baseUrl, body);
  }

  deleteRate(rateId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${rateId}`);
  }
}
