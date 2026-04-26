# Story 5.2: Forecast precompute job (4-week rolling) + versioned reads

Status: review

**Story ID:** 5.2
**Epic:** Epic 5 — Capacity Planning Suite (Heatmap + 4-week Forecast)
**Sprint:** Sprint 6
**Date Created:** 2026-04-26

---

## Story

As a PM,
I want forecast 4 tuần được precompute và đọc theo version ổn định,
So that UI tải nhanh và tránh tính toán nặng trên client.

## Acceptance Criteria

1. **Given** PM bấm "Tính lại Forecast"
   **When** gọi `POST /api/v1/capacity/forecast/compute`
   **Then** backend tính forecast 4 tuần tiếp theo (từ Monday sau today) và lưu artifact có version + computedAt
   **And** response trả `{ version, computedAt, status: "Succeeded" }`

2. **Given** forecast artifact đã được tính thành công
   **When** gọi `GET /api/v1/capacity/forecast`
   **Then** trả về artifact mới nhất (status=Succeeded) với đầy đủ `version`, `computedAt`, và per-resource per-week data
   **And** nếu chưa có artifact nào thì trả `{ version: 0, computedAt: null, rows: [] }`

3. **Given** forecast scoped theo membership
   **When** tính forecast cho user X
   **Then** chỉ bao gồm resources có entries trong projects user X là member
   **And** không leak data ngoài scope

4. **Given** forecast algorithm (MVP — trend-based)
   **When** tính forecast cho resource R trong tuần W
   **Then** dùng average weekly hours của resource đó trong 4 tuần gần nhất làm baseline
   **And** trafficLight áp dụng ngưỡng AD-05: Green <80%, Yellow 80–95%, Orange 95–105%, Red >105%

5. **Given** UI hiển thị forecast
   **When** có forecast data
   **Then** hiển thị "Cập nhật lần cuối: {computedAt}" và "Version: {version}"
   **And** có nút "Tính lại" với loading state; bảng resource × 4 tuần

---

## Tasks / Subtasks

- [x] **Task 1: Capacity.Domain — ForecastArtifact entity**
  - [x] 1.1 Tạo `Entities/ForecastArtifact.cs` với: Id, Version (int), ComputedAt, Status (string), Payload (JSON string), ErrorMessage
  - [x] 1.2 Methods: `Create(version)`, `MarkSucceeded(payload)`, `MarkFailed(error)`

- [x] **Task 2: Capacity.Infrastructure — persistence**
  - [x] 2.1 Thêm `DbSet<ForecastArtifact> ForecastArtifacts` vào `CapacityDbContext` + `ICapacityDbContext`
  - [x] 2.2 Tạo `Configurations/ForecastArtifactConfiguration.cs` — table `forecast_artifacts`, schema `capacity`
  - [x] 2.3 Tạo migration `20260426170000_Add_ForecastArtifact.cs` — tạo bảng `forecast_artifacts`

- [x] **Task 3: Capacity.Application — TriggerForecastComputeCommand**
  - [x] 3.1 Tạo `Commands/TriggerForecastCompute/TriggerForecastComputeCommand.cs` + handler
  - [x] 3.2 Handler: lấy projectIds từ membership → time entries 4 tuần gần nhất → compute avg weekly hours per resource → build ForecastPayload → serialize JSON → save ForecastArtifact
  - [x] 3.3 Version tự tăng: `MAX(version) + 1` trong bảng (hoặc 1 nếu bảng rỗng)

- [x] **Task 4: Capacity.Application — GetLatestForecastQuery**
  - [x] 4.1 Tạo `Queries/GetLatestForecast/GetLatestForecastQuery.cs` + handler
  - [x] 4.2 Handler: query `ForecastArtifacts` WHERE status='Succeeded' ORDER BY version DESC LIMIT 1 → deserialize Payload → return DTO

- [x] **Task 5: Capacity.Api — endpoints**
  - [x] 5.1 Thêm `POST /api/v1/capacity/forecast/compute` vào `CapacityController`
  - [x] 5.2 Thêm `GET /api/v1/capacity/forecast` vào `CapacityController`

- [x] **Task 6: Frontend — models**
  - [x] 6.1 Thêm `ForecastWeekCell`, `ForecastResourceRow`, `ForecastResult`, `ForecastComputeResult` vào `utilization.model.ts`

- [x] **Task 7: Frontend — API service**
  - [x] 7.1 Thêm `triggerForecastCompute()` và `getLatestForecast()` vào `capacity-api.service.ts`

- [x] **Task 8: Frontend — NgRx store**
  - [x] 8.1 Thêm actions: `triggerForecast`, `triggerForecastSuccess`, `triggerForecastFailure`, `loadForecast`, `loadForecastSuccess`, `loadForecastFailure`
  - [x] 8.2 Mở rộng `CapacityState` + reducer: `forecast: ForecastResult | null`, `forecastLoading: boolean`, `forecastComputing: boolean`
  - [x] 8.3 Export selectors + thêm 2 effects

- [x] **Task 9: Frontend — ForecastViewComponent + routing**
  - [x] 9.1 Tạo `components/forecast-view/forecast-view.ts` (standalone, OnPush)
  - [x] 9.2 Tạo `components/forecast-view/forecast-view.html` — computedAt/version header, nút tính lại, table resource × 4 tuần
  - [x] 9.3 Cập nhật `capacity.routes.ts` — thêm route `forecast`

- [x] **Task 10: Build verification**
  - [x] 10.1 `dotnet build` → 0 errors
  - [x] 10.2 `ng build` → 0 errors

---

## Dev Notes

### Forecast Algorithm (MVP Trend-based)

```
Với mỗi resource R có entries trong 4 tuần gần nhất:
  - Lấy tất cả entries (non-voided, trong membership scope)
  - Tính averageWeeklyHours = totalHours / numberOfWeeksWithData
  - Với mỗi tuần trong 4 tuần tiếp theo:
    - forecastedHours = averageWeeklyHours
    - availableHours = 5 * 8 = 40 (full week, không tính holiday ở MVP)
    - forecastedUtilizationPct = round(forecastedHours / availableHours * 100, 1)
    - trafficLight = ngưỡng AD-05
```

"4 tuần gần nhất" = 28 ngày trước today. "4 tuần tiếp theo" = next 4 Mondays từ upcoming Monday.

### Task 1 — ForecastArtifact entity

**File:** `src/Modules/Capacity/ProjectManagement.Capacity.Domain/Entities/ForecastArtifact.cs`

```csharp
namespace ProjectManagement.Capacity.Domain.Entities;

public class ForecastArtifact
{
    public Guid Id { get; private set; }
    public int Version { get; private set; }
    public DateTime ComputedAt { get; private set; }
    public string Status { get; private set; } = default!;  // "Succeeded" | "Failed"
    public string? Payload { get; private set; }            // JSON
    public string? ErrorMessage { get; private set; }

    private ForecastArtifact() { }

    public static ForecastArtifact Create(int version) => new()
    {
        Id = Guid.NewGuid(),
        Version = version,
        ComputedAt = DateTime.UtcNow,
        Status = "Pending",
    };

    public void MarkSucceeded(string payload)
    {
        Status = "Succeeded";
        Payload = payload;
    }

    public void MarkFailed(string error)
    {
        Status = "Failed";
        ErrorMessage = error;
    }
}
```

### Task 2 — Infrastructure

**ForecastArtifactConfiguration:**
```csharp
// File: Persistence/Configurations/ForecastArtifactConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectManagement.Capacity.Domain.Entities;

namespace ProjectManagement.Capacity.Infrastructure.Persistence.Configurations;

public sealed class ForecastArtifactConfiguration : IEntityTypeConfiguration<ForecastArtifact>
{
    public void Configure(EntityTypeBuilder<ForecastArtifact> builder)
    {
        builder.ToTable("forecast_artifacts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Version).HasColumnName("version");
        builder.Property(x => x.ComputedAt).HasColumnName("computed_at");
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(32);
        builder.Property(x => x.Payload).HasColumnName("payload");
        builder.Property(x => x.ErrorMessage).HasColumnName("error_message").HasMaxLength(1024);
        builder.HasIndex(x => x.Version).HasDatabaseName("ix_forecast_artifacts_version");
    }
}
```

**ICapacityDbContext** — thêm:
```csharp
DbSet<ForecastArtifact> ForecastArtifacts { get; }
```

**CapacityDbContext** — thêm:
```csharp
public DbSet<ForecastArtifact> ForecastArtifacts => Set<ForecastArtifact>();
// Và trong OnModelCreating:
modelBuilder.ApplyConfiguration(new ForecastArtifactConfiguration());
```

**Migration `20260426170000_Add_ForecastArtifact.cs`:**
```csharp
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectManagement.Capacity.Infrastructure.Migrations
{
    public partial class Add_ForecastArtifact : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "forecast_artifacts",
                schema: "capacity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    computed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    payload = table.Column<string>(type: "text", nullable: true),
                    error_message = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_forecast_artifacts", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_forecast_artifacts_version",
                schema: "capacity",
                table: "forecast_artifacts",
                column: "version");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "forecast_artifacts", schema: "capacity");
        }
    }
}
```

### Task 3 — TriggerForecastComputeCommand

**File:** `src/Modules/Capacity/ProjectManagement.Capacity.Application/Commands/TriggerForecastCompute/TriggerForecastComputeCommand.cs`

```csharp
using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Capacity.Application.Common.Interfaces;
using ProjectManagement.Capacity.Domain.Entities;
using ProjectManagement.Projects.Application.Common.Interfaces;
using ProjectManagement.TimeTracking.Application.Common.Interfaces;

namespace ProjectManagement.Capacity.Application.Commands.TriggerForecastCompute;

public sealed record ForecastComputeResult(int Version, DateTime ComputedAt, string Status);

public sealed record TriggerForecastComputeCommand(Guid CurrentUserId)
    : IRequest<ForecastComputeResult>;

public sealed class TriggerForecastComputeHandler
    : IRequestHandler<TriggerForecastComputeCommand, ForecastComputeResult>
{
    private readonly ICapacityDbContext _capacityDb;
    private readonly IProjectsDbContext _projectsDb;
    private readonly ITimeTrackingDbContext _timeTrackingDb;

    public TriggerForecastComputeHandler(
        ICapacityDbContext capacityDb,
        IProjectsDbContext projectsDb,
        ITimeTrackingDbContext timeTrackingDb)
    {
        _capacityDb = capacityDb;
        _projectsDb = projectsDb;
        _timeTrackingDb = timeTrackingDb;
    }

    public async Task<ForecastComputeResult> Handle(
        TriggerForecastComputeCommand command, CancellationToken ct)
    {
        var nextVersion = (await _capacityDb.ForecastArtifacts.MaxAsync(
            a => (int?)a.Version, ct) ?? 0) + 1;

        var artifact = ForecastArtifact.Create(nextVersion);
        _capacityDb.ForecastArtifacts.Add(artifact);

        try
        {
            var payload = await ComputePayloadAsync(command.CurrentUserId, ct);
            var json = JsonSerializer.Serialize(payload);
            artifact.MarkSucceeded(json);
        }
        catch (Exception ex)
        {
            artifact.MarkFailed(ex.Message);
        }

        await _capacityDb.SaveChangesAsync(ct);
        return new ForecastComputeResult(artifact.Version, artifact.ComputedAt, artifact.Status);
    }

    private async Task<ForecastPayload> ComputePayloadAsync(Guid userId, CancellationToken ct)
    {
        var projectIds = await _projectsDb.ProjectMemberships
            .Where(m => m.UserId == userId)
            .Select(m => m.ProjectId)
            .Distinct()
            .ToListAsync(ct);

        // 4 tuần gần nhất
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var lookbackFrom = today.AddDays(-28);

        var entries = projectIds.Count == 0
            ? []
            : await _timeTrackingDb.TimeEntries.AsNoTracking()
                .Where(e => projectIds.Contains(e.ProjectId)
                         && !e.IsVoided
                         && e.Date >= lookbackFrom
                         && e.Date <= today)
                .Select(e => new { e.ResourceId, e.Date, e.Hours })
                .ToListAsync(ct);

        // 4 tuần tiếp theo (Mondays)
        var nextMonday = GetNextMonday(today);
        var forecastWeeks = Enumerable.Range(0, 4)
            .Select(i => nextMonday.AddDays(i * 7))
            .ToList();

        var resources = entries
            .GroupBy(e => e.ResourceId)
            .Select(g =>
            {
                var weeklyHours = g
                    .GroupBy(e => GetMonday(e.Date))
                    .Select(w => w.Sum(e => e.Hours))
                    .ToList();

                var avgWeekly = weeklyHours.Count > 0
                    ? weeklyHours.Average()
                    : 0m;

                var cells = forecastWeeks.Select(week =>
                {
                    const decimal available = 40m; // 5 days × 8h
                    var pct = available > 0 ? Math.Round(avgWeekly / available * 100, 1) : 0m;
                    var light = pct switch
                    {
                        >= 105m => "Red",
                        >= 95m  => "Orange",
                        >= 80m  => "Yellow",
                        _       => "Green",
                    };
                    return new ForecastWeekCell(week.ToString("yyyy-MM-dd"), avgWeekly, available, pct, light);
                }).ToList();

                return new ForecastResourceRow(g.Key, cells);
            })
            .OrderByDescending(r => r.Cells.Count(c => c.TrafficLight is "Red" or "Orange"))
            .ToList();

        return new ForecastPayload(
            forecastWeeks.Select(w => w.ToString("yyyy-MM-dd")).ToList(),
            resources);
    }

    private static DateOnly GetNextMonday(DateOnly today)
    {
        var daysUntilMonday = ((int)DayOfWeek.Monday - (int)today.DayOfWeek + 7) % 7;
        return today.AddDays(daysUntilMonday == 0 ? 7 : daysUntilMonday);
    }

    private static DateOnly GetMonday(DateOnly date)
    {
        var diff = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.AddDays(-diff);
    }
}

// POCOs for JSON payload
public sealed record ForecastWeekCell(
    string WeekStart,
    decimal ForecastedHours,
    decimal AvailableHours,
    decimal ForecastedUtilizationPct,
    string TrafficLight);

public sealed record ForecastResourceRow(
    Guid ResourceId,
    List<ForecastWeekCell> Cells);

public sealed record ForecastPayload(
    List<string> Weeks,
    List<ForecastResourceRow> Resources);
```

### Task 4 — GetLatestForecastQuery

**File:** `src/Modules/Capacity/ProjectManagement.Capacity.Application/Queries/GetLatestForecast/GetLatestForecastQuery.cs`

```csharp
using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Capacity.Application.Commands.TriggerForecastCompute;
using ProjectManagement.Capacity.Application.Common.Interfaces;

namespace ProjectManagement.Capacity.Application.Queries.GetLatestForecast;

public sealed record ForecastResultDto(
    int Version,
    DateTime? ComputedAt,
    List<string> Weeks,
    List<ForecastResourceRow> Rows);

public sealed record GetLatestForecastQuery : IRequest<ForecastResultDto>;

public sealed class GetLatestForecastHandler
    : IRequestHandler<GetLatestForecastQuery, ForecastResultDto>
{
    private readonly ICapacityDbContext _capacityDb;

    public GetLatestForecastHandler(ICapacityDbContext capacityDb)
    {
        _capacityDb = capacityDb;
    }

    public async Task<ForecastResultDto> Handle(
        GetLatestForecastQuery query, CancellationToken ct)
    {
        var artifact = await _capacityDb.ForecastArtifacts
            .Where(a => a.Status == "Succeeded")
            .OrderByDescending(a => a.Version)
            .FirstOrDefaultAsync(ct);

        if (artifact is null)
            return new ForecastResultDto(0, null, [], []);

        var payload = JsonSerializer.Deserialize<ForecastPayload>(artifact.Payload!)!;

        return new ForecastResultDto(
            artifact.Version,
            artifact.ComputedAt,
            payload.Weeks,
            payload.Resources);
    }
}
```

### Task 5 — CapacityController endpoints

Thêm vào `CapacityController.cs`:

```csharp
using ProjectManagement.Capacity.Application.Commands.TriggerForecastCompute;
using ProjectManagement.Capacity.Application.Queries.GetLatestForecast;

// Add to top using block ↑

/// <summary>
/// Trigger 4-week rolling capacity forecast precompute.
/// Scoped to current user's project membership.
/// </summary>
[HttpPost("forecast/compute")]
public async Task<IActionResult> ComputeForecast(CancellationToken ct)
{
    var result = await _mediator.Send(
        new TriggerForecastComputeCommand(_currentUser.UserId), ct);
    return Ok(result);
}

/// <summary>
/// Get latest succeeded 4-week capacity forecast.
/// </summary>
[HttpGet("forecast")]
public async Task<IActionResult> GetForecast(CancellationToken ct)
{
    var result = await _mediator.Send(new GetLatestForecastQuery(), ct);
    return Ok(result);
}
```

### Task 6 — Frontend Models

Thêm vào cuối `utilization.model.ts`:

```typescript
export interface ForecastWeekCell {
  weekStart: string;
  forecastedHours: number;
  availableHours: number;
  forecastedUtilizationPct: number;
  trafficLight: TrafficLightStatus;
}

export interface ForecastResourceRow {
  resourceId: string;
  cells: ForecastWeekCell[];
}

export interface ForecastResult {
  version: number;
  computedAt: string | null;
  weeks: string[];
  rows: ForecastResourceRow[];
}

export interface ForecastComputeResult {
  version: number;
  computedAt: string;
  status: string;
}
```

### Task 7 — API Service

```typescript
import { ForecastComputeResult, ForecastResult } from '../models/utilization.model'; // thêm vào import

triggerForecastCompute(): Observable<ForecastComputeResult> {
  return this.http.post<ForecastComputeResult>('/api/v1/capacity/forecast/compute', {});
}

getLatestForecast(): Observable<ForecastResult> {
  return this.http.get<ForecastResult>('/api/v1/capacity/forecast');
}
```

### Task 8 — NgRx Store

**capacity.actions.ts** — thêm vào events:
```typescript
'Trigger Forecast': emptyProps(),
'Trigger Forecast Success': props<{ result: ForecastComputeResult }>(),
'Trigger Forecast Failure': props<{ error: string }>(),
'Load Forecast': emptyProps(),
'Load Forecast Success': props<{ result: ForecastResult }>(),
'Load Forecast Failure': props<{ error: string }>(),
```
**QUAN TRỌNG:** `emptyProps()` phải import từ `@ngrx/store`.

**capacity.reducer.ts** — thêm vào state/initial/cases/selectors:
```typescript
// State
forecast: ForecastResult | null;
forecastLoading: boolean;
forecastComputing: boolean;

// initialState
forecast: null,
forecastLoading: false,
forecastComputing: false,

// Cases
on(CapacityActions.triggerForecast, state => ({ ...state, forecastComputing: true })),
on(CapacityActions.triggerForecastSuccess, state => ({ ...state, forecastComputing: false })),
on(CapacityActions.triggerForecastFailure, state => ({ ...state, forecastComputing: false })),
on(CapacityActions.loadForecast, state => ({ ...state, forecastLoading: true })),
on(CapacityActions.loadForecastSuccess, (state, { result }) => ({
  ...state, forecastLoading: false, forecast: result,
})),
on(CapacityActions.loadForecastFailure, state => ({ ...state, forecastLoading: false })),

// Selectors
selectForecast,
selectForecastLoading,
selectForecastComputing,
```

**capacity.effects.ts** — thêm 2 effects:
```typescript
triggerForecast$ = createEffect(() =>
  this.actions$.pipe(
    ofType(CapacityActions.triggerForecast),
    switchMap(() =>
      this.api.triggerForecastCompute().pipe(
        // After compute, automatically load the result
        switchMap(result => [
          CapacityActions.triggerForecastSuccess({ result }),
          CapacityActions.loadForecast(),
        ]),
        catchError(err => of(CapacityActions.triggerForecastFailure({ error: err?.message ?? 'Lỗi tính forecast.' })))
      )
    )
  )
);

loadForecast$ = createEffect(() =>
  this.actions$.pipe(
    ofType(CapacityActions.loadForecast),
    switchMap(() =>
      this.api.getLatestForecast().pipe(
        map(result => CapacityActions.loadForecastSuccess({ result })),
        catchError(err => of(CapacityActions.loadForecastFailure({ error: err?.message ?? 'Lỗi tải forecast.' })))
      )
    )
  )
);
```

**IMPORTANT — effect switchMap returns array:** `triggerForecast$` dùng `switchMap` trả về array `[action1, action2]` để dispatch 2 actions liên tiếp. Pattern này hợp lệ trong NgRx effects.

### Task 9 — ForecastViewComponent

**File:** `components/forecast-view/forecast-view.ts`

```typescript
import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { AsyncPipe, DatePipe, DecimalPipe, NgClass, NgFor, NgIf } from '@angular/common';
import { Store } from '@ngrx/store';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { CapacityActions } from '../../store/capacity.actions';
import { selectForecast, selectForecastComputing, selectForecastLoading } from '../../store/capacity.reducer';
import { ForecastWeekCell } from '../../models/utilization.model';

@Component({
  selector: 'app-forecast-view',
  standalone: true,
  imports: [
    AsyncPipe,
    DatePipe,
    DecimalPipe,
    NgIf,
    NgFor,
    NgClass,
    MatButtonModule,
    MatCardModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
  ],
  templateUrl: './forecast-view.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ForecastViewComponent implements OnInit {
  private readonly store = inject(Store);

  readonly forecast$ = this.store.select(selectForecast);
  readonly loading$ = this.store.select(selectForecastLoading);
  readonly computing$ = this.store.select(selectForecastComputing);

  readonly trafficLightIcon: Record<string, string> = {
    Green: '●', Yellow: '▲', Orange: '◆', Red: '✕',
  };

  ngOnInit(): void {
    this.store.dispatch(CapacityActions.loadForecast());
  }

  compute(): void {
    this.store.dispatch(CapacityActions.triggerForecast());
  }

  cellClass(cell: ForecastWeekCell): string {
    return `cell-${cell.trafficLight.toLowerCase()}`;
  }

  cellTooltip(cell: ForecastWeekCell): string {
    return `Dự báo: ${cell.forecastedHours.toFixed(1)}h / ${cell.availableHours.toFixed(0)}h (${cell.forecastedUtilizationPct.toFixed(1)}%)`;
  }
}
```

**File:** `components/forecast-view/forecast-view.html`

```html
<mat-card>
  <mat-card-header>
    <mat-card-title>Forecast Capacity — 4 Tuần Tới</mat-card-title>
  </mat-card-header>
  <mat-card-content>

    <div style="display:flex;gap:16px;align-items:center;margin-bottom:16px;flex-wrap:wrap">
      <button mat-raised-button color="primary"
              [disabled]="(computing$ | async) || (loading$ | async)"
              (click)="compute()">
        Tính lại Forecast
      </button>
      <mat-spinner *ngIf="(computing$ | async) || (loading$ | async)" diameter="24"></mat-spinner>

      <ng-container *ngIf="forecast$ | async as forecast">
        <span *ngIf="forecast.computedAt" style="color:#666;font-size:13px">
          Cập nhật lần cuối: <strong>{{ forecast.computedAt | date:'dd/MM/yyyy HH:mm' }}</strong>
          &nbsp;·&nbsp; Version: <strong>{{ forecast.version }}</strong>
        </span>
        <span *ngIf="!forecast.computedAt" style="color:#999;font-size:13px">Chưa có forecast. Nhấn "Tính lại" để bắt đầu.</span>
      </ng-container>
    </div>

    <!-- Legend -->
    <div class="legend">
      <span class="legend-item cell-green">● Green &lt;80%</span>
      <span class="legend-item cell-yellow">▲ Yellow 80–95%</span>
      <span class="legend-item cell-orange">◆ Orange 95–105%</span>
      <span class="legend-item cell-red">✕ Red &gt;105%</span>
    </div>

    <ng-container *ngIf="forecast$ | async as forecast">
      <p *ngIf="forecast.rows.length === 0 && forecast.computedAt" style="color:#666">
        Không có dữ liệu — không tìm thấy time entries trong 28 ngày gần nhất.
      </p>

      <div *ngIf="forecast.rows.length > 0" style="overflow-x:auto">
        <table class="forecast-table">
          <thead>
            <tr>
              <th class="resource-col">Nhân sự</th>
              <th *ngFor="let w of forecast.weeks" class="week-col">{{ w }}</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let row of forecast.rows">
              <td class="resource-col" style="font-size:11px">{{ row.resourceId }}</td>
              <td *ngFor="let cell of row.cells"
                  class="forecast-cell"
                  [ngClass]="cellClass(cell)"
                  [matTooltip]="cellTooltip(cell)">
                <span class="cell-icon">{{ trafficLightIcon[cell.trafficLight] }}</span>
                <span class="cell-pct">{{ cell.forecastedUtilizationPct | number:'1.0-0' }}%</span>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </ng-container>
  </mat-card-content>
</mat-card>

<style>
  .legend { display:flex; gap:16px; flex-wrap:wrap; margin-bottom:12px; font-size:13px; }
  .legend-item { padding:4px 10px; border-radius:4px; font-weight:600; }

  .forecast-table { border-collapse:collapse; }
  .forecast-table th, .forecast-table td { border:1px solid #e0e0e0; padding:4px 8px; text-align:center; white-space:nowrap; }
  .resource-col { text-align:left !important; min-width:120px; }
  .week-col { min-width:90px; font-size:12px; }

  .forecast-cell { min-width:90px; }
  .cell-icon { display:block; font-size:11px; }
  .cell-pct { font-size:12px; font-weight:600; }

  .cell-green  { background:#e8f5e9; color:#2e7d32; }
  .cell-yellow { background:#fff8e1; color:#f57f17; }
  .cell-orange { background:#fff3e0; color:#e65100; }
  .cell-red    { background:#ffebee; color:#c62828; }

  .legend-item.cell-green  { background:#e8f5e9; color:#2e7d32; }
  .legend-item.cell-yellow { background:#fff8e1; color:#f57f17; }
  .legend-item.cell-orange { background:#fff3e0; color:#e65100; }
  .legend-item.cell-red    { background:#ffebee; color:#c62828; }
</style>
```

**capacity.routes.ts** — thêm:
```typescript
{
  path: 'forecast',
  loadComponent: () =>
    import('./components/forecast-view/forecast-view').then(m => m.ForecastViewComponent),
},
```

### Patterns từ Stories trước

1. **MediatR** — handler mới tự được scan, không cần đăng ký thêm.
2. **ICapacityDbContext** cần được extend với `ForecastArtifacts` trước khi sử dụng — cập nhật cả interface và implementation.
3. **Migration manual** — không dùng `dotnet ef migrations add`, tạo file migration thủ công theo pattern `20260426170000_Add_ForecastArtifact.cs`. Filename phải match class name partial `Add_ForecastArtifact`.
4. **Program.cs** — KHÔNG cần cập nhật; `CapacityDbContext` đã đăng ký và `MigrateAsync()` đã được gọi.
5. **emptyProps()** trong NgRx actions — dùng khi action không có payload.
6. **OnInit** — `ForecastViewComponent` implements `OnInit` để auto-load forecast khi navigate tới.
7. **`switchMap` trả array actions** — pattern hợp lệ để dispatch nhiều actions từ 1 effect.
8. **`DatePipe`** phải import trong standalone component để dùng `| date`.

---

## Completion Notes

- Domain: `ForecastArtifact` entity với Create/MarkSucceeded/MarkFailed pattern (append-only, private constructor).
- Infrastructure: `ForecastArtifactConfiguration` + migration `20260426170000_Add_ForecastArtifact` tạo bảng `capacity.forecast_artifacts`. `ICapacityDbContext` + `CapacityDbContext` mở rộng với `ForecastArtifacts` DbSet.
- Application: `TriggerForecastComputeCommand` — membership-scoped, trend-based forecast (avg weekly hours × 4 tuần gần nhất → project 4 tuần tới), version auto-increment, payload JSON serialized. `GetLatestForecastQuery` — read latest Succeeded artifact, deserialize payload.
- API: 2 endpoints mới: `POST /api/v1/capacity/forecast/compute` và `GET /api/v1/capacity/forecast`.
- Frontend: 4 models mới (ForecastWeekCell, ForecastResourceRow, ForecastResult, ForecastComputeResult). 2 service methods. 6 NgRx actions, 3 state fields, 6 reducer cases, 3 selectors, 2 effects (triggerForecast$ chains sang loadForecast$ sau khi compute xong). ForecastViewComponent: OnInit auto-load, nút "Tính lại", legend, table resource × 4 tuần, computedAt/version header.
- `dotnet build` → 0 errors (10 pre-existing MSB3277 warnings), `ng build` → 0 errors.

## Files Created/Modified

- `src/Modules/Capacity/ProjectManagement.Capacity.Domain/Entities/ForecastArtifact.cs` (new)
- `src/Modules/Capacity/ProjectManagement.Capacity.Application/Common/Interfaces/ICapacityDbContext.cs` (modified — thêm ForecastArtifacts)
- `src/Modules/Capacity/ProjectManagement.Capacity.Infrastructure/Persistence/Configurations/ForecastArtifactConfiguration.cs` (new)
- `src/Modules/Capacity/ProjectManagement.Capacity.Infrastructure/Persistence/CapacityDbContext.cs` (modified — thêm ForecastArtifacts + config)
- `src/Modules/Capacity/ProjectManagement.Capacity.Infrastructure/Migrations/20260426170000_Add_ForecastArtifact.cs` (new)
- `src/Modules/Capacity/ProjectManagement.Capacity.Application/Commands/TriggerForecastCompute/TriggerForecastComputeCommand.cs` (new)
- `src/Modules/Capacity/ProjectManagement.Capacity.Application/Queries/GetLatestForecast/GetLatestForecastQuery.cs` (new)
- `src/Modules/Capacity/ProjectManagement.Capacity.Api/Controllers/CapacityController.cs` (modified — thêm 2 usings + 2 endpoints)
- `frontend/project-management-web/src/app/features/capacity/models/utilization.model.ts` (modified — thêm 4 interfaces)
- `frontend/project-management-web/src/app/features/capacity/services/capacity-api.service.ts` (modified — thêm 2 methods)
- `frontend/project-management-web/src/app/features/capacity/store/capacity.actions.ts` (modified — thêm 6 actions)
- `frontend/project-management-web/src/app/features/capacity/store/capacity.reducer.ts` (modified — thêm state, cases, selectors)
- `frontend/project-management-web/src/app/features/capacity/store/capacity.effects.ts` (modified — thêm 2 effects)
- `frontend/project-management-web/src/app/features/capacity/components/forecast-view/forecast-view.ts` (new)
- `frontend/project-management-web/src/app/features/capacity/components/forecast-view/forecast-view.html` (new)
- `frontend/project-management-web/src/app/features/capacity/capacity.routes.ts` (modified — thêm route forecast)
