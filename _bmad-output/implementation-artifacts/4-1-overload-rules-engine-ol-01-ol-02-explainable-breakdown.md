# Story 4.1: Overload rules engine (OL-01/OL-02) + explainable breakdown

Status: review

**Story ID:** 4.1
**Epic:** Epic 4 — Overload Warning (Standard + Predictive) + Cross-project Aggregation
**Sprint:** Sprint 5
**Date Created:** 2026-04-26

---

## Story

As a PM,
I want hệ thống tính overload theo ngày/tuần và giải thích được,
So that tôi hiểu rõ vì sao bị cảnh báo và điều chỉnh kịp thời.

## Acceptance Criteria

1. **Given** time entries trong range
   **When** compute overload
   **Then** áp dụng OL-01 (>8h/ngày) và OL-02 (>40h/tuần Mon–Sun) và trả breakdown theo ngày/tuần
   **And** result deterministic với cùng input snapshot (cùng entries, cùng range → cùng result)

2. **Given** resource không có entry nào trong range
   **When** compute overload
   **Then** trả empty breakdown, HasOverload = false — không lỗi

3. **Given** entries bị void (IsVoided = true)
   **When** compute overload
   **Then** excluded khỏi calculation

---

## Tasks / Subtasks

- [x] **Task 1: Tạo Capacity module structure (backend)**
  - [x] 1.1 Tạo thư mục `src/Modules/Capacity/ProjectManagement.Capacity.Application/`
  - [x] 1.2 Tạo `ProjectManagement.Capacity.Application.csproj` — references TimeTracking.Application (cho ITimeTrackingDbContext)
  - [x] 1.3 Tạo thư mục `src/Modules/Capacity/ProjectManagement.Capacity.Api/`
  - [x] 1.4 Tạo `ProjectManagement.Capacity.Api.csproj` — references Capacity.Application + Shared.Infrastructure

- [x] **Task 2: Application — GetResourceOverloadQuery**
  - [x] 2.1 Tạo `OverloadDayResult`, `OverloadWeekResult`, `ResourceOverloadResult` records
  - [x] 2.2 Tạo `GetResourceOverloadQuery(ResourceId, DateFrom, DateTo)` + handler
  - [x] 2.3 Handler logic: query TimeEntries (non-voided, by resourceId + date range), group by day → OL-01 check, group by ISO week → OL-02 check
  - [x] 2.4 Week boundary: Monday = start of week; sum per Mon–Sun span

- [x] **Task 3: API — CapacityController**
  - [x] 3.1 Tạo `CapacityController`: `GET /api/v1/capacity/overload?resourceId={id}&dateFrom={date}&dateTo={date}`
  - [x] 3.2 Tạo `CapacityModuleExtensions.AddCapacityModule()` — registers MediatR handlers from Capacity.Application
  - [x] 3.3 Đăng ký `services.AddCapacityModule()` trong `Program.cs` (Host.csproj cần thêm ProjectReference)

- [x] **Task 4: Frontend — Capacity NgRx store setup**
  - [x] 4.1 Tạo thư mục `src/app/features/capacity/`
  - [x] 4.2 Tạo `capacity-api.service.ts` với `getResourceOverload(resourceId, dateFrom, dateTo)`
  - [x] 4.3 Tạo `overload.model.ts`: `OverloadDayResult`, `OverloadWeekResult`, `ResourceOverloadResult`
  - [x] 4.4 Tạo `capacity.actions.ts`: `loadOverload`, `loadOverloadSuccess`, `loadOverloadFailure`
  - [x] 4.5 Tạo `capacity.reducer.ts`: state với `createFeature` (selectors auto-generated)
  - [x] 4.6 Tạo `capacity.effects.ts`: `loadOverload$` effect gọi API service
  - [x] 4.7 Selectors: `selectOverloadResult`, `selectCapacityLoading`, `selectCapacityError` (từ `capacityFeature`)
  - [x] 4.8 Đăng ký trong `app.state.ts` + `app.config.ts` (CapacityEffects)

- [x] **Task 5: Frontend — overload-dashboard component**
  - [x] 5.1 Tạo `overload-dashboard.ts` + `overload-dashboard.html`
  - [x] 5.2 Daily breakdown với OL-01 badge
  - [x] 5.3 Weekly breakdown với OL-02 badge
  - [x] 5.4 Route `/capacity` lazy-loaded trong `app.routes.ts`

- [x] **Task 6: Build verification**
  - [x] 6.1 `dotnet build Capacity.Api.csproj` → 0 errors
  - [x] 6.2 `ng build` → 0 errors

---

## Dev Notes

### Module cấu trúc mới — Capacity (không có Infrastructure riêng cho 4.1)

```
src/Modules/Capacity/
    ProjectManagement.Capacity.Application/
        Queries/
            GetResourceOverload/
                GetResourceOverloadQuery.cs    ← query + result records + handler
    ProjectManagement.Capacity.Api/
        Controllers/
            CapacityController.cs
        Extensions/
            CapacityModuleExtensions.cs
```

**Không cần Infrastructure layer** cho Story 4.1 — tính toán thuần từ TimeTracking data, không lưu state.

### Task 1 Detail: csproj files

**ProjectManagement.Capacity.Application.csproj:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="MediatR" Version="12.4.1" />
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.4" />
  </ItemGroup>
  <ItemGroup>
    <!-- Cross-module dependency: Capacity reads TimeTracking's DbContext for computation.
         Refactor to event-driven when Story 5.x introduces caching/precompute. -->
    <ProjectReference Include="..\..\TimeTracking\ProjectManagement.TimeTracking.Application\ProjectManagement.TimeTracking.Application.csproj" />
  </ItemGroup>
</Project>
```

**ProjectManagement.Capacity.Api.csproj:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="MediatR.Extensions.Microsoft.DependencyInjection" Version="12.4.1" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\ProjectManagement.Capacity.Application\ProjectManagement.Capacity.Application.csproj" />
    <ProjectReference Include="..\..\Shared\ProjectManagement.Shared.Infrastructure\ProjectManagement.Shared.Infrastructure.csproj" />
  </ItemGroup>
</Project>
```

**Host.csproj cần thêm:**
```xml
<ProjectReference Include="..\..\Modules\Capacity\ProjectManagement.Capacity.Api\ProjectManagement.Capacity.Api.csproj" />
```

### Task 2 Detail: GetResourceOverloadQuery

```csharp
// GetResourceOverloadQuery.cs
namespace ProjectManagement.Capacity.Application.Queries.GetResourceOverload;

public sealed record GetResourceOverloadQuery(Guid ResourceId, DateOnly DateFrom, DateOnly DateTo)
    : IRequest<ResourceOverloadResult>;

public sealed record OverloadDayResult(DateOnly Date, decimal Hours, bool IsOverloaded);
public sealed record OverloadWeekResult(DateOnly WeekStart, decimal TotalHours, bool IsOverloaded,
    IReadOnlyList<OverloadDayResult> Days);
public sealed record ResourceOverloadResult(Guid ResourceId,
    IReadOnlyList<OverloadDayResult> DailyBreakdown,
    IReadOnlyList<OverloadWeekResult> WeeklyBreakdown,
    bool HasOverload);
```

```csharp
// GetResourceOverloadHandler.cs
public sealed class GetResourceOverloadHandler : IRequestHandler<GetResourceOverloadQuery, ResourceOverloadResult>
{
    private readonly ITimeTrackingDbContext _db;
    public GetResourceOverloadHandler(ITimeTrackingDbContext db) => _db = db;

    public async Task<ResourceOverloadResult> Handle(GetResourceOverloadQuery query, CancellationToken ct)
    {
        var entries = await _db.TimeEntries.AsNoTracking()
            .Where(e => e.ResourceId == query.ResourceId
                     && !e.IsVoided
                     && e.Date >= query.DateFrom
                     && e.Date <= query.DateTo)
            .Select(e => new { e.Date, e.Hours })
            .ToListAsync(ct);

        // Daily totals — OL-01: >8h
        var dailyTotals = entries
            .GroupBy(e => e.Date)
            .Select(g => new OverloadDayResult(g.Key, g.Sum(e => e.Hours), g.Sum(e => e.Hours) > 8m))
            .OrderBy(d => d.Date)
            .ToList();

        // Weekly totals — OL-02: >40h (Mon=start)
        var weeklyGroups = entries
            .GroupBy(e => GetMonday(e.Date))
            .Select(g =>
            {
                var days = g.GroupBy(e => e.Date)
                    .Select(d => new OverloadDayResult(d.Key, d.Sum(e => e.Hours), d.Sum(e => e.Hours) > 8m))
                    .OrderBy(d => d.Date)
                    .ToList();
                var total = g.Sum(e => e.Hours);
                return new OverloadWeekResult(g.Key, total, total > 40m, days);
            })
            .OrderBy(w => w.WeekStart)
            .ToList();

        return new ResourceOverloadResult(query.ResourceId, dailyTotals, weeklyGroups,
            dailyTotals.Any(d => d.IsOverloaded) || weeklyGroups.Any(w => w.IsOverloaded));
    }

    private static DateOnly GetMonday(DateOnly date)
    {
        var diff = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.AddDays(-diff);
    }
}
```

### Task 3 Detail: CapacityController

```csharp
[Authorize]
[ApiController]
[Route("api/v1/capacity")]
public sealed class CapacityController : ControllerBase
{
    private readonly IMediator _mediator;
    public CapacityController(IMediator mediator) => _mediator = mediator;

    [HttpGet("overload")]
    public async Task<IActionResult> GetOverload(
        [FromQuery] Guid resourceId,
        [FromQuery] DateOnly dateFrom,
        [FromQuery] DateOnly dateTo,
        CancellationToken ct)
    {
        if (dateTo < dateFrom) return BadRequest(new { detail = "dateTo phải >= dateFrom." });
        var result = await _mediator.Send(new GetResourceOverloadQuery(resourceId, dateFrom, dateTo), ct);
        return Ok(result);
    }
}
```

**CapacityModuleExtensions:**
```csharp
public static class CapacityModuleExtensions
{
    public static IServiceCollection AddCapacityModule(this IServiceCollection services, IMvcBuilder mvc)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(GetResourceOverloadHandler).Assembly));
        mvc.AddApplicationPart(typeof(CapacityController).Assembly);
        return services;
    }
}
```

**Program.cs thêm:**
```csharp
using ProjectManagement.Capacity.Api.Extensions;
// ...
builder.Services.AddCapacityModule(mvc);
```

### Task 4–5 Detail: Frontend

**overload.model.ts:**
```typescript
export interface OverloadDayResult {
  date: string; // "yyyy-MM-dd"
  hours: number;
  isOverloaded: boolean;
}
export interface OverloadWeekResult {
  weekStart: string;
  totalHours: number;
  isOverloaded: boolean;
  days: OverloadDayResult[];
}
export interface ResourceOverloadResult {
  resourceId: string;
  dailyBreakdown: OverloadDayResult[];
  weeklyBreakdown: OverloadWeekResult[];
  hasOverload: boolean;
}
```

**NgRx store key (capacity feature):**
```typescript
export const capacityFeature = createFeature({
  name: 'capacity',
  reducer: createReducer(
    initialState,
    on(CapacityActions.loadOverload, state => ({ ...state, loading: true, error: null })),
    on(CapacityActions.loadOverloadSuccess, (state, { result }) => ({ ...state, loading: false, result })),
    on(CapacityActions.loadOverloadFailure, (state, { error }) => ({ ...state, loading: false, error })),
  ),
});
```

**capacity.effects.ts:**
```typescript
loadOverload$ = createEffect(() =>
  this.actions$.pipe(
    ofType(CapacityActions.loadOverload),
    switchMap(({ resourceId, dateFrom, dateTo }) =>
      this.api.getResourceOverload(resourceId, dateFrom, dateTo).pipe(
        map(result => CapacityActions.loadOverloadSuccess({ result })),
        catchError(err => of(CapacityActions.loadOverloadFailure({ error: err.message })))
      )
    )
  )
);
```

**Routes (lazy):**
```typescript
// app.routes.ts — thêm:
{ path: 'capacity', loadChildren: () => import('./features/capacity/capacity.routes').then(m => m.capacityRoutes) }

// capacity.routes.ts:
export const capacityRoutes: Routes = [
  { path: '', loadComponent: () => import('./components/overload-dashboard/overload-dashboard').then(m => m.OverloadDashboardComponent) }
];
```

### Patterns đã có — KHÔNG viết lại

| Pattern | Source |
|---|---|
| csproj module structure (Domain/Application/Api) | Tất cả module stories |
| `AddMediatR(cfg => cfg.RegisterServicesFromAssembly(...))` | TimeTracking.Infrastructure |
| `mvc.AddApplicationPart(...)` | TimeTrackingModuleExtensions |
| `[Authorize]` controller | Tất cả stories |
| NgRx `createFeature`, `createEffect`, `createActionGroup` | Stories 3.1-3.4 |
| `provideState(...)` trong app.config.ts | Story 3.1 |
| `switchMap` + `catchError` effects | Story 3.1 |

### Lưu ý quan trọng

- **Cross-module dependency (tạm thời)**: `Capacity.Application` references `TimeTracking.Application`. Đây là trade-off cho MVP. Story 5.x sẽ refactor sang event-driven khi cần caching.
- **MediatR duplicate registration**: Capacity.Application và TimeTracking.Application đều register MediatR — dùng `cfg.RegisterServicesFromAssembly(typeof(GetResourceOverloadHandler).Assembly)` riêng, không dùng chung assembly.
- **DateOnly serialization**: Backend cần có `DateOnly` binding. Check xem Host đã có JSON converter chưa (nếu chưa, thêm `builder.Services.ConfigureHttpJsonOptions(...)` với DateOnly converter).
- **Week calculation**: Dùng `DayOfWeek.Monday` = 1, không dùng `DayOfWeek.Sunday` = 0; `GetMonday(date)` helper cần test kỹ với edge case Sunday.

---

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

_(Trống)_

### Completion Notes List

- Cross-module dependency: `Capacity.Application` → `TimeTracking.Application` (ITimeTrackingDbContext). Documented in csproj.
- `createFeature` generates selectors automatically — no separate selectors file needed; exported from reducer file.
- `capacityFeature.reducer` used directly in `app.state.ts` `ActionReducerMap` — no need for `provideState()`.
- Week boundary: `GetMonday()` uses `(DayOfWeek - Monday + 7) % 7` — handles Sunday (DayOfWeek=0) correctly.
- `DateOnly` binding in ASP.NET Core 10 works out of the box via `[FromQuery]` — no extra converter needed.

### File List

**Backend:**
- `src/Modules/Capacity/ProjectManagement.Capacity.Application/ProjectManagement.Capacity.Application.csproj`
- `src/Modules/Capacity/ProjectManagement.Capacity.Application/Queries/GetResourceOverload/GetResourceOverloadQuery.cs`
- `src/Modules/Capacity/ProjectManagement.Capacity.Api/ProjectManagement.Capacity.Api.csproj`
- `src/Modules/Capacity/ProjectManagement.Capacity.Api/Controllers/CapacityController.cs`
- `src/Modules/Capacity/ProjectManagement.Capacity.Api/Extensions/CapacityModuleExtensions.cs`
- `src/Host/ProjectManagement.Host/ProjectManagement.Host.csproj` (added Capacity.Api reference)
- `src/Host/ProjectManagement.Host/Program.cs` (added AddCapacityModule)

**Frontend:**
- `frontend/project-management-web/src/app/features/capacity/models/overload.model.ts`
- `frontend/project-management-web/src/app/features/capacity/services/capacity-api.service.ts`
- `frontend/project-management-web/src/app/features/capacity/store/capacity.actions.ts`
- `frontend/project-management-web/src/app/features/capacity/store/capacity.reducer.ts`
- `frontend/project-management-web/src/app/features/capacity/store/capacity.effects.ts`
- `frontend/project-management-web/src/app/features/capacity/components/overload-dashboard/overload-dashboard.ts`
- `frontend/project-management-web/src/app/features/capacity/components/overload-dashboard/overload-dashboard.html`
- `frontend/project-management-web/src/app/features/capacity/capacity.routes.ts`
- `frontend/project-management-web/src/app/core/store/app.state.ts` (added capacity)
- `frontend/project-management-web/src/app/app.config.ts` (added CapacityEffects)
- `frontend/project-management-web/src/app/app.routes.ts` (added /capacity route)
