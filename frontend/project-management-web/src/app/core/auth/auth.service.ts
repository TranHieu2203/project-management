import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { TokenService } from './token.service';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  expiresIn: number;
}

export interface MeResponse {
  id: string;
  email: string;
  fullName: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly tokenService = inject(TokenService);
  private readonly apiBase = '/api/v1/auth';

  login(request: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiBase}/login`, request).pipe(
      tap(res => this.tokenService.setToken(res.accessToken))
    );
  }

  logout(): void {
    this.tokenService.clearToken();
  }

  me(): Observable<MeResponse> {
    return this.http.get<MeResponse>(`${this.apiBase}/me`);
  }
}
