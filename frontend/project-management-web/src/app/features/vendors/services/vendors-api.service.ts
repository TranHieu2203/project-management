import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Vendor } from '../models/vendor.model';

@Injectable({ providedIn: 'root' })
export class VendorsApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/v1/vendors';

  getVendors(activeOnly?: boolean): Observable<Vendor[]> {
    const params: Record<string, string> = {};
    if (activeOnly !== undefined) params['activeOnly'] = String(activeOnly);
    return this.http.get<Vendor[]>(this.baseUrl, { params });
  }

  getVendorById(vendorId: string): Observable<Vendor> {
    return this.http.get<Vendor>(`${this.baseUrl}/${vendorId}`);
  }

  createVendor(code: string, name: string, description?: string): Observable<Vendor> {
    return this.http.post<Vendor>(this.baseUrl, { code, name, description });
  }

  updateVendor(vendorId: string, name: string, description: string | undefined, version: number): Observable<Vendor> {
    const headers = new HttpHeaders({ 'If-Match': `"${version}"` });
    return this.http.put<Vendor>(`${this.baseUrl}/${vendorId}`, { name, description }, { headers });
  }

  inactivateVendor(vendorId: string, version: number): Observable<void> {
    const headers = new HttpHeaders({ 'If-Match': `"${version}"` });
    return this.http.delete<void>(`${this.baseUrl}/${vendorId}`, { headers });
  }
}
