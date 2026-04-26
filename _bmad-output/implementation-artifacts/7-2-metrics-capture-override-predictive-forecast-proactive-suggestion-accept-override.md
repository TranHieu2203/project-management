# Story 7.2: Metrics capture (override predictive, forecast proactive, suggestion accept/override)

Status: review

**Story ID:** 7.2
**Epic:** Epic 7 — Operations Layer (Notifications + In-product transparency metrics)
**Sprint:** Sprint 8
**Date Created:** 2026-04-26

---

## Story

As a PM,
I want hệ thống ghi nhận metrics hành vi quan trọng,
So that có dữ liệu để cải tiến ngưỡng cảnh báo và gợi ý phân công sau này.

## Acceptance Criteria

1. **Given** PM click "Bỏ qua cảnh báo" trên TrafficLightWidget (Story 4.3)
   **When** override được thực hiện
   **Then** hệ thống ghi `MetricEvent` với `eventType=predictive_override`, `actorId`, `contextJson` (resourceId, trafficLight, utilizationPct, weekStart), `correlationId`, `occurredAt`

2. **Given** PM click nút hành động trên một delta item trong ForecastDeltaTable (Story 5.3)
   **When** PM thực hiện hành động dựa trên forecast hint
   **Then** hệ thống ghi `MetricEvent` với `eventType=forecast_proactive`, `actorId`, `contextJson` (resourceId, weekStart, deltaPct, hint), `correlationId`

3. **Given** metric events đã được ghi
   **When** gọi `GET /api/v1/metrics/summary?from=&to=&eventType=`
   **Then** trả về tổng hợp: `totalEvents`, `byType` (count per eventType), `byDay` (date string → count)

4. **Given** eventType không nằm trong whitelist hợp lệ
   **When** POST `/api/v1/metrics/events` với eventType không hợp lệ
   **Then** trả `400 ProblemDetails` với message rõ ràng

5. **Given** metrics API gặp lỗi (timeout, 5xx)
   **When** frontend gọi `recordEvent(...)`
   **Then** lỗi bị swallow hoàn toàn — KHÔNG hiển thị toast/snackbar, KHÔNG chặn main user flow

---

## Tasks / Subtasks

- [x] **Task 1: Tạo Metrics module — Domain**
  - [x] 1.1 Tạo `src/Modules/Metrics/ProjectManagement.Metrics.Domain/Entities/MetricEvent.cs`
  - [x] 1.2 Tạo `src/Modules/Metrics/ProjectManagement.Metrics.Domain/Enums/MetricEventType.cs`
  - [x] 1.3 Tạo `src/Modules/Metrics/ProjectManagement.Metrics.Domain/ProjectManagement.Metrics.Domain.csproj`

- [x] **Task 2: Tạo Metrics module — Application**
  - [x] 2.1 Tạo `Common/Interfaces/IMetricsDbContext.cs` — `DbSet<MetricEvent>` + `SaveChangesAsync`
  - [x] 2.2 Tạo `Commands/RecordMetricEvent/RecordMetricEventCommand.cs` + handler
  - [x] 2.3 Tạo `Queries/GetMetricSummary/GetMetricSummaryQuery.cs` + handler + `MetricSummaryDto`
  - [x] 2.4 Tạo `ProjectManagement.Metrics.Application.csproj` (refs: Domain, MediatR, EF Core 10.0.7)

- [x] **Task 3: Tạo Metrics module — Infrastructure**
  - [x] 3.1 Tạo `Persistence/MetricsDbContext.cs` — schema `"metrics"`
  - [x] 3.2 Tạo `Persistence/Configurations/MetricEventConfiguration.cs`
  - [x] 3.3 Tạo migration thủ công `Migrations/20260426170000_InitialMetrics.cs` (1 bảng)
  - [x] 3.4 Tạo `Migrations/MetricsDbContextModelSnapshot.cs`
  - [x] 3.5 Tạo `ProjectManagement.Metrics.Infrastructure.csproj` (Npgsql.EFCore 10.0.1, EF.Design 10.0.7)

- [x] **Task 4: Tạo Metrics module — Api**
  - [x] 4.1 Tạo `Controllers/MetricsController.cs`:
         `POST /api/v1/metrics/events` + `GET /api/v1/metrics/summary`
  - [x] 4.2 Tạo `Extensions/MetricsModuleExtensions.cs`
  - [x] 4.3 Tạo `ProjectManagement.Metrics.Api.csproj`

- [x] **Task 5: Đăng ký Metrics module trong Host**
  - [x] 5.1 Thêm Metrics.Api reference vào `ProjectManagement.Host.csproj`
  - [x] 5.2 Thêm `AddMetricsModule(...)` vào `Program.cs`
  - [x] 5.3 Thêm `MetricsDbContext.Database.MigrateAsync()` vào auto-migrate block
  - [x] 5.4 Thêm Metrics folder + 4 projects vào `ProjectManagement.slnx`

- [x] **Task 6: Frontend — MetricsApiService**
  - [x] 6.1 Tạo `src/app/core/services/metrics-api.service.ts` (fire-and-forget, providedIn: 'root')

- [x] **Task 7: Frontend — Tích hợp vào overload-dashboard (Story 4.3)**
  - [x] 7.1 Inject `MetricsApiService` vào `overload-dashboard.ts`
  - [x] 7.2 Gọi `metricsService.recordEvent('predictive_override', {...})` ngay sau khi dispatch `logOverride` action

- [x] **Task 8: Frontend — Tích hợp vào forecast-view (Story 5.3)**
  - [x] 8.1 Inject `MetricsApiService` vào `forecast-view.ts`
  - [x] 8.2 Thêm button "Đánh dấu đã xử lý" trên mỗi delta item có hint là "Nguy cơ overload"
  - [x] 8.3 Gọi `metricsService.recordEvent('forecast_proactive', {...})` khi PM click button đó

- [x] **Task 9: Build verification**
  - [x] 9.1 `dotnet build` → 0 errors
  - [x] 9.2 `ng build` → 0 errors

---

## Dev Notes

### Tổng quan module

Story này tạo module `Metrics` hoàn toàn mới theo pattern 4 csproj giống Notifications (Story 7.1) và Reporting (Story 6.1). Module nhẹ — chỉ có 1 entity, 1 command, 1 query.

```
src/Modules/Metrics/
├── ProjectManagement.Metrics.Domain/
├── ProjectManagement.Metrics.Application/
├── ProjectManagement.Metrics.Infrastructure/
└── ProjectManagement.Metrics.Api/
```

**Schema DB:** `metrics` — tách biệt với Notifications (`notifications`) và Capacity (`capacity`).

---

### Task 1 — Domain

**MetricEvent entity:**
```csharp
// File: Metrics.Domain/Entities/MetricEvent.cs
public class MetricEvent
{
    public Guid Id { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public Guid ActorId { get; private set; }
    public string? ContextJson { get; private set; }
    public string? CorrelationId { get; private set; }
    public DateTime OccurredAt { get; private set; }

    public static MetricEvent Create(
        string eventType, Guid actorId,
        string? contextJson, string? correlationId)
        => new()
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            ActorId = actorId,
            ContextJson = contextJson,
            CorrelationId = correlationId,
            OccurredAt = DateTime.UtcNow
        };
}
```

**MetricEventType constants:**
```csharp
// File: Metrics.Domain/Enums/MetricEventType.cs
public static class MetricEventType
{
    public const string PredictiveOverride  = "predictive_override";
    public const string ForecastProactive   = "forecast_proactive";
    public const string SuggestionAccept    = "suggestion_accept";    // future Sprint
    public const string SuggestionOverride  = "suggestion_override";  // future Sprint

    private static readonly HashSet<string> _valid = new()
    {
        PredictiveOverride, ForecastProactive,
        SuggestionAccept, SuggestionOverride
    };

    public static bool IsValid(string type) => _valid.Contains(type);
}
```

**Domain.csproj** — không có PackageReference, chỉ là POCO.

---

### Task 2 — Application

**IMetricsDbContext:**
```csharp
public interface IMetricsDbContext
{
    DbSet<MetricEvent> MetricEvents { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
```

**RecordMetricEventCommand:**
```csharp
public record RecordMetricEventCommand(
    string EventType,
    Guid ActorId,
    string? ContextJson,
    string? CorrelationId
) : IRequest;

public class RecordMetricEventHandler : IRequestHandler<RecordMetricEventCommand>
{
    public async Task Handle(RecordMetricEventCommand cmd, CancellationToken ct)
    {
        // Validate whitelist — trả ValidationException nếu sai
        if (!MetricEventType.IsValid(cmd.EventType))
            throw new ValidationException($"Invalid eventType: {cmd.EventType}");

        var ev = MetricEvent.Create(cmd.EventType, cmd.ActorId, cmd.ContextJson, cmd.CorrelationId);
        _db.MetricEvents.Add(ev);
        await _db.SaveChangesAsync(ct);
    }
}
```

**GetMetricSummaryQuery:**
```csharp
public record GetMetricSummaryQuery(
    DateTime? From,
    DateTime? To,
    string? EventType
) : IRequest<MetricSummaryDto>;

public record MetricSummaryDto(
    int TotalEvents,
    List<MetricCountByTypeDto> ByType,
    Dictionary<string, int> ByDay   // key: "yyyy-MM-dd"
);

public record MetricCountByTypeDto(string EventType, int Count);

// Handler
public async Task<MetricSummaryDto> Handle(GetMetricSummaryQuery q, CancellationToken ct)
{
    var query = _db.MetricEvents.AsNoTracking();

    if (q.From.HasValue)   query = query.Where(e => e.OccurredAt >= q.From.Value);
    if (q.To.HasValue)     query = query.Where(e => e.OccurredAt <= q.To.Value);
    if (!string.IsNullOrEmpty(q.EventType))
        query = query.Where(e => e.EventType == q.EventType);

    var events = await query.ToListAsync(ct);

    var byType = events
        .GroupBy(e => e.EventType)
        .Select(g => new MetricCountByTypeDto(g.Key, g.Count()))
        .ToList();

    var byDay = events
        .GroupBy(e => e.OccurredAt.ToString("yyyy-MM-dd"))
        .ToDictionary(g => g.Key, g => g.Count());

    return new MetricSummaryDto(events.Count, byType, byDay);
}
```

**Application.csproj dependencies:**
```xml
<PackageReference Include="MediatR" Version="12.4.1" />
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.7" />
<ProjectReference Include="...\Metrics.Domain\..." />
```

---

### Task 3 — Infrastructure

**MetricsDbContext:**
```csharp
public class MetricsDbContext : DbContext, IMetricsDbContext
{
    public MetricsDbContext(DbContextOptions<MetricsDbContext> options) : base(options) { }

    public DbSet<MetricEvent> MetricEvents => Set<MetricEvent>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.HasDefaultSchema("metrics");
        mb.ApplyConfigurationsFromAssembly(typeof(MetricsDbContext).Assembly);
    }
}
```

**MetricEventConfiguration:**
```csharp
public class MetricEventConfiguration : IEntityTypeConfiguration<MetricEvent>
{
    public void Configure(EntityTypeBuilder<MetricEvent> b)
    {
        b.ToTable("metric_events");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.EventType).HasColumnName("event_type").HasMaxLength(50).IsRequired();
        b.Property(x => x.ActorId).HasColumnName("actor_id").IsRequired();
        b.Property(x => x.ContextJson).HasColumnName("context_json");
        b.Property(x => x.CorrelationId).HasColumnName("correlation_id").HasMaxLength(100);
        b.Property(x => x.OccurredAt).HasColumnName("occurred_at").IsRequired();

        b.HasIndex(x => new { x.EventType, x.OccurredAt })
            .HasDatabaseName("ix_metric_events_type_occurred");
    }
}
```

**Migration thủ công** — tạo file `20260426170000_InitialMetrics.cs`, KHÔNG dùng `dotnet ef migrations add` (giống pattern Notifications/Reporting):

```csharp
public partial class InitialMetrics : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "metrics");

        migrationBuilder.CreateTable(
            name: "metric_events",
            schema: "metrics",
            columns: table => new
            {
                id = table.Column<Guid>(nullable: false),
                event_type = table.Column<string>(maxLength: 50, nullable: false),
                actor_id = table.Column<Guid>(nullable: false),
                context_json = table.Column<string>(nullable: true),
                correlation_id = table.Column<string>(maxLength: 100, nullable: true),
                occurred_at = table.Column<DateTime>(nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_metric_events", x => x.id));

        migrationBuilder.CreateIndex(
            name: "ix_metric_events_type_occurred",
            schema: "metrics",
            table: "metric_events",
            columns: new[] { "event_type", "occurred_at" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "metric_events", schema: "metrics");
    }
}
```

**Infrastructure.csproj:**
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.7" PrivateAssets="all" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.1" />
<ProjectReference Include="...\Metrics.Application\..." />
<ProjectReference Include="...\Shared.Infrastructure\..." />
```

---

### Task 4 — Api Controller

```csharp
[ApiController]
[Route("api/v1/metrics")]
[Authorize]
public class MetricsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    // POST /api/v1/metrics/events
    [HttpPost("events")]
    public async Task<IActionResult> RecordEvent(
        [FromBody] RecordMetricEventRequest req, CancellationToken ct)
    {
        await _mediator.Send(new RecordMetricEventCommand(
            req.EventType,
            _currentUser.UserId,
            req.ContextJson,
            req.CorrelationId), ct);
        return NoContent();
    }

    // GET /api/v1/metrics/summary
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? eventType,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new GetMetricSummaryQuery(from, to, eventType), ct);
        return Ok(result);
    }
}

public sealed record RecordMetricEventRequest(
    string EventType,
    string? ContextJson,
    string? CorrelationId);
```

**MetricsModuleExtensions:**
```csharp
public static IServiceCollection AddMetricsModule(
    this IServiceCollection services, IConfiguration configuration, IMvcBuilder mvc)
{
    var connectionString = configuration.GetConnectionString("Default")!;

    services.AddDbContext<MetricsDbContext>(
        opts => opts.UseNpgsql(connectionString));
    services.AddScoped<IMetricsDbContext>(
        sp => sp.GetRequiredService<MetricsDbContext>());

    services.AddMediatR(cfg =>
        cfg.RegisterServicesFromAssembly(typeof(RecordMetricEventHandler).Assembly));

    mvc.AddApplicationPart(typeof(MetricsController).Assembly);
    return services;
}
```

---

### Task 5 — Host registration

**Program.cs** — thêm sau `AddNotificationsModule`:
```csharp
builder.Services.AddMetricsModule(builder.Configuration, mvc);
```

**Auto-migrate block:**
```csharp
await app.Services.GetRequiredService<MetricsDbContext>().Database.MigrateAsync();
```

**Host.csproj:**
```xml
<ProjectReference Include="...\Metrics.Api\ProjectManagement.Metrics.Api.csproj" />
```

---

### Task 6 — Frontend MetricsApiService

Đặt tại `src/app/core/services/metrics-api.service.ts` — **NOT** trong `features/` vì đây là cross-cutting concern được dùng bởi nhiều features.

```typescript
// File: src/app/core/services/metrics-api.service.ts
import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Injectable({ providedIn: 'root' })
export class MetricsApiService {
  private readonly http = inject(HttpClient);

  recordEvent(
    eventType: string,
    context: Record<string, unknown>,
    correlationId?: string
  ): void {
    // Fire-and-forget: KHÔNG await, KHÔNG throw, KHÔNG show toast
    this.http.post('/api/v1/metrics/events', {
      eventType,
      contextJson: JSON.stringify(context),
      correlationId: correlationId ?? crypto.randomUUID(),
    }).subscribe({ error: () => {} });  // swallow all errors silently
  }
}
```

**Quan trọng:**
- `subscribe({ error: () => {} })` — bắt buộc để tránh UnhandledError
- KHÔNG inject vào NgRx Effect — inject trực tiếp vào component (fire-and-forget không cần state)
- `crypto.randomUUID()` có sẵn trên Chrome 100+/Edge 100+ (browser support của dự án)

---

### Task 7 — Tích hợp overload-dashboard (Story 4.3)

**File cần sửa:** `src/app/features/capacity/components/overload-dashboard/overload-dashboard.ts`

Thêm `MetricsApiService` vào component và gọi sau khi dispatch `logOverride`:

```typescript
// Thêm import
import { MetricsApiService } from '../../../../core/services/metrics-api.service';
import { MetricEventType } from '../../../../core/models/metric-event-type';

// Trong class:
private readonly metricsApi = inject(MetricsApiService);

// Trong method xử lý override (tìm method dispatch logOverride):
onOverrideWarning(override: LogOverrideRequest): void {
  // Existing dispatch — GIỮ NGUYÊN
  this.store.dispatch(CapacityActions.logOverride({ override }));

  // NEW: ghi metric event — fire-and-forget
  this.metricsApi.recordEvent(MetricEventType.PredictiveOverride, {
    resourceId: override.resourceId,
    dateFrom: override.dateFrom,
    dateTo: override.dateTo,
    trafficLight: override.trafficLight,
  });
}
```

**Lưu ý:** Không sửa NgRx effect `logOverride$` — metric logging là side effect của UI component, không phải của store.

---

### Task 8 — Tích hợp forecast-view (Story 5.3)

**File cần sửa:** `src/app/features/capacity/components/forecast-view/forecast-view.ts` và `forecast-view.html`

**Bước 8.1:** Thêm button vào template cho các delta item có `currentTrafficLight` là `"red"` hoặc `"orange"`:

```html
<!-- Trong bảng delta (forecast-view.html), thêm cột cuối -->
<td>
  @if (item.currentTrafficLight === 'red' || item.currentTrafficLight === 'orange') {
    <button mat-stroked-button color="warn" (click)="onDeltaAction(item)">
      Đánh dấu đã xử lý
    </button>
  }
</td>
```

**Bước 8.2:** Thêm handler trong `forecast-view.ts`:

```typescript
private readonly metricsApi = inject(MetricsApiService);

onDeltaAction(item: ForecastDeltaItem): void {
  this.metricsApi.recordEvent(MetricEventType.ForecastProactive, {
    resourceId: item.resourceId,
    weekStart: item.weekStart,
    deltaPct: item.deltaPct,
    hint: item.hint,
    currentTrafficLight: item.currentTrafficLight,
  });
  // Optional UX: disable button sau click để tránh double-fire
}
```

---

### MetricEventType constants cho frontend

Tạo file `src/app/core/models/metric-event-type.ts` để dùng chung:

```typescript
// File: src/app/core/models/metric-event-type.ts
export const MetricEventType = {
  PredictiveOverride: 'predictive_override',
  ForecastProactive:  'forecast_proactive',
  SuggestionAccept:   'suggestion_accept',   // reserved — future sprint
  SuggestionOverride: 'suggestion_override',  // reserved — future sprint
} as const;
```

---

### Quan hệ với Story 4.3 (CapacityOverride vs MetricEvent)

Story 4.3 đã tạo `CapacityOverride` entity trong schema `capacity` — ghi override vào domain model của Capacity module để serve business logic (warn-only, history). Story 7.2 tạo `MetricEvent` trong schema `metrics` — ghi analytics event để serve **algorithm tuning** (là dữ liệu riêng biệt, không thay thế nhau).

Không xóa hoặc sửa `LogCapacityOverrideCommand` — giữ nguyên. Hai records cùng được tạo cho 1 override action: 1 domain record trong `capacity.capacity_overrides`, 1 analytics record trong `metrics.metric_events`.

---

### Suggestion accept/override — scope của story này

`suggestion_accept` và `suggestion_override` là event types được **định nghĩa** trong story này nhưng **chưa được trigger** từ UI — vì Smart Assignment UI chưa được implement (FR51, Sprint 7+). Dev cần:
- Định nghĩa constant trong `MetricEventType` (cả FE lẫn BE) — ✅ đã có trong task 1.2 và core/models
- KHÔNG tạo UI hay thêm call đến chúng trong story này
- Story tiếp theo cho Smart Assignment sẽ inject `MetricsApiService` và call `recordEvent(MetricEventType.SuggestionAccept, ...)` khi PM accept gợi ý

---

### Patterns từ Story 7.1 áp dụng cho story này

| Pattern | Story 7.1 | Story 7.2 |
|---|---|---|
| Module csproj | 4 csproj (Domain/App/Infra/Api) | Giống hệt |
| Migration | Tạo thủ công, không `dotnet ef` | Giống hệt |
| EF Core version | 10.0.7 trong Application | Giống hệt |
| Schema naming | `notifications` | `metrics` |
| BackgroundService | PeriodicTimer + IServiceScopeFactory | Không có (story này không cần worker) |
| Frontend service | NO NgRx, service trực tiếp | Giống — fire-and-forget, NO NgRx |

---

### Anti-patterns cần tránh

- **KHÔNG** thêm `await` cho `metricsApi.recordEvent(...)` — phải fire-and-forget
- **KHÔNG** hiển thị bất kỳ error UI nào khi metrics call fail
- **KHÔNG** inject `MetricsDbContext` sang module khác — chỉ dùng trong Metrics module
- **KHÔNG** block user action chờ metric ghi xong
- **KHÔNG** dùng `dotnet ef migrations add` — tạo migration thủ công
- **KHÔNG** dùng `string.Format` hay interpolation trong Serilog — dùng structured logging
- **KHÔNG** để `ContextJson` là required ở DB — nullable column (`text`)
- **KHÔNG** query toàn bộ `metric_events` không filter — luôn apply `From`/`To` hoặc `eventType` filter trong summary endpoint

---

### Lưu ý migration ModelSnapshot

Tạo `MetricsDbContextModelSnapshot.cs` theo cùng pattern như `NotificationsDbContextModelSnapshot.cs` từ Story 7.1 — tham chiếu file đó để copy structure, thay entity và table name.

---

## Dev Agent Record

**Agent:** Claude Sonnet 4.6
**Date:** 2026-04-26
**Status:** All 9 tasks completed. `dotnet build` → 0 errors. `ng build` → 0 errors.

**Implementation notes:**
- `GetMetricSummaryQuery` loads `{ EventType, OccurredAt }` into memory before grouping — EF Core 10 cannot translate `DateTime.ToString(format)` to SQL.
- `ArgumentException` from handler is caught by `GlobalExceptionMiddleware` → 400 ProblemDetails (no custom exception needed).
- `Metrics.Infrastructure.csproj` does NOT reference `Auth.Domain` — `actorId` comes from `ICurrentUserService` resolved in the controller.
- Frontend button in `forecast-view.html` uses `@if` (Angular 17+ built-in control flow) instead of `*ngIf` to avoid deprecation warning.

---

## Files sẽ tạo / sửa

**Backend — Domain:**
- `src/Modules/Metrics/ProjectManagement.Metrics.Domain/Entities/MetricEvent.cs` — mới
- `src/Modules/Metrics/ProjectManagement.Metrics.Domain/Enums/MetricEventType.cs` — mới
- `src/Modules/Metrics/ProjectManagement.Metrics.Domain/ProjectManagement.Metrics.Domain.csproj` — mới

**Backend — Application:**
- `src/Modules/Metrics/ProjectManagement.Metrics.Application/Common/Interfaces/IMetricsDbContext.cs` — mới
- `src/Modules/Metrics/ProjectManagement.Metrics.Application/Commands/RecordMetricEvent/RecordMetricEventCommand.cs` — mới
- `src/Modules/Metrics/ProjectManagement.Metrics.Application/Queries/GetMetricSummary/GetMetricSummaryQuery.cs` — mới
- `src/Modules/Metrics/ProjectManagement.Metrics.Application/ProjectManagement.Metrics.Application.csproj` — mới

**Backend — Infrastructure:**
- `src/Modules/Metrics/ProjectManagement.Metrics.Infrastructure/Persistence/MetricsDbContext.cs` — mới
- `src/Modules/Metrics/ProjectManagement.Metrics.Infrastructure/Persistence/Configurations/MetricEventConfiguration.cs` — mới
- `src/Modules/Metrics/ProjectManagement.Metrics.Infrastructure/Migrations/20260426170000_InitialMetrics.cs` — mới
- `src/Modules/Metrics/ProjectManagement.Metrics.Infrastructure/Migrations/MetricsDbContextModelSnapshot.cs` — mới
- `src/Modules/Metrics/ProjectManagement.Metrics.Infrastructure/ProjectManagement.Metrics.Infrastructure.csproj` — mới

**Backend — Api:**
- `src/Modules/Metrics/ProjectManagement.Metrics.Api/Controllers/MetricsController.cs` — mới
- `src/Modules/Metrics/ProjectManagement.Metrics.Api/Extensions/MetricsModuleExtensions.cs` — mới
- `src/Modules/Metrics/ProjectManagement.Metrics.Api/ProjectManagement.Metrics.Api.csproj` — mới

**Backend — Host:**
- `src/Host/ProjectManagement.Host/ProjectManagement.Host.csproj` — thêm Metrics.Api ref
- `src/Host/ProjectManagement.Host/Program.cs` — thêm `AddMetricsModule` + `MetricsDbContext.MigrateAsync`

**Solution:**
- `ProjectManagement.slnx` — thêm Metrics folder với 4 projects

**Frontend:**
- `src/app/core/services/metrics-api.service.ts` — mới
- `src/app/core/models/metric-event-type.ts` — mới
- `src/app/features/capacity/components/overload-dashboard/overload-dashboard.ts` — sửa (thêm inject + onOverrideWarning call)
- `src/app/features/capacity/components/forecast-view/forecast-view.ts` — sửa (thêm inject + onDeltaAction)
- `src/app/features/capacity/components/forecast-view/forecast-view.html` — sửa (thêm button "Đánh dấu đã xử lý")
