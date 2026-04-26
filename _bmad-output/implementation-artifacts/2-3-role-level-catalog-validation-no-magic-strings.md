# Story 2.3: Role/Level Catalog + Validation (No Magic Strings)

Status: review

**Story ID:** 2.3
**Epic:** Epic 2 — Workforce (People/Vendor) + Rate Model + Audit Foundation
**Sprint:** Sprint 3
**Date Created:** 2026-04-26

---

## Story

As a PM,
I want dùng danh mục Role/Level chuẩn hoá khi cấu hình rate và phân bổ,
So that dữ liệu nhất quán và tránh nhập tự do gây sai lệch.

## Acceptance Criteria

1. **Given** hệ thống có catalog `ResourceRole` và `ResourceLevel`
   **When** user gọi `GET /api/v1/lookups/roles` và `GET /api/v1/lookups/levels`
   **Then** trả danh sách giá trị hợp lệ dưới dạng `{ value, label }` array

2. **Given** frontend cần hiển thị dropdown role/level
   **When** component rate-form (Story 2.4) mount
   **Then** dùng catalog đã load từ store, không hardcode string trong template

3. **Given** enum `ResourceRole` và `ResourceLevel` là nguồn truth duy nhất
   **When** Story 2.4 tạo Rate handler validate
   **Then** `Enum.TryParse` validate role/level — cùng codebase, không dùng magic string

## Tasks / Subtasks

- [x] **Task 1: Domain Enums (BE)**
  - [x] 1.1 Tạo `ResourceRole.cs` trong `Workforce.Domain/Enums/`
  - [x] 1.2 Tạo `ResourceLevel.cs` trong `Workforce.Domain/Enums/`

- [x] **Task 2: Application Layer (BE)**
  - [x] 2.1 Tạo `GetRoleLevelCatalogQuery` + Handler — trả `RoleLevelCatalogDto`
  - [x] 2.2 Tạo `RoleLevelCatalogDto` record

- [x] **Task 3: API Controller (BE)**
  - [x] 3.1 Tạo `LookupsController.cs` tại `/api/v1/lookups`
  - [x] 3.2 `GET /api/v1/lookups/roles` → trả `LookupItemDto[]`
  - [x] 3.3 `GET /api/v1/lookups/levels` → trả `LookupItemDto[]`
  - [x] 3.4 Tạo `LookupItemDto` record `(string Value, string Label)`

- [x] **Task 4: Frontend Catalog Store (FE)**
  - [x] 4.1 Tạo `lookup.model.ts` (`LookupItem { value, label }`)
  - [x] 4.2 Tạo `lookups.actions.ts`
  - [x] 4.3 Tạo `lookups.reducer.ts`
  - [x] 4.4 Tạo `lookups.selectors.ts`
  - [x] 4.5 Tạo `lookups.effects.ts`
  - [x] 4.6 Tạo `lookups-api.service.ts`
  - [x] 4.7 Đăng ký trong `app.state.ts` + `app.config.ts`

- [x] **Task 5: Build verification**
  - [x] 5.1 `dotnet build` → 0 errors
  - [x] 5.2 `ng build` → 0 errors

---

## Dev Notes

### Workforce Module đã có — KHÔNG tạo lại

| Đã có | Ghi chú |
|---|---|
| `ResourceType.cs` enum | Pattern mẫu cho ResourceRole/ResourceLevel |
| `WorkforceDbContext` | Không cần sửa — catalog không cần DB |
| `WorkforceModuleExtensions` | Đã đăng ký LookupsController cùng assembly |
| `VendorsController` / `ResourcesController` | Pattern mẫu |
| NgRx vendors store | Pattern mẫu cho lookups store |

### Task 1 Detail: Domain Enums

```csharp
// Workforce.Domain/Enums/ResourceRole.cs
namespace ProjectManagement.Workforce.Domain.Enums;

public enum ResourceRole
{
    Developer,
    PM,
    QA,
    BA,
    DevOps,
    Designer,
    TechLead,
    Architect
}
```

```csharp
// Workforce.Domain/Enums/ResourceLevel.cs
namespace ProjectManagement.Workforce.Domain.Enums;

public enum ResourceLevel
{
    Junior,
    Mid,
    Senior,
    Lead,
    Principal
}
```

**Lưu ý:** Enum values cố định, không có DB. Story 2.4 handler dùng `Enum.TryParse<ResourceRole>(cmd.Role, out var role)` để validate.

### Task 2 Detail: Application Layer

**LookupItemDto:**
```csharp
// Workforce.Application/DTOs/LookupItemDto.cs
public sealed record LookupItemDto(string Value, string Label);
```

**RoleLevelCatalogDto:**
```csharp
// Workforce.Application/DTOs/RoleLevelCatalogDto.cs
public sealed record RoleLevelCatalogDto(
    List<LookupItemDto> Roles,
    List<LookupItemDto> Levels
);
```

**GetRoleLevelCatalogQuery:**
```csharp
// Query không cần params — trả toàn bộ catalog
public sealed record GetRoleLevelCatalogQuery() : IRequest<RoleLevelCatalogDto>;
```

**GetRoleLevelCatalogHandler:**
```csharp
public sealed class GetRoleLevelCatalogHandler : IRequestHandler<GetRoleLevelCatalogQuery, RoleLevelCatalogDto>
{
    public Task<RoleLevelCatalogDto> Handle(GetRoleLevelCatalogQuery query, CancellationToken ct)
    {
        var roles = Enum.GetValues<ResourceRole>()
            .Select(r => new LookupItemDto(r.ToString(), ToLabel(r)))
            .ToList();

        var levels = Enum.GetValues<ResourceLevel>()
            .Select(l => new LookupItemDto(l.ToString(), ToLabel(l)))
            .ToList();

        return Task.FromResult(new RoleLevelCatalogDto(roles, levels));
    }

    private static string ToLabel(ResourceRole r) => r switch
    {
        ResourceRole.Developer  => "Developer",
        ResourceRole.PM         => "Project Manager",
        ResourceRole.QA         => "QA Engineer",
        ResourceRole.BA         => "Business Analyst",
        ResourceRole.DevOps     => "DevOps Engineer",
        ResourceRole.Designer   => "UI/UX Designer",
        ResourceRole.TechLead   => "Tech Lead",
        ResourceRole.Architect  => "Solutions Architect",
        _                       => r.ToString()
    };

    private static string ToLabel(ResourceLevel l) => l switch
    {
        ResourceLevel.Junior    => "Junior",
        ResourceLevel.Mid       => "Mid-level",
        ResourceLevel.Senior    => "Senior",
        ResourceLevel.Lead      => "Lead",
        ResourceLevel.Principal => "Principal",
        _                       => l.ToString()
    };
}
```

**Lưu ý quan trọng:** Handler này **không query DB** — không cần `IWorkforceDbContext`. Thuần logic từ enum. Đây là intentional design: catalog là code constant, không lưu DB.

### Task 3 Detail: API Controller

```csharp
// Workforce.Api/Controllers/LookupsController.cs
[Authorize]
[ApiController]
[Route("api/v1/lookups")]
public sealed class LookupsController : ControllerBase
{
    private readonly IMediator _mediator;

    public LookupsController(IMediator mediator) => _mediator = mediator;

    /// <summary>GET /api/v1/lookups/roles — Trả danh sách vai trò hợp lệ</summary>
    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles(CancellationToken ct)
    {
        var catalog = await _mediator.Send(new GetRoleLevelCatalogQuery(), ct);
        return Ok(catalog.Roles);
    }

    /// <summary>GET /api/v1/lookups/levels — Trả danh sách level hợp lệ</summary>
    [HttpGet("levels")]
    public async Task<IActionResult> GetLevels(CancellationToken ct)
    {
        var catalog = await _mediator.Send(new GetRoleLevelCatalogQuery(), ct);
        return Ok(catalog.Levels);
    }
}
```

**Lưu ý:** `LookupsController` nằm trong `Workforce.Api` — cùng assembly với `VendorsController` và `ResourcesController`. `WorkforceModuleExtensions.AddApplicationPart(typeof(VendorsController).Assembly)` đã đăng ký assembly này. **Không cần gọi AddApplicationPart lần nữa.**

**Tối ưu:** Có thể gọi `GetRoleLevelCatalogQuery` một lần và trả full catalog nếu muốn `GET /api/v1/lookups/catalog`. Nhưng split thành `roles` + `levels` endpoint riêng để frontend có thể load độc lập khi cần.

### Task 4 Detail: Frontend

**lookup.model.ts:**
```typescript
export interface LookupItem {
  value: string;
  label: string;
}
```

**Vị trí file:**
```
frontend/src/app/features/
└── lookups/
    ├── models/lookup.model.ts
    ├── store/
    │   ├── lookups.actions.ts
    │   ├── lookups.reducer.ts
    │   ├── lookups.selectors.ts
    │   └── lookups.effects.ts
    └── services/lookups-api.service.ts
```

**lookups.actions.ts:**
```typescript
export const LookupsActions = createActionGroup({
  source: 'Lookups',
  events: {
    'Load Catalog': emptyProps(),
    'Load Catalog Success': props<{ roles: LookupItem[]; levels: LookupItem[] }>(),
    'Load Catalog Failure': props<{ error: string }>(),
  },
});
```

**lookups.reducer.ts:**
```typescript
export interface LookupsState {
  roles: LookupItem[];
  levels: LookupItem[];
  loaded: boolean;
  error: string | null;
}

export const initialLookupsState: LookupsState = {
  roles: [],
  levels: [],
  loaded: false,
  error: null,
};

export const lookupsReducer = createReducer(
  initialLookupsState,
  on(LookupsActions.loadCatalog, state => ({ ...state, error: null })),
  on(LookupsActions.loadCatalogSuccess, (state, { roles, levels }) =>
    ({ ...state, roles, levels, loaded: true })
  ),
  on(LookupsActions.loadCatalogFailure, (state, { error }) =>
    ({ ...state, error })
  ),
);
```

**lookups.selectors.ts:**
```typescript
const selectLookupsState = (state: AppState) => state.lookups;
export const selectRoles = createSelector(selectLookupsState, s => s.roles);
export const selectLevels = createSelector(selectLookupsState, s => s.levels);
export const selectLookupsLoaded = createSelector(selectLookupsState, s => s.loaded);
```

**lookups-api.service.ts:**
```typescript
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
```

**lookups.effects.ts:**
```typescript
loadCatalog$ = createEffect(() =>
  this.actions$.pipe(
    ofType(LookupsActions.loadCatalog),
    switchMap(() =>
      forkJoin({
        roles: this.lookupsApi.getRoles(),
        levels: this.lookupsApi.getLevels(),
      }).pipe(
        map(({ roles, levels }) => LookupsActions.loadCatalogSuccess({ roles, levels })),
        catchError((err: HttpErrorResponse) =>
          of(LookupsActions.loadCatalogFailure({ error: err.error?.detail ?? 'Không thể tải catalog.' }))
        )
      )
    )
  )
);
```

**Quan trọng:** Import `forkJoin` từ `'rxjs'`, không phải từ `'rxjs/operators'`.

**app.state.ts — thêm:**
```typescript
import { lookupsReducer, LookupsState } from '../../features/lookups/store/lookups.reducer';
// AppState: lookups: LookupsState
// reducers: lookups: lookupsReducer
```

**app.config.ts — thêm:**
```typescript
import { LookupsEffects } from './features/lookups/store/lookups.effects';
// provideEffects([..., LookupsEffects])
```

**Khi dùng:** Story 2.4 (rate-form component) sẽ dispatch `LookupsActions.loadCatalog()` nếu `!lookupsLoaded` và dùng `selectRoles` / `selectLevels` cho dropdown. **Không cần tạo component ở Story 2.3** — story này chỉ scaffold the store + API.

### Patterns đã có — KHÔNG viết lại

| Pattern | Source | Ghi chú |
|---|---|---|
| `ResourceType.cs` enum | Story 2.2 | Áp dụng y chang cho ResourceRole/Level |
| `createReducer` (không createFeature) | Story 2.1, 2.2 | Tránh TypeScript error với NgRx |
| `[Authorize]` trên Controller | Story 2.1 | Catalog vẫn cần auth |
| NgRx vendors/resources store | Story 2.1, 2.2 | Pattern mẫu — vendors.actions.ts |
| `provideEffects([...])` trong app.config | Story 2.2 | Thêm LookupsEffects vào mảng hiện tại |

### Lỗi cần tránh

1. **Không lưu catalog xuống DB** — Handler không cần `IWorkforceDbContext`, thuần enum-to-DTO conversion
2. **Không dùng `createFeature`** — dùng `createReducer` để tránh TypeScript lỗi với state không có adapter
3. **Không dùng `type` làm prop name trong NgRx actions** — reserved keyword, sẽ gây lỗi build (xem lỗi Story 2.2)
4. **`forkJoin` từ `'rxjs'`** — không phải `'rxjs/operators'`
5. **`LookupsController` không cần AddApplicationPart riêng** — cùng assembly `Workforce.Api`

---

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

_(Trống)_

### Completion Notes List

- Handler không cần `IWorkforceDbContext` — thuần enum-to-DTO, không query DB
- `LookupsController` cùng assembly `Workforce.Api` — không cần AddApplicationPart riêng
- `forkJoin` từ `'rxjs'` dùng trong effects để load roles + levels song song
- dotnet build: 0 errors; ng build: 0 errors

### File List

**Backend:**
- `src/Modules/Workforce/ProjectManagement.Workforce.Domain/Enums/ResourceRole.cs`
- `src/Modules/Workforce/ProjectManagement.Workforce.Domain/Enums/ResourceLevel.cs`
- `src/Modules/Workforce/ProjectManagement.Workforce.Application/DTOs/LookupItemDto.cs`
- `src/Modules/Workforce/ProjectManagement.Workforce.Application/DTOs/RoleLevelCatalogDto.cs`
- `src/Modules/Workforce/ProjectManagement.Workforce.Application/Lookups/Queries/GetRoleLevelCatalog/GetRoleLevelCatalogQuery.cs`
- `src/Modules/Workforce/ProjectManagement.Workforce.Application/Lookups/Queries/GetRoleLevelCatalog/GetRoleLevelCatalogHandler.cs`
- `src/Modules/Workforce/ProjectManagement.Workforce.Api/Controllers/LookupsController.cs`

**Frontend:**
- `frontend/project-management-web/src/app/features/lookups/models/lookup.model.ts`
- `frontend/project-management-web/src/app/features/lookups/store/lookups.actions.ts`
- `frontend/project-management-web/src/app/features/lookups/store/lookups.reducer.ts`
- `frontend/project-management-web/src/app/features/lookups/store/lookups.selectors.ts`
- `frontend/project-management-web/src/app/features/lookups/store/lookups.effects.ts`
- `frontend/project-management-web/src/app/features/lookups/services/lookups-api.service.ts`
- `frontend/project-management-web/src/app/core/store/app.state.ts` _(updated)_
- `frontend/project-management-web/src/app/app.config.ts` _(updated)_
