import { TestBed } from '@angular/core/testing';
import { TokenService } from './token.service';

const TOKEN_KEY = 'pm_access_token';

function makeJwt(expOffsetSeconds: number): string {
  const payload = { exp: Math.floor(Date.now() / 1000) + expOffsetSeconds };
  const encodedPayload = btoa(JSON.stringify(payload));
  return `header.${encodedPayload}.signature`;
}

describe('TokenService', () => {
  let service: TokenService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(TokenService);
    localStorage.removeItem(TOKEN_KEY);
  });

  afterEach(() => {
    localStorage.removeItem(TOKEN_KEY);
  });

  it('setToken then getToken round-trip', () => {
    service.setToken('test-token');
    expect(service.getToken()).toBe('test-token');
  });

  it('clearToken makes getToken return null', () => {
    service.setToken('test-token');
    service.clearToken();
    expect(service.getToken()).toBeNull();
  });

  it('isTokenValid returns false when no token', () => {
    expect(service.isTokenValid()).toBe(false);
  });

  it('isTokenValid returns false for expired token', () => {
    service.setToken(makeJwt(-60));
    expect(service.isTokenValid()).toBe(false);
  });

  it('isTokenValid returns true for valid token', () => {
    service.setToken(makeJwt(3600));
    expect(service.isTokenValid()).toBe(true);
  });
});
