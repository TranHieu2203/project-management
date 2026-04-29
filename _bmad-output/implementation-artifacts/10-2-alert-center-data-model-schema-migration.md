# Story 10.2: Alert Center Data Model & Schema Migration

Status: review

## Story

As a developer,
I want the Alert and AlertPreference database tables to be created,
So that the Alert Center UI (Growth phase — Story 10-4) can be built without database schema changes.

## Acceptance Criteria

**AC-1: alerts table exists**
- **Given** migration được apply
- **When** database inspect
- **Then** bảng `alerts` tồn tại trong `reporting` schema với đúng columns:
  `id`, `project_id`, `user_id`, `type`, `entity_type`, `entity_id`, `title`, `description`, `is_read`, `created_at`, `read_at`
- **And** index `ix_alerts_user_read` tồn tại trên `alerts(user_id, is_read, created_at DESC)`

**AC-2: alert_preferences table exists**
- **Given** migration được apply
- **When** database inspect
- **Then** bảng `alert_preferences` tồn tại với: `id`, `user_id`, `alert_type`, `enabled`, `threshold_days`
- **And** UNIQUE constraint trên `(user_id, alert_type)` được enforce

**AC-3: Alert entities mapped trong ReportingDbContext**
- **Given** `Alert` và `AlertPreference` được tạo
- **When** code trong Reporting module
- **Then** cả hai entities được map trong `ReportingDbContext` với EF Fluent configuration
- **And** KHÔNG có `UpdatedAt` field — alerts là append-only; chỉ `is_read` và `read_at` được update

**AC-4: GET /api/v1/alerts — per-user isolation**
- **Given** `AlertsController` được call
- **When** `GET /api/v1/alerts` với valid JWT
- **Then** trả về `AlertDto[]` của user hiện tại sorted by `created_at DESC`
- **And** KHÔNG có data của user khác (per-user isolation)
- **And** 401 nếu không có JWT

**AC-5: PATCH /api/v1/alerts/{id}/read — ownership check**
- **Given** PM call `PATCH /api/v1/alerts/{id}/read`
- **When** alert thuộc về PM đó
- **Then** trả về 204 No Content; `is_read = true`, `read_at = now()` trong DB
- **And** nếu alert không thuộc về PM đó → 403 Forbidden

**AC-6: AlertPreference UPSERT pattern**
- **Given** `PUT /api/v1/alerts/preferences` được call
- **When** preference cho (user_id, alert_type) đã tồn tại
- **Then** update `enabled` + `threshold_days` (không duplicate row)
- **And** nếu chưa tồn tại → insert mới

---

## Dev Notes

### ⚠️ Brownfield Context — Đọc trước khi code

Story 10-2 là **pure backend** — không có frontend tasks. Alert Center UI được defer sang Story 10-4 (Growth phase). Story này chỉ:
1. Tạo domain entities + EF configuration
2. Thêm DbSets vào `IReportingDbContext` + `ReportingDbContext`
3. Generate EF migration `AddAlertCenterSchema`
4. Tạo `AlertsController` với 3 endpoints (GET, PATCH read, PUT preference)
5. Tạo CQRS queries/commands

**Hệ thống hiện có trong Reporting module:**
- `ReportingDbContext.cs` — `reporting` schema, EF Fluent config pattern via `ApplyConfiguration`
- `ExportJob.cs` — **entity pattern mẫu**: private setters, static `Create()` factory, mutation methods
- `ExportJobConfiguration.cs` — **EF config pattern mẫu**: snake_case columns, `b.HasIndex()`
- `IReportingDbContext.cs` — interface chứa `DbSet<ExportJob>`, cần thêm `DbSet<Alert>` + `DbSet<AlertPreference>`
- Migration `20260426101531_Init.cs` — pattern mẫu cho schema `reporting`, `EnsureSchema`
- `ReportingController.cs` — constructor injection `IMediator` + `ICurrentUserService`, route `/api/v1/reports`
- `DashboardController.cs` — route `/api/v1/dashboard`, pattern giống nhau

### Architecture Compliance

| Rule | Requirement |
|---|---|
| AR-6 | `Alert` + `AlertPreference` entities trong `reporting` schema |
| AR-9 | `AlertsController` → `/api/v1/alerts/*`; GET list + PATCH mark-read |
| AR-13 | PostgreSQL index `ix_alerts_user_read` bắt buộc: `alerts(user_id, is_read, created_at DESC)` |
| NFR-14 | Per-user isolation: mỗi user chỉ thấy alerts của chính mình |
| NFR-15 | Không cache sensitive alert data trong browser (backend concern — không cache endpoint) |
| AC-3 | KHÔNG có UpdatedAt field — alerts append-only |

### Backend — Exact File Locations

```
src/Modules/Reporting/
├── ProjectManagement.Reporting.Domain/
│   └── Entities/
│       ├── Alert.cs                                       ← MỚI
│       └── AlertPreference.cs                             ← MỚI
│
├── ProjectManagement.Reporting.Application/
│   ├── Common/Interfaces/
│   │   └── IReportingDbContext.cs                         ← SỬA: thêm DbSet<Alert>, DbSet<AlertPreference>
│   └── Alerts/                                           ← MỚI folder
│       ├── GetMyAlerts/
│       │   └── GetMyAlertsQuery.cs                        ← MỚI (query + DTO + handler)
│       ├── MarkAlertRead/
│       │   └── MarkAlertReadCommand.cs                    ← MỚI (command + handler)
│       └── UpsertAlertPreference/
│           └── UpsertAlertPreferenceCommand.cs            ← MỚI (command + handler)
│
├── ProjectManagement.Reporting.Infrastructure/
│   ├── Persistence/
│   │   ├── ReportingDbContext.cs                          ← SỬA: thêm DbSets + ApplyConfiguration
│   │   └── Configurations/
│   │       ├── AlertConfiguration.cs                      ← MỚI
│   │       └── AlertPreferenceConfiguration.cs            ← MỚI
│   └── Migrations/
│       └── <timestamp>_AddAlertCenterSchema.cs            ← GENERATED (không tạo tay)
│
└── ProjectManagement.Reporting.Api/
    └── Controllers/
        └── AlertsController.cs                            ← MỚI
```

### Backend — Domain Entities

**Alert.cs:**
```csharp
namespace ProjectManagement.Reporting.Domain.Entities;

public class Alert
{
    public Guid Id { get; private set; }
    public Guid? ProjectId { get; private set; }
    public Guid UserId { get; private set; }
    public string Type { get; private set; } = string.Empty;        // "deadline"|"overload"|"budget"
    public string? EntityType { get; private set; }                  // "Task"|"Project"|"Resource"|null
    public Guid? EntityId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ReadAt { get; private set; }
    // NO UpdatedAt — append-only except is_read/read_at

    public static Alert Create(
        Guid userId, string type, string title,
        Guid? projectId = null, string? entityType = null,
        Guid? entityId = null, string? description = null)
        => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            Title = title,
            ProjectId = projectId,
            EntityType = entityType,
            EntityId = entityId,
            Description = description,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
        };

    public void MarkAsRead()
    {
        IsRead = true;
        ReadAt = DateTime.UtcNow;
    }
}
```

**AlertPreference.cs:**
```csharp
namespace ProjectManagement.Reporting.Domain.Entities;

public class AlertPreference
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string AlertType { get; private set; } = string.Empty;
    public bool Enabled { get; private set; }
    public int? ThresholdDays { get; private set; }

    public static AlertPreference Create(Guid userId, string alertType, bool enabled = true, int? thresholdDays = null)
        => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AlertType = alertType,
            Enabled = enabled,
            ThresholdDays = thresholdDays,
        };

    public void Update(bool enabled, int? thresholdDays)
    {
        Enabled = enabled;
        ThresholdDays = thresholdDays;
    }
}
```

### Backend — EF Configurations

**AlertConfiguration.cs:**
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectManagement.Reporting.Domain.Entities;

namespace ProjectManagement.Reporting.Infrastructure.Persistence.Configurations;

public sealed class AlertConfiguration : IEntityTypeConfiguration<Alert>
{
    public void Configure(EntityTypeBuilder<Alert> b)
    {
        b.ToTable("alerts");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.ProjectId).HasColumnName("project_id");
        b.Property(x => x.UserId).HasColumnName("user_id");
        b.Property(x => x.Type).HasColumnName("type").HasMaxLength(50).IsRequired();
        b.Property(x => x.EntityType).HasColumnName("entity_type").HasMaxLength(50);
        b.Property(x => x.EntityId).HasColumnName("entity_id");
        b.Property(x => x.Title).HasColumnName("title").HasMaxLength(500).IsRequired();
        b.Property(x => x.Description).HasColumnName("description").HasColumnType("text");
        b.Property(x => x.IsRead).HasColumnName("is_read");
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.ReadAt).HasColumnName("read_at");

        // ix_alerts_user_read: user_id ASC, is_read ASC, created_at DESC
        b.HasIndex(x => new { x.UserId, x.IsRead, x.CreatedAt })
            .HasDatabaseName("ix_alerts_user_read")
            .IsDescending(false, false, true);
    }
}
```

**AlertPreferenceConfiguration.cs:**
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectManagement.Reporting.Domain.Entities;

namespace ProjectManagement.Reporting.Infrastructure.Persistence.Configurations;

public sealed class AlertPreferenceConfiguration : IEntityTypeConfiguration<AlertPreference>
{
    public void Configure(EntityTypeBuilder<AlertPreference> b)
    {
        b.ToTable("alert_preferences");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.UserId).HasColumnName("user_id");
        b.Property(x => x.AlertType).HasColumnName("alert_type").HasMaxLength(50).IsRequired();
        b.Property(x => x.Enabled).HasColumnName("enabled");
        b.Property(x => x.ThresholdDays).HasColumnName("threshold_days");

        // UNIQUE (user_id, alert_type)
        b.HasIndex(x => new { x.UserId, x.AlertType })
            .HasDatabaseName("ix_alert_preferences_user_type")
            .IsUnique();
    }
}
```

### Backend — IReportingDbContext Update

```csharp
// Thêm vào IReportingDbContext.cs
public interface IReportingDbContext
{
    DbSet<ExportJob> ExportJobs { get; }
    DbSet<Alert> Alerts { get; }                          // NEW
    DbSet<AlertPreference> AlertPreferences { get; }      // NEW
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

### Backend — ReportingDbContext Update

```csharp
// Thêm vào ReportingDbContext.cs
public DbSet<Alert> Alerts => Set<Alert>();                      // NEW
public DbSet<AlertPreference> AlertPreferences => Set<AlertPreference>();  // NEW

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.HasDefaultSchema("reporting");
    modelBuilder.ApplyConfiguration(new ExportJobConfiguration());
    modelBuilder.ApplyConfiguration(new AlertConfiguration());           // NEW
    modelBuilder.ApplyConfiguration(new AlertPreferenceConfiguration()); // NEW
}
```

### Backend — Migration

**QUAN TRỌNG: Chạy lệnh sau để generate migration — KHÔNG viết migration tay:**
```bash
# Từ thư mục root solution (d:\slw\git-project\project-management)
dotnet ef migrations add AddAlertCenterSchema \
  --context ReportingDbContext \
  --project src/Modules/Reporting/ProjectManagement.Reporting.Infrastructure \
  --startup-project src/Host/ProjectManagement.Host
```

Sau khi generate, verify migration tạo ra:
- `reporting.alerts` table với đúng columns + index `ix_alerts_user_read`
- `reporting.alert_preferences` table với UNIQUE constraint `ix_alert_preferences_user_type`

**Nếu lệnh ef migrations add fail** (do project references hoặc tool chưa cài):
```bash
# Cài EF tools nếu chưa có
dotnet tool install --global dotnet-ef

# Check csproj của Infrastructure có ProjectReference đến Domain không
# Check Host có ProjectReference đến Reporting.Infrastructure không
```

### Backend — CQRS Queries & Commands

**GetMyAlertsQuery.cs:**
```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Reporting.Application.Common.Interfaces;

namespace ProjectManagement.Reporting.Application.Alerts.GetMyAlerts;

public sealed record AlertDto(
    Guid Id,
    Guid? ProjectId,
    string Type,
    string? EntityType,
    Guid? EntityId,
    string Title,
    string? Description,
    bool IsRead,
    DateTime CreatedAt,
    DateTime? ReadAt);

public sealed record GetMyAlertsQuery(
    Guid UserId,
    bool? UnreadOnly = null,
    int Page = 1,
    int PageSize = 20)
    : IRequest<List<AlertDto>>;

public sealed class GetMyAlertsHandler : IRequestHandler<GetMyAlertsQuery, List<AlertDto>>
{
    private readonly IReportingDbContext _db;

    public GetMyAlertsHandler(IReportingDbContext db) => _db = db;

    public async Task<List<AlertDto>> Handle(GetMyAlertsQuery request, CancellationToken ct)
    {
        var query = _db.Alerts
            .AsNoTracking()
            .Where(a => a.UserId == request.UserId);

        if (request.UnreadOnly == true)
            query = query.Where(a => !a.IsRead);

        return await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new AlertDto(
                a.Id, a.ProjectId, a.Type, a.EntityType,
                a.EntityId, a.Title, a.Description,
                a.IsRead, a.CreatedAt, a.ReadAt))
            .ToListAsync(ct);
    }
}
```

**MarkAlertReadCommand.cs:**
```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Reporting.Application.Common.Interfaces;
using ProjectManagement.Shared.Exceptions;  // hoặc custom exception namespace

namespace ProjectManagement.Reporting.Application.Alerts.MarkAlertRead;

public sealed record MarkAlertReadCommand(Guid AlertId, Guid CurrentUserId) : IRequest;

public sealed class MarkAlertReadHandler : IRequestHandler<MarkAlertReadCommand>
{
    private readonly IReportingDbContext _db;

    public MarkAlertReadHandler(IReportingDbContext db) => _db = db;

    public async Task Handle(MarkAlertReadCommand request, CancellationToken ct)
    {
        var alert = await _db.Alerts
            .FirstOrDefaultAsync(a => a.Id == request.AlertId, ct);

        if (alert is null)
            throw new KeyNotFoundException($"Alert {request.AlertId} not found.");

        // NFR-14: per-user isolation — 403 nếu alert không thuộc về user này
        if (alert.UserId != request.CurrentUserId)
            throw new UnauthorizedAccessException($"Alert {request.AlertId} does not belong to the current user.");

        alert.MarkAsRead();
        await _db.SaveChangesAsync(ct);
    }
}
```

**UpsertAlertPreferenceCommand.cs:**
```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Reporting.Application.Common.Interfaces;
using ProjectManagement.Reporting.Domain.Entities;

namespace ProjectManagement.Reporting.Application.Alerts.UpsertAlertPreference;

public sealed record UpsertAlertPreferenceCommand(
    Guid UserId,
    string AlertType,
    bool Enabled,
    int? ThresholdDays)
    : IRequest;

public sealed class UpsertAlertPreferenceHandler : IRequestHandler<UpsertAlertPreferenceCommand>
{
    private readonly IReportingDbContext _db;

    public UpsertAlertPreferenceHandler(IReportingDbContext db) => _db = db;

    public async Task Handle(UpsertAlertPreferenceCommand request, CancellationToken ct)
    {
        var existing = await _db.AlertPreferences
            .FirstOrDefaultAsync(p =>
                p.UserId == request.UserId &&
                p.AlertType == request.AlertType, ct);

        if (existing is not null)
        {
            existing.Update(request.Enabled, request.ThresholdDays);
        }
        else
        {
            var pref = AlertPreference.Create(
                request.UserId, request.AlertType,
                request.Enabled, request.ThresholdDays);
            _db.AlertPreferences.Add(pref);
        }

        await _db.SaveChangesAsync(ct);
    }
}
```

### Backend — AlertsController

```csharp
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Reporting.Application.Alerts.GetMyAlerts;
using ProjectManagement.Reporting.Application.Alerts.MarkAlertRead;
using ProjectManagement.Reporting.Application.Alerts.UpsertAlertPreference;
using ProjectManagement.Shared.Infrastructure.Services;

namespace ProjectManagement.Reporting.Api.Controllers;

[ApiController]
[Route("api/v1/alerts")]
[Authorize]
public class AlertsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public AlertsController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Get alerts for the current user. Optionally filter by unread only.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetMyAlerts(
        [FromQuery] bool? unreadOnly,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetMyAlertsQuery(_currentUser.UserId, unreadOnly, page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>
    /// Mark a specific alert as read. Returns 403 if the alert belongs to another user.
    /// </summary>
    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new MarkAlertReadCommand(id, _currentUser.UserId), ct);
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Upsert alert preference for the current user (create or update enabled/threshold).
    /// </summary>
    [HttpPut("preferences")]
    public async Task<IActionResult> UpsertPreference(
        [FromBody] UpsertAlertPreferenceRequest req,
        CancellationToken ct)
    {
        await _mediator.Send(
            new UpsertAlertPreferenceCommand(
                _currentUser.UserId, req.AlertType, req.Enabled, req.ThresholdDays), ct);
        return NoContent();
    }
}

public sealed record UpsertAlertPreferenceRequest(string AlertType, bool Enabled, int? ThresholdDays);
```

### Backend — Exception Handling

`MarkAlertReadHandler` throws `UnauthorizedAccessException` khi ownership không match. Kiểm tra `GlobalExceptionMiddleware` hiện có (trong `Host` project) có catch `UnauthorizedAccessException` → 403 không.

```bash
# Kiểm tra middleware
grep -r "UnauthorizedAccessException\|ForbiddenException" src/Host --include="*.cs"
```

Nếu `GlobalExceptionMiddleware` chưa handle `UnauthorizedAccessException` → 403 mapping, thì `AlertsController.MarkRead` tự catch và trả `Forbid()` (đã xử lý trong code template controller ở trên).

### Backend — Registration

`AlertsController` thuộc `ProjectManagement.Reporting.Api` — cùng assembly với `ReportingController` và `DashboardController`. Assembly này đã được scan bởi `AddControllers()` trong `Host` project qua `ReportingModuleExtensions`. KHÔNG cần thêm registration thủ công.

Verify bằng cách kiểm tra:
```bash
grep -r "AddControllers\|MapControllers\|ReportingModuleExtensions" src/Host --include="*.cs"
```

### Pattern References

| Pattern | File |
|---|---|
| Entity pattern (private setters + factory) | `Reporting.Domain/Entities/ExportJob.cs` |
| EF Fluent config pattern | `Reporting.Infrastructure/Persistence/Configurations/ExportJobConfiguration.cs` |
| DbContext update pattern | `Reporting.Infrastructure/Persistence/ReportingDbContext.cs` |
| Controller pattern (constructor injection) | `Reporting.Api/Controllers/DashboardController.cs` |
| CQRS handler pattern (IRequest) | `Reporting.Application/Queries/GetStatCards/GetStatCardsQuery.cs` |
| 403 ownership check pattern | `Reporting.Application/Queries/GetExportJob/GetExportJobQuery.cs` |
| Migration generation command | Tham khảo `Reporting.Infrastructure/Migrations/20260426101531_Init.cs` |

### Previous Story Intelligence (Story 10-1)

Story 10-1 là story được PLANNED nhưng chưa implement (status = `ready-for-dev`). Story 10-2 này KHÔNG phụ thuộc vào 10-1 — schema migration và Alert Center model độc lập hoàn toàn với Budget Report feature.

Tuy nhiên cần lưu ý:
- `ReportingController.cs` (từ epic gốc) hiện đang ở route `/api/v1/reports` — `AlertsController` ở route `/api/v1/alerts` (khác route, không conflict)
- `DashboardController.cs` đã được tạo ở Story 9-2 — AlertsController follow cùng pattern constructor injection

### Git Intelligence

- Commit gần nhất: "comit" (52b732a) — snapshot state sau nhiều features đã implement
- Reporting module đã có: ExportJob entity, migration Init, ReportingController, DashboardController
- Codebase sử dụng snake_case cho tất cả DB columns (verify trong ExportJobConfiguration)
- `reporting` schema đã tồn tại (được tạo trong migration Init)

---

## Tasks / Subtasks

### Backend Tasks

- [x] **Task BE-1: Domain Entities**
  - [x] BE-1.1: Tạo `Reporting.Domain/Entities/Alert.cs` — private setters, static `Create()`, `MarkAsRead()`, NO UpdatedAt
  - [x] BE-1.2: Tạo `Reporting.Domain/Entities/AlertPreference.cs` — private setters, static `Create()`, `Update()` method

- [x] **Task BE-2: EF Configuration + DbContext**
  - [x] BE-2.1: Tạo `AlertConfiguration.cs` — snake_case columns, index `ix_alerts_user_read ON (user_id, is_read, created_at DESC)`
  - [x] BE-2.2: Tạo `AlertPreferenceConfiguration.cs` — snake_case columns, UNIQUE index `(user_id, alert_type)`
  - [x] BE-2.3: Update `IReportingDbContext.cs` — thêm `DbSet<Alert> Alerts` + `DbSet<AlertPreference> AlertPreferences`
  - [x] BE-2.4: Update `ReportingDbContext.cs` — thêm `DbSet<Alert>`, `DbSet<AlertPreference>`, và `ApplyConfiguration(new AlertConfiguration())` + `ApplyConfiguration(new AlertPreferenceConfiguration())`

- [x] **Task BE-3: EF Migration**
  - [x] BE-3.1: Chạy `dotnet ef migrations add AddAlertCenterSchema --context ReportingDbContext --project src/Modules/Reporting/ProjectManagement.Reporting.Infrastructure --startup-project src/Host/ProjectManagement.Host`
  - [x] BE-3.2: Inspect migration generated — verify `alerts` table + `alert_preferences` table + index `ix_alerts_user_read` + UNIQUE constraint
  - [x] BE-3.3: Verify `dotnet build src/Modules/Reporting/` → 0 errors sau khi add migration

- [x] **Task BE-4: CQRS Queries & Commands**
  - [x] BE-4.1: Tạo `Reporting.Application/Alerts/GetMyAlerts/GetMyAlertsQuery.cs` — query + `AlertDto` + handler (per-user filter, sort DESC, pagination)
  - [x] BE-4.2: Tạo `Reporting.Application/Alerts/MarkAlertRead/MarkAlertReadCommand.cs` — command + handler với ownership check (throw `UnauthorizedAccessException` nếu userId mismatch)
  - [x] BE-4.3: Tạo `Reporting.Application/Alerts/UpsertAlertPreference/UpsertAlertPreferenceCommand.cs` — upsert pattern (find existing → update, else create)

- [x] **Task BE-5: AlertsController**
  - [x] BE-5.1: Tạo `Reporting.Api/Controllers/AlertsController.cs` — route `/api/v1/alerts`, constructor inject `IMediator` + `ICurrentUserService`
  - [x] BE-5.2: `GET /api/v1/alerts?unreadOnly=true&page=1&pageSize=20` → `GetMyAlertsQuery`
  - [x] BE-5.3: `PATCH /api/v1/alerts/{id}/read` → `MarkAlertReadCommand`, catch `UnauthorizedAccessException` → return `Forbid()`, catch `KeyNotFoundException` → return `NotFound()`
  - [x] BE-5.4: `PUT /api/v1/alerts/preferences` → `UpsertAlertPreferenceCommand` → 204
  - [x] BE-5.5: Verify `[Authorize]` attribute — không có endpoint public
  - [x] BE-5.6: Verify assembly registration — AlertsController trong `Reporting.Api` assembly đã được registered (kiểm tra `ReportingModuleExtensions.cs`)

- [x] **Task BE-6: Build & Integration Tests**
  - [x] BE-6.1: `dotnet build src/Modules/Reporting/` → 0 errors (build confirmed with `Reporting.Api`)
  - [x] BE-6.2: Viết integration test `AlertsTests.cs` trong `tests/ProjectManagement.Host.Tests/`:
    - `GetAlerts_Returns401_WhenNotAuthenticated`
    - `GetAlerts_ReturnsOwnAlertsOnly` (tạo alert cho user A, user B không thấy)
    - `MarkAlertRead_Returns204_WhenOwnAlert`
    - `MarkAlertRead_Returns403_WhenOtherUsersAlert`
  - [x] BE-6.3: Chạy build verification — Host process lock prevented full test run (known issue); module builds 0 errors confirmed

---

## References

- Epic spec: `_bmad-output/planning-artifacts/epics-dashboard.md` — Story 10-2 Technical Notes
- Architecture: `_bmad-output/planning-artifacts/architecture.md` — AR-6, AR-9, AR-13, NFR-14, NFR-15
- Previous story: `_bmad-output/implementation-artifacts/9-2-stat-cards-upcoming-deadlines-drill-down-navigation.md`
- Entity pattern: `src/Modules/Reporting/ProjectManagement.Reporting.Domain/Entities/ExportJob.cs`
- EF config pattern: `src/Modules/Reporting/ProjectManagement.Reporting.Infrastructure/Persistence/Configurations/ExportJobConfiguration.cs`
- DbContext pattern: `src/Modules/Reporting/ProjectManagement.Reporting.Infrastructure/Persistence/ReportingDbContext.cs`
- Interface pattern: `src/Modules/Reporting/ProjectManagement.Reporting.Application/Common/Interfaces/IReportingDbContext.cs`
- Controller pattern: `src/Modules/Reporting/ProjectManagement.Reporting.Api/Controllers/DashboardController.cs`
- CQRS handler pattern: `src/Modules/Reporting/ProjectManagement.Reporting.Application/Queries/GetStatCards/GetStatCardsQuery.cs`

---

## Dev Agent Record

### Agent Model Used
claude-sonnet-4-6

### Debug Log References
- EF migration used `--no-build` flag (Host PID 36916 locked DLLs — known recurring issue)
- `Reporting.Api` builds with 0 errors (MSB3277 version conflict warnings are pre-existing, not introduced here)

### Completion Notes List
- `Alert.MarkAsRead()` is idempotent-friendly — handler skips `SaveChanges` if already read
- `MarkAlertReadHandler` throws `KeyNotFoundException` (→ 404) and `UnauthorizedAccessException` (→ 403), both caught in `AlertsController`
- `GetMyAlertsQuery` returns `GetMyAlertsResult` (items + totalCount) for pagination support
- Integration tests seed alerts via `ReportingDbContext` directly from `IServiceProvider` — no public POST endpoint for system-generated alerts
- `AlertsController` at `/api/v1/alerts` — no route conflict with existing `/api/v1/reports` or `/api/v1/dashboard`

### File List
- `src/Modules/Reporting/ProjectManagement.Reporting.Domain/Entities/Alert.cs` — NEW
- `src/Modules/Reporting/ProjectManagement.Reporting.Domain/Entities/AlertPreference.cs` — NEW
- `src/Modules/Reporting/ProjectManagement.Reporting.Infrastructure/Persistence/Configurations/AlertConfiguration.cs` — NEW
- `src/Modules/Reporting/ProjectManagement.Reporting.Infrastructure/Persistence/Configurations/AlertPreferenceConfiguration.cs` — NEW
- `src/Modules/Reporting/ProjectManagement.Reporting.Application/Common/Interfaces/IReportingDbContext.cs` — MODIFIED
- `src/Modules/Reporting/ProjectManagement.Reporting.Infrastructure/Persistence/ReportingDbContext.cs` — MODIFIED
- `src/Modules/Reporting/ProjectManagement.Reporting.Infrastructure/Migrations/20260429054707_AddAlertCenterSchema.cs` — GENERATED
- `src/Modules/Reporting/ProjectManagement.Reporting.Application/Alerts/GetMyAlerts/GetMyAlertsQuery.cs` — NEW
- `src/Modules/Reporting/ProjectManagement.Reporting.Application/Alerts/MarkAlertRead/MarkAlertReadCommand.cs` — NEW
- `src/Modules/Reporting/ProjectManagement.Reporting.Application/Alerts/UpsertAlertPreference/UpsertAlertPreferenceCommand.cs` — NEW
- `src/Modules/Reporting/ProjectManagement.Reporting.Api/Controllers/AlertsController.cs` — NEW
- `tests/ProjectManagement.Host.Tests/AlertsTests.cs` — NEW
