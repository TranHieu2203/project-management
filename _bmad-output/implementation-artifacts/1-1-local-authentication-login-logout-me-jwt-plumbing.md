# Story 1.1: Local Authentication (login/logout/me) + JWT plumbing

Status: review

**Story ID:** 1.1
**Epic:** Epic 1 — Authentication + Portfolio/Project Setup + Gantt Interactive (Core Planning)
**Sprint:** Sprint 1
**Date Created:** 2026-04-25

---

## Story

As a PM,
I want đăng nhập/đăng xuất và duy trì session bằng JWT,
so that tôi có thể sử dụng hệ thống một cách an toàn và nhất quán trên staging/demo.

## Acceptance Criteria

1. **Given** user tồn tại và `isActive=true`
   **When** user gọi `POST /api/v1/auth/login` (xem lưu ý route ở Dev Notes) với `email`/`password` hợp lệ
   **Then** trả `200` với `accessToken` (JWT 8h) + `expiresIn` + thông tin user tối thiểu (`id`, `email`, `displayName`)
   **And** response body là JSON camelCase, lỗi theo chuẩn `ProblemDetails`

2. **Given** email/password không hợp lệ **hoặc** user `isActive=false`
   **When** gọi `POST .../auth/login`
   **Then** trả `401 ProblemDetails` với message **chung** ("Email hoặc mật khẩu không chính xác")
   **And** không leak thông tin "email có tồn tại hay không"

3. **Given** client có JWT hợp lệ (chưa hết 8h)
   **When** gọi `GET .../auth/me`
   **Then** trả `200` với `{ id, email, displayName }`

4. **Given** client có JWT expired hoặc không có JWT
   **When** gọi `GET .../auth/me`
   **Then** trả `401 ProblemDetails`

5. **Given** user đang đăng nhập
   **When** gọi `POST .../auth/logout`
   **Then** trả `204` — logout là client-side token disposal (Phase 1 không có server-side revocation; document rõ)

6. **Given** bất kỳ lỗi 4xx/5xx nào từ auth endpoints
   **When** trả response
   **Then** body luôn là `ProblemDetails` (không trả error shape tự chế)

7. **Given** user nhập email/password hợp lệ trên màn hình login của Angular app
   **When** submit form
   **Then** app hiển thị loading state → gọi API → lưu JWT vào `localStorage` key `pm_access_token`
   **And** redirect đến `/projects` (hoặc `returnUrl` nếu có) sau khi login thành công

8. **Given** user chưa đăng nhập cố truy cập route protected
   **When** Angular auth guard kiểm tra token
   **Then** redirect đến `/login?returnUrl={attempted-path}`
   **And** sau khi login thành công redirect về `returnUrl`

## Tasks / Subtasks

- [x] Task 1: Kiểm tra Backend Auth module đã hoàn chỉnh (AC: #1–6)
  - [x] 1.1 Đọc `AuthController.cs` — xác nhận route prefix thực tế (`/api/v1/auth` hay `/api/auth`) và document vào story
  - [x] 1.2 Xác nhận 3 endpoints: `POST login`, `GET me`, `POST logout` — đúng method, response shape, HTTP status
  - [x] 1.3 Verify `TokenService` sinh JWT expiry = 8h; `AuthSeeder` tạo user test với credentials đã biết
  - [x] 1.4 Verify login trả cùng 1 message cho invalid email, wrong password, inactive user (no email enumeration)
  - [x] 1.5 Nếu AuthController chưa đúng: patch theo spec (đây là trường hợp hiếm — module đã complete từ Story 1.0)

- [x] Task 2: Tạo frontend `features/auth/` với login UI (AC: #7)
  - [x] 2.1 Tạo `features/auth/auth.routes.ts` — route `/login` lazy load `LoginComponent`
  - [x] 2.2 Tạo `features/auth/components/login/login.ts` — standalone component + ReactiveForm + Material
  - [x] 2.3 Tạo `features/auth/components/login/login.html` — card layout, 2 fields, loading state, error message
  - [x] 2.4 Tạo `features/auth/services/auth-api.service.ts` — HTTP wrapper cho login/logout/me (inject HttpClient)

- [x] Task 3: Tạo NgRx auth store (AC: #7)
  - [x] 3.1 Tạo `features/auth/store/auth.actions.ts` — login/loginSuccess/loginFailure/logout/loadCurrentUser
  - [x] 3.2 Tạo `features/auth/store/auth.reducer.ts` — `AuthState { user, isLoading, error }`
  - [x] 3.3 Tạo `features/auth/store/auth.effects.ts` — login effect → API → store token → navigate; logout effect
  - [x] 3.4 Tạo `features/auth/store/auth.selectors.ts` — selectCurrentUser, selectAuthLoading, selectAuthError, selectIsAuthenticated
  - [x] 3.5 Cập nhật `core/store/app.state.ts` — thêm `auth: AuthState` vào root state và reducers map
  - [x] 3.6 Đăng ký `provideEffects([AuthEffects])` trong `app.config.ts`

- [x] Task 4: Wire routes và auth guard (AC: #8)
  - [x] 4.1 Cập nhật `app.routes.ts` — `/login` route (public), routes protected bọc bằng `canActivate: [authGuard]`
  - [x] 4.2 Cập nhật `auth.guard.ts` trong `core/auth/` — lưu `returnUrl` vào redirect `/login?returnUrl=...`
  - [x] 4.3 Cập nhật `auth.interceptor.ts` — verify skip URL match đúng với route prefix thực tế của AuthController

- [x] Task 5: Viết tests (AC: #1–8)
  - [x] 5.1 Integration test BE: `POST .../auth/login` happy path → 200 + token shape đúng
  - [x] 5.2 Integration test BE: `POST .../auth/login` wrong password + inactive user → 401, same message
  - [x] 5.3 Integration test BE: `GET .../auth/me` valid token → 200; expired/missing token → 401
  - [x] 5.4 Integration test BE: `POST .../auth/logout` → 204
  - [x] 5.5 Vitest: `auth.effects.spec.ts` — loginSuccess flow (token stored, navigate called), loginFailure flow
  - [x] 5.6 Vitest: `login.component.spec.ts` — render form, submit dispatches action, error displayed

## Dev Notes

### ⚠️ Trạng Thái Hiện Tại — ĐỌC TRƯỚC KHI CODE

**Backend Auth Module: ĐÃ HOÀN CHỈNH từ Story 1.0**

Từ Story 1.0 implementation notes (status: review):
```
src/Modules/Auth/ — HOÀN CHỈNH:
├── Domain/     → ApplicationUser
├── Application/ → JWT options, models, ITokenService
├── Infrastructure/ → AuthDbContext, migrations, TokenService, AuthSeeder
└── Api/ → AuthController, extensions
```
Auth module đã được wired vào `Program.cs` và `Host.csproj`.
**KHÔNG viết lại Auth module** — chỉ đọc, verify, và test.

**Frontend Core Auth: ĐÃ TẠO từ Story 1.0**

Những file này TỒN TẠI — không tạo lại:
- `src/app/core/auth/auth.service.ts` — đã có login/logout/me API calls
- `src/app/core/auth/token.service.ts` — đã có (localStorage key: `pm_access_token`, JWT exp check)
- `src/app/core/auth/auth.guard.ts` — đã có (CanActivateFn, check token)
- `src/app/core/interceptors/auth.interceptor.ts` — đã wired (Bearer header, skip login endpoint)
- `src/app/core/interceptors/error.interceptor.ts` — đã wired (401 → clear token + redirect /login)

**Story 1.1 cần BUILD MỚI:**
- `features/auth/` UI components và NgRx store
- `app.routes.ts` wiring với auth guard
- Tests end-to-end

### Backend — Route Prefix Cần Verify

**Action đầu tiên của Task 1**: Đọc `AuthController.cs` xem route attribute thực tế.

Architecture spec bắt buộc prefix `/api/v1/`. Nhưng epics AC dùng `/api/auth/` (không có v1). Hai khả năng:
- `[Route("api/v1/auth")]` — consistent với convention → ACs là `/api/v1/auth/login`
- `[Route("api/auth")]` — auth đặc biệt không có version → ACs là `/api/auth/login`

Sau khi xác nhận, **cập nhật `auth.interceptor.ts`** để skip URL match đúng (xem phần interceptor bên dưới).

### Backend — JWT Contract

```
JWT claims bắt buộc (verify trong TokenService):
- sub: userId (Guid string)
- email: user email
- displayName: user display name
- role: ["PM"] hoặc role tương ứng
- exp: DateTime.UtcNow + 8 giờ

Login response shape:
{
  "accessToken": "eyJ...",
  "expiresIn": 28800,        // 8 * 3600 giây
  "user": {
    "id": "guid",
    "email": "user@example.com",
    "displayName": "Nguyễn Văn A"
  }
}
```

### Backend — AuthSeeder Credentials

Kiểm tra `src/Modules/Auth/Infrastructure/AuthSeeder.cs` để lấy test credentials cho integration tests. Thường sẽ có dạng:
```
Admin: admin@pm.local / Admin@123 (ví dụ)
PM:    pm@pm.local    / Pm@1234 (ví dụ)
```
Dùng credentials từ AuthSeeder thực tế (không hardcode assumption).

### Backend — No Email Enumeration

AuthController PHẢI trả cùng message cho tất cả auth failures:
```json
// ✅ ĐÚNG — không phân biệt lý do
{ "title": "Email hoặc mật khẩu không chính xác", "status": 401 }

// ❌ SAI — leak thông tin
{ "title": "Email không tồn tại" }
{ "title": "Tài khoản bị khoá" }
{ "title": "Mật khẩu sai" }
```

### Backend — GlobalExceptionMiddleware (từ Story 1.0)

Middleware order đã thiết lập trong `Program.cs`:
```
CorrelationIdMiddleware → SerilogRequestLogging → UseAuthentication → UseAuthorization → GlobalExceptionMiddleware → MapControllers
```

**Lưu ý quan trọng từ Story 1.0**: `ProblemDetails.Extensions` với `[JsonExtensionData]` attribute serialize keys ở **ROOT JSON level**, không phải nested dưới `"extensions"`:
```json
// Response thực tế cho 409:
{
  "type": "...",
  "title": "...",
  "status": 409,
  "current": { ... },    // ← ở root, không phải "extensions.current"
  "eTag": "\"2\""        // ← ở root
}
```
Quan trọng cho integration tests — assert đúng JSON path.

### Frontend — Features Auth Structure

```
src/app/features/auth/
├── auth.routes.ts               ← { path: 'login', component: LoginComponent }
├── components/
│   └── login/
│       ├── login.ts             ← standalone, ChangeDetectionStrategy.OnPush
│       ├── login.html           ← MatCard, MatFormField, MatInput, MatButton
│       └── login.scss
├── services/
│   └── auth-api.service.ts      ← HttpClient wrapper (injectable)
└── store/
    ├── auth.actions.ts
    ├── auth.reducer.ts
    ├── auth.effects.ts
    └── auth.selectors.ts
```

### Frontend — auth-api.service.ts Pattern

Service này là HTTP wrapper cho feature auth, KHÁC với `core/auth/auth.service.ts`:

```typescript
// features/auth/services/auth-api.service.ts
@Injectable({ providedIn: 'root' })
export class AuthApiService {
  private http = inject(HttpClient);
  private baseUrl = '/api/v1/auth'; // điều chỉnh theo route thực tế

  login(email: string, password: string): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.baseUrl}/login`, { email, password });
  }

  me(): Observable<UserInfo> {
    return this.http.get<UserInfo>(`${this.baseUrl}/me`);
  }

  logout(): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/logout`, {});
  }
}

// Interfaces (trong file hoặc models/auth.model.ts)
interface LoginResponse {
  accessToken: string;
  expiresIn: number;
  user: UserInfo;
}

interface UserInfo {
  id: string;
  email: string;
  displayName: string;
}
```

### Frontend — NgRx Auth Store

**AuthState:**
```typescript
// features/auth/store/auth.reducer.ts
export interface AuthState {
  user: UserInfo | null;
  isLoading: boolean;
  error: string | null;
}

const initialState: AuthState = {
  user: null,
  isLoading: false,
  error: null,
};
```

**Actions:**
```typescript
// features/auth/store/auth.actions.ts — dùng createActionGroup
export const AuthActions = createActionGroup({
  source: 'Auth',
  events: {
    'Login': props<{ email: string; password: string }>(),
    'Login Success': props<{ user: UserInfo }>(),
    'Login Failure': props<{ error: string }>(),
    'Logout': emptyProps(),
    'Logout Success': emptyProps(),
    'Load Current User Success': props<{ user: UserInfo }>(),
  }
});
```

**Effects (login):**
```typescript
// features/auth/store/auth.effects.ts
login$ = createEffect(() => this.actions$.pipe(
  ofType(AuthActions.login),
  switchMap(({ email, password }) =>
    this.authApiService.login(email, password).pipe(
      tap(res => this.tokenService.setToken(res.accessToken)),
      map(res => AuthActions.loginSuccess({ user: res.user })),
      catchError(err => of(AuthActions.loginFailure({
        error: err.error?.title ?? 'Đăng nhập thất bại. Vui lòng thử lại.'
      })))
    )
  )
));

loginSuccess$ = createEffect(() => this.actions$.pipe(
  ofType(AuthActions.loginSuccess),
  tap(() => {
    const returnUrl = this.router.parseUrl(this.router.url).queryParams['returnUrl'];
    this.router.navigateByUrl(returnUrl ?? '/projects');
  })
), { dispatch: false });

logout$ = createEffect(() => this.actions$.pipe(
  ofType(AuthActions.logout),
  switchMap(() =>
    this.authApiService.logout().pipe(
      finalize(() => {
        this.tokenService.clearToken();
        this.router.navigate(['/login']);
      }),
      map(() => AuthActions.logoutSuccess()),
      catchError(() => of(AuthActions.logoutSuccess())) // logout luôn thành công client-side
    )
  )
));
```

**Selectors:**
```typescript
// features/auth/store/auth.selectors.ts
export const selectAuthState = (state: AppState) => state.auth;
export const selectCurrentUser = createSelector(selectAuthState, s => s.user);
export const selectAuthLoading = createSelector(selectAuthState, s => s.isLoading);
export const selectAuthError = createSelector(selectAuthState, s => s.error);
export const selectIsAuthenticated = createSelector(selectCurrentUser, user => user !== null);
```

### Frontend — Login Component

```typescript
// features/auth/components/login/login.ts
@Component({
  standalone: true,
  selector: 'app-login',
  imports: [
    ReactiveFormsModule,
    MatCardModule, MatFormFieldModule, MatInputModule,
    MatButtonModule, MatProgressSpinnerModule,
    AsyncPipe, NgIf
  ],
  templateUrl: './login.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LoginComponent {
  private store = inject(Store);

  protected isLoading$ = this.store.select(selectAuthLoading);
  protected error$ = this.store.select(selectAuthError);

  protected form = new FormGroup({
    email: new FormControl('', [Validators.required, Validators.email]),
    password: new FormControl('', [Validators.required, Validators.minLength(6)])
  });

  protected onSubmit(): void {
    if (this.form.valid) {
      this.store.dispatch(AuthActions.login({
        email: this.form.value.email!,
        password: this.form.value.password!
      }));
    }
  }
}
```

**UI requirements cho login.html:**
- `mat-card` căn giữa, width 400px (responsive), padding 24–32px
- Tiêu đề: "Project Management" (h2) + subtitle "Đăng nhập để tiếp tục"
- Field Email: `matInput type="email"`, label "Email", required
- Field Password: `matInput type="password"`, label "Mật khẩu", required
- Button "Đăng nhập" (`mat-raised-button color="primary"`): disabled khi `form.invalid || (isLoading$ | async)`
- Loading spinner (inline hoặc overlay nhẹ) khi `isLoading$`
- Error message block hiện khi `error$` có giá trị: dùng `mat-error` style hoặc `class="error-message"`
- Không redirect tự động khi đã có token — auth guard xử lý (nếu đã login → guard redirect về /projects)

### Frontend — app.routes.ts Cập Nhật

```typescript
// src/app/app.routes.ts
export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/auth/components/login/login').then(m => m.LoginComponent)
  },
  {
    path: '',
    canActivate: [authGuard],
    children: [
      {
        path: 'projects',
        loadChildren: () => import('./features/projects/projects.routes').then(m => m.projectsRoutes)
      },
      { path: '', redirectTo: 'projects', pathMatch: 'full' }
    ]
  },
  { path: '**', redirectTo: 'login' }
];
```

**Lưu ý:** Dùng `loadComponent` cho login (single component) thay vì `loadChildren` (cho feature với nhiều routes).

### Frontend — auth.guard.ts Cập Nhật

Cập nhật guard hiện có để lưu `returnUrl`:
```typescript
// src/app/core/auth/auth.guard.ts
export const authGuard: CanActivateFn = (route, state) => {
  const tokenService = inject(TokenService);
  const router = inject(Router);

  if (tokenService.isTokenValid()) {
    return true;
  }

  // Lưu attempted URL để redirect về sau login
  return router.createUrlTree(['/login'], {
    queryParams: { returnUrl: state.url }
  });
};
```

### Frontend — auth.interceptor.ts Verify

Verify `auth.interceptor.ts` skip đúng URL pattern (phải match với route thực tế của AuthController):

```typescript
// Nếu auth route là /api/v1/auth/login:
const isLoginRequest = req.url.endsWith('/auth/login');

// Nếu auth route là /api/auth/login:
const isLoginRequest = req.url.includes('/auth/login');
```

Đảm bảo `me` và `logout` endpoints VẪN gắn Bearer header (chỉ skip `login`).

### Frontend — app.config.ts Cập Nhật

Thêm `AuthEffects` vào `provideEffects`:
```typescript
// src/app/app.config.ts
import { AuthEffects } from './features/auth/store/auth.effects';
// ...
provideEffects([AuthEffects]),
// Nếu đã có provideEffects([]) từ Story 1.0 — thêm AuthEffects vào array
```

### Frontend — app.state.ts Cập Nhật

```typescript
// src/app/core/store/app.state.ts
import { authReducer, AuthState } from '../../features/auth/store/auth.reducer';

export interface AppState {
  auth: AuthState;
  // ... các features state sau
}

export const reducers: ActionReducerMap<AppState> = {
  auth: authReducer,
};
```

### Project Structure Notes

**Backend files cần ĐỌC (không sửa trừ khi cần patch nhỏ):**
```
src/Modules/Auth/
├── ProjectManagement.Auth.Api/Controllers/AuthController.cs   ← verify routes
├── ProjectManagement.Auth.Application/Services/ITokenService.cs
├── ProjectManagement.Auth.Infrastructure/Services/TokenService.cs  ← JWT 8h
└── ProjectManagement.Auth.Infrastructure/AuthSeeder.cs            ← test credentials
```

**Frontend files cần TẠO MỚI:**
```
src/app/features/auth/
├── auth.routes.ts
├── components/login/login.ts (.html, .scss)
├── services/auth-api.service.ts
└── store/auth.actions.ts, auth.reducer.ts, auth.effects.ts, auth.selectors.ts
```

**Frontend files cần CẬP NHẬT:**
```
src/app/app.routes.ts              ← thêm /login route, bọc protected routes
src/app/app.config.ts              ← thêm AuthEffects
src/app/core/store/app.state.ts    ← thêm auth state + reducer
src/app/core/auth/auth.guard.ts    ← thêm returnUrl logic
src/app/core/interceptors/auth.interceptor.ts  ← verify skip URL
```

**Tests cần TẠO:**
```
tests/Modules.Auth.IntegrationTests/AuthControllerTests.cs    ← hoặc tương tự
frontend/src/app/features/auth/store/auth.effects.spec.ts
frontend/src/app/features/auth/components/login/login.spec.ts
```

### Anti-Patterns — Tránh Những Lỗi Này

**Backend:**
❌ **KHÔNG** viết lại AuthController hay TokenService — chỉ verify và patch tối thiểu nếu cần
❌ **KHÔNG** trả message khác nhau cho invalid email vs wrong password (email enumeration)
❌ **KHÔNG** include stack trace trong ProblemDetails response

**Frontend:**
❌ **KHÔNG** gọi `AuthApiService.login()` trực tiếp từ component — dispatch `AuthActions.login` qua NgRx
❌ **KHÔNG** lưu token trong `sessionStorage` — chỉ `localStorage` key `pm_access_token`
❌ **KHÔNG** dùng NgModules — Angular 21 standalone components hoàn toàn
❌ **KHÔNG** import từ `features/` trong `core/` hay ngược lại ngoài pattern đã định:
  ```
  features/auth/store/ → core/auth/token.service.ts  (OK — feature dùng core service)
  core/auth/auth.guard.ts → features/auth/store/     (CẤM — core không depend vào features)
  ```
  Nếu cần auth state trong guard: inject `Store` trực tiếp và dùng `selectIsAuthenticated`

❌ **KHÔNG** retry `POST /auth/login` trong retry interceptor — chỉ retry GET và network errors (đã cấu hình từ Story 1.0)

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 1.1]
- [Source: _bmad-output/planning-artifacts/architecture.md#Phần 5.2 Authentication & Security (D-04, D-05)]
- [Source: _bmad-output/planning-artifacts/architecture.md#Section 7.2 Frontend Structure - features/auth/]
- [Source: _bmad-output/planning-artifacts/architecture.md#Section 5.4 State Management Patterns]
- [Source: _bmad-output/planning-artifacts/architecture.md#Section 5.5 Error Handling Patterns]
- [Source: _bmad-output/implementation-artifacts/1-0-starter-template-setup-angular-net-modular-monolith-repo-skeleton.md#Implementation Notes]

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

### Completion Notes List

### File List
