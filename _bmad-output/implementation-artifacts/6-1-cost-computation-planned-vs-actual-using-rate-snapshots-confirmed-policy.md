# Story 6.1: Cost computation (planned vs actual) using rate snapshots + confirmed policy

Status: review

**Story ID:** 6.1
**Epic:** Epic 6 — Cost Tracking & Official Reporting (Confirmed vs Estimated) + Export
**Sprint:** Sprint 7
**Date Created:** 2026-04-26

---

## Story

As a PM,
I want hệ thống tính cost planned vs actual theo rule confirmed/estimated,
So that báo cáo phản ánh đúng độ tin cậy dữ liệu.

## Acceptance Criteria

1. **Given** có TimeEntries với nhiều EntryType
   **When** gọi `GET /api/v1/reports/cost?dateFrom=YYYY-MM-DD&dateTo=YYYY-MM-DD`
   **Then** chỉ dùng `VendorConfirmed` (outsource) và `PmAdjusted` (inhouse) cho official cost
   **And** trả về `estimatedCost`, `officialCost`, `confirmedPct` (= officialCost / (officialCost + estimatedCost) * 100)
   **And** kết quả chỉ chứa dữ liệu từ các projects user là member

2. **Given** filter `projectId` optional
   **When** cung cấp `projectId`
   **Then** chỉ tính cho project đó (vẫn kiểm tra membership)

3. **Given** UI dashboard
   **When** render cost-dashboard
   **Then** hiển thị summary card (estimatedCost, officialCost, confirmedPct) + breakdown theo project

---

## Tasks / Subtasks

- [x] **Task 1: Tạo Reporting module structure (4 csproj)**
  - [x] 1.1 Tạo `src/Modules/Reporting/ProjectManagement.Reporting.Domain/` — Domain.csproj
  - [x] 1.2 Tạo `src/Modules/Reporting/ProjectManagement.Reporting.Application/` — Application.csproj (ref Domain + TimeTracking.Application + Projects.Application)
  - [x] 1.3 Tạo `src/Modules/Reporting/ProjectManagement.Reporting.Infrastructure/` — Infrastructure.csproj (ref Application + Shared.Infrastructure)
  - [x] 1.4 Tạo `src/Modules/Reporting/ProjectManagement.Reporting.Api/` — Api.csproj (ref Application + Infrastructure + Shared.Infrastructure)
  - [x] 1.5 Thêm `ProjectManagement.Reporting.Api` vào `ProjectManagement.slnx`

- [x] **Task 2: Reporting.Application — GetCostSummaryQuery**
  - [x] 2.1 Tạo `Queries/GetCostSummary/GetCostSummaryQuery.cs` + handler
  - [x] 2.2 Handler: lấy projectIds từ `IProjectsDbContext.ProjectMemberships` theo `CurrentUserId`; áp dụng filter `ProjectId` nếu có
  - [x] 2.3 Query `ITimeTrackingDbContext.TimeEntries` WHERE `ProjectId IN projectIds AND Date BETWEEN dateFrom AND dateTo AND IsVoided = false`
  - [x] 2.4 Tính `officialCost` = SUM(CostAtTime) WHERE EntryType IN ("VendorConfirmed", "PmAdjusted")
  - [x] 2.5 Tính `estimatedCost` = SUM(CostAtTime) WHERE EntryType = "Estimated"
  - [x] 2.6 Tính `confirmedPct` = `officialCost / (officialCost + estimatedCost) * 100` (0 nếu mẫu = 0)
  - [x] 2.7 Group-by-project cho `byProject[]` breakdown

- [x] **Task 3: Reporting.Api — ReportingController**
  - [x] 3.1 Tạo `Controllers/ReportingController.cs` — endpoint `GET /api/v1/reports/cost`
  - [x] 3.2 Tạo `Extensions/ReportingModuleExtensions.cs` — `AddReportingModule()`

- [x] **Task 4: Wiring Host**
  - [x] 4.1 Thêm `<ProjectReference>` đến `ProjectManagement.Reporting.Api.csproj` trong `ProjectManagement.Host.csproj`
  - [x] 4.2 Thêm `services.AddReportingModule(builder.Configuration, mvc)` vào `Program.cs`
  - [x] 4.3 Không cần migrate ReportingDb trong Story 6-1 (không có entity)

- [x] **Task 5: Frontend — models + service**
  - [x] 5.1 Tạo `features/reporting/models/cost-report.model.ts` — interfaces `CostProjectBreakdown`, `CostSummaryResult`
  - [x] 5.2 Tạo `features/reporting/services/reporting-api.service.ts` — `getCostSummary(dateFrom, dateTo, projectId?)`

- [x] **Task 6: Frontend — NgRx store**
  - [x] 6.1 Tạo `features/reporting/store/reporting.actions.ts` — `createActionGroup({ source: 'Reporting', events: {...} })`
  - [x] 6.2 Tạo `features/reporting/store/reporting.reducer.ts` — `createFeature({ name: 'reporting', reducer })`
  - [x] 6.3 Tạo `features/reporting/store/reporting.effects.ts` — `loadCostSummary$` effect
  - [x] 6.4 Đăng ký reducer trong `app.state.ts` và `app.config.ts`

- [x] **Task 7: Frontend — CostDashboardComponent**
  - [x] 7.1 Tạo `features/reporting/components/cost-dashboard/cost-dashboard.ts` + `cost-dashboard.html`
  - [x] 7.2 Tạo `features/reporting/reporting.routes.ts` — path `cost`
  - [x] 7.3 Đăng ký `{ path: 'reporting', ... }` trong `app.routes.ts`

- [x] **Task 8: Build verification**
  - [x] 8.1 `dotnet build` → 0 errors
  - [x] 8.2 `ng build` → 0 errors

---

## Dev Notes

### Tổng quan: Reporting là module MỚI, phải tạo toàn bộ structure

Story 6-1 tạo skeleton module Reporting và implement `GetCostSummaryQuery`. Không cần DB/migrations — query đọc cross-module qua DI (giống Capacity module pattern).

---

### Task 1 — Cấu trúc csproj files

**Theo cùng pattern với Capacity module.** Dưới đây là nội dung cụ thể cho từng file:

**`ProjectManagement.Reporting.Domain.csproj`**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\..\Shared\ProjectManagement.Shared.Domain\ProjectManagement.Shared.Domain.csproj" />
  </ItemGroup>
</Project>
```

**`ProjectManagement.Reporting.Application.csproj`**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="MediatR" Version="12.4.1" />
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.4" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\ProjectManagement.Reporting.Domain\ProjectManagement.Reporting.Domain.csproj" />
    <ProjectReference Include="..\..\TimeTracking\ProjectManagement.TimeTracking.Application\ProjectManagement.TimeTracking.Application.csproj" />
    <ProjectReference Include="..\..\Projects\ProjectManagement.Projects.Application\ProjectManagement.Projects.Application.csproj" />
  </ItemGroup>
</Project>
```

**`ProjectManagement.Reporting.Infrastructure.csproj`**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.7">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.1" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\ProjectManagement.Reporting.Application\ProjectManagement.Reporting.Application.csproj" />
    <ProjectReference Include="..\..\..\Shared\ProjectManagement.Shared.Infrastructure\ProjectManagement.Shared.Infrastructure.csproj" />
  </ItemGroup>
</Project>
```

**`ProjectManagement.Reporting.Api.csproj`**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\ProjectManagement.Reporting.Application\ProjectManagement.Reporting.Application.csproj" />
    <ProjectReference Include="..\ProjectManagement.Reporting.Infrastructure\ProjectManagement.Reporting.Infrastructure.csproj" />
    <ProjectReference Include="..\..\..\Shared\ProjectManagement.Shared.Infrastructure\ProjectManagement.Shared.Infrastructure.csproj" />
  </ItemGroup>
</Project>
```

**File structure mục tiêu:**
```
src/Modules/Reporting/
├── ProjectManagement.Reporting.Domain/
│   └── ProjectManagement.Reporting.Domain.csproj
├── ProjectManagement.Reporting.Application/
│   ├── Queries/
│   │   └── GetCostSummary/
│   │       └── GetCostSummaryQuery.cs
│   └── ProjectManagement.Reporting.Application.csproj
├── ProjectManagement.Reporting.Infrastructure/
│   └── ProjectManagement.Reporting.Infrastructure.csproj
└── ProjectManagement.Reporting.Api/
    ├── Controllers/
    │   └── ReportingController.cs
    ├── Extensions/
    │   └── ReportingModuleExtensions.cs
    └── ProjectManagement.Reporting.Api.csproj
```

---

### Task 2 — GetCostSummaryQuery

**File:** `src/Modules/Reporting/ProjectManagement.Reporting.Application/Queries/GetCostSummary/GetCostSummaryQuery.cs`

**EntryType string values (từ TimeTracking module):**
- `"Estimated"` — ước tính; KHÔNG dùng cho official cost
- `"PmAdjusted"` — PM điều chỉnh (inhouse); DÙNG cho official cost
- `"VendorConfirmed"` — vendor confirmed (outsource); DÙNG cho official cost

**Cost đã snapshot trong TimeEntry.CostAtTime** — không cần tính lại; chỉ SUM.

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Projects.Application.Common.Interfaces;
using ProjectManagement.TimeTracking.Application.Common.Interfaces;

namespace ProjectManagement.Reporting.Application.Queries.GetCostSummary;

public sealed record CostProjectBreakdown(
    Guid ProjectId,
    decimal EstimatedCost,
    decimal OfficialCost,
    decimal VendorConfirmedCost,
    decimal PmAdjustedCost,
    decimal ConfirmedPct);

public sealed record CostSummaryResult(
    DateOnly DateFrom,
    DateOnly DateTo,
    int ProjectCount,
    decimal TotalEstimatedCost,
    decimal TotalOfficialCost,
    decimal ConfirmedPct,
    IReadOnlyList<CostProjectBreakdown> ByProject);

public sealed record GetCostSummaryQuery(
    Guid CurrentUserId,
    DateOnly DateFrom,
    DateOnly DateTo,
    Guid? ProjectId = null)
    : IRequest<CostSummaryResult>;

public sealed class GetCostSummaryHandler
    : IRequestHandler<GetCostSummaryQuery, CostSummaryResult>
{
    private readonly IProjectsDbContext _projectsDb;
    private readonly ITimeTrackingDbContext _timeTrackingDb;

    public GetCostSummaryHandler(
        IProjectsDbContext projectsDb,
        ITimeTrackingDbContext timeTrackingDb)
    {
        _projectsDb = projectsDb;
        _timeTrackingDb = timeTrackingDb;
    }

    public async Task<CostSummaryResult> Handle(
        GetCostSummaryQuery query, CancellationToken ct)
    {
        var projectIds = await _projectsDb.ProjectMemberships
            .Where(m => m.UserId == query.CurrentUserId)
            .Select(m => m.ProjectId)
            .Distinct()
            .ToListAsync(ct);

        if (query.ProjectId.HasValue)
        {
            if (!projectIds.Contains(query.ProjectId.Value))
                return EmptyResult(query);
            projectIds = new List<Guid> { query.ProjectId.Value };
        }

        if (projectIds.Count == 0)
            return EmptyResult(query);

        var entries = await _timeTrackingDb.TimeEntries
            .AsNoTracking()
            .Where(e =>
                projectIds.Contains(e.ProjectId) &&
                e.Date >= query.DateFrom &&
                e.Date <= query.DateTo &&
                !e.IsVoided)
            .Select(e => new { e.ProjectId, e.EntryType, e.CostAtTime })
            .ToListAsync(ct);

        var byProject = entries
            .GroupBy(e => e.ProjectId)
            .Select(g =>
            {
                var estimated  = g.Where(e => e.EntryType == "Estimated").Sum(e => e.CostAtTime);
                var pmAdj      = g.Where(e => e.EntryType == "PmAdjusted").Sum(e => e.CostAtTime);
                var vendorConf = g.Where(e => e.EntryType == "VendorConfirmed").Sum(e => e.CostAtTime);
                var official   = pmAdj + vendorConf;
                var total      = official + estimated;
                var pct        = total == 0 ? 0m : Math.Round(official / total * 100, 1);
                return new CostProjectBreakdown(g.Key, estimated, official, vendorConf, pmAdj, pct);
            })
            .OrderByDescending(p => p.OfficialCost)
            .ToList();

        var totalEstimated = byProject.Sum(p => p.EstimatedCost);
        var totalOfficial  = byProject.Sum(p => p.OfficialCost);
        var grandTotal     = totalOfficial + totalEstimated;
        var overallPct     = grandTotal == 0 ? 0m : Math.Round(totalOfficial / grandTotal * 100, 1);

        return new CostSummaryResult(
            query.DateFrom, query.DateTo,
            byProject.Count,
            totalEstimated, totalOfficial, overallPct,
            byProject);
    }

    private static CostSummaryResult EmptyResult(GetCostSummaryQuery q) =>
        new(q.DateFrom, q.DateTo, 0, 0, 0, 0, []);
}
```

---

### Task 3 — ReportingController + Extensions

**`ReportingModuleExtensions.cs`** — theo đúng pattern của `CapacityModuleExtensions.cs`:

```csharp
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectManagement.Reporting.Api.Controllers;
using ProjectManagement.Reporting.Application.Queries.GetCostSummary;

namespace ProjectManagement.Reporting.Api.Extensions;

public static class ReportingModuleExtensions
{
    public static IServiceCollection AddReportingModule(
        this IServiceCollection services,
        IConfiguration configuration,
        IMvcBuilder mvc)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(GetCostSummaryHandler).Assembly));

        mvc.AddApplicationPart(typeof(ReportingController).Assembly);
        return services;
    }
}
```

**`ReportingController.cs`:**

```csharp
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Reporting.Application.Queries.GetCostSummary;
using ProjectManagement.Shared.Infrastructure.Services;

namespace ProjectManagement.Reporting.Api.Controllers;

[ApiController]
[Route("api/v1/reports")]
[Authorize]
public class ReportingController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public ReportingController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Cost summary: planned (estimated) vs actual (official = VendorConfirmed + PmAdjusted).
    /// </summary>
    [HttpGet("cost")]
    public async Task<IActionResult> GetCostSummary(
        [FromQuery] DateOnly dateFrom,
        [FromQuery] DateOnly dateTo,
        [FromQuery] Guid? projectId,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new GetCostSummaryQuery(_currentUser.UserId, dateFrom, dateTo, projectId), ct);
        return Ok(result);
    }
}
```

---

### Task 4 — Host wiring

**`ProjectManagement.Host.csproj`** — thêm dòng này vào `<ItemGroup>` ProjectReferences:
```xml
<ProjectReference Include="..\..\Modules\Reporting\ProjectManagement.Reporting.Api\ProjectManagement.Reporting.Api.csproj" />
```

**`Program.cs`** — thêm sau `builder.Services.AddCapacityModule(...)`:
```csharp
using ProjectManagement.Reporting.Api.Extensions;

// ...

builder.Services.AddReportingModule(builder.Configuration, mvc);
```

**Không thêm migrate** cho Reporting trong Program.cs — Story 6-1 không có DB entity.

---

### Task 5 — Frontend models + service

**`features/reporting/models/cost-report.model.ts`:**
```typescript
export interface CostProjectBreakdown {
  projectId: string;
  estimatedCost: number;
  officialCost: number;
  vendorConfirmedCost: number;
  pmAdjustedCost: number;
  confirmedPct: number;
}

export interface CostSummaryResult {
  dateFrom: string;
  dateTo: string;
  projectCount: number;
  totalEstimatedCost: number;
  totalOfficialCost: number;
  confirmedPct: number;
  byProject: CostProjectBreakdown[];
}
```

**`features/reporting/services/reporting-api.service.ts`:**
```typescript
import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CostSummaryResult } from '../models/cost-report.model';

@Injectable({ providedIn: 'root' })
export class ReportingApiService {
  private readonly http = inject(HttpClient);

  getCostSummary(dateFrom: string, dateTo: string, projectId?: string): Observable<CostSummaryResult> {
    let params = new HttpParams().set('dateFrom', dateFrom).set('dateTo', dateTo);
    if (projectId) params = params.set('projectId', projectId);
    return this.http.get<CostSummaryResult>('/api/v1/reports/cost', { params });
  }
}
```

---

### Task 6 — NgRx store

**`features/reporting/store/reporting.actions.ts`:**
```typescript
import { createActionGroup, emptyProps, props } from '@ngrx/store';
import { CostSummaryResult } from '../models/cost-report.model';

export const ReportingActions = createActionGroup({
  source: 'Reporting',
  events: {
    'Load Cost Summary': props<{ dateFrom: string; dateTo: string; projectId?: string }>(),
    'Load Cost Summary Success': props<{ result: CostSummaryResult }>(),
    'Load Cost Summary Failure': props<{ error: string }>(),
  },
});
```

**`features/reporting/store/reporting.reducer.ts`:**
```typescript
import { createFeature, createReducer, on } from '@ngrx/store';
import { CostSummaryResult } from '../models/cost-report.model';
import { ReportingActions } from './reporting.actions';

export interface ReportingState {
  costSummary: CostSummaryResult | null;
  loading: boolean;
  error: string | null;
}

const initialState: ReportingState = {
  costSummary: null,
  loading: false,
  error: null,
};

export const reportingFeature = createFeature({
  name: 'reporting',
  reducer: createReducer(
    initialState,
    on(ReportingActions.loadCostSummary, state => ({ ...state, loading: true, error: null })),
    on(ReportingActions.loadCostSummarySuccess, (state, { result }) => ({
      ...state, loading: false, costSummary: result,
    })),
    on(ReportingActions.loadCostSummaryFailure, (state, { error }) => ({
      ...state, loading: false, error,
    })),
  ),
});

export const {
  selectCostSummary,
  selectLoading: selectReportingLoading,
  selectError: selectReportingError,
} = reportingFeature;
```

**`features/reporting/store/reporting.effects.ts`:**
```typescript
import { Injectable, inject } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { catchError, map, of, switchMap } from 'rxjs';
import { ReportingApiService } from '../services/reporting-api.service';
import { ReportingActions } from './reporting.actions';

@Injectable()
export class ReportingEffects {
  private readonly actions$ = inject(Actions);
  private readonly api = inject(ReportingApiService);

  loadCostSummary$ = createEffect(() =>
    this.actions$.pipe(
      ofType(ReportingActions.loadCostSummary),
      switchMap(({ dateFrom, dateTo, projectId }) =>
        this.api.getCostSummary(dateFrom, dateTo, projectId).pipe(
          map(result => ReportingActions.loadCostSummarySuccess({ result })),
          catchError(err => of(ReportingActions.loadCostSummaryFailure({ error: err?.message ?? 'Lỗi tải cost summary.' })))
        )
      )
    )
  );
}
```

**`app.state.ts`** — thêm vào:
```typescript
import { reportingFeature, ReportingState } from '../../features/reporting/store/reporting.reducer';

export interface AppState {
  // ... existing ...
  reporting: ReportingState;
}

export const reducers: ActionReducerMap<AppState> = {
  // ... existing ...
  reporting: reportingFeature.reducer,
};
```

**`app.config.ts`** — thêm `ReportingEffects` vào `provideEffects(...)` call.

---

### Task 7 — CostDashboardComponent + routing

**`features/reporting/components/cost-dashboard/cost-dashboard.ts`:**
```typescript
import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { AsyncPipe, DatePipe, DecimalPipe, NgFor, NgIf } from '@angular/common';
import { Store } from '@ngrx/store';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ReactiveFormsModule, FormBuilder } from '@angular/forms';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { ReportingActions } from '../../store/reporting.actions';
import { selectCostSummary, selectReportingLoading } from '../../store/reporting.reducer';

@Component({
  selector: 'app-cost-dashboard',
  standalone: true,
  imports: [AsyncPipe, DatePipe, DecimalPipe, NgIf, NgFor,
    MatButtonModule, MatCardModule, MatProgressSpinnerModule,
    ReactiveFormsModule, MatInputModule, MatFormFieldModule],
  templateUrl: './cost-dashboard.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CostDashboardComponent implements OnInit {
  private readonly store = inject(Store);
  private readonly fb = inject(FormBuilder);

  readonly costSummary$ = this.store.select(selectCostSummary);
  readonly loading$ = this.store.select(selectReportingLoading);

  readonly form = this.fb.nonNullable.group({
    dateFrom: [this.defaultDateFrom()],
    dateTo: [this.defaultDateTo()],
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    const { dateFrom, dateTo } = this.form.getRawValue();
    this.store.dispatch(ReportingActions.loadCostSummary({ dateFrom, dateTo }));
  }

  private defaultDateFrom(): string {
    const d = new Date();
    d.setMonth(d.getMonth() - 1);
    return d.toISOString().substring(0, 10);
  }

  private defaultDateTo(): string {
    return new Date().toISOString().substring(0, 10);
  }
}
```

**`features/reporting/components/cost-dashboard/cost-dashboard.html`** — minimal template:
```html
<mat-card>
  <mat-card-header>
    <mat-card-title>Báo cáo Chi phí</mat-card-title>
  </mat-card-header>
  <mat-card-content>
    <form [formGroup]="form" (ngSubmit)="load()" style="display:flex;gap:12px;align-items:flex-end;margin-bottom:16px;flex-wrap:wrap">
      <mat-form-field>
        <mat-label>Từ ngày</mat-label>
        <input matInput type="date" formControlName="dateFrom">
      </mat-form-field>
      <mat-form-field>
        <mat-label>Đến ngày</mat-label>
        <input matInput type="date" formControlName="dateTo">
      </mat-form-field>
      <button mat-raised-button color="primary" type="submit"
              [disabled]="loading$ | async">Xem báo cáo</button>
      <mat-spinner *ngIf="loading$ | async" diameter="24"></mat-spinner>
    </form>

    <ng-container *ngIf="costSummary$ | async as summary">
      <div style="display:flex;gap:16px;flex-wrap:wrap;margin-bottom:24px">
        <mat-card style="min-width:160px">
          <mat-card-content>
            <div style="font-size:12px;color:#666">Chi phí Ước tính</div>
            <div style="font-size:20px;font-weight:600">{{ summary.totalEstimatedCost | number:'1.0-0' }}</div>
          </mat-card-content>
        </mat-card>
        <mat-card style="min-width:160px">
          <mat-card-content>
            <div style="font-size:12px;color:#666">Chi phí Chính thức</div>
            <div style="font-size:20px;font-weight:600;color:#2e7d32">{{ summary.totalOfficialCost | number:'1.0-0' }}</div>
          </mat-card-content>
        </mat-card>
        <mat-card style="min-width:160px">
          <mat-card-content>
            <div style="font-size:12px;color:#666">% Xác nhận</div>
            <div style="font-size:20px;font-weight:600">{{ summary.confirmedPct | number:'1.1-1' }}%</div>
          </mat-card-content>
        </mat-card>
      </div>

      <p *ngIf="summary.byProject.length === 0" style="color:#999">
        Không có dữ liệu trong khoảng thời gian này.
      </p>

      <div *ngIf="summary.byProject.length > 0" style="overflow-x:auto">
        <table class="cost-table">
          <thead>
            <tr>
              <th>Project ID</th>
              <th>Ước tính</th>
              <th>Chính thức</th>
              <th>VendorConfirmed</th>
              <th>PmAdjusted</th>
              <th>% XN</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let p of summary.byProject">
              <td style="font-size:11px">{{ p.projectId }}</td>
              <td>{{ p.estimatedCost | number:'1.0-0' }}</td>
              <td style="font-weight:600;color:#2e7d32">{{ p.officialCost | number:'1.0-0' }}</td>
              <td>{{ p.vendorConfirmedCost | number:'1.0-0' }}</td>
              <td>{{ p.pmAdjustedCost | number:'1.0-0' }}</td>
              <td>{{ p.confirmedPct | number:'1.1-1' }}%</td>
            </tr>
          </tbody>
        </table>
      </div>
    </ng-container>
  </mat-card-content>
</mat-card>

<style>
  .cost-table { border-collapse:collapse; width:100%; }
  .cost-table th, .cost-table td { border:1px solid #e0e0e0; padding:6px 10px; text-align:right; white-space:nowrap; }
  .cost-table th { background:#f5f5f5; font-size:12px; }
  .cost-table td:first-child, .cost-table th:first-child { text-align:left; }
</style>
```

**`features/reporting/reporting.routes.ts`:**
```typescript
import { Routes } from '@angular/router';

export const reportingRoutes: Routes = [
  {
    path: 'cost',
    loadComponent: () =>
      import('./components/cost-dashboard/cost-dashboard').then(m => m.CostDashboardComponent),
  },
  { path: '', redirectTo: 'cost', pathMatch: 'full' },
];
```

**`app.routes.ts`** — thêm vào children array:
```typescript
{
  path: 'reporting',
  loadChildren: () =>
    import('./features/reporting/reporting.routes').then(m => m.reportingRoutes),
},
```

---

### Patterns từ Stories trước

1. **Cross-module query**: Inject `IProjectsDbContext` + `ITimeTrackingDbContext` trực tiếp trong handler (không qua interface lớp Reporting) — đã làm trong Capacity module.
2. **Membership-scope bắt buộc**: Mọi query phải filter theo `ProjectMemberships WHERE UserId = CurrentUserId` trước rồi mới query TimeEntries.
3. **IsVoided = false bắt buộc**: Query TimeEntries luôn thêm `!e.IsVoided` để loại bỏ void entries.
4. **CostAtTime đã snapshot**: Không tính lại `hours × rate` — dùng `CostAtTime` đã có sẵn.
5. **`createFeature()` pattern**: Dùng `createFeature({ name: 'reporting', reducer })` để auto-generate selectors.
6. **Không cần DB trong 6-1**: `AddReportingModule()` chỉ cần `AddMediatR` + `AddApplicationPart`. Không gọi `AddDbContext`. Không migrate trong Program.cs.
7. **MediatR handler discovery**: `RegisterServicesFromAssembly(typeof(GetCostSummaryHandler).Assembly)` — phải reference đúng handler type.

---

### Anti-patterns cần tránh

- **KHÔNG** tính lại `hours * rate` — `CostAtTime` đã là snapshot chính xác
- **KHÔNG** query TimeEntries mà bỏ sót membership check — data leak
- **KHÔNG** include VoidedEntries trong tính cost — luôn filter `!e.IsVoided`
- **KHÔNG** tạo `IReportingDbContext` trong 6-1 — chưa cần, tránh thêm complexity
- **KHÔNG** quên đăng ký reducer trong `app.state.ts` VÀ effects trong `app.config.ts`

---

## Completion Notes

- Backend: Tạo mới toàn bộ `Reporting` module (4 csproj: Domain, Application, Infrastructure, Api). Module không có DB entity trong 6-1 — query đọc cross-module qua DI (giống Capacity module pattern).
- `GetCostSummaryQuery`: membership-scoped (filter `IProjectsDbContext.ProjectMemberships` trước); query `ITimeTrackingDbContext.TimeEntries` với `!IsVoided`; tính `estimatedCost` (EntryType="Estimated"), `officialCost` (PmAdjusted+VendorConfirmed); `confirmedPct = official/(official+estimated)*100`; group-by-project cho `byProject[]`.
- API: `GET /api/v1/reports/cost` với params `dateFrom`, `dateTo`, `projectId?` (optional).
- `ReportingModuleExtensions.AddReportingModule()` — chỉ register MediatR + ApplicationPart. Không gọi AddDbContext, không migrate.
- `Host.csproj` + `Program.cs` updated để wire module.
- Frontend: `features/reporting` mới — models (`CostProjectBreakdown`, `CostSummaryResult`), service (`ReportingApiService`), NgRx store (`ReportingActions`, `reportingFeature`, `ReportingEffects`), `CostDashboardComponent`, `reporting.routes.ts`.
- `app.state.ts` + `app.config.ts` updated: `reporting: reportingFeature.reducer` + `ReportingEffects`.
- `app.routes.ts` updated: `{ path: 'reporting', loadChildren: () => reportingRoutes }`.
- `dotnet build` → 0 errors (10 pre-existing MSB3277 warnings từ EF Core version conflict — không liên quan 6-1).
- `ng build` → 0 errors.

## Files Created/Modified

- `src/Modules/Reporting/ProjectManagement.Reporting.Domain/ProjectManagement.Reporting.Domain.csproj` (new)
- `src/Modules/Reporting/ProjectManagement.Reporting.Application/ProjectManagement.Reporting.Application.csproj` (new)
- `src/Modules/Reporting/ProjectManagement.Reporting.Application/Queries/GetCostSummary/GetCostSummaryQuery.cs` (new)
- `src/Modules/Reporting/ProjectManagement.Reporting.Infrastructure/ProjectManagement.Reporting.Infrastructure.csproj` (new)
- `src/Modules/Reporting/ProjectManagement.Reporting.Api/ProjectManagement.Reporting.Api.csproj` (new)
- `src/Modules/Reporting/ProjectManagement.Reporting.Api/Controllers/ReportingController.cs` (new)
- `src/Modules/Reporting/ProjectManagement.Reporting.Api/Extensions/ReportingModuleExtensions.cs` (new)
- `src/Host/ProjectManagement.Host/ProjectManagement.Host.csproj` (modified — thêm Reporting.Api reference)
- `src/Host/ProjectManagement.Host/Program.cs` (modified — thêm using + AddReportingModule)
- `ProjectManagement.slnx` (modified — thêm Reporting module folder)
- `frontend/project-management-web/src/app/features/reporting/models/cost-report.model.ts` (new)
- `frontend/project-management-web/src/app/features/reporting/services/reporting-api.service.ts` (new)
- `frontend/project-management-web/src/app/features/reporting/store/reporting.actions.ts` (new)
- `frontend/project-management-web/src/app/features/reporting/store/reporting.reducer.ts` (new)
- `frontend/project-management-web/src/app/features/reporting/store/reporting.effects.ts` (new)
- `frontend/project-management-web/src/app/features/reporting/components/cost-dashboard/cost-dashboard.ts` (new)
- `frontend/project-management-web/src/app/features/reporting/components/cost-dashboard/cost-dashboard.html` (new)
- `frontend/project-management-web/src/app/features/reporting/reporting.routes.ts` (new)
- `frontend/project-management-web/src/app/core/store/app.state.ts` (modified — thêm reporting state)
- `frontend/project-management-web/src/app/app.config.ts` (modified — thêm ReportingEffects)
- `frontend/project-management-web/src/app/app.routes.ts` (modified — thêm reporting route)
