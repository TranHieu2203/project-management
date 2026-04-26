# Story 4.4: Cross-project aggregation view (membership-only) + non-leak

Status: review

**Story ID:** 4.4
**Epic:** Epic 4 — Overload Warning (Standard + Predictive) + Cross-project Aggregation
**Sprint:** Sprint 5
**Date Created:** 2026-04-26

---

## Story

As a PM,
I want xem tổng hợp overload cross-project trong phạm vi tôi có quyền,
So that tôi có cái nhìn tổng quan mà không lộ dữ liệu project khác.

## Acceptance Criteria

1. **Given** user là member của nhiều projects
   **When** xem overload cross-project
   **Then** chỉ aggregate trên projects user có membership
   **And** kết quả trả về per-resource: tổng giờ, số ngày OL-01, số tuần OL-02, hasOverload

2. **Given** user không có membership ở bất kỳ project nào
   **When** gọi cross-project endpoint
   **Then** trả về `resources: []`, `projectCount: 0` — không lỗi, không leak

3. **Given** endpoint không nhận `projectId` tham số
   **When** PM cố tình thăm dò project họ không có quyền
   **Then** không thể suy luận project tồn tại — data tự động scoped theo membership

---

## Tasks / Subtasks

- [x] **Task 1: Capacity.Application — GetCrossProjectOverloadQuery**
  - [x] 1.1 Thêm `ProjectReference` tới `Projects.Application` trong `Capacity.Application.csproj`
  - [x] 1.2 Tạo `Queries/GetCrossProjectOverload/GetCrossProjectOverloadQuery.cs` + handler
  - [x] 1.3 Handler logic: lấy projectIds từ membership → entries trong projects → aggregate per resource

- [x] **Task 2: Capacity.Api — endpoint**
  - [x] 2.1 Thêm `GET /api/v1/capacity/cross-project?dateFrom=&dateTo=` vào `CapacityController`
  - [x] 2.2 Controller lấy `currentUserId` từ `ICurrentUserService` và truyền vào query

- [x] **Task 3: Frontend — models + API service**
  - [x] 3.1 Thêm `ResourceOverloadSummary`, `CrossProjectOverloadResult` vào `utilization.model.ts`
  - [x] 3.2 Thêm `getCrossProjectOverload(dateFrom, dateTo)` vào `capacity-api.service.ts`

- [x] **Task 4: Frontend — NgRx store**
  - [x] 4.1 Thêm actions: `loadCrossProject`, `loadCrossProjectSuccess`, `loadCrossProjectFailure`
  - [x] 4.2 Mở rộng `CapacityState` + reducer: thêm `crossProject: CrossProjectOverloadResult | null`, `crossProjectLoading: boolean`
  - [x] 4.3 Thêm effect `loadCrossProject$`

- [x] **Task 5: Frontend — CrossProjectAggregationComponent + routing**
  - [x] 5.1 Tạo `cross-project-aggregation.ts` + `.html`
  - [x] 5.2 Cập nhật `capacity.routes.ts` — thêm route `cross-project`

- [x] **Task 6: Build verification**
  - [x] 6.1 `dotnet build` → 0 errors
  - [x] 6.2 `ng build` → 0 errors

---

## Dev Notes

### Kiến trúc cross-module (pattern từ Story 4.1/4.3)

`Capacity.Application` đã có cross-module ref tới `TimeTracking.Application`. Story 4.4 thêm ref tới `Projects.Application`:

```xml
<!-- Capacity.Application.csproj — thêm vào ItemGroup ref hiện có -->
<ProjectReference Include="..\..\Projects\ProjectManagement.Projects.Application\ProjectManagement.Projects.Application.csproj" />
```

**KHÔNG** tạo thêm Domain/Infrastructure mới — Story 4.4 chỉ thêm query logic, không cần persistence mới.

### Task 1 — GetCrossProjectOverloadQuery

**File:** `src/Modules/Capacity/ProjectManagement.Capacity.Application/Queries/GetCrossProjectOverload/GetCrossProjectOverloadQuery.cs`

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Projects.Application.Common.Interfaces;
using ProjectManagement.TimeTracking.Application.Common.Interfaces;

namespace ProjectManagement.Capacity.Application.Queries.GetCrossProjectOverload;

public sealed record ResourceOverloadSummary(
    Guid ResourceId,
    decimal TotalHours,
    int OverloadedDays,
    int OverloadedWeeks,
    bool HasOverload);

public sealed record CrossProjectOverloadResult(
    IReadOnlyList<ResourceOverloadSummary> Resources,
    DateOnly DateFrom,
    DateOnly DateTo,
    int ProjectCount);

public sealed record GetCrossProjectOverloadQuery(
    Guid CurrentUserId, DateOnly DateFrom, DateOnly DateTo)
    : IRequest<CrossProjectOverloadResult>;

public sealed class GetCrossProjectOverloadHandler
    : IRequestHandler<GetCrossProjectOverloadQuery, CrossProjectOverloadResult>
{
    private readonly IProjectsDbContext _projectsDb;
    private readonly ITimeTrackingDbContext _timeTrackingDb;

    public GetCrossProjectOverloadHandler(
        IProjectsDbContext projectsDb,
        ITimeTrackingDbContext timeTrackingDb)
    {
        _projectsDb = projectsDb;
        _timeTrackingDb = timeTrackingDb;
    }

    public async Task<CrossProjectOverloadResult> Handle(
        GetCrossProjectOverloadQuery query, CancellationToken ct)
    {
        // Step 1: Get project IDs where current user is a member
        var projectIds = await _projectsDb.ProjectMemberships
            .Where(m => m.UserId == query.CurrentUserId)
            .Select(m => m.ProjectId)
            .Distinct()
            .ToListAsync(ct);

        if (projectIds.Count == 0)
            return new CrossProjectOverloadResult([], query.DateFrom, query.DateTo, 0);

        // Step 2: Get non-voided time entries for those projects in date range
        var entries = await _timeTrackingDb.TimeEntries.AsNoTracking()
            .Where(e => projectIds.Contains(e.ProjectId)
                     && !e.IsVoided
                     && e.Date >= query.DateFrom
                     && e.Date <= query.DateTo)
            .Select(e => new { e.ResourceId, e.Date, e.Hours })
            .ToListAsync(ct);

        // Step 3: Aggregate per resource
        var resources = entries
            .GroupBy(e => e.ResourceId)
            .Select(g =>
            {
                var totalHours = g.Sum(e => e.Hours);

                var overloadedDays = g
                    .GroupBy(e => e.Date)
                    .Count(d => d.Sum(e => e.Hours) > 8m);

                var overloadedWeeks = g
                    .GroupBy(e => GetMonday(e.Date))
                    .Count(w => w.Sum(e => e.Hours) > 40m);

                return new ResourceOverloadSummary(
                    g.Key, totalHours, overloadedDays, overloadedWeeks,
                    overloadedDays > 0 || overloadedWeeks > 0);
            })
            .OrderByDescending(r => r.TotalHours)
            .ToList();

        return new CrossProjectOverloadResult(resources, query.DateFrom, query.DateTo, projectIds.Count);
    }

    private static DateOnly GetMonday(DateOnly date)
    {
        var diff = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.AddDays(-diff);
    }
}
```

### Task 2 — API endpoint

`CapacityController` cần inject `ICurrentUserService` (đã có trong Shared.Infrastructure, được register trong `Program.cs`):

```csharp
// Thêm vào CapacityController constructor:
private readonly ICurrentUserService _currentUser;

public CapacityController(IMediator mediator, ICurrentUserService currentUser)
{
    _mediator = mediator;
    _currentUser = currentUser;
}

// Endpoint mới:
[HttpGet("cross-project")]
public async Task<IActionResult> GetCrossProjectOverload(
    [FromQuery] DateOnly dateFrom,
    [FromQuery] DateOnly dateTo,
    CancellationToken ct)
{
    if (dateTo < dateFrom)
        return BadRequest(new { detail = "dateTo phải >= dateFrom." });

    var result = await _mediator.Send(
        new GetCrossProjectOverloadQuery(_currentUser.UserId, dateFrom, dateTo), ct);
    return Ok(result);
}
```

**Không cần** `resourceId` trong cross-project endpoint — data scoped tự động theo membership của current user.

### Task 3 — Frontend models

**Thêm vào `utilization.model.ts`** (KHÔNG tạo file mới):

```typescript
export interface ResourceOverloadSummary {
  resourceId: string;
  totalHours: number;
  overloadedDays: number;
  overloadedWeeks: number;
  hasOverload: boolean;
}

export interface CrossProjectOverloadResult {
  resources: ResourceOverloadSummary[];
  dateFrom: string;
  dateTo: string;
  projectCount: number;
}
```

**`capacity-api.service.ts`** — thêm method:

```typescript
getCrossProjectOverload(dateFrom: string, dateTo: string): Observable<CrossProjectOverloadResult> {
  return this.http.get<CrossProjectOverloadResult>('/api/v1/capacity/cross-project', {
    params: new HttpParams().set('dateFrom', dateFrom).set('dateTo', dateTo),
  });
}
```

### Task 4 — NgRx store

**`capacity.actions.ts`** — thêm:
```typescript
'Load Cross Project': props<{ dateFrom: string; dateTo: string }>(),
'Load Cross Project Success': props<{ result: CrossProjectOverloadResult }>(),
'Load Cross Project Failure': props<{ error: string }>(),
```

**`capacity.reducer.ts`** — mở rộng state:
```typescript
crossProject: CrossProjectOverloadResult | null;
crossProjectLoading: boolean;

// initialState:
crossProject: null,
crossProjectLoading: false,

// Reducer cases:
on(CapacityActions.loadCrossProject, state => ({ ...state, crossProjectLoading: true })),
on(CapacityActions.loadCrossProjectSuccess, (state, { result }) => ({
  ...state, crossProjectLoading: false, crossProject: result,
})),
on(CapacityActions.loadCrossProjectFailure, state => ({ ...state, crossProjectLoading: false })),
```

Selectors auto-generated bởi `createFeature`: `selectCrossProject`, `selectCrossProjectLoading`.

**`capacity.effects.ts`** — thêm:
```typescript
loadCrossProject$ = createEffect(() =>
  this.actions$.pipe(
    ofType(CapacityActions.loadCrossProject),
    switchMap(({ dateFrom, dateTo }) =>
      this.api.getCrossProjectOverload(dateFrom, dateTo).pipe(
        map(result => CapacityActions.loadCrossProjectSuccess({ result })),
        catchError(err => of(CapacityActions.loadCrossProjectFailure({ error: err?.message ?? 'Lỗi.' })))
      )
    )
  )
);
```

### Task 5 — CrossProjectAggregationComponent

**`cross-project-aggregation.ts`:**
```typescript
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { AsyncPipe, DecimalPipe, NgClass, NgIf } from '@angular/common';
import { Store } from '@ngrx/store';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { CapacityActions } from '../../store/capacity.actions';
import { selectCrossProject, selectCrossProjectLoading } from '../../store/capacity.reducer';

@Component({
  selector: 'app-cross-project-aggregation',
  standalone: true,
  imports: [
    ReactiveFormsModule, AsyncPipe, DecimalPipe, NgIf, NgClass,
    MatButtonModule, MatCardModule, MatFormFieldModule, MatInputModule,
    MatProgressSpinnerModule, MatTableModule,
  ],
  templateUrl: './cross-project-aggregation.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CrossProjectAggregationComponent {
  private readonly store = inject(Store);
  private readonly fb = inject(FormBuilder);

  readonly result$ = this.store.select(selectCrossProject);
  readonly loading$ = this.store.select(selectCrossProjectLoading);

  readonly form = this.fb.nonNullable.group({
    dateFrom: ['', Validators.required],
    dateTo: ['', Validators.required],
  });

  readonly columns = ['resourceId', 'totalHours', 'overloadedDays', 'overloadedWeeks', 'status'];

  load(): void {
    if (this.form.invalid) return;
    const { dateFrom, dateTo } = this.form.getRawValue();
    this.store.dispatch(CapacityActions.loadCrossProject({ dateFrom, dateTo }));
  }
}
```

**`cross-project-aggregation.html`:**
```html
<mat-card>
  <mat-card-header>
    <mat-card-title>Tổng hợp Overload Cross-Project</mat-card-title>
  </mat-card-header>
  <mat-card-content>
    <form [formGroup]="form" (ngSubmit)="load()" style="display:flex;gap:12px;align-items:flex-start;flex-wrap:wrap;margin-bottom:16px">
      <mat-form-field style="width:160px">
        <mat-label>Từ ngày (yyyy-MM-dd)</mat-label>
        <input matInput formControlName="dateFrom" placeholder="2026-04-01" />
      </mat-form-field>
      <mat-form-field style="width:160px">
        <mat-label>Đến ngày (yyyy-MM-dd)</mat-label>
        <input matInput formControlName="dateTo" placeholder="2026-04-30" />
      </mat-form-field>
      <button mat-raised-button color="primary" type="submit" [disabled]="form.invalid || (loading$ | async)">
        Xem tổng hợp
      </button>
      <mat-spinner *ngIf="loading$ | async" diameter="24" style="align-self:center"></mat-spinner>
    </form>

    <ng-container *ngIf="result$ | async as result">
      <p style="color:#666;margin-bottom:12px">
        Phạm vi: {{ result.projectCount }} project bạn có quyền. {{ result.resources.length }} nhân sự có time entries.
      </p>
      <p *ngIf="result.resources.length === 0" style="color:#666">Không có time entries trong khoảng ngày này.</p>

      <table *ngIf="result.resources.length > 0" mat-table [dataSource]="result.resources" style="width:100%">
        <ng-container matColumnDef="resourceId">
          <th mat-header-cell *matHeaderCellDef>Resource ID</th>
          <td mat-cell *matCellDef="let row">{{ row.resourceId }}</td>
        </ng-container>
        <ng-container matColumnDef="totalHours">
          <th mat-header-cell *matHeaderCellDef>Tổng giờ</th>
          <td mat-cell *matCellDef="let row">{{ row.totalHours | number:'1.1-1' }}h</td>
        </ng-container>
        <ng-container matColumnDef="overloadedDays">
          <th mat-header-cell *matHeaderCellDef>Ngày OL-01</th>
          <td mat-cell *matCellDef="let row">{{ row.overloadedDays }}</td>
        </ng-container>
        <ng-container matColumnDef="overloadedWeeks">
          <th mat-header-cell *matHeaderCellDef>Tuần OL-02</th>
          <td mat-cell *matCellDef="let row">{{ row.overloadedWeeks }}</td>
        </ng-container>
        <ng-container matColumnDef="status">
          <th mat-header-cell *matHeaderCellDef>Trạng thái</th>
          <td mat-cell *matCellDef="let row">
            <span *ngIf="row.hasOverload" style="color:#c62828;font-weight:600">Overload</span>
            <span *ngIf="!row.hasOverload" style="color:#2e7d32">OK</span>
          </td>
        </ng-container>
        <tr mat-header-row *matHeaderRowDef="columns"></tr>
        <tr mat-row *matRowDef="let row; columns: columns;" [ngClass]="{'overload-row': row.hasOverload}"></tr>
      </table>
    </ng-container>
  </mat-card-content>
</mat-card>

<style>
  .overload-row td { background: #fff5f5; }
</style>
```

**`capacity.routes.ts`** — thêm route:
```typescript
export const capacityRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./components/overload-dashboard/overload-dashboard').then(m => m.OverloadDashboardComponent),
  },
  {
    path: 'cross-project',
    loadComponent: () =>
      import('./components/cross-project-aggregation/cross-project-aggregation').then(m => m.CrossProjectAggregationComponent),
  },
];
```

### Non-leak guarantee — cách hoạt động

| Tình huống | Kết quả |
|---|---|
| User A là member project P1, P2 | Chỉ thấy resources có entries trong P1, P2 |
| User A không phải member project P3 | P3's resources không xuất hiện — invisible |
| Hacker gọi endpoint, không member bất kỳ đâu | `resources: [], projectCount: 0` |
| Không có `projectId` param | Không thể probe existence của specific project |

### Patterns đã có — KHÔNG viết lại

| Pattern | Source |
|---|---|
| `IProjectsDbContext.ProjectMemberships` filter by userId | `GetProjectListHandler.cs` — `p.Members.Any(m => m.UserId == ...)` |
| `ITimeTrackingDbContext` cross-module query | `GetResourceOverloadQuery.cs` (Story 4.1) |
| `GetMonday()` helper | `GetResourceOverloadQuery.cs` — copy y nguyên |
| OL-01 (>8h/day), OL-02 (>40h/week) rules | Story 4.1 pattern |
| `ICurrentUserService.UserId` trong Controller | `ProjectsController.cs` constructor |
| `createActionGroup` + `createFeature` | Existing `capacity.actions.ts` + `capacity.reducer.ts` |
| `switchMap` effect | Existing `capacity.effects.ts` |
| Standalone component pattern | `OverloadDashboardComponent`, `TrafficLightWidgetComponent` |

### Anti-patterns — KHÔNG làm

- **KHÔNG** thêm `projectId` param vào endpoint — đó chính là non-leak: user không thể specify project họ không có quyền
- **KHÔNG** tạo thêm Domain/Infrastructure project — Story 4.4 chỉ cần query logic
- **KHÔNG** query tất cả entries rồi join với memberships trên client — filter phải chạy phía server
- **KHÔNG** thêm navigation link cross-project vào dashboard hiện tại — chỉ cần route, không cần sidebar update (scope của story)
- **KHÔNG** register `IProjectsDbContext` trong CapacityModule — nó đã được register bởi `AddProjectsModule` trong Host; chỉ cần inject

### MediatR handler registration

Tất cả handlers mới (kể cả `GetCrossProjectOverloadHandler`) trong `Capacity.Application` assembly được auto-registered bởi:
```csharp
cfg.RegisterServicesFromAssembly(typeof(GetResourceOverloadHandler).Assembly);
```
KHÔNG cần thêm registration riêng.

---

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

- IDE hint "CrossProjectOverloadResult is declared but never read" on import — fixed immediately by completing reducer state extension before moving to next task.

### Completion Notes List

- `GetCrossProjectOverloadHandler` nhận cả `IProjectsDbContext` + `ITimeTrackingDbContext` — hai interface được register bởi các module khác nhau, DI container tự resolve.
- Non-leak: endpoint không nhận `projectId` param — user chỉ thấy data trong projects họ là member, không thể probe existence.
- `GetMonday()` copy y nguyên từ `GetResourceOverloadQuery` — không tạo shared helper để tránh circular dep.
- `CrossProjectAggregationComponent` là standalone component với lazy route — bundle tách riêng khỏi overload-dashboard.

### File List

- `src/Modules/Capacity/ProjectManagement.Capacity.Application/ProjectManagement.Capacity.Application.csproj` (modified — added Projects.Application ref)
- `src/Modules/Capacity/ProjectManagement.Capacity.Application/Queries/GetCrossProjectOverload/GetCrossProjectOverloadQuery.cs` (new)
- `src/Modules/Capacity/ProjectManagement.Capacity.Api/Controllers/CapacityController.cs` (modified — ICurrentUserService injection + cross-project endpoint)
- `frontend/.../capacity/models/utilization.model.ts` (modified — ResourceOverloadSummary, CrossProjectOverloadResult)
- `frontend/.../capacity/services/capacity-api.service.ts` (modified — getCrossProjectOverload)
- `frontend/.../capacity/store/capacity.actions.ts` (modified — loadCrossProject actions)
- `frontend/.../capacity/store/capacity.reducer.ts` (modified — crossProject state + selectors)
- `frontend/.../capacity/store/capacity.effects.ts` (modified — loadCrossProject$ effect)
- `frontend/.../capacity/components/cross-project-aggregation/cross-project-aggregation.ts` (new)
- `frontend/.../capacity/components/cross-project-aggregation/cross-project-aggregation.html` (new)
- `frontend/.../capacity/capacity.routes.ts` (modified — added cross-project route)
