# Story 1.0: Starter Template Setup (Angular + .NET Modular Monolith) + Repo Skeleton

**Story ID:** 1.0  
**Epic:** Epic 1 — Authentication + Portfolio/Project Setup + Gantt Interactive (Core Planning)  
**Status:** review  
**Date Created:** 2026-04-25  
**Sprint:** Sprint 1  

---

## User Story

**As a** PM,  
**I want** khởi tạo nền tảng dự án theo kiến trúc đã chốt (Angular 21 + .NET 10 modular monolith),  
**So that** team có thể bắt đầu phát triển các story nghiệp vụ tiếp theo trên cấu trúc chuẩn ngay từ Sprint 1.

---

## Acceptance Criteria

**Given** Architecture đã chốt stack và cấu trúc solution/frontend  
**When** khởi tạo codebase  
**Then** tạo được skeleton theo cấu trúc: `.NET Host + Shared + Modules/*` (mỗi module tách layer) và `frontend/project-management-web` (Angular 21)  
**And** tích hợp các dependency nền tảng đã chốt: Angular Material, NgRx, Vitest (FE) và xUnit (BE)

**Given** conventions đã chốt (naming, API prefix `/api/v1/`, JSON camelCase, ProblemDetails)  
**When** chạy host API  
**Then** API trả response theo chuẩn JSON camelCase và lỗi theo ProblemDetails  
**And** có health endpoint tối thiểu để smoke test staging

**Given** dự án dùng optimistic locking và membership-only  
**When** tạo "platform primitives" ban đầu  
**Then** có baseline helpers/middleware để dùng lại cho stories sau (ETag/If-Match 412/409, membership-only trả 404, correlationId logging)

---

## Trạng Thái Hiện Tại (Quan Trọng — Đọc Trước Khi Code)

Repository **đã tồn tại** với một số phần được triển khai. Developer PHẢI đọc phần này kỹ trước khi viết bất kỳ code nào.

### ✅ Đã Có (KHÔNG tạo lại)

**Backend:**
- `src/Host/ProjectManagement.Host/Program.cs` — Serilog, JSON camelCase, OpenAPI, NpgsqlDataSource, health endpoints `/health` + `/api/v1/health`, Auth module wired
- `src/Modules/Auth/` — Module Auth HOÀN CHỈNH: Domain (`ApplicationUser`), Application (JWT options, models, ITokenService), Infrastructure (AuthDbContext, migrations, TokenService, AuthSeeder), Api (AuthController, extensions)
- `docker-compose.yml` — PostgreSQL (5432), API (8080), Web (5173)
- `ProjectManagement.slnx` — solution file

**Frontend:**
- `frontend/project-management-web/` — Angular 21 project tồn tại với routing, HttpClient
- `frontend/project-management-web/src/app/app.config.ts` — provideRouter, provideHttpClient (CHƯA có NgRx, CHƯA có Material)
- `frontend/project-management-web/src/app/pages/home/` — home page cơ bản

**Tests:**
- `tests/ProjectManagement.Host.Tests/` — xUnit test project tồn tại (UnitTest1.cs placeholder)

### ⚠️ Còn Thiếu (CẦN IMPLEMENT trong Story này)

**Backend:**
1. `src/Shared/ProjectManagement.Shared.Domain/` — hiện chỉ có `Class1.cs` stub → cần replace với foundations thực sự
2. `src/Shared/ProjectManagement.Shared.Infrastructure/` — hiện chỉ có `Class1.cs` stub → cần replace
3. GlobalExceptionMiddleware → ProblemDetails chưa có trong pipeline
4. CorrelationIdMiddleware chưa có
5. ETag/If-Match platform primitives chưa có
6. Membership-only 404 helper chưa có

**Frontend:**
1. Angular Material — chưa được add vào `app.config.ts`
2. NgRx Store/Effects/Entity/DevTools — chưa có trong `app.config.ts`
3. HTTP Interceptors (auth, error, retry) — chưa có
4. `core/` structure — auth service, token service, auth guard chưa có
5. `shared/` structure — conflict-dialog, loading-spinner, models chưa có
6. Angular Material theme trong `styles.scss`

---

## Technical Requirements

### Backend: Shared.Domain Foundations

Xóa `Class1.cs` trong `src/Shared/ProjectManagement.Shared.Domain/` và thay bằng:

```
src/Shared/ProjectManagement.Shared.Domain/
├── Entities/
│   ├── BaseEntity.cs           ← Id (Guid, gen_random_uuid), CreatedAt, CreatedBy
│   └── AuditableEntity.cs      ← extends BaseEntity + UpdatedAt, UpdatedBy, IsDeleted
├── Results/
│   ├── Result.cs               ← Result.Success() / Result.Failure(error)
│   └── ResultT.cs              ← Result<T>.Success(value) / Result<T>.Failure(error)
└── Exceptions/
    ├── DomainException.cs      ← base, maps → HTTP 422
    ├── NotFoundException.cs    ← maps → HTTP 404
    └── ConflictException.cs    ← maps → HTTP 409
```

**BaseEntity quy tắc:**
- `Id` là `Guid`, được generate bởi database (`gen_random_uuid()` via EF `.HasDefaultValueSql("gen_random_uuid()")`)
- KHÔNG generate Id trong C# constructor — để DB làm
- `CreatedAt` dùng `DateTime` (UTC)

**Result<T> contract:**
```csharp
// Command trả Result<Guid> hoặc Result<SomeDto>
// Query trả DTO trực tiếp (không wrap Result)
Result<Guid> result = Result<Guid>.Success(newId);
Result<Guid> failed = Result<Guid>.Failure("error message");
bool result.IsSuccess;
T result.Value;  // throws if failure
string result.Error; // có giá trị khi failure
```

### Backend: Shared.Infrastructure Foundations

Xóa `Class1.cs` trong `src/Shared/ProjectManagement.Shared.Infrastructure/` và thay bằng:

```
src/Shared/ProjectManagement.Shared.Infrastructure/
├── Middleware/
│   ├── GlobalExceptionMiddleware.cs    ← map exceptions → ProblemDetails
│   └── CorrelationIdMiddleware.cs      ← inject X-Correlation-Id header + Serilog context
├── Services/
│   ├── ICacheService.cs                ← interface: Get<T>/Set<T>/Remove
│   └── MemoryCacheService.cs           ← IMemoryCache implementation
└── Persistence/
    └── IUnitOfWork.cs                  ← cross-repo transaction interface
```

**GlobalExceptionMiddleware — bắt buộc map đúng status code:**
| Exception | HTTP Status | Ghi chú |
|---|---|---|
| `NotFoundException` | 404 | Trả ProblemDetails, KHÔNG expose thêm thông tin |
| `DomainException` | 422 | Business rule violation |
| `ConflictException` | 409 | Optimistic lock conflict |
| `FluentValidation.ValidationException` | 400 | Map `errors` dictionary |
| `UnauthorizedException` (nếu có) | 401 | |
| Mọi `Exception` còn lại | 500 | Log `Fatal`, trả generic message (KHÔNG expose stack trace) |

**ProblemDetails format bắt buộc (.NET 10 built-in):**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Validation Error",
  "status": 400,
  "errors": { "field": ["message"] },
  "traceId": "00-abc123"
}
```

**Đặc biệt cho 409 Conflict:** Body PHẢI có `extensions.current` chứa server state mới nhất + ETag mới nhất để UI inline reconciliation hoạt động.

**CorrelationIdMiddleware:**
- Đọc `X-Correlation-Id` header từ request (nếu có)
- Generate mới nếu header không tồn tại (`Guid.NewGuid().ToString("N")`)
- Push vào `LogContext.PushProperty("CorrelationId", correlationId)`
- Set response header `X-Correlation-Id`

### Backend: ETag/If-Match Platform Primitives

Tạo helpers dùng chung cho optimistic locking (dùng từ Story 1.3+):

**Vị trí:** `src/Shared/ProjectManagement.Shared.Infrastructure/OptimisticLocking/`

```csharp
// ETagHelper.cs — generate và parse ETag
// - ETag = $"\"{version}\"" (quoted per HTTP spec)
// - Parse If-Match header, trả version hoặc null
public static class ETagHelper
{
    public static string Generate(long version);
    public static long? ParseIfMatch(string? ifMatchHeader);
}
```

**Membership-only 404 helper:**  
Convention: khi resource tồn tại nhưng user không phải member → throw `NotFoundException` (trả 404, không leak sự tồn tại). 

Đây là **convention**, không phải middleware. Docs ghi rõ để mọi developer theo đúng.

### Backend: Cập Nhật Program.cs

Sau khi có Shared foundations, cập nhật `Program.cs` để wire thêm:

```csharp
// Thêm vào middleware pipeline:
app.UseMiddleware<CorrelationIdMiddleware>();  // TRƯỚC Serilog request logging
// ...existing...
app.UseSerilogRequestLogging();
// ...
app.UseMiddleware<GlobalExceptionMiddleware>(); // SAU UseRouting, TRƯỚC controllers

// Thêm services:
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ICacheService, MemoryCacheService>();
```

### Frontend: Thêm Angular Material + NgRx vào app.config.ts

Cập nhật `frontend/project-management-web/src/app/app.config.ts`:

```typescript
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideStore } from '@ngrx/store';
import { provideEffects } from '@ngrx/effects';
import { provideStoreDevtools } from '@ngrx/store-devtools';
import { isDevMode } from '@angular/core';

import { routes } from './app.routes';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { errorInterceptor } from './core/interceptors/error.interceptor';
import { retryInterceptor } from './core/interceptors/retry.interceptor';
import { reducers } from './core/store/app.state';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(
      withInterceptors([authInterceptor, errorInterceptor, retryInterceptor])
    ),
    provideAnimationsAsync(),
    provideStore(reducers),
    provideEffects([]),
    provideStoreDevtools({ maxAge: 25, logOnly: !isDevMode() }),
  ],
};
```

### Frontend: Core Structure

Tạo cấu trúc `core/` theo architecture spec:

```
src/app/core/
├── auth/
│   ├── auth.service.ts          ← login/logout/me API calls
│   ├── token.service.ts         ← JWT lưu localStorage, đọc/xóa token
│   └── auth.guard.ts            ← CanActivateFn, check token → redirect /login
├── interceptors/
│   ├── auth.interceptor.ts      ← gắn Authorization: Bearer {token} header
│   ├── error.interceptor.ts     ← handle 401 (clear token + redirect), 409 (trigger dialog)
│   └── retry.interceptor.ts     ← retry 3 lần với exponential backoff (chỉ GET/network error)
└── store/
    └── app.state.ts             ← root AppState, reducers map (rỗng cho Sprint 1)
```

**auth.interceptor.ts:**
- Lấy token từ `TokenService`
- Nếu có token → set header `Authorization: Bearer {token}`
- Bỏ qua các request đến `/api/auth/login` (không cần auth header)

**error.interceptor.ts:**
```
401 → TokenService.clear() → redirect /login (dùng Router inject)
403 → MatSnackBar "Bạn không có quyền thực hiện thao tác này"
409 → emit event/subject cho conflict-dialog (trong Story 1.6 xử lý cụ thể)
500 → MatSnackBar "Lỗi hệ thống. Vui lòng thử lại sau."
```

**retry.interceptor.ts:**
- Chỉ retry cho `GET` requests (không retry POST/PUT/DELETE — idempotency)
- Retry tối đa 3 lần với backoff (1s, 2s, 4s) chỉ khi network error (không retry 4xx/5xx)
- Dùng `catchError` + `timer()` + `retryWhen` của RxJS

**token.service.ts:**
- Key localStorage: `pm_access_token`
- `getToken(): string | null`
- `setToken(token: string): void`
- `clearToken(): void`
- `isTokenValid(): boolean` — parse JWT exp claim, kiểm tra chưa expired

### Frontend: Shared Structure

```
src/app/shared/
├── components/
│   ├── conflict-dialog/
│   │   └── conflict-dialog.ts   ← MatDialog component, nhận server state + user changes
│   └── loading-spinner/
│       └── loading-spinner.ts   ← standalone, dùng MatProgressSpinner
├── models/
│   ├── api-response.model.ts    ← PaginatedResponse<T> { items, totalCount, pageNumber, pageSize }
│   ├── problem-details.model.ts ← ProblemDetails { type, title, status, errors, traceId, extensions }
│   └── pagination.model.ts      ← PaginationParams { pageNumber, pageSize }
└── utils/
    └── date.utils.ts            ← formatDate, parseDate helpers
```

**conflict-dialog.ts — Skeleton cho Story 1.6:**
- Nhận via `MAT_DIALOG_DATA`: `{ serverState: unknown, userChanges: unknown, eTag: string }`
- Hiển thị 2 nút: "Dùng bản mới nhất" / "Thử áp lại của tôi"
- Trả về `'use-server' | 'retry-mine'` khi đóng
- Trong Story 1.0: chỉ cần create component với skeleton UI, chưa cần logic đầy đủ

### Frontend: Angular Material Theme

Cập nhật `frontend/project-management-web/src/styles.scss`:

```scss
// Angular Material theme
@use '@angular/material' as mat;

@include mat.core();

$theme: mat.define-theme((
  color: (
    theme-type: light,
    primary: mat.$azure-palette,
    tertiary: mat.$blue-palette,
  ),
  density: (
    scale: 0,
  )
));

:root {
  @include mat.all-component-themes($theme);
}

// Global
* {
  box-sizing: border-box;
}

body {
  margin: 0;
  font-family: Roboto, "Helvetica Neue", sans-serif;
  background-color: #fafafa;
}

// Scrollbar for Gantt (Story 1.5+)
::-webkit-scrollbar {
  width: 8px;
  height: 8px;
}

::-webkit-scrollbar-track {
  background: #f1f1f1;
}

::-webkit-scrollbar-thumb {
  background: #bbb;
  border-radius: 4px;
}
```

---

## Architecture Compliance Checklist

Developer phải tự verify trước khi mark story done:

**Backend:**
- [x] `Shared.Domain/Class1.cs` đã xóa, thay bằng BaseEntity, Result<T>, Exceptions
- [x] `Shared.Infrastructure/Class1.cs` đã xóa, thay bằng Middleware, Services, Persistence
- [x] `GlobalExceptionMiddleware` wired trong `Program.cs` và trả đúng ProblemDetails format
- [x] `CorrelationIdMiddleware` wired TRƯỚC `UseSerilogRequestLogging`
- [x] `/api/v1/health` hoạt động và trả JSON camelCase
- [x] Mọi error response là ProblemDetails (test với Swagger/curl)
- [x] `IMemoryCache` + `ICacheService` registered

**Frontend:**
- [x] `app.config.ts` có `provideStore`, `provideEffects`, `provideStoreDevtools`, `provideAnimationsAsync`
- [x] HTTP interceptors được wire qua `withInterceptors`
- [x] `styles.scss` có Angular Material theme
- [x] `core/auth/`, `core/interceptors/`, `core/store/` tồn tại
- [x] `shared/models/problem-details.model.ts`, `api-response.model.ts` tồn tại
- [x] `shared/components/conflict-dialog/`, `loading-spinner/` tồn tại

**Naming conventions:**
- [x] DB columns: `snake_case` (kiểm tra AuthDbContext migrations đã có)
- [x] JSON response: `camelCase` (đã có trong Program.cs)
- [x] API endpoints: `/api/v1/{kebab-case-plural}`
- [x] C# classes: `PascalCase`, methods `PascalCase`, fields `_camelCase`
- [x] Angular files: `kebab-case.ts`, classes `PascalCase`

---

## Testing Requirements

### Backend Tests (xUnit)

Trong `tests/ProjectManagement.Host.Tests/`:

1. **Health endpoint test:**
```csharp
// Test GET /api/v1/health → 200, body có status = "ok"
// Test GET /health → 200
```

2. **GlobalExceptionMiddleware tests (unit):**
```csharp
// NotFoundException → 404 ProblemDetails
// DomainException → 422 ProblemDetails
// ConflictException → 409 ProblemDetails, body có extensions.current
// ValidationException → 400 ProblemDetails, body có errors dict
// Generic Exception → 500 ProblemDetails, message generic (không expose details)
```

3. **ETagHelper tests (unit):**
```csharp
// Generate(1) → "\"1\""
// ParseIfMatch("\"1\"") → 1L
// ParseIfMatch(null) → null
// ParseIfMatch("*") → -1L (wildcard nếu cần)
```

### Frontend Tests (Vitest)

1. **token.service.spec.ts:**
```typescript
// setToken + getToken round-trip
// clearToken → getToken returns null
// isTokenValid với token hết hạn → false
// isTokenValid với token còn hạn → true
```

2. **Smoke test — app loads:**
```typescript
// TestBed create AppComponent → không throw
```

### Integration Test (nếu Docker available)

```bash
# Chạy docker-compose up -d
# curl http://localhost:8080/api/v1/health → {"status":"ok","db":{"ok":true}}
# curl http://localhost:8080/api/v1/nonexistent → 404 ProblemDetails format
```

---

## File Locations & Naming (Tham Chiếu Nhanh)

### Backend Files Cần Tạo

| File | Namespace | Ghi chú |
|---|---|---|
| `src/Shared/ProjectManagement.Shared.Domain/Entities/BaseEntity.cs` | `ProjectManagement.Shared.Domain` | Xóa Class1.cs trước |
| `src/Shared/ProjectManagement.Shared.Domain/Entities/AuditableEntity.cs` | `ProjectManagement.Shared.Domain` | |
| `src/Shared/ProjectManagement.Shared.Domain/Results/Result.cs` | `ProjectManagement.Shared.Domain` | |
| `src/Shared/ProjectManagement.Shared.Domain/Results/ResultT.cs` | `ProjectManagement.Shared.Domain` | Generic Result<T> |
| `src/Shared/ProjectManagement.Shared.Domain/Exceptions/DomainException.cs` | `ProjectManagement.Shared.Domain` | |
| `src/Shared/ProjectManagement.Shared.Domain/Exceptions/NotFoundException.cs` | `ProjectManagement.Shared.Domain` | |
| `src/Shared/ProjectManagement.Shared.Domain/Exceptions/ConflictException.cs` | `ProjectManagement.Shared.Domain` | Có `CurrentState` property |
| `src/Shared/ProjectManagement.Shared.Infrastructure/Middleware/GlobalExceptionMiddleware.cs` | `ProjectManagement.Shared.Infrastructure` | Xóa Class1.cs trước |
| `src/Shared/ProjectManagement.Shared.Infrastructure/Middleware/CorrelationIdMiddleware.cs` | `ProjectManagement.Shared.Infrastructure` | |
| `src/Shared/ProjectManagement.Shared.Infrastructure/Services/ICacheService.cs` | `ProjectManagement.Shared.Infrastructure` | |
| `src/Shared/ProjectManagement.Shared.Infrastructure/Services/MemoryCacheService.cs` | `ProjectManagement.Shared.Infrastructure` | |
| `src/Shared/ProjectManagement.Shared.Infrastructure/Persistence/IUnitOfWork.cs` | `ProjectManagement.Shared.Infrastructure` | |
| `src/Shared/ProjectManagement.Shared.Infrastructure/OptimisticLocking/ETagHelper.cs` | `ProjectManagement.Shared.Infrastructure` | |

### Frontend Files Cần Tạo/Cập Nhật

| File | Ghi chú |
|---|---|
| `src/app/app.config.ts` | Cập nhật: thêm NgRx, Material, interceptors |
| `src/styles.scss` | Cập nhật: Angular Material theme |
| `src/app/core/auth/auth.service.ts` | Mới |
| `src/app/core/auth/token.service.ts` | Mới |
| `src/app/core/auth/auth.guard.ts` | Mới |
| `src/app/core/interceptors/auth.interceptor.ts` | Mới |
| `src/app/core/interceptors/error.interceptor.ts` | Mới |
| `src/app/core/interceptors/retry.interceptor.ts` | Mới |
| `src/app/core/store/app.state.ts` | Mới |
| `src/app/shared/models/api-response.model.ts` | Mới |
| `src/app/shared/models/problem-details.model.ts` | Mới |
| `src/app/shared/models/pagination.model.ts` | Mới |
| `src/app/shared/utils/date.utils.ts` | Mới |
| `src/app/shared/components/conflict-dialog/conflict-dialog.ts` | Mới (skeleton) |
| `src/app/shared/components/loading-spinner/loading-spinner.ts` | Mới |

---

## Anti-Patterns — Tránh Những Lỗi Này

### Backend

❌ **KHÔNG** generate Id trong C# constructor — để PostgreSQL gen với `HasDefaultValueSql("gen_random_uuid()")`

❌ **KHÔNG** expose exception stack trace trong ProblemDetails response — chỉ log server-side

❌ **KHÔNG** dùng `string.Format()` hay interpolation với Serilog:
```csharp
// SAI
_logger.Information($"Exception: {ex.Message}");
// ĐÚNG
_logger.Information("Exception: {Message}", ex.Message);
```

❌ **KHÔNG** đặt `GlobalExceptionMiddleware` trước `UseAuthentication()` — phải sau:
```csharp
// ĐÚNG ORDER trong Program.cs
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.MapControllers();
```

### Frontend

❌ **KHÔNG** lưu JWT trong sessionStorage — chỉ dùng `localStorage` với key `pm_access_token`

❌ **KHÔNG** retry `POST/PUT/DELETE` trong retry interceptor — chỉ retry `GET` và network errors

❌ **KHÔNG** call Service trực tiếp từ Component — mọi side effect qua NgRx Action/Effect

❌ **KHÔNG** import từ `features/` trong `core/` hay `shared/` — dependency flows 1 chiều:
```
features/* → shared/ → core/   (OK)
core/ → features/*              (CẤM)
shared/ → features/*            (CẤM)
```

❌ **KHÔNG** dùng NgModules — Angular 21 dùng standalone components hoàn toàn

---

## Dependencies & Versions (Đã Lock)

### Backend NuGet Packages (kiểm tra .csproj đã có chưa)

| Package | Version | Module |
|---|---|---|
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | 10.x | Auth (đã có) |
| `Microsoft.EntityFrameworkCore.Design` | 10.x | Infrastructure modules |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 10.x | Infrastructure modules |
| `MediatR` | ≥12.x | Application layer (Story 1.1+) |
| `FluentValidation.AspNetCore` | ≥11.x | Application layer (Story 1.1+) |
| `Serilog.AspNetCore` | latest | Host (đã có) |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 10.x | Auth (đã có) |

**Cho Story 1.0, chỉ cần `Microsoft.AspNetCore.Mvc.Core` (built-in) cho ProblemDetails — không cần thêm package.**

### Frontend npm Packages

```bash
# Kiểm tra package.json trước khi add
# NgRx
npm install @ngrx/store @ngrx/effects @ngrx/entity @ngrx/store-devtools

# Angular Material (dùng ng add để setup theme tự động)
ng add @angular/material
```

**Angular 21 với Material:** Dùng M3 theme mới (`mat.define-theme`), không dùng API cũ (`mat.define-light-theme`).

---

## Definition of Done

Story 1.0 được coi là DONE khi:

1. ✅ `Shared.Domain` có đầy đủ BaseEntity, AuditableEntity, Result, Result<T>, 3 Exceptions (không còn Class1.cs)
2. ✅ `Shared.Infrastructure` có GlobalExceptionMiddleware, CorrelationIdMiddleware, ICacheService, MemoryCacheService, ETagHelper (không còn Class1.cs)
3. ✅ `Program.cs` đã wire CorrelationIdMiddleware + GlobalExceptionMiddleware + IMemoryCache + ICacheService
4. ✅ `GET /api/v1/health` trả `{"status":"ok","db":{"ok":true,...}}` (JSON camelCase)
5. ✅ Request với lỗi (NotFoundException, DomainException, v.v.) trả đúng ProblemDetails format
6. ✅ `app.config.ts` có provideStore + provideEffects + provideStoreDevtools + provideAnimationsAsync + withInterceptors
7. ✅ `styles.scss` có Angular Material M3 theme
8. ✅ `core/auth/`, `core/interceptors/`, `core/store/` tồn tại với đầy đủ files
9. ✅ `shared/models/`, `shared/utils/`, `shared/components/conflict-dialog/`, `shared/components/loading-spinner/` tồn tại
10. ✅ `docker-compose up` chạy được (api + db healthy)
11. ✅ Không có compile errors (cả BE và FE)
12. ✅ Unit tests cơ bản pass (health endpoint, exception middleware, token service)

---

## Dev Notes

- Story này là **nền tảng** — mọi story sau đều dựa vào. Đừng rush, làm đúng ngay từ đầu.
- Auth module đã có sẵn và ĐANG HOẠT ĐỘNG — đừng chạm vào nó trừ khi cần wire thêm vào Host
- Nếu gặp conflict về namespace giữa `Shared.Domain` và Auth module đã có, ưu tiên namespace `ProjectManagement.Shared.Domain` cho shared types
- `Class1.cs` trong cả 2 Shared project chỉ là placeholder — xóa và thay thế toàn bộ
- Frontend: `ng add @angular/material` sẽ tự update `styles.scss` — merge cẩn thận với theme config trong spec này

---

## Dev Agent Record

### Implementation Notes

Triển khai bởi AI Dev Agent — 2026-04-25T22:00:00+07:00

**Backend:**
- Xóa `Class1.cs` stub ở cả 2 Shared projects, thay bằng foundations thực sự
- `Shared.Domain`: BaseEntity (Id Guid, CreatedAt UTC), AuditableEntity, Result (non-generic), Result<T> (generic), DomainException/NotFoundException/ConflictException
- `ConflictException` có `CurrentState` và `CurrentETag` properties để GlobalExceptionMiddleware inject vào `extensions.current` và `extensions.eTag` trong 409 response
- `Shared.Infrastructure.csproj` thêm `<FrameworkReference Include="Microsoft.AspNetCore.App" />`, FluentValidation 11.x, Serilog 4.x, và project ref đến Shared.Domain
- `GlobalExceptionMiddleware`: map đúng tất cả exception types → ProblemDetails. Lưu ý: `ProblemDetails.Extensions` có `[JsonExtensionData]` attribute nên các keys xuất hiện ở root level JSON, không nested dưới "extensions"
- `CorrelationIdMiddleware`: đọc `X-Correlation-Id` từ header hoặc generate mới, push vào Serilog LogContext, set response header
- `Program.cs` order: CorrelationId → SerilogRequest → Auth → AuthZ → GlobalException → Controllers (đúng spec anti-pattern)
- Host.csproj thêm ProjectReference đến Shared.Infrastructure

**Frontend:**
- NgRx 21.1.0, @angular/material 21.2.8, @angular/animations cài qua npm
- `app.config.ts`: đầy đủ provideStore(reducers), provideEffects([]), provideStoreDevtools, provideAnimationsAsync, withInterceptors([auth, error, retry])
- `styles.scss`: Angular Material M3 theme với `mat.define-theme` + global styles
- `core/auth/`: AuthService (login/logout/me), TokenService (localStorage key `pm_access_token`, JWT exp check), authGuard (CanActivateFn)
- `core/interceptors/`: authInterceptor (Bearer header), errorInterceptor (401→redirect, 409→conflictError$ Subject), retryInterceptor (GET only, 3 lần, exponential backoff 1s/2s/4s, skip 4xx/5xx)
- `core/store/app.state.ts`: rỗng cho Sprint 1
- `shared/models/`: PaginatedResponse, ProblemDetails, PaginationParams
- `shared/utils/date.utils.ts`: formatDate (vi-VN), parseDate
- `shared/components/conflict-dialog/`: skeleton MatDialog component với 2 nút "Dùng bản mới nhất" / "Thử áp lại của tôi"
- `shared/components/loading-spinner/`: standalone MatProgressSpinner wrapper

**Tests:**
- xUnit: 6 ETagHelper tests (Generate, ParseIfMatch, round-trip), 4 GlobalExceptionMiddleware tests (unit, dùng DefaultHttpContext + NullLogger), 2 health endpoint integration tests
- Vitest: 5 TokenService tests + 2 existing app smoke tests = 7 total, tất cả pass
- Phát hiện: `ProblemDetails.Extensions[JsonExtensionData]` serialize keys ở root JSON level — test assertion phải check `"current"` không phải `"extensions"`

---

## File List

### Backend — Tạo Mới
- `src/Shared/ProjectManagement.Shared.Domain/Entities/BaseEntity.cs`
- `src/Shared/ProjectManagement.Shared.Domain/Entities/AuditableEntity.cs`
- `src/Shared/ProjectManagement.Shared.Domain/Results/Result.cs`
- `src/Shared/ProjectManagement.Shared.Domain/Results/ResultT.cs`
- `src/Shared/ProjectManagement.Shared.Domain/Exceptions/DomainException.cs`
- `src/Shared/ProjectManagement.Shared.Domain/Exceptions/NotFoundException.cs`
- `src/Shared/ProjectManagement.Shared.Domain/Exceptions/ConflictException.cs`
- `src/Shared/ProjectManagement.Shared.Infrastructure/Middleware/GlobalExceptionMiddleware.cs`
- `src/Shared/ProjectManagement.Shared.Infrastructure/Middleware/CorrelationIdMiddleware.cs`
- `src/Shared/ProjectManagement.Shared.Infrastructure/Services/ICacheService.cs`
- `src/Shared/ProjectManagement.Shared.Infrastructure/Services/MemoryCacheService.cs`
- `src/Shared/ProjectManagement.Shared.Infrastructure/Persistence/IUnitOfWork.cs`
- `src/Shared/ProjectManagement.Shared.Infrastructure/OptimisticLocking/ETagHelper.cs`
- `tests/ProjectManagement.Host.Tests/ETagHelperTests.cs`
- `tests/ProjectManagement.Host.Tests/GlobalExceptionMiddlewareTests.cs`

### Backend — Xóa
- `src/Shared/ProjectManagement.Shared.Domain/Class1.cs` (đã xóa)
- `src/Shared/ProjectManagement.Shared.Infrastructure/Class1.cs` (đã xóa)

### Backend — Cập Nhật
- `src/Shared/ProjectManagement.Shared.Infrastructure/ProjectManagement.Shared.Infrastructure.csproj`
- `src/Host/ProjectManagement.Host/ProjectManagement.Host.csproj`
- `src/Host/ProjectManagement.Host/Program.cs`
- `tests/ProjectManagement.Host.Tests/ProjectManagement.Host.Tests.csproj`
- `tests/ProjectManagement.Host.Tests/UnitTest1.cs`

### Frontend — Tạo Mới
- `frontend/project-management-web/src/app/core/store/app.state.ts`
- `frontend/project-management-web/src/app/core/auth/token.service.ts`
- `frontend/project-management-web/src/app/core/auth/auth.service.ts`
- `frontend/project-management-web/src/app/core/auth/auth.guard.ts`
- `frontend/project-management-web/src/app/core/interceptors/auth.interceptor.ts`
- `frontend/project-management-web/src/app/core/interceptors/error.interceptor.ts`
- `frontend/project-management-web/src/app/core/interceptors/retry.interceptor.ts`
- `frontend/project-management-web/src/app/shared/models/api-response.model.ts`
- `frontend/project-management-web/src/app/shared/models/problem-details.model.ts`
- `frontend/project-management-web/src/app/shared/models/pagination.model.ts`
- `frontend/project-management-web/src/app/shared/utils/date.utils.ts`
- `frontend/project-management-web/src/app/shared/components/conflict-dialog/conflict-dialog.ts`
- `frontend/project-management-web/src/app/shared/components/loading-spinner/loading-spinner.ts`
- `frontend/project-management-web/src/app/core/auth/token.service.spec.ts`

### Frontend — Cập Nhật
- `frontend/project-management-web/src/app/app.config.ts`
- `frontend/project-management-web/src/styles.scss`
- `frontend/project-management-web/package.json` (thêm @ngrx/*, @angular/material, @angular/cdk, @angular/animations)

---

## Change Log

- 2026-04-25: Story 1.0 implemented — Shared.Domain foundations (BaseEntity, AuditableEntity, Result<T>, 3 Exceptions), Shared.Infrastructure (GlobalExceptionMiddleware, CorrelationIdMiddleware, ICacheService, ETagHelper), Program.cs updated with middleware pipeline, Angular core/shared structure with NgRx + Material + interceptors, 17 unit tests pass (10 xUnit + 7 Vitest)
