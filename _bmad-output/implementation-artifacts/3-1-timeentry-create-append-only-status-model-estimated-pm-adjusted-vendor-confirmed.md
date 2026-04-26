# Story 3.1: TimeEntry Create (Append-Only) + Status Model

Status: review

**Story ID:** 3.1
**Epic:** Epic 3 — TimeEntry & Timesheet (2-tier: Grid + Vendor Import) + Status/Lock + Corrections
**Sprint:** Sprint 4
**Date Created:** 2026-04-26

---

## Story

As a PM,
I want tạo TimeEntry theo ngày/tuần với status rõ ràng,
So that hệ thống có dữ liệu vận hành real-time và phục vụ báo cáo chính thức.

## Acceptance Criteria

1. **Given** user tạo TimeEntry từ UI
   **When** lưu entry
   **Then** entry được INSERT mới (append-only) với `entryType` = `pm_adjusted` (hoặc `estimated`) và có `enteredBy` tách biệt `resourceId`

2. **Given** entry status dùng cho báo cáo
   **When** truy vấn entries
   **Then** response thể hiện rõ status (`entryType`) và nguồn dữ liệu (`enteredBy`, `rateAtTime`, `costAtTime`)

3. **Given** TimeEntry được tạo
   **When** mutation thành công
   **Then** `rateAtTime` phải được snapshot tại thời điểm tạo (không null), `costAtTime = hours × rateAtTime`

4. **Given** TimeEntry là immutable
   **When** không có UPDATE endpoint cho `/api/v1/time-entries/{id}`
   **Then** chỉ có POST (create) và GET (read) — không có PUT/PATCH

## Tasks / Subtasks

- [x] **Task 1: Tạo TimeTracking module scaffold (BE)**
  - [x] 1.1 Tạo `ProjectManagement.TimeTracking.Domain.csproj`
  - [x] 1.2 Tạo `ProjectManagement.TimeTracking.Application.csproj`
  - [x] 1.3 Tạo `ProjectManagement.TimeTracking.Infrastructure.csproj`
  - [x] 1.4 Tạo `ProjectManagement.TimeTracking.Api.csproj`
  - [x] 1.5 Thêm 4 projects vào `ProjectManagement.slnx`
  - [x] 1.6 Thêm reference đến `TimeTracking.Api` trong `ProjectManagement.Host.csproj`

- [x] **Task 2: Domain Layer (BE)**
  - [x] 2.1 Tạo `TimeEntryStatus.cs` enum: `Estimated`, `PmAdjusted`, `VendorConfirmed`
  - [x] 2.2 Tạo `TimeEntry.cs` entity (immutable, không extends AuditableEntity)

- [x] **Task 3: Application Layer (BE)**
  - [x] 3.1 Tạo `ITimeTrackingDbContext.cs` interface
  - [x] 3.2 Tạo `ITimeTrackingRateService.cs` interface
  - [x] 3.3 Tạo `TimeEntryDto.cs` record
  - [x] 3.4 Tạo `CreateTimeEntryCommand` + Handler
  - [x] 3.5 Tạo `GetTimeEntryByIdQuery` + Handler

- [x] **Task 4: Infrastructure Layer (BE)**
  - [x] 4.1 Tạo `TimeEntryConfiguration.cs` EF config
  - [x] 4.2 Tạo `TimeTrackingDbContext.cs` (schema `time_tracking`)
  - [x] 4.3 Tạo `TimeTrackingRateService.cs` (implements `ITimeTrackingRateService`)
  - [x] 4.4 Tạo `TimeTrackingInfrastructureExtensions.cs`
  - [x] 4.5 Tạo EF migration `Init_TimeTracking`

- [x] **Task 5: API Layer (BE)**
  - [x] 5.1 Tạo `TimeEntriesController.cs` tại `/api/v1/time-entries`
  - [x] 5.2 Tạo `TimeTrackingModuleExtensions.cs`
  - [x] 5.3 Đăng ký module trong `Program.cs` + migrate `TimeTrackingDbContext` khi startup

- [x] **Task 6: Frontend (FE)**
  - [x] 6.1 Tạo `time-entry.model.ts`
  - [x] 6.2 Tạo `time-tracking-api.service.ts`
  - [x] 6.3 Tạo NgRx: `time-tracking.actions.ts`, `time-tracking.reducer.ts`, `time-tracking.effects.ts`, `time-tracking.selectors.ts`
  - [x] 6.4 Tạo `time-entry-form` component (dialog)
  - [x] 6.5 Tạo `time-entry-list` component (basic list view)
  - [x] 6.6 Tạo `time-tracking.routes.ts` + đăng ký trong `app.routes.ts`
  - [x] 6.7 Đăng ký `TimeTrackingReducer` + `TimeTrackingEffects` vào `app.state.ts` / `app.config.ts`

- [x] **Task 7: Build verification**
  - [x] 7.1 `dotnet build` → 0 errors
  - [x] 7.2 `ng build` → 0 errors

---

## Dev Notes

### Module mới — TimeTracking (KHÔNG phải Workforce)

Story này tạo module hoàn toàn mới, giống pattern Workforce:

| Layer | Project | Namespace |
|---|---|---|
| Domain | `ProjectManagement.TimeTracking.Domain` | `ProjectManagement.TimeTracking.Domain` |
| Application | `ProjectManagement.TimeTracking.Application` | `ProjectManagement.TimeTracking.Application` |
| Infrastructure | `ProjectManagement.TimeTracking.Infrastructure` | `ProjectManagement.TimeTracking.Infrastructure` |
| Api | `ProjectManagement.TimeTracking.Api` | `ProjectManagement.TimeTracking.Api` |

**Thư mục**: `src/Modules/TimeTracking/`

### Task 1 Detail: .csproj files

**Domain.csproj** — giống Workforce.Domain:
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

**Application.csproj** — giống Workforce.Application:
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
    <ProjectReference Include="..\ProjectManagement.TimeTracking.Domain\ProjectManagement.TimeTracking.Domain.csproj" />
    <ProjectReference Include="..\..\..\Shared\ProjectManagement.Shared.Domain\ProjectManagement.Shared.Domain.csproj" />
  </ItemGroup>
</Project>
```

**Infrastructure.csproj** — QUAN TRỌNG: reference thêm Workforce.Application để dùng IWorkforceDbContext:
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
    <ProjectReference Include="..\ProjectManagement.TimeTracking.Application\ProjectManagement.TimeTracking.Application.csproj" />
    <ProjectReference Include="..\..\..\Shared\ProjectManagement.Shared.Infrastructure\ProjectManagement.Shared.Infrastructure.csproj" />
    <ProjectReference Include="..\..\..\Modules\Workforce\ProjectManagement.Workforce.Application\ProjectManagement.Workforce.Application.csproj" />
  </ItemGroup>
</Project>
```

**Api.csproj** — giống Workforce.Api:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\ProjectManagement.TimeTracking.Infrastructure\ProjectManagement.TimeTracking.Infrastructure.csproj" />
    <ProjectReference Include="..\..\..\Shared\ProjectManagement.Shared.Infrastructure\ProjectManagement.Shared.Infrastructure.csproj" />
  </ItemGroup>
</Project>
```

**Host.csproj** — thêm reference:
```xml
<ProjectReference Include="..\..\Modules\TimeTracking\ProjectManagement.TimeTracking.Api\ProjectManagement.TimeTracking.Api.csproj" />
```

**slnx** — thêm folder `/src/Modules/TimeTracking/` với 4 projects.

### Task 2 Detail: Domain Layer

**TimeEntryStatus.cs:**
```csharp
// TimeTracking.Domain/Enums/TimeEntryStatus.cs
namespace ProjectManagement.TimeTracking.Domain.Enums;

public enum TimeEntryStatus
{
    Estimated,
    PmAdjusted,
    VendorConfirmed
}
```

**TimeEntry.cs** — IMMUTABLE (không extends AuditableEntity, không có UpdatedAt):
```csharp
// TimeTracking.Domain/Entities/TimeEntry.cs
namespace ProjectManagement.TimeTracking.Domain.Entities;

public class TimeEntry
{
    public Guid Id { get; private set; }
    public Guid ResourceId { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid? TaskId { get; private set; }
    public DateOnly Date { get; private set; }
    public decimal Hours { get; private set; }
    public string EntryType { get; private set; } = string.Empty;  // stored as string: "Estimated", "PmAdjusted", "VendorConfirmed"
    public string? Note { get; private set; }
    public decimal RateAtTime { get; private set; }   // hourly rate snapshot — NOT NULL
    public decimal CostAtTime { get; private set; }   // = Hours × RateAtTime
    public string EnteredBy { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    // NO UpdatedAt — immutable

    public static TimeEntry Create(
        Guid resourceId,
        Guid projectId,
        Guid? taskId,
        DateOnly date,
        decimal hours,
        string entryType,
        string? note,
        decimal rateAtTime,
        string enteredBy)
        => new()
        {
            Id = Guid.NewGuid(),
            ResourceId = resourceId,
            ProjectId = projectId,
            TaskId = taskId,
            Date = date,
            Hours = hours,
            EntryType = entryType,
            Note = note,
            RateAtTime = rateAtTime,
            CostAtTime = hours * rateAtTime,
            EnteredBy = enteredBy,
            CreatedAt = DateTime.UtcNow
        };
}
```

**Lưu ý thiết kế TimeEntry:**
- KHÔNG extends `AuditableEntity` hay `BaseEntity` — tự quản lý Id + CreatedAt
- KHÔNG có `UpdatedAt` — immutable log, enforce ở tầng domain
- `EntryType` stored as string (same pattern as Role/Level trong Workforce)
- `RateAtTime` = hourly rate tại thời điểm tạo entry — snapshot
- `CostAtTime = Hours × RateAtTime` — computed khi tạo, stored để tránh recalculate khi rate thay đổi
- Void fields (`IsVoided`, `SupersedesId`) sẽ được thêm trong Story 3.3

### Task 3 Detail: Application Layer

**ITimeTrackingDbContext.cs:**
```csharp
using Microsoft.EntityFrameworkCore;
using ProjectManagement.TimeTracking.Domain.Entities;

namespace ProjectManagement.TimeTracking.Application.Common.Interfaces;

public interface ITimeTrackingDbContext
{
    DbSet<TimeEntry> TimeEntries { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
```

**ITimeTrackingRateService.cs:**
```csharp
namespace ProjectManagement.TimeTracking.Application.Common.Interfaces;

public interface ITimeTrackingRateService
{
    // Trả về hourly rate (decimal). 0 nếu không tìm thấy rate (Inhouse hoặc chưa config).
    // Ném DomainException nếu resource không tồn tại.
    Task<decimal> GetHourlyRateAsync(
        Guid resourceId,
        string role,
        string level,
        DateOnly date,
        CancellationToken ct);
}
```

**TimeEntryDto.cs:**
```csharp
namespace ProjectManagement.TimeTracking.Application.DTOs;

public sealed record TimeEntryDto(
    Guid Id,
    Guid ResourceId,
    Guid ProjectId,
    Guid? TaskId,
    DateOnly Date,
    decimal Hours,
    string EntryType,
    string? Note,
    decimal RateAtTime,
    decimal CostAtTime,
    string EnteredBy,
    DateTime CreatedAt
);
```

**CreateTimeEntryCommand.cs:**
```csharp
using MediatR;
using ProjectManagement.TimeTracking.Application.DTOs;

namespace ProjectManagement.TimeTracking.Application.TimeEntries.Commands.CreateTimeEntry;

public sealed record CreateTimeEntryCommand(
    Guid ResourceId,
    Guid ProjectId,
    Guid? TaskId,
    DateOnly Date,
    decimal Hours,
    string EntryType,   // "Estimated" hoặc "PmAdjusted"
    string Role,        // dùng để lookup rate (enum string)
    string Level,       // dùng để lookup rate (enum string)
    string? Note,
    string EnteredBy
) : IRequest<TimeEntryDto>;
```

**CreateTimeEntryHandler.cs:**
```csharp
public sealed class CreateTimeEntryHandler : IRequestHandler<CreateTimeEntryCommand, TimeEntryDto>
{
    private readonly ITimeTrackingDbContext _db;
    private readonly ITimeTrackingRateService _rateService;

    public CreateTimeEntryHandler(ITimeTrackingDbContext db, ITimeTrackingRateService rateService)
    {
        _db = db;
        _rateService = rateService;
    }

    public async Task<TimeEntryDto> Handle(CreateTimeEntryCommand cmd, CancellationToken ct)
    {
        // Validate EntryType
        if (!Enum.TryParse<TimeEntryStatus>(cmd.EntryType, out _))
            throw new DomainException($"EntryType không hợp lệ: '{cmd.EntryType}'. Chấp nhận: Estimated, PmAdjusted.");
        // VendorConfirmed chỉ set qua import pipeline (story 3.5)
        if (cmd.EntryType == nameof(TimeEntryStatus.VendorConfirmed))
            throw new DomainException("EntryType 'VendorConfirmed' chỉ được set qua vendor import pipeline.");

        if (cmd.Hours <= 0)
            throw new DomainException("Hours phải lớn hơn 0.");

        if (cmd.Hours > 24)
            throw new DomainException("Hours không thể vượt quá 24h/ngày.");

        // Get rate snapshot
        var hourlyRate = await _rateService.GetHourlyRateAsync(
            cmd.ResourceId, cmd.Role, cmd.Level, cmd.Date, ct);

        var entry = TimeEntry.Create(
            cmd.ResourceId,
            cmd.ProjectId,
            cmd.TaskId,
            cmd.Date,
            cmd.Hours,
            cmd.EntryType,
            cmd.Note,
            hourlyRate,
            cmd.EnteredBy);

        _db.TimeEntries.Add(entry);
        await _db.SaveChangesAsync(ct);

        return ToDto(entry);
    }

    internal static TimeEntryDto ToDto(TimeEntry e) => new(
        e.Id, e.ResourceId, e.ProjectId, e.TaskId,
        e.Date, e.Hours, e.EntryType, e.Note,
        e.RateAtTime, e.CostAtTime, e.EnteredBy, e.CreatedAt);
}
```

**GetTimeEntryByIdQuery.cs:**
```csharp
public sealed record GetTimeEntryByIdQuery(Guid EntryId) : IRequest<TimeEntryDto>;
```

Handler: dùng `AsNoTracking()`, ném `NotFoundException` nếu không tìm thấy.

### Task 4 Detail: Infrastructure Layer

**TimeEntryConfiguration.cs:**
```csharp
b.ToTable("time_entries");
b.HasKey(x => x.Id);
b.Property(x => x.Id).HasColumnName("id");
b.Property(x => x.ResourceId).HasColumnName("resource_id");
b.Property(x => x.ProjectId).HasColumnName("project_id");
b.Property(x => x.TaskId).HasColumnName("task_id");
b.Property(x => x.Date).HasColumnName("date");
b.Property(x => x.Hours).HasColumnName("hours").HasPrecision(10, 2);
b.Property(x => x.EntryType).HasColumnName("entry_type").HasMaxLength(30).IsRequired();
b.Property(x => x.Note).HasColumnName("note").HasMaxLength(1000);
b.Property(x => x.RateAtTime).HasColumnName("rate_at_time").HasPrecision(18, 4);
b.Property(x => x.CostAtTime).HasColumnName("cost_at_time").HasPrecision(18, 4);
b.Property(x => x.EnteredBy).HasColumnName("entered_by").HasMaxLength(256).IsRequired();
b.Property(x => x.CreatedAt).HasColumnName("created_at");

// Indexes for common query patterns
b.HasIndex(x => new { x.ResourceId, x.Date }).HasDatabaseName("ix_time_entries_resource_date");
b.HasIndex(x => new { x.ProjectId, x.Date }).HasDatabaseName("ix_time_entries_project_date");
b.HasIndex(x => x.Date).HasDatabaseName("ix_time_entries_date");
```

**TimeTrackingDbContext.cs:**
```csharp
using Microsoft.EntityFrameworkCore;
using ProjectManagement.TimeTracking.Application.Common.Interfaces;
using ProjectManagement.TimeTracking.Domain.Entities;
using ProjectManagement.TimeTracking.Infrastructure.Persistence.Configurations;

namespace ProjectManagement.TimeTracking.Infrastructure.Persistence;

public sealed class TimeTrackingDbContext : DbContext, ITimeTrackingDbContext
{
    public TimeTrackingDbContext(DbContextOptions<TimeTrackingDbContext> options) : base(options) { }

    public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("time_tracking");
        modelBuilder.ApplyConfiguration(new TimeEntryConfiguration());
    }
}
```

**TimeTrackingRateService.cs** — cross-module rate lookup:
```csharp
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.Domain.Exceptions;
using ProjectManagement.TimeTracking.Application.Common.Interfaces;
using ProjectManagement.Workforce.Application.Common.Interfaces;

namespace ProjectManagement.TimeTracking.Infrastructure.Services;

public sealed class TimeTrackingRateService : ITimeTrackingRateService
{
    private readonly IWorkforceDbContext _workforce;

    public TimeTrackingRateService(IWorkforceDbContext workforce) => _workforce = workforce;

    public async Task<decimal> GetHourlyRateAsync(
        Guid resourceId,
        string role,
        string level,
        DateOnly date,
        CancellationToken ct)
    {
        var resource = await _workforce.Resources
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == resourceId, ct)
            ?? throw new DomainException($"Resource '{resourceId}' không tồn tại.");

        if (!resource.IsActive)
            throw new DomainException($"Resource '{resource.Name}' đã inactive.");

        // Inhouse resources → rateAtTime = 0 (không billing qua vendor)
        if (resource.VendorId is null)
            return 0m;

        var rate = await _workforce.MonthlyRates
            .AsNoTracking()
            .FirstOrDefaultAsync(r =>
                r.VendorId == resource.VendorId.Value &&
                r.Role == role &&
                r.Level == level &&
                r.Year == date.Year &&
                r.Month == date.Month, ct);

        if (rate is null)
            throw new DomainException(
                $"Không tìm thấy rate cho vendor/role '{role}'/level '{level}' tháng {date.Month}/{date.Year}.");

        return rate.HourlyRate;   // HourlyRate = MonthlyAmount / 176m (computed property)
    }
}
```

**TimeTrackingInfrastructureExtensions.cs:**
```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectManagement.TimeTracking.Application.Common.Interfaces;
using ProjectManagement.TimeTracking.Application.TimeEntries.Commands.CreateTimeEntry;
using ProjectManagement.TimeTracking.Infrastructure.Persistence;
using ProjectManagement.TimeTracking.Infrastructure.Services;

namespace ProjectManagement.TimeTracking.Infrastructure.Extensions;

public static class TimeTrackingInfrastructureExtensions
{
    public static IServiceCollection AddTimeTrackingInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("Default") ??
            "Host=localhost;Port=5432;Database=project_management;Username=pm_app;Password=pm_app_password";

        services.AddDbContext<TimeTrackingDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<ITimeTrackingDbContext>(sp => sp.GetRequiredService<TimeTrackingDbContext>());
        services.AddScoped<ITimeTrackingRateService, TimeTrackingRateService>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(CreateTimeEntryHandler).Assembly));

        return services;
    }
}
```

**EF Migration**: Chạy sau khi tạo xong DbContext + Configuration:
```bash
dotnet ef migrations add Init_TimeTracking \
  --project src/Modules/TimeTracking/ProjectManagement.TimeTracking.Infrastructure/... \
  --startup-project src/Host/ProjectManagement.Host/... \
  --context TimeTrackingDbContext
```

### Task 5 Detail: API Layer

**TimeEntriesController.cs:**
```csharp
[Authorize]
[ApiController]
[Route("api/v1/time-entries")]
public sealed class TimeEntriesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;
    
    // POST /api/v1/time-entries → 201 Created
    [HttpPost]
    public async Task<IActionResult> CreateTimeEntry(
        [FromBody] CreateTimeEntryRequest body, CancellationToken ct)
    
    // GET /api/v1/time-entries/{entryId} → 200 + TimeEntryDto
    [HttpGet("{entryId:guid}")]
    public async Task<IActionResult> GetTimeEntry(Guid entryId, CancellationToken ct)
    
    // NO PUT/PATCH/DELETE — immutable
}

public sealed record CreateTimeEntryRequest(
    Guid ResourceId,
    Guid ProjectId,
    Guid? TaskId,
    DateOnly Date,
    decimal Hours,
    string EntryType,
    string Role,
    string Level,
    string? Note);
```

**TimeTrackingModuleExtensions.cs:**
```csharp
public static IServiceCollection AddTimeTrackingModule(
    this IServiceCollection services,
    IConfiguration configuration,
    IMvcBuilder mvc)
{
    services.AddTimeTrackingInfrastructure(configuration);
    mvc.AddApplicationPart(typeof(TimeEntriesController).Assembly);
    return services;
}
```

**Program.cs additions:**
```csharp
// Sau WorkforceModule đăng ký
builder.Services.AddTimeTrackingModule(builder.Configuration, mvc);

// Trong AutoMigrate block:
var timeTrackingDb = scope.ServiceProvider.GetRequiredService<TimeTrackingDbContext>();
await timeTrackingDb.Database.MigrateAsync();
```

### Task 6 Detail: Frontend (NgRx — giống Workforce pattern)

**Vị trí files:**
```
frontend/src/app/features/time-tracking/
├── models/
│   └── time-entry.model.ts
├── services/
│   └── time-tracking-api.service.ts
├── store/
│   ├── time-tracking.actions.ts
│   ├── time-tracking.reducer.ts
│   ├── time-tracking.effects.ts
│   └── time-tracking.selectors.ts
├── components/
│   ├── time-entry-form/
│   │   ├── time-entry-form.ts
│   │   └── time-entry-form.html
│   └── time-entry-list/
│       ├── time-entry-list.ts
│       └── time-entry-list.html
└── time-tracking.routes.ts
```

**time-entry.model.ts:**
```typescript
export interface TimeEntry {
  id: string;
  resourceId: string;
  projectId: string;
  taskId?: string;
  date: string;          // ISO date string "2026-04-26"
  hours: number;
  entryType: string;     // "Estimated" | "PmAdjusted" | "VendorConfirmed"
  note?: string;
  rateAtTime: number;
  costAtTime: number;
  enteredBy: string;
  createdAt: string;
}
```

**time-tracking.actions.ts:**
```typescript
import { createActionGroup, emptyProps, props } from '@ngrx/store';
import { TimeEntry } from '../models/time-entry.model';

export const TimeTrackingActions = createActionGroup({
  source: 'TimeTracking',
  events: {
    'Load Entries': props<{ projectId?: string; resourceId?: string }>(),
    'Load Entries Success': props<{ entries: TimeEntry[] }>(),
    'Load Entries Failure': props<{ error: string }>(),
    'Create Entry': props<{
      resourceId: string;
      projectId: string;
      taskId?: string;
      date: string;
      hours: number;
      entryType: string;
      role: string;
      level: string;
      note?: string;
    }>(),
    'Create Entry Success': props<{ entry: TimeEntry }>(),
    'Create Entry Failure': props<{ error: string }>(),
  },
});
```

**NgRx Reducer pattern** — dùng `createReducer` + `createEntityAdapter` (giống Workforce):
```typescript
import { createEntityAdapter, EntityState } from '@ngrx/entity';
import { createReducer, on } from '@ngrx/store';

export interface TimeTrackingState extends EntityState<TimeEntry> {
  loading: boolean;
  error: string | null;
}

const adapter = createEntityAdapter<TimeEntry>();
```

**time-entry-form.ts** — Dialog với form fields:
- ResourceId: input (GUID)
- ProjectId: input (GUID)
- Date: MatDatepicker
- Hours: number input
- EntryType: select (Estimated / PmAdjusted)
- Role: select (từ lookups catalog)
- Level: select (từ lookups catalog)
- Note: optional textarea

**app.state.ts** — thêm `timeTracking: TimeTrackingState`
**app.config.ts** — thêm `TimeTrackingEffects`
**app.routes.ts** — thêm `/time-tracking` lazy route

### Patterns đã có — KHÔNG viết lại

| Pattern | Source | Ghi chú |
|---|---|---|
| 4-layer module structure | Workforce module | Domain, Application, Infrastructure, Api |
| `createReducer` (không `createFeature`) | Story 2.1+ | createEntityAdapter + EntityState |
| `[Authorize]` controller | Story 2.1 | Tất cả endpoints cần auth |
| `AsNoTracking()` trong query handler | Story 2.2 | Read-only queries |
| `HasDefaultSchema()` trong DbContext | Workforce | Schema `time_tracking` |
| MediatR registration | WorkforceInfrastructureExtensions | `RegisterServicesFromAssembly(typeof(Handler).Assembly)` |
| `ICurrentUserService` | Shared.Infrastructure | Dùng trong controller để lấy user ID |
| Rate HourlyRate property | MonthlyRate entity | `HourlyRate = MonthlyAmount / 176m` (computed) |

### Lỗi cần tránh

1. **TimeEntry TUYỆT ĐỐI không có UpdatedAt** — entity không extends AuditableEntity
2. **rate_at_time không bao giờ null** — phải có rate hoặc throw DomainException (trừ Inhouse = 0)
3. **DateOnly trong EF Core + Npgsql**: DateOnly được support mặc định, không cần custom conversion
4. **Schema isolation**: TimeTrackingDbContext dùng `modelBuilder.HasDefaultSchema("time_tracking")` — không dùng chung với `workforce` hay `public`
5. **MediatR double registration**: Nếu TimeTracking.Application và Workforce.Application đều đăng ký MediatR, chú ý `AddMediatR` trong TimeTrackingInfrastructureExtensions chỉ scan `TimeTracking.Application` assembly
6. **VendorConfirmed chặn trong API**: `entryType = "VendorConfirmed"` chỉ được tạo qua vendor import (Story 3.5), API create phải từ chối
7. **action prop `type` reserved trong NgRx**: Không dùng prop tên `type` trong createActionGroup props (rename nếu cần)
8. **Host.csproj**: Cần thêm `<ProjectReference>` đến `TimeTracking.Api` — nếu quên sẽ không build được Host

---

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

_(Trống)_

### Completion Notes List

- Tạo TimeTracking module hoàn chỉnh (4 layers: Domain, Application, Infrastructure, Api)
- `TimeEntry` entity KHÔNG extends AuditableEntity — không có UpdatedAt (immutable design)
- `TimeEntryStatus` enum: Estimated, PmAdjusted, VendorConfirmed
- `ITimeTrackingRateService` interface + `TimeTrackingRateService` implementation cross-module: inject IWorkforceDbContext để lookup rate từ MonthlyRate (VendorId + Role + Level + Month)
- Inhouse resources (VendorId = null) → rateAtTime = 0 (không billing qua vendor)
- VendorConfirmed bị chặn tại API — chỉ import pipeline mới set được (Story 3.5)
- EF migration `Init_TimeTracking` tạo thành công với schema `time_tracking`
- Frontend: NgRx store + effects + selector + time-entry-form dialog + time-entry-list component
- `dotnet build` → 0 errors, `ng build` → 0 errors

### File List

**Backend (BE):**
- `src/Modules/TimeTracking/ProjectManagement.TimeTracking.Domain/ProjectManagement.TimeTracking.Domain.csproj` (new)
- `src/Modules/TimeTracking/ProjectManagement.TimeTracking.Application/ProjectManagement.TimeTracking.Application.csproj` (new)
- `src/Modules/TimeTracking/ProjectManagement.TimeTracking.Infrastructure/ProjectManagement.TimeTracking.Infrastructure.csproj` (new)
- `src/Modules/TimeTracking/ProjectManagement.TimeTracking.Api/ProjectManagement.TimeTracking.Api.csproj` (new)
- `src/Modules/TimeTracking/ProjectManagement.TimeTracking.Domain/Enums/TimeEntryStatus.cs` (new)
- `src/Modules/TimeTracking/ProjectManagement.TimeTracking.Domain/Entities/TimeEntry.cs` (new)
- `src/Modules/TimeTracking/ProjectManagement.TimeTracking.Application/Common/Interfaces/ITimeTrackingDbContext.cs` (new)
- `src/Modules/TimeTracking/ProjectManagement.TimeTracking.Application/Common/Interfaces/ITimeTrackingRateService.cs` (new)
- `src/Modules/TimeTracking/ProjectManagement.TimeTracking.Application/DTOs/TimeEntryDto.cs` (new)
- `src/Modules/TimeTracking/ProjectManagement.TimeTracking.Application/TimeEntries/Commands/CreateTimeEntry/CreateTimeEntryCommand.cs` (new)
- `src/Modules/TimeTracking/ProjectManagement.TimeTracking.Application/TimeEntries/Commands/CreateTimeEntry/CreateTimeEntryHandler.cs` (new)
- `src/Modules/TimeTracking/ProjectManagement.TimeTracking.Application/TimeEntries/Queries/GetTimeEntryById/GetTimeEntryByIdQuery.cs` (new)
- `src/Modules/TimeTracking/ProjectManagement.TimeTracking.Application/TimeEntries/Queries/GetTimeEntryById/GetTimeEntryByIdHandler.cs` (new)
- `src/Modules/TimeTracking/ProjectManagement.TimeTracking.Infrastructure/Persistence/Configurations/TimeEntryConfiguration.cs` (new)
- `src/Modules/TimeTracking/ProjectManagement.TimeTracking.Infrastructure/Persistence/TimeTrackingDbContext.cs` (new)
- `src/Modules/TimeTracking/ProjectManagement.TimeTracking.Infrastructure/Services/TimeTrackingRateService.cs` (new)
- `src/Modules/TimeTracking/ProjectManagement.TimeTracking.Infrastructure/Extensions/TimeTrackingInfrastructureExtensions.cs` (new)
- `src/Modules/TimeTracking/ProjectManagement.TimeTracking.Infrastructure/Migrations/[timestamp]_Init_TimeTracking.cs` (new)
- `src/Modules/TimeTracking/ProjectManagement.TimeTracking.Api/Controllers/TimeEntriesController.cs` (new)
- `src/Modules/TimeTracking/ProjectManagement.TimeTracking.Api/Extensions/TimeTrackingModuleExtensions.cs` (new)
- `src/Host/ProjectManagement.Host/ProjectManagement.Host.csproj` (modified)
- `src/Host/ProjectManagement.Host/Program.cs` (modified)
- `ProjectManagement.slnx` (modified)

**Frontend (FE):**
- `frontend/project-management-web/src/app/features/time-tracking/models/time-entry.model.ts` (new)
- `frontend/project-management-web/src/app/features/time-tracking/services/time-tracking-api.service.ts` (new)
- `frontend/project-management-web/src/app/features/time-tracking/store/time-tracking.actions.ts` (new)
- `frontend/project-management-web/src/app/features/time-tracking/store/time-tracking.reducer.ts` (new)
- `frontend/project-management-web/src/app/features/time-tracking/store/time-tracking.effects.ts` (new)
- `frontend/project-management-web/src/app/features/time-tracking/store/time-tracking.selectors.ts` (new)
- `frontend/project-management-web/src/app/features/time-tracking/components/time-entry-form/time-entry-form.ts` (new)
- `frontend/project-management-web/src/app/features/time-tracking/components/time-entry-form/time-entry-form.html` (new)
- `frontend/project-management-web/src/app/features/time-tracking/components/time-entry-list/time-entry-list.ts` (new)
- `frontend/project-management-web/src/app/features/time-tracking/components/time-entry-list/time-entry-list.html` (new)
- `frontend/project-management-web/src/app/features/time-tracking/time-tracking.routes.ts` (new)
- `frontend/project-management-web/src/app/core/store/app.state.ts` (modified)
- `frontend/project-management-web/src/app/app.config.ts` (modified)
- `frontend/project-management-web/src/app/app.routes.ts` (modified)
