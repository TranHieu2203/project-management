import { Injectable } from '@angular/core';

const TOKEN_KEY = 'pm_access_token';

@Injectable({ providedIn: 'root' })
export class TokenService {
  getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  setToken(token: string): void {
    localStorage.setItem(TOKEN_KEY, token);
  }

  clearToken(): void {
    localStorage.removeItem(TOKEN_KEY);
  }

  isTokenValid(): boolean {
    const token = this.getToken();
    if (!token) return false;

    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      const expMs = payload.exp * 1000;
      return Date.now() < expMs;
    } catch {
      return false;
    }
  }

  getUserFromToken(): { id: string; email: string; displayName: string | null } | null {
    const token = this.getToken();
    if (!token) return null;
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      const id: string = payload['sub'];
      const email: string = payload['email'] ?? '';
      // .NET JwtSecurityTokenHandler maps ClaimTypes.Name → "unique_name"
      const displayName: string | null = payload['unique_name'] ?? payload['name'] ?? email ?? null;
      if (!id) return null;
      return { id, email, displayName };
    } catch {
      return null;
    }
  }
}
