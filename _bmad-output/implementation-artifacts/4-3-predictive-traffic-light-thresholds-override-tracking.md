# Story 4.3: Predictive traffic-light thresholds + override tracking

Status: review

**Story ID:** 4.3
**Epic:** Epic 4 — Overload Warning (Standard + Predictive) + Cross-project Aggregation
**Sprint:** Sprint 5
**Date Created:** 2026-04-26

---

## Story

As a PM,
I want thấy dự báo overload theo traffic-light trước khi xác nhận,
So that tôi phòng ngừa trước khi quá tải thực xảy ra.

## Acceptance Criteria

1. **Given** capacity utilization %
   **When** hiển thị predictive status
   **Then** dùng ngưỡng Green<80, Yellow 80–95, Orange 95–105, Red>105
   **And** nếu PM override cảnh báo thì ghi event/metric để tuning sau

2. **Given** predictive status hiển thị
   **When** PM xem widget
   **Then** UI hiển thị nhãn "Dự báo" tách biệt với overload "đã xảy ra" (OL-01/OL-02)
   **And** có giải thích ngắn (top 3 ngày có giờ cao nhất)

3. **Given** status != Green
   **When** PM click "Bỏ qua cảnh báo"
   **Then** backend ghi `CapacityOverride` record (resourceId, dateFrom, dateTo, trafficLight, overriddenBy, overriddenAt)
   **And** widget vẫn hiển thị sau override (warn-only, không ẩn)

---

## Tasks / Subtasks

- [x] **Task 1: Capacity.Domain — CapacityOverride entity**
  - [x] 1.1 Tạo `src/Modules/Capacity/ProjectManagement.Capacity.Domain/ProjectManagement.Capacity.Domain.csproj`
  - [x] 1.2 Tạo `src/Modules/Capacity/ProjectManagement.Capacity.Domain/Entities/CapacityOverride.cs`

- [x] **Task 2: Capacity.Infrastructure — DbContext + migration**
  - [x] 2.1 Tạo `src/Modules/Capacity/ProjectManagement.Capacity.Infrastructure/ProjectManagement.Capacity.Infrastructure.csproj`
  - [x] 2.2 Tạo `ICapacityDbContext` trong `Capacity.Application/Common/Interfaces/ICapacityDbContext.cs`
  - [x] 2.3 Tạo `CapacityDbContext.cs` — schema "capacity"
  - [x] 2.4 Tạo `CapacityOverrideConfiguration.cs`
  - [x] 2.5 Tạo manual migration `20260426160000_Init_Capacity.cs`
  - [x] 2.6 Tạo `CapacityInfrastructureExtensions.cs`

- [x] **Task 3: Capacity.Application — queries + command**
  - [x] 3.1 Tạo `GetCapacityUtilizationQuery` + handler: tính utilizationPct, trafficLight, top 3 ngày
  - [x] 3.2 Tạo `LogCapacityOverrideCommand` + handler: insert CapacityOverride via ICapacityDbContext
  - [x] 3.3 Cập nhật `Capacity.Application.csproj` — thêm ProjectReference tới Capacity.Domain

- [x] **Task 4: Capacity.Api — endpoints + DI wiring**
  - [x] 4.1 Thêm `GET /api/v1/capacity/utilization` vào `CapacityController`
  - [x] 4.2 Thêm `POST /api/v1/capacity/overrides` vào `CapacityController`
  - [x] 4.3 Cập nhật `CapacityModuleExtensions.AddCapacityModule(IConfiguration, IMvcBuilder)` — gọi `AddCapacityInfrastructure`
  - [x] 4.4 Cập nhật `Capacity.Api.csproj` — thêm ProjectReference tới Capacity.Infrastructure

- [x] **Task 5: Host — migration + DI update**
  - [x] 5.1 Cập nhật `Program.cs` — đổi `AddCapacityModule(mvc)` → `AddCapacityModule(builder.Configuration, mvc)`
  - [x] 5.2 Thêm `CapacityDbContext.Database.MigrateAsync()` vào block `autoMigrate`
  - [x] 5.3 Thêm Capacity folder + 4 projects vào `ProjectManagement.slnx`

- [x] **Task 6: Frontend — models + API service**
  - [x] 6.1 Tạo `frontend/.../capacity/models/utilization.model.ts` — `CapacityUtilizationResult`, `TrafficLightStatus`, `TopContribution`, `LogOverrideRequest`
  - [x] 6.2 Thêm `getCapacityUtilization` + `logCapacityOverride` vào `capacity-api.service.ts`

- [x] **Task 7: Frontend — NgRx store**
  - [x] 7.1 Thêm actions vào `capacity.actions.ts`: `loadUtilization`, `loadUtilizationSuccess`, `loadUtilizationFailure`, `logOverride`, `logOverrideSuccess`, `logOverrideFailure`
  - [x] 7.2 Mở rộng `CapacityState` + reducer: thêm `utilization: CapacityUtilizationResult | null`, `utilizationLoading: boolean`
  - [x] 7.3 Thêm effects vào `capacity.effects.ts`: `loadUtilization$`, `logOverride$`

- [x] **Task 8: Frontend — TrafficLightWidgetComponent**
  - [x] 8.1 Tạo `traffic-light-widget.ts` + `.html`: hiển thị color badge, "Dự báo: X%", top 3 ngày, override button
  - [x] 8.2 Tích hợp vào `overload-dashboard.ts` + `.html` — dispatch `loadUtilization` khi form submit, xử lý override

- [x] **Task 9: Build verification**
  - [x] 9.1 `dotnet build` (backend) → 0 errors
  - [x] 9.2 `ng build` (frontend) → 0 errors

---

## Dev Notes

### Thresholds (AD-05 — kiến trúc đã quyết định)

| Màu | Ngưỡng utilization% |
|---|---|
| 🟢 Green | < 80% |
| 🟡 Yellow | 80–95% |
| 🟠 Orange | 95–105% |
| 🔴 Red | > 105% |

Thứ tự ưu tiên: Red > Orange > Yellow > Green (so sánh float theo thứ tự).

### Công thức utilization%

```
availableHours = countWeekdays(dateFrom, dateTo) * 8
actualHours    = sum(non-voided TimeEntries.Hours)
utilizationPct = (actualHours / availableHours) * 100   // nếu availableHours = 0 → trả về 0
```

`countWeekdays` — đếm Thứ Hai đến Thứ Sáu trong đoạn `[dateFrom, dateTo]` (inclusive).

### Task 1 — Capacity.Domain

**`ProjectManagement.Capacity.Domain.csproj`** (refs Shared.Domain như pattern):
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

**`Entities/CapacityOverride.cs`:**
```csharp
namespace ProjectManagement.Capacity.Domain.Entities;

public class CapacityOverride
{
    public Guid Id { get; private set; }
    public Guid ResourceId { get; private set; }
    public DateOnly DateFrom { get; private set; }
    public DateOnly DateTo { get; private set; }
    public string TrafficLight { get; private set; } = default!;
    public string OverriddenBy { get; private set; } = default!;
    public DateTime OverriddenAt { get; private set; }

    private CapacityOverride() { }

    public static CapacityOverride Create(Guid resourceId, DateOnly dateFrom, DateOnly dateTo,
        string trafficLight, string overriddenBy) => new()
    {
        Id = Guid.NewGuid(),
        ResourceId = resourceId,
        DateFrom = dateFrom,
        DateTo = dateTo,
        TrafficLight = trafficLight,
        OverriddenBy = overriddenBy,
        OverriddenAt = DateTime.UtcNow,
    };
}
```

### Task 2 — Capacity.Infrastructure

**`ProjectManagement.Capacity.Infrastructure.csproj`** (pattern từ TimeTracking.Infrastructure.csproj):
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
    <ProjectReference Include="..\ProjectManagement.Capacity.Application\ProjectManagement.Capacity.Application.csproj" />
    <ProjectReference Include="..\ProjectManagement.Capacity.Domain\ProjectManagement.Capacity.Domain.csproj" />
    <ProjectReference Include="..\..\..\Shared\ProjectManagement.Shared.Infrastructure\ProjectManagement.Shared.Infrastructure.csproj" />
  </ItemGroup>
</Project>
```

**`Application/Common/Interfaces/ICapacityDbContext.cs`** (trong Capacity.Application):
```csharp
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Capacity.Domain.Entities;

namespace ProjectManagement.Capacity.Application.Common.Interfaces;

public interface ICapacityDbContext
{
    DbSet<CapacityOverride> CapacityOverrides { get; }
    Task<int> SaveChangesAsync(CancellationToken ct);
}
```

**`Persistence/CapacityDbContext.cs`:**
```csharp
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Capacity.Application.Common.Interfaces;
using ProjectManagement.Capacity.Domain.Entities;
using ProjectManagement.Capacity.Infrastructure.Persistence.Configurations;

namespace ProjectManagement.Capacity.Infrastructure.Persistence;

public sealed class CapacityDbContext : DbContext, ICapacityDbContext
{
    public CapacityDbContext(DbContextOptions<CapacityDbContext> options) : base(options) { }
    public DbSet<CapacityOverride> CapacityOverrides => Set<CapacityOverride>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("capacity");
        modelBuilder.ApplyConfiguration(new CapacityOverrideConfiguration());
    }
}
```

**`Persistence/Configurations/CapacityOverrideConfiguration.cs`:**
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectManagement.Capacity.Domain.Entities;

namespace ProjectManagement.Capacity.Infrastructure.Persistence.Configurations;

public sealed class CapacityOverrideConfiguration : IEntityTypeConfiguration<CapacityOverride>
{
    public void Configure(EntityTypeBuilder<CapacityOverride> builder)
    {
        builder.ToTable("capacity_overrides");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ResourceId).HasColumnName("resource_id");
        builder.Property(x => x.DateFrom).HasColumnName("date_from");
        builder.Property(x => x.DateTo).HasColumnName("date_to");
        builder.Property(x => x.TrafficLight).HasColumnName("traffic_light").HasMaxLength(16);
        builder.Property(x => x.OverriddenBy).HasColumnName("overridden_by").HasMaxLength(256);
        builder.Property(x => x.OverriddenAt).HasColumnName("overridden_at");
    }
}
```

**Manual migration `Migrations/20260426160000_Init_Capacity.cs`:**
```csharp
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectManagement.Capacity.Infrastructure.Migrations
{
    public partial class Init_Capacity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(name: "capacity");
            migrationBuilder.CreateTable(
                name: "capacity_overrides",
                schema: "capacity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date_from = table.Column<DateOnly>(type: "date", nullable: false),
                    date_to = table.Column<DateOnly>(type: "date", nullable: false),
                    traffic_light = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    overridden_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    overridden_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table => table.PrimaryKey("pk_capacity_overrides", x => x.id));

            migrationBuilder.CreateIndex(
                name: "ix_capacity_overrides_resource_id",
                schema: "capacity",
                table: "capacity_overrides",
                column: "resource_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "capacity_overrides", schema: "capacity");
        }
    }
}
```

**`Extensions/CapacityInfrastructureExtensions.cs`:**
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectManagement.Capacity.Application.Common.Interfaces;
using ProjectManagement.Capacity.Infrastructure.Persistence;

namespace ProjectManagement.Capacity.Infrastructure.Extensions;

public static class CapacityInfrastructureExtensions
{
    public static IServiceCollection AddCapacityInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("Default") ??
            "Host=localhost;Port=5432;Database=project_management;Username=pm_app;Password=pm_app_password";

        services.AddDbContext<CapacityDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<ICapacityDbContext>(sp => sp.GetRequiredService<CapacityDbContext>());
        return services;
    }
}
```

### Task 3 — Capacity.Application additions

**Cập nhật `Capacity.Application.csproj`** — thêm ref tới Domain:
```xml
<ItemGroup>
  <ProjectReference Include="..\ProjectManagement.Capacity.Domain\ProjectManagement.Capacity.Domain.csproj" />
  <!-- existing cross-module ref -->
  <ProjectReference Include="..\..\TimeTracking\ProjectManagement.TimeTracking.Application\..." />
</ItemGroup>
```

**`Queries/GetCapacityUtilization/GetCapacityUtilizationQuery.cs`:**
```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.TimeTracking.Application.Common.Interfaces;

namespace ProjectManagement.Capacity.Application.Queries.GetCapacityUtilization;

public sealed record TopContribution(DateOnly Date, decimal Hours);

public sealed record CapacityUtilizationResult(
    Guid ResourceId,
    decimal UtilizationPct,
    decimal AvailableHours,
    decimal ActualHours,
    string TrafficLight,         // "Green" | "Yellow" | "Orange" | "Red"
    IReadOnlyList<TopContribution> TopContributions);

public sealed record GetCapacityUtilizationQuery(Guid ResourceId, DateOnly DateFrom, DateOnly DateTo)
    : IRequest<CapacityUtilizationResult>;

public sealed class GetCapacityUtilizationHandler
    : IRequestHandler<GetCapacityUtilizationQuery, CapacityUtilizationResult>
{
    private readonly ITimeTrackingDbContext _db;
    public GetCapacityUtilizationHandler(ITimeTrackingDbContext db) => _db = db;

    public async Task<CapacityUtilizationResult> Handle(
        GetCapacityUtilizationQuery query, CancellationToken ct)
    {
        var entries = await _db.TimeEntries.AsNoTracking()
            .Where(e => e.ResourceId == query.ResourceId
                     && !e.IsVoided
                     && e.Date >= query.DateFrom
                     && e.Date <= query.DateTo)
            .Select(e => new { e.Date, e.Hours })
            .ToListAsync(ct);

        var availableHours = CountWeekdays(query.DateFrom, query.DateTo) * 8m;
        var actualHours = entries.Sum(e => e.Hours);
        var utilizationPct = availableHours > 0
            ? Math.Round(actualHours / availableHours * 100, 1)
            : 0m;

        var trafficLight = utilizationPct switch
        {
            >= 105m => "Red",
            >= 95m  => "Orange",
            >= 80m  => "Yellow",
            _       => "Green",
        };

        var topContributions = entries
            .GroupBy(e => e.Date)
            .Select(g => new TopContribution(g.Key, g.Sum(e => e.Hours)))
            .OrderByDescending(d => d.Hours)
            .Take(3)
            .ToList();

        return new CapacityUtilizationResult(
            query.ResourceId, utilizationPct, availableHours, actualHours, trafficLight, topContributions);
    }

    private static int CountWeekdays(DateOnly from, DateOnly to)
    {
        var count = 0;
        for (var d = from; d <= to; d = d.AddDays(1))
            if (d.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
                count++;
        return count;
    }
}
```

**`Commands/LogCapacityOverride/LogCapacityOverrideCommand.cs`:**
```csharp
using MediatR;
using ProjectManagement.Capacity.Application.Common.Interfaces;
using ProjectManagement.Capacity.Domain.Entities;

namespace ProjectManagement.Capacity.Application.Commands.LogCapacityOverride;

public sealed record LogCapacityOverrideCommand(
    Guid ResourceId, DateOnly DateFrom, DateOnly DateTo,
    string TrafficLight, string OverriddenBy) : IRequest;

public sealed class LogCapacityOverrideHandler : IRequestHandler<LogCapacityOverrideCommand>
{
    private readonly ICapacityDbContext _db;
    public LogCapacityOverrideHandler(ICapacityDbContext db) => _db = db;

    public async Task Handle(LogCapacityOverrideCommand cmd, CancellationToken ct)
    {
        var record = CapacityOverride.Create(
            cmd.ResourceId, cmd.DateFrom, cmd.DateTo, cmd.TrafficLight, cmd.OverriddenBy);
        _db.CapacityOverrides.Add(record);
        await _db.SaveChangesAsync(ct);
    }
}
```

### Task 4 — Capacity.Api updates

**Cập nhật `CapacityModuleExtensions.cs`:**
```csharp
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectManagement.Capacity.Api.Controllers;
using ProjectManagement.Capacity.Application.Commands.LogCapacityOverride;
using ProjectManagement.Capacity.Application.Queries.GetCapacityUtilization;
using ProjectManagement.Capacity.Application.Queries.GetResourceOverload;
using ProjectManagement.Capacity.Infrastructure.Extensions;

namespace ProjectManagement.Capacity.Api.Extensions;

public static class CapacityModuleExtensions
{
    public static IServiceCollection AddCapacityModule(
        this IServiceCollection services,
        IConfiguration configuration,
        IMvcBuilder mvc)
    {
        services.AddCapacityInfrastructure(configuration);

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(GetResourceOverloadHandler).Assembly);
            cfg.RegisterServicesFromAssembly(typeof(LogCapacityOverrideHandler).Assembly);
        });

        mvc.AddApplicationPart(typeof(CapacityController).Assembly);
        return services;
    }
}
```

**Cập nhật `CapacityController.cs`** — thêm 2 endpoints:
```csharp
// GET /api/v1/capacity/utilization
[HttpGet("utilization")]
public async Task<IActionResult> GetUtilization(
    [FromQuery] Guid resourceId,
    [FromQuery] DateOnly dateFrom,
    [FromQuery] DateOnly dateTo,
    CancellationToken ct)
{
    if (dateTo < dateFrom)
        return BadRequest(new { detail = "dateTo phải >= dateFrom." });
    var result = await _mediator.Send(new GetCapacityUtilizationQuery(resourceId, dateFrom, dateTo), ct);
    return Ok(result);
}

// POST /api/v1/capacity/overrides
public sealed record LogOverrideRequest(Guid ResourceId, DateOnly DateFrom, DateOnly DateTo, string TrafficLight);

[HttpPost("overrides")]
public async Task<IActionResult> LogOverride([FromBody] LogOverrideRequest req, CancellationToken ct)
{
    var userName = User.Identity?.Name ?? "unknown";
    await _mediator.Send(new LogCapacityOverrideCommand(
        req.ResourceId, req.DateFrom, req.DateTo, req.TrafficLight, userName), ct);
    return NoContent();
}
```

**`Capacity.Api.csproj`** — thêm ref:
```xml
<ProjectReference Include="..\ProjectManagement.Capacity.Infrastructure\ProjectManagement.Capacity.Infrastructure.csproj" />
```

### Task 5 — Host updates

**`Program.cs`** — thay đổi:
1. Thêm using: `using ProjectManagement.Capacity.Infrastructure.Persistence;`
2. Đổi: `builder.Services.AddCapacityModule(mvc)` → `builder.Services.AddCapacityModule(builder.Configuration, mvc)`
3. Trong block `if (autoMigrate)`, sau `timeTrackingDb.Database.MigrateAsync()`, thêm:
```csharp
var capacityDb = scope.ServiceProvider.GetRequiredService<CapacityDbContext>();
await capacityDb.Database.MigrateAsync();
```

**`ProjectManagement.slnx`** — thêm Capacity folder (sau TimeTracking, trước Shared):
```xml
<Folder Name="/src/Modules/Capacity/">
  <Project Path="src/Modules/Capacity/ProjectManagement.Capacity.Api/ProjectManagement.Capacity.Api.csproj" />
  <Project Path="src/Modules/Capacity/ProjectManagement.Capacity.Application/ProjectManagement.Capacity.Application.csproj" />
  <Project Path="src/Modules/Capacity/ProjectManagement.Capacity.Domain/ProjectManagement.Capacity.Domain.csproj" />
  <Project Path="src/Modules/Capacity/ProjectManagement.Capacity.Infrastructure/ProjectManagement.Capacity.Infrastructure.csproj" />
</Folder>
```

### Task 6 — Frontend models

**Tạo `models/utilization.model.ts`** (file mới, KHÔNG sửa `overload.model.ts`):
```typescript
export type TrafficLightStatus = 'Green' | 'Yellow' | 'Orange' | 'Red';

export interface TopContribution {
  date: string;
  hours: number;
}

export interface CapacityUtilizationResult {
  resourceId: string;
  utilizationPct: number;
  availableHours: number;
  actualHours: number;
  trafficLight: TrafficLightStatus;
  topContributions: TopContribution[];
}

export interface LogOverrideRequest {
  resourceId: string;
  dateFrom: string;
  dateTo: string;
  trafficLight: TrafficLightStatus;
}
```

**`capacity-api.service.ts`** — thêm 2 methods:
```typescript
getCapacityUtilization(resourceId: string, dateFrom: string, dateTo: string): Observable<CapacityUtilizationResult> {
  return this.http.get<CapacityUtilizationResult>(`${this.base}/utilization`, {
    params: { resourceId, dateFrom, dateTo },
  });
}

logCapacityOverride(request: LogOverrideRequest): Observable<void> {
  return this.http.post<void>(`${this.base}/overrides`, request);
}
```

### Task 7 — NgRx store updates

**`capacity.actions.ts`** — thêm vào createActionGroup:
```typescript
'Load Utilization': props<{ resourceId: string; dateFrom: string; dateTo: string }>(),
'Load Utilization Success': props<{ utilization: CapacityUtilizationResult }>(),
'Load Utilization Failure': props<{ error: string }>(),
'Log Override': props<{ request: LogOverrideRequest }>(),
'Log Override Success': emptyProps(),
'Log Override Failure': props<{ error: string }>(),
```

**`capacity.reducer.ts`** — mở rộng state:
```typescript
export interface CapacityState {
  result: ResourceOverloadResult | null;
  loading: boolean;
  error: string | null;
  lastUpdated: string | null;
  utilization: CapacityUtilizationResult | null;       // NEW
  utilizationLoading: boolean;                          // NEW
}

// Thêm reducer cases:
on(CapacityActions.loadUtilization, state => ({ ...state, utilizationLoading: true })),
on(CapacityActions.loadUtilizationSuccess, (state, { utilization }) => ({
  ...state, utilizationLoading: false, utilization,
})),
on(CapacityActions.loadUtilizationFailure, state => ({ ...state, utilizationLoading: false })),
on(CapacityActions.logOverride, state => state),
on(CapacityActions.logOverrideSuccess, state => state),
on(CapacityActions.logOverrideFailure, state => state),
```

Selectors tự động từ `createFeature`: `selectUtilization`, `selectUtilizationLoading`.

**`capacity.effects.ts`** — thêm:
```typescript
loadUtilization$ = createEffect(() =>
  this.actions$.pipe(
    ofType(CapacityActions.loadUtilization),
    switchMap(({ resourceId, dateFrom, dateTo }) =>
      this.api.getCapacityUtilization(resourceId, dateFrom, dateTo).pipe(
        map(utilization => CapacityActions.loadUtilizationSuccess({ utilization })),
        catchError(err => of(CapacityActions.loadUtilizationFailure({ error: err?.message ?? 'Lỗi.' })))
      )
    )
  )
);

logOverride$ = createEffect(() =>
  this.actions$.pipe(
    ofType(CapacityActions.logOverride),
    switchMap(({ request }) =>
      this.api.logCapacityOverride(request).pipe(
        map(() => CapacityActions.logOverrideSuccess()),
        catchError(err => of(CapacityActions.logOverrideFailure({ error: err?.message ?? 'Lỗi.' })))
      )
    )
  )
);
```

### Task 8 — TrafficLightWidgetComponent

**`traffic-light-widget.ts`:**
```typescript
import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { DecimalPipe, NgClass, NgFor, NgIf } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { CapacityUtilizationResult } from '../../models/utilization.model';

@Component({
  selector: 'app-traffic-light-widget',
  standalone: true,
  imports: [NgIf, NgFor, NgClass, DecimalPipe, MatButtonModule, MatIconModule],
  templateUrl: './traffic-light-widget.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TrafficLightWidgetComponent {
  @Input() utilization: CapacityUtilizationResult | null = null;
  @Input() loading = false;
  @Output() override = new EventEmitter<void>();

  get statusColor(): string {
    return { Green: '#2e7d32', Yellow: '#f9a825', Orange: '#e65100', Red: '#c62828' }
      [this.utilization?.trafficLight ?? 'Green'] ?? '#2e7d32';
  }

  get statusLabel(): string {
    return { Green: '🟢 Bình thường', Yellow: '🟡 Chú ý', Orange: '🟠 Cảnh báo', Red: '🔴 Quá tải' }
      [this.utilization?.trafficLight ?? 'Green'] ?? '';
  }

  get canOverride(): boolean {
    return !!this.utilization && this.utilization.trafficLight !== 'Green';
  }
}
```

**`traffic-light-widget.html`:**
```html
<div *ngIf="utilization" class="tl-widget">
  <div class="tl-header">
    <span class="tl-label">Dự báo</span>
    <span class="tl-status" [style.color]="statusColor">{{ statusLabel }}</span>
    <span class="tl-pct" [style.color]="statusColor">
      {{ utilization.utilizationPct | number:'1.1-1' }}%
    </span>
  </div>

  <div class="tl-detail" *ngIf="utilization.topContributions.length > 0">
    <span style="font-size:12px;color:#666">Top 3 ngày:</span>
    <span *ngFor="let c of utilization.topContributions" class="tl-day">
      {{ c.date }}: {{ c.hours | number:'1.1-1' }}h
    </span>
  </div>

  <div class="tl-meta">
    <span style="font-size:12px;color:#999">
      {{ utilization.actualHours | number:'1.1-1' }}h / {{ utilization.availableHours | number:'1.0-0' }}h có sẵn
    </span>
    <button *ngIf="canOverride" mat-stroked-button (click)="override.emit()" style="font-size:12px">
      Bỏ qua cảnh báo
    </button>
  </div>
</div>

<style>
  .tl-widget { border:1px solid #ddd; border-radius:4px; padding:12px; margin-bottom:16px; }
  .tl-header { display:flex; align-items:center; gap:12px; margin-bottom:8px; }
  .tl-label { font-weight:600; font-size:14px; }
  .tl-status { font-weight:500; }
  .tl-pct { font-weight:700; font-size:18px; }
  .tl-detail { display:flex; gap:8px; flex-wrap:wrap; margin-bottom:8px; font-size:13px; }
  .tl-day { background:#f5f5f5; padding:2px 6px; border-radius:3px; }
  .tl-meta { display:flex; align-items:center; justify-content:space-between; }
</style>
```

**Cập nhật `overload-dashboard.ts`** — thêm utilization wiring:
```typescript
// Thêm vào imports array của @Component:
TrafficLightWidgetComponent,

// Thêm selectors:
readonly utilization$ = this.store.select(selectUtilization);
readonly utilizationLoading$ = this.store.select(selectUtilizationLoading);

// Cập nhật startPolling():
startPolling(): void {
  if (this.form.invalid) return;
  const { resourceId, dateFrom, dateTo } = this.form.getRawValue();
  this.store.dispatch(CapacityActions.startPolling({ resourceId, dateFrom, dateTo }));
  this.store.dispatch(CapacityActions.loadUtilization({ resourceId, dateFrom, dateTo }));
}

// Thêm override handler:
onOverride(): void {
  const util = /* snapshot từ async pipe */;
  // Dùng store một-chiều:
}
```

**NOTE:** Override handler cần snapshot. Dùng pattern `take(1)` với `combineLatest`:
```typescript
onOverride(): void {
  const { resourceId, dateFrom, dateTo } = this.form.getRawValue();
  // Lấy current trafficLight từ store một lần:
  this.utilization$.pipe(take(1)).subscribe(u => {
    if (!u) return;
    this.store.dispatch(CapacityActions.logOverride({
      request: { resourceId, dateFrom, dateTo, trafficLight: u.trafficLight }
    }));
  });
}
```
Import `take` từ `rxjs/operators`.

**Cập nhật `overload-dashboard.html`** — thêm widget sau form, trước overload banner:
```html
<!-- Traffic light widget — Dự báo section -->
<app-traffic-light-widget
  *ngIf="utilization$ | async as utilization"
  [utilization]="utilization"
  [loading]="!!(utilizationLoading$ | async)"
  (override)="onOverride()"
  style="display:block;margin-bottom:16px">
</app-traffic-light-widget>
```

### Patterns đã có — KHÔNG viết lại

| Pattern | Source |
|---|---|
| `ITimeTrackingDbContext` injection | `GetResourceOverloadQuery` — Story 4.1 |
| DbContext + EF Config | TimeTracking.Infrastructure.Persistence |
| Manual migration format | `20260426140000_AddPeriodLock_TimeTracking.cs` |
| `createActionGroup` + `createFeature` | `capacity.actions.ts` + `capacity.reducer.ts` |
| `@Input()` only component | `OverloadWarningBannerComponent` — Story 4.2 |
| `MediatR IRequest` (void) | VoidTimeEntryHandler, UnlockPeriodCommand |
| `switchMap` effect pattern | `capacity.effects.ts` — Story 4.2 |

### Anti-patterns — KHÔNG làm

- **KHÔNG** tạo CapacityModelSnapshot.cs (designer file) — chỉ cần manual migration
- **KHÔNG** thêm `ITimeTrackingDbContext.SaveChangesAsync` call từ Capacity handler — dùng `ICapacityDbContext`
- **KHÔNG** đặt `CapacityOverride` trong TimeTracking schema — phải là schema "capacity"
- **KHÔNG** tạo `traffic-light-widget.css` riêng — dùng inline `<style>` như pattern existing
- **KHÔNG** sử dụng Signal/computed trong TrafficLightWidget — `@Input()` + `get` property là đủ (như OverloadWarningBannerComponent)
- **KHÔNG** register `LogCapacityOverrideHandler` trong `GetResourceOverloadHandler.Assembly` — handler ở 2 assembly khác nhau → register cả 2 trong `AddMediatR`

### Lưu ý MediatR handler assembly

`GetResourceOverloadHandler` và `GetCapacityUtilizationHandler` cùng trong `Capacity.Application` assembly.
`LogCapacityOverrideHandler` cũng trong `Capacity.Application` assembly.
→ Chỉ cần 1 assembly registration: `cfg.RegisterServicesFromAssembly(typeof(GetResourceOverloadHandler).Assembly)`.

---

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

- IDE warning "TrafficLightWidgetComponent is not used within the template" — fixed by updating overload-dashboard.html to include `<app-traffic-light-widget>` before the warning banner.

### Completion Notes List

- Tạo mới Capacity.Domain project với `CapacityOverride` entity (private constructor + static factory).
- Tạo mới Capacity.Infrastructure project: `CapacityDbContext` (schema "capacity"), EF config, manual migration `20260426160000_Init_Capacity`, `CapacityInfrastructureExtensions`.
- `ICapacityDbContext` interface trong Capacity.Application — tách biệt với `ITimeTrackingDbContext`.
- `GetCapacityUtilizationQuery`: tính `utilizationPct = actualHours / (weekdays*8) * 100`, traffic-light theo AD-05 thresholds, top 3 ngày.
- `LogCapacityOverrideCommand`: append-only, ghi `CapacityOverride` record với overriddenBy từ JWT identity.
- `AddCapacityModule` signature mở rộng: thêm `IConfiguration` param để gọi `AddCapacityInfrastructure`.
- `TrafficLightWidgetComponent`: `@Input()` only (no NgRx inside), `@Output() override` EventEmitter.
- `onOverride()` trong dashboard: dùng `take(1)` snapshot từ `utilization$` — không subscribe liên tục.
- `loadUtilization` dispatch song song với `startPolling` khi form submit — không polling utilization (one-shot).

### File List

- `src/Modules/Capacity/ProjectManagement.Capacity.Domain/ProjectManagement.Capacity.Domain.csproj` (new)
- `src/Modules/Capacity/ProjectManagement.Capacity.Domain/Entities/CapacityOverride.cs` (new)
- `src/Modules/Capacity/ProjectManagement.Capacity.Infrastructure/ProjectManagement.Capacity.Infrastructure.csproj` (new)
- `src/Modules/Capacity/ProjectManagement.Capacity.Infrastructure/Persistence/CapacityDbContext.cs` (new)
- `src/Modules/Capacity/ProjectManagement.Capacity.Infrastructure/Persistence/Configurations/CapacityOverrideConfiguration.cs` (new)
- `src/Modules/Capacity/ProjectManagement.Capacity.Infrastructure/Migrations/20260426160000_Init_Capacity.cs` (new)
- `src/Modules/Capacity/ProjectManagement.Capacity.Infrastructure/Extensions/CapacityInfrastructureExtensions.cs` (new)
- `src/Modules/Capacity/ProjectManagement.Capacity.Application/Common/Interfaces/ICapacityDbContext.cs` (new)
- `src/Modules/Capacity/ProjectManagement.Capacity.Application/Queries/GetCapacityUtilization/GetCapacityUtilizationQuery.cs` (new)
- `src/Modules/Capacity/ProjectManagement.Capacity.Application/Commands/LogCapacityOverride/LogCapacityOverrideCommand.cs` (new)
- `src/Modules/Capacity/ProjectManagement.Capacity.Application/ProjectManagement.Capacity.Application.csproj` (modified — added Domain ref)
- `src/Modules/Capacity/ProjectManagement.Capacity.Api/ProjectManagement.Capacity.Api.csproj` (modified — added Infrastructure ref)
- `src/Modules/Capacity/ProjectManagement.Capacity.Api/Extensions/CapacityModuleExtensions.cs` (modified — IConfiguration param)
- `src/Modules/Capacity/ProjectManagement.Capacity.Api/Controllers/CapacityController.cs` (modified — 2 new endpoints)
- `src/Host/ProjectManagement.Host/Program.cs` (modified — config param + CapacityDbContext migration)
- `ProjectManagement.slnx` (modified — added Capacity folder with 4 projects)
- `frontend/.../capacity/models/utilization.model.ts` (new)
- `frontend/.../capacity/services/capacity-api.service.ts` (modified — 2 new methods)
- `frontend/.../capacity/store/capacity.actions.ts` (modified — 6 new actions)
- `frontend/.../capacity/store/capacity.reducer.ts` (modified — utilization state)
- `frontend/.../capacity/store/capacity.effects.ts` (modified — loadUtilization$, logOverride$)
- `frontend/.../capacity/components/traffic-light-widget/traffic-light-widget.ts` (new)
- `frontend/.../capacity/components/traffic-light-widget/traffic-light-widget.html` (new)
- `frontend/.../capacity/components/overload-dashboard/overload-dashboard.ts` (modified — widget integration)
- `frontend/.../capacity/components/overload-dashboard/overload-dashboard.html` (modified — widget in template)
