import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { LookupItem } from '../models/lookup.model';

@Injectable({ providedIn: 'root' })
export class LookupsApiService {
  private readonly http = inject(HttpClient);

  getRoles(): Observable<LookupItem[]> {
    return this.http.get<LookupItem[]>('/api/v1/lookups/roles');
  }

  getLevels(): Observable<LookupItem[]> {
    return this.http.get<LookupItem[]>('/api/v1/lookups/levels');
  }
}
