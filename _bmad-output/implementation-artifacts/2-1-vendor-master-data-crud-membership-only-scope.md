# Story 2.1: Vendor Master Data CRUD + Membership-Only Scope

Status: review

**Story ID:** 2.1
**Epic:** Epic 2 — Workforce (People/Vendor) + Rate Model + Audit Foundation
**Sprint:** Sprint 3
**Date Created:** 2026-04-26

---

## Story

As a PM,
I want quản lý danh sách vendor (active/inactive) và thông tin cơ bản,
So that tôi có thể quản lý workforce outsource và làm nền cho import/timesheet.

## Acceptance Criteria

1. **Given** user đã đăng nhập
   **When** user tạo/sửa/inactive vendor
   **Then** vendor được lưu với trạng thái rõ ràng (active/inactive) và không xoá dữ liệu lịch sử

2. **Given** user không xác thực (unauthenticated)
   **When** truy cập vendor endpoints
   **Then** trả `401 Unauthorized` (theo `[Authorize]` middleware chuẩn)

3. **Given** update/delete vendor yêu cầu version
   **When** thiếu `If-Match` header
   **Then** trả `412 PreconditionFailed ProblemDetails`
   **When** `If-Match` version mismatch với server
   **Then** trả `409 Conflict ProblemDetails` với `extensions.current` (vendor hiện tại) và `extensions.eTag`

4. **Given** tạo vendor với code đã tồn tại
   **When** POST /api/v1/vendors
   **Then** trả `409 Conflict ProblemDetails`

5. **Given** tìm vendor không tồn tại
   **When** GET /api/v1/vendors/{id}
   **Then** trả `404 Not Found ProblemDetails`

## Tasks / Subtasks

- [x] **Task 1: Domain Entity + Enum (BE)**
  - [x] 1.1 Tạo `Vendor.cs` entity trong `Modules/Workforce/ProjectManagement.Workforce.Domain/Entities/`
  - [x] 1.2 Tạo 4 .csproj files cho Workforce module

- [x] **Task 2: Application Layer (BE)**
  - [x] 2.1 Tạo `IWorkforceDbContext.cs` interface
  - [x] 2.2 Tạo `VendorDto.cs` record
  - [x] 2.3 Tạo `CreateVendorCommand` + Handler
  - [x] 2.4 Tạo `UpdateVendorCommand` + Handler
  - [x] 2.5 Tạo `InactivateVendorCommand` + Handler
  - [x] 2.6 Tạo `GetVendorListQuery` + Handler
  - [x] 2.7 Tạo `GetVendorByIdQuery` + Handler

- [x] **Task 3: Infrastructure Layer (BE)**
  - [x] 3.1 Tạo `WorkforceDbContext.cs` với schema `workforce`
  - [x] 3.2 Tạo `VendorConfiguration.cs` EF fluent config
  - [x] 3.3 Tạo EF migration cho `vendors` table (`Init_Workforce`)
  - [x] 3.4 Tạo `WorkforceInfrastructureExtensions.cs` DI registration

- [x] **Task 4: API Controller (BE)**
  - [x] 4.1 Tạo `VendorsController.cs` tại `/api/v1/vendors`
  - [x] 4.2 Tạo `WorkforceModuleExtensions.cs` để wire vào Host
  - [x] 4.3 Đăng ký module trong `Program.cs` + AutoMigrate block

- [x] **Task 5: Frontend NgRx Store (FE)**
  - [x] 5.1 Tạo `vendor.model.ts`
  - [x] 5.2 Tạo `vendors.actions.ts`
  - [x] 5.3 Tạo `vendors.reducer.ts`
  - [x] 5.4 Tạo `vendors.selectors.ts`
  - [x] 5.5 Tạo `vendors.effects.ts`
  - [x] 5.6 Đăng ký VendorState trong `app.state.ts` + VendorsEffects trong `app.config.ts`

- [x] **Task 6: Frontend Service + Components (FE)**
  - [x] 6.1 Tạo `vendors-api.service.ts`
  - [x] 6.2 Tạo `vendor-list` component (table với Material)
  - [x] 6.3 Tạo `vendor-form` component (MatDialog)
  - [x] 6.4 Tạo `vendors.routes.ts` và đăng ký trong `app.routes.ts`

- [x] **Task 7: Build verification**
  - [x] 7.1 `dotnet build` → 0 errors (5 pre-existing version warnings)
  - [x] 7.2 `ng build` → 0 errors, bundle `vendor-list` chunk xuất hiện

---

## Dev Notes

### Module Structure — Workforce (MỚI, chưa tồn tại)

Architecture không document rõ Workforce module riêng nhưng Epic 2 cần nó. Tạo theo pattern giống Projects module:

```
src/Modules/Workforce/
├── ProjectManagement.Workforce.Domain/
│   └── Entities/
│       └── Vendor.cs
├── ProjectManagement.Workforce.Application/
│   ├── Common/
│   │   └── Interfaces/
│   │       └── IWorkforceDbContext.cs
│   ├── Vendors/
│   │   ├── Commands/
│   │   │   ├── CreateVendor/
│   │   │   ├── UpdateVendor/
│   │   │   └── InactivateVendor/
│   │   └── Queries/
│   │       ├── GetVendorById/
│   │       └── GetVendorList/
│   └── DTOs/
│       └── VendorDto.cs
├── ProjectManagement.Workforce.Infrastructure/
│   ├── Persistence/
│   │   ├── WorkforceDbContext.cs
│   │   ├── Configurations/
│   │   │   └── VendorConfiguration.cs
│   │   └── Migrations/
│   └── Extensions/
│       └── WorkforceInfrastructureExtensions.cs
└── ProjectManagement.Workforce.Api/
    ├── Controllers/
    │   └── VendorsController.cs
    └── Extensions/
        └── WorkforceModuleExtensions.cs
```

---

### Task 1 Detail: Vendor Entity

```csharp
// Modules/Workforce/ProjectManagement.Workforce.Domain/Entities/Vendor.cs
using ProjectManagement.Shared.Domain.Entities;

namespace ProjectManagement.Workforce.Domain.Entities;

public class Vendor : AuditableEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public int Version { get; private set; }

    public static Vendor Create(string code, string name, string? description, string createdBy)
        => new()
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            Description = description,
            IsActive = true,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };

    public void Update(string name, string? description, string updatedBy)
    {
        Name = name;
        Description = description;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
        Version++;
    }

    public void Inactivate(string updatedBy)
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
        Version++;
    }

    public void Reactivate(string updatedBy)
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
        Version++;
    }
}
```

**`IsDeleted` (từ AuditableEntity):** KHÔNG dùng — vendor không bị xoá thật. Dùng `IsActive` để toggle trạng thái.

### .csproj Files

**ProjectManagement.Workforce.Domain.csproj:**
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

**ProjectManagement.Workforce.Application.csproj:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="FluentValidation" Version="11.11.0" />
    <PackageReference Include="MediatR" Version="12.4.1" />
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.4" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\ProjectManagement.Workforce.Domain\ProjectManagement.Workforce.Domain.csproj" />
    <ProjectReference Include="..\..\..\Shared\ProjectManagement.Shared.Domain\ProjectManagement.Shared.Domain.csproj" />
  </ItemGroup>
</Project>
```

**ProjectManagement.Workforce.Infrastructure.csproj:**
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
    <ProjectReference Include="..\ProjectManagement.Workforce.Application\ProjectManagement.Workforce.Application.csproj" />
    <ProjectReference Include="..\..\..\Shared\ProjectManagement.Shared.Infrastructure\ProjectManagement.Shared.Infrastructure.csproj" />
  </ItemGroup>
</Project>
```

**ProjectManagement.Workforce.Api.csproj:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\ProjectManagement.Workforce.Infrastructure\ProjectManagement.Workforce.Infrastructure.csproj" />
    <ProjectReference Include="..\..\..\Shared\ProjectManagement.Shared.Infrastructure\ProjectManagement.Shared.Infrastructure.csproj" />
  </ItemGroup>
</Project>
```

**Thêm vào ProjectManagement.slnx:**
```xml
<Folder Name="/src/Modules/Workforce/">
  <Project Path="src/Modules/Workforce/ProjectManagement.Workforce.Api/ProjectManagement.Workforce.Api.csproj" />
  <Project Path="src/Modules/Workforce/ProjectManagement.Workforce.Application/ProjectManagement.Workforce.Application.csproj" />
  <Project Path="src/Modules/Workforce/ProjectManagement.Workforce.Domain/ProjectManagement.Workforce.Domain.csproj" />
  <Project Path="src/Modules/Workforce/ProjectManagement.Workforce.Infrastructure/ProjectManagement.Workforce.Infrastructure.csproj" />
</Folder>
```

**Host project phải reference Workforce.Api:**
```xml
<!-- ProjectManagement.Host.csproj — thêm: -->
<ProjectReference Include="..\..\Modules\Workforce\ProjectManagement.Workforce.Api\ProjectManagement.Workforce.Api.csproj" />
```

---

### Task 2 Detail: Application Layer

**IWorkforceDbContext:**
```csharp
// Application/Common/Interfaces/IWorkforceDbContext.cs
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Workforce.Domain.Entities;

namespace ProjectManagement.Workforce.Application.Common.Interfaces;

public interface IWorkforceDbContext
{
    DbSet<Vendor> Vendors { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
```

**VendorDto:**
```csharp
// Application/DTOs/VendorDto.cs
namespace ProjectManagement.Workforce.Application.DTOs;

public sealed record VendorDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    bool IsActive,
    int Version,
    DateTime CreatedAt,
    string CreatedBy,
    DateTime? UpdatedAt,
    string? UpdatedBy
);
```

**CreateVendorCommand + Handler:**
```csharp
// Commands/CreateVendor/CreateVendorCommand.cs
public sealed record CreateVendorCommand(
    string Code, string Name, string? Description, string CreatedBy
) : IRequest<VendorDto>;

// Commands/CreateVendor/CreateVendorHandler.cs
public sealed class CreateVendorHandler : IRequestHandler<CreateVendorCommand, VendorDto>
{
    private readonly IWorkforceDbContext _db;
    public CreateVendorHandler(IWorkforceDbContext db) => _db = db;

    public async Task<VendorDto> Handle(CreateVendorCommand cmd, CancellationToken ct)
    {
        var exists = await _db.Vendors.AnyAsync(v => v.Code == cmd.Code, ct);
        if (exists)
            throw new ConflictException($"Vendor với code '{cmd.Code}' đã tồn tại.");

        var vendor = Vendor.Create(cmd.Code, cmd.Name, cmd.Description, cmd.CreatedBy);
        _db.Vendors.Add(vendor);
        await _db.SaveChangesAsync(ct);
        return ToDto(vendor);
    }
}
```

**UpdateVendorCommand + Handler:**
```csharp
// Commands/UpdateVendor/UpdateVendorCommand.cs
public sealed record UpdateVendorCommand(
    Guid VendorId, string Name, string? Description, int ExpectedVersion, string UpdatedBy
) : IRequest<VendorDto>;

// Handler pattern:
// 1. Load vendor hoặc throw NotFoundException("Vendor không tồn tại.")
// 2. Check version: if (vendor.Version != cmd.ExpectedVersion) throw ConflictException(msg, ToDto(vendor), ETagHelper.Generate(vendor.Version))
// 3. vendor.Update(cmd.Name, cmd.Description, cmd.UpdatedBy)
// 4. SaveChangesAsync
// 5. return ToDto(vendor)
```

**InactivateVendorCommand + Handler:**
```csharp
// Commands/InactivateVendor/InactivateVendorCommand.cs
public sealed record InactivateVendorCommand(
    Guid VendorId, int ExpectedVersion, string UpdatedBy
) : IRequest<VendorDto>;

// Handler: load → version check → vendor.Inactivate() → save → return dto
```

**GetVendorListQuery + Handler:**
```csharp
// Queries/GetVendorList/GetVendorListQuery.cs
public sealed record GetVendorListQuery(bool? ActiveOnly = null) : IRequest<List<VendorDto>>;

// Handler: query = _db.Vendors.AsNoTracking()
// if (query.ActiveOnly == true) query = query.Where(v => v.IsActive)
// order by code
// return list of DTOs
```

**GetVendorByIdQuery + Handler:**
```csharp
// Queries/GetVendorById/GetVendorByIdQuery.cs
public sealed record GetVendorByIdQuery(Guid VendorId) : IRequest<VendorDto>;

// Handler: load or throw NotFoundException("Vendor không tồn tại.")
```

**Helper `ToDto` trong handlers:**
```csharp
private static VendorDto ToDto(Vendor v) => new(
    v.Id, v.Code, v.Name, v.Description, v.IsActive, v.Version,
    v.CreatedAt, v.CreatedBy, v.UpdatedAt, v.UpdatedBy);
```

---

### Task 3 Detail: Infrastructure Layer

**WorkforceDbContext:**
```csharp
// Infrastructure/Persistence/WorkforceDbContext.cs
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Workforce.Application.Common.Interfaces;
using ProjectManagement.Workforce.Domain.Entities;
using ProjectManagement.Workforce.Infrastructure.Persistence.Configurations;

namespace ProjectManagement.Workforce.Infrastructure.Persistence;

public sealed class WorkforceDbContext : DbContext, IWorkforceDbContext
{
    public WorkforceDbContext(DbContextOptions<WorkforceDbContext> options) : base(options) { }

    public DbSet<Vendor> Vendors => Set<Vendor>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("workforce");
        modelBuilder.ApplyConfiguration(new VendorConfiguration());
    }
}
```

**VendorConfiguration:**
```csharp
// Infrastructure/Persistence/Configurations/VendorConfiguration.cs
public sealed class VendorConfiguration : IEntityTypeConfiguration<Vendor>
{
    public void Configure(EntityTypeBuilder<Vendor> b)
    {
        b.ToTable("vendors");  // schema đã set ở DbContext level (HasDefaultSchema)

        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        b.Property(x => x.Description).HasColumnName("description").HasMaxLength(500).IsRequired(false);
        b.Property(x => x.IsActive).HasColumnName("is_active");
        b.Property(x => x.Version).HasColumnName("version");
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.CreatedBy).HasColumnName("created_by").HasMaxLength(256);
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        b.Property(x => x.UpdatedBy).HasColumnName("updated_by").HasMaxLength(256);
        b.Property(x => x.IsDeleted).HasColumnName("is_deleted");

        b.HasIndex(x => x.Code).IsUnique().HasDatabaseName("uq_vendors_code");
    }
}
```

**Migration:** Chạy EF migrations từ Host project với `--context WorkforceDbContext`.

**WorkforceInfrastructureExtensions:**
```csharp
// Infrastructure/Extensions/WorkforceInfrastructureExtensions.cs
public static IServiceCollection AddWorkforceInfrastructure(
    this IServiceCollection services, IConfiguration configuration)
{
    var connectionString = configuration.GetConnectionString("Default") ??
        "Host=localhost;Port=5432;Database=project_management;Username=pm_app;Password=pm_app_password";

    services.AddDbContext<WorkforceDbContext>(options =>
        options.UseNpgsql(connectionString));

    services.AddScoped<IWorkforceDbContext>(sp => sp.GetRequiredService<WorkforceDbContext>());

    services.AddMediatR(cfg =>
        cfg.RegisterServicesFromAssembly(typeof(CreateVendorHandler).Assembly));

    return services;
}
```

---

### Task 4 Detail: API Layer

**VendorsController:**
```csharp
[Authorize]
[ApiController]
[Route("api/v1/vendors")]
public sealed class VendorsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    // GET /api/v1/vendors?activeOnly=true
    [HttpGet]
    public async Task<IActionResult> GetVendors([FromQuery] bool? activeOnly, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetVendorListQuery(activeOnly), ct);
        return Ok(result);
    }

    // GET /api/v1/vendors/{vendorId}
    [HttpGet("{vendorId:guid}")]
    public async Task<IActionResult> GetVendor(Guid vendorId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetVendorByIdQuery(vendorId), ct);
        Response.Headers.ETag = ETagHelper.Generate(result.Version);
        return Ok(result);
    }

    // POST /api/v1/vendors  → 201 Created + Location header
    [HttpPost]
    public async Task<IActionResult> CreateVendor([FromBody] CreateVendorRequest body, CancellationToken ct)
    {
        var cmd = new CreateVendorCommand(body.Code, body.Name, body.Description, _currentUser.UserId.ToString());
        var result = await _mediator.Send(cmd, ct);
        Response.Headers.ETag = ETagHelper.Generate(result.Version);
        return CreatedAtAction(nameof(GetVendor), new { vendorId = result.Id }, result);
    }

    // PUT /api/v1/vendors/{vendorId}  — requires If-Match
    [HttpPut("{vendorId:guid}")]
    public async Task<IActionResult> UpdateVendor(Guid vendorId, [FromBody] UpdateVendorRequest body, CancellationToken ct)
    {
        var version = ETagHelper.ParseIfMatch(Request.Headers.IfMatch);
        if (version is null)
            return StatusCode(412, new ProblemDetails { Status = 412, Title = "Precondition Required",
                Detail = "If-Match header là bắt buộc." });

        var cmd = new UpdateVendorCommand(vendorId, body.Name, body.Description, (int)version, _currentUser.UserId.ToString());
        var result = await _mediator.Send(cmd, ct);
        Response.Headers.ETag = ETagHelper.Generate(result.Version);
        return Ok(result);
    }

    // DELETE /api/v1/vendors/{vendorId}  — inactivate, requires If-Match → 204
    [HttpDelete("{vendorId:guid}")]
    public async Task<IActionResult> InactivateVendor(Guid vendorId, CancellationToken ct)
    {
        var version = ETagHelper.ParseIfMatch(Request.Headers.IfMatch);
        if (version is null)
            return StatusCode(412, new ProblemDetails { Status = 412, Title = "Precondition Required",
                Detail = "If-Match header là bắt buộc." });

        var cmd = new InactivateVendorCommand(vendorId, (int)version, _currentUser.UserId.ToString());
        await _mediator.Send(cmd, ct);
        return NoContent();
    }
}

public sealed record CreateVendorRequest(string Code, string Name, string? Description);
public sealed record UpdateVendorRequest(string Name, string? Description);
```

**WorkforceModuleExtensions:**
```csharp
public static IServiceCollection AddWorkforceModule(
    this IServiceCollection services, IConfiguration configuration, IMvcBuilder mvc)
{
    services.AddWorkforceInfrastructure(configuration);
    mvc.AddApplicationPart(typeof(VendorsController).Assembly);
    return services;
}
```

**Program.cs — thêm:**
```csharp
// Trong using block:
using ProjectManagement.Workforce.Api.Extensions;

// Sau AddProjectsModule:
builder.Services.AddWorkforceModule(builder.Configuration, mvc);

// Trong AutoMigrate block (sau ProjectsDb migrate):
var workforceDb = scope.ServiceProvider.GetRequiredService<WorkforceDbContext>();
await workforceDb.Database.MigrateAsync();
```

---

### Task 5 Detail: Frontend NgRx Store

**vendor.model.ts:**
```typescript
// features/vendors/models/vendor.model.ts
export interface Vendor {
  id: string;
  code: string;
  name: string;
  description?: string;
  isActive: boolean;
  version: number;
  createdAt: string;
  createdBy: string;
  updatedAt?: string;
  updatedBy?: string;
}
```

**vendors.actions.ts:**
```typescript
export const VendorsActions = createActionGroup({
  source: 'Vendors',
  events: {
    'Load Vendors': props<{ activeOnly?: boolean }>(),
    'Load Vendors Success': props<{ vendors: Vendor[] }>(),
    'Load Vendors Failure': props<{ error: string }>(),
    'Select Vendor': props<{ vendorId: string | null }>(),
    'Create Vendor': props<{ code: string; name: string; description?: string }>(),
    'Create Vendor Success': props<{ vendor: Vendor }>(),
    'Create Vendor Failure': props<{ error: string }>(),
    'Update Vendor': props<{ vendorId: string; name: string; description?: string; version: number }>(),
    'Update Vendor Success': props<{ vendor: Vendor }>(),
    'Update Vendor Failure': props<{ error: string }>(),
    'Update Vendor Conflict': props<{ serverState: Vendor; eTag: string; pendingName: string; pendingDescription?: string }>(),
    'Inactivate Vendor': props<{ vendorId: string; version: number }>(),
    'Inactivate Vendor Success': props<{ vendor: Vendor }>(),
    'Inactivate Vendor Failure': props<{ error: string }>(),
    'Clear Conflict': emptyProps(),
  },
});
```

**vendors.reducer.ts — dùng `createReducer` (KHÔNG dùng `createFeature`):**
```typescript
export interface VendorConflictState {
  serverState: Vendor;
  eTag: string;
  pendingName: string;
  pendingDescription?: string;
}

export interface VendorsState extends EntityState<Vendor> {
  selectedId: string | null;
  loading: boolean;
  creating: boolean;
  updating: boolean;
  inactivating: boolean;
  error: string | null;
  conflict: VendorConflictState | null;
}

export const vendorsAdapter = createEntityAdapter<Vendor>();
export const initialVendorsState: VendorsState = vendorsAdapter.getInitialState({
  selectedId: null, loading: false, creating: false, updating: false,
  inactivating: false, error: null, conflict: null,
});

export const vendorsReducer = createReducer(
  initialVendorsState,
  on(VendorsActions.loadVendors, state => ({ ...state, loading: true, error: null })),
  on(VendorsActions.loadVendorsSuccess, (state, { vendors }) =>
    vendorsAdapter.setAll(vendors, { ...state, loading: false })),
  on(VendorsActions.loadVendorsFailure, (state, { error }) => ({ ...state, loading: false, error })),
  on(VendorsActions.selectVendor, (state, { vendorId }) => ({ ...state, selectedId: vendorId })),
  on(VendorsActions.createVendor, state => ({ ...state, creating: true, error: null })),
  on(VendorsActions.createVendorSuccess, (state, { vendor }) =>
    vendorsAdapter.addOne(vendor, { ...state, creating: false })),
  on(VendorsActions.createVendorFailure, (state, { error }) => ({ ...state, creating: false, error })),
  on(VendorsActions.updateVendor, state => ({ ...state, updating: true, error: null })),
  on(VendorsActions.updateVendorSuccess, (state, { vendor }) =>
    vendorsAdapter.updateOne({ id: vendor.id, changes: vendor }, { ...state, updating: false })),
  on(VendorsActions.updateVendorFailure, (state, { error }) => ({ ...state, updating: false, error })),
  on(VendorsActions.updateVendorConflict, (state, { serverState, eTag, pendingName, pendingDescription }) => ({
    ...state, updating: false, conflict: { serverState, eTag, pendingName, pendingDescription }
  })),
  on(VendorsActions.inactivateVendor, state => ({ ...state, inactivating: true, error: null })),
  on(VendorsActions.inactivateVendorSuccess, (state, { vendor }) =>
    vendorsAdapter.updateOne({ id: vendor.id, changes: vendor }, { ...state, inactivating: false })),
  on(VendorsActions.inactivateVendorFailure, (state, { error }) => ({ ...state, inactivating: false, error })),
  on(VendorsActions.clearConflict, state => ({ ...state, conflict: null })),
);
```

**vendors.selectors.ts:**
```typescript
const selectVendorsState = (state: AppState) => state.vendors;
const { selectAll, selectEntities } = vendorsAdapter.getSelectors(selectVendorsState);
export const selectAllVendors = selectAll;
export const selectVendorEntities = selectEntities;
export const selectVendorsLoading = createSelector(selectVendorsState, s => s.loading);
export const selectVendorsCreating = createSelector(selectVendorsState, s => s.creating);
export const selectVendorConflict = createSelector(selectVendorsState, s => s.conflict);
export const selectSelectedVendorId = createSelector(selectVendorsState, s => s.selectedId);
export const selectSelectedVendor = createSelector(
  selectVendorEntities, selectSelectedVendorId,
  (entities, id) => (id ? entities[id] : null)
);
```

**vendors.effects.ts — pattern giống projects.effects.ts:**
- `loadVendors$`: gọi `vendorsApiService.getVendors(activeOnly)` → success/failure
- `createVendor$`: gọi `vendorsApiService.createVendor(...)` → success/failure
- `updateVendor$`: gọi `vendorsApiService.updateVendor(...)` → success, 409 → updateVendorConflict, failure
- `inactivateVendor$`: gọi `vendorsApiService.inactivateVendor(...)` → success/failure
- 409 conflict: response body có `extensions.current` (server state) và `extensions.eTag`

**Đăng ký vào AppState (app.state.ts):**
```typescript
import { vendorsReducer, VendorsState } from '../../features/vendors/store/vendors.reducer';
// Thêm vào AppState interface: vendors: VendorsState
// Thêm vào reducers map: vendors: vendorsReducer
```

**Đăng ký VendorsEffects trong app.config.ts:**
```typescript
import { VendorsEffects } from './features/vendors/store/vendors.effects';
// Thêm VendorsEffects vào provideEffects([...])
```

---

### Task 6 Detail: Frontend Components

**vendors-api.service.ts:**
```typescript
@Injectable({ providedIn: 'root' })
export class VendorsApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/v1/vendors';

  getVendors(activeOnly?: boolean): Observable<Vendor[]> {
    const params = activeOnly !== undefined ? { activeOnly } : {};
    return this.http.get<Vendor[]>(this.baseUrl, { params });
  }

  getVendorById(vendorId: string): Observable<Vendor> {
    return this.http.get<Vendor>(`${this.baseUrl}/${vendorId}`);
  }

  createVendor(code: string, name: string, description?: string): Observable<Vendor> {
    return this.http.post<Vendor>(this.baseUrl, { code, name, description });
  }

  updateVendor(vendorId: string, name: string, description: string | undefined, version: number): Observable<Vendor> {
    const headers = new HttpHeaders({ 'If-Match': `"${version}"` });
    return this.http.put<Vendor>(`${this.baseUrl}/${vendorId}`, { name, description }, { headers });
  }

  inactivateVendor(vendorId: string, version: number): Observable<void> {
    const headers = new HttpHeaders({ 'If-Match': `"${version}"` });
    return this.http.delete<void>(`${this.baseUrl}/${vendorId}`, { headers });
  }
}
```

**vendor-list component:**
- Material table (`MatTableModule`) hiển thị: Code, Tên, Trạng thái (chip Active/Inactive), Hành động
- Toolbar: nút "Thêm Vendor" (mở MatDialog VendorFormComponent)
- Mỗi row: nút Edit (mở form), nút Inactivate (confirm dialog)
- `ChangeDetectionStrategy.OnPush`
- Dùng `@if` và `@for` (Angular 17+ control flow)
- `ngOnInit`: dispatch `VendorsActions.loadVendors({})`
- `ngOnDestroy`: không cần clear (vendors là global master data)

**vendor-form component (MatDialog):**
- Reactive form: `code` (required, disabled khi edit), `name` (required), `description` (optional)
- Close → dispatch createVendor hoặc updateVendor
- Nhận `data: Vendor | null` qua `MAT_DIALOG_DATA`

**vendors.routes.ts:**
```typescript
import { Routes } from '@angular/router';

export const vendorsRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./components/vendor-list/vendor-list').then(m => m.VendorListComponent),
  },
];
```

**app.routes.ts — thêm vendors route:**
```typescript
{
  path: 'vendors',
  loadChildren: () =>
    import('./features/vendors/vendors.routes').then(m => m.vendorsRoutes),
},
```

---

### Patterns đã có — KHÔNG viết lại

| Pattern/File | Đã có | Ghi chú |
|---|---|---|
| `AuditableEntity` | `Shared.Domain` | Vendor extends trực tiếp |
| `BaseEntity` | `Shared.Domain` | Via AuditableEntity |
| `ConflictException(msg, currentState, eTag)` | `Shared.Domain` | Đã có 3-arg constructor |
| `NotFoundException(msg)` | `Shared.Domain` | Dùng ngay |
| `GlobalExceptionMiddleware` | `Shared.Infrastructure` | Map exceptions → ProblemDetails |
| `ETagHelper.Generate/ParseIfMatch` | `Shared.Infrastructure` | Dùng ngay trong controller |
| `ICurrentUserService` | `Shared.Infrastructure` | Inject vào controller |
| `[Authorize]` + 401 | ASP.NET middleware | Không cần tự xử lý |
| `createReducer` (không phải createFeature) | Pattern từ Story 1.6 | Dùng để tránh TypeScript errors với EntityState |

---

### Lỗi cần tránh

1. **Host.csproj phải reference Workforce.Api** — nếu thiếu thì `AddApplicationPart` không hoạt động và controller không được đăng ký
2. **Program.cs AutoMigrate** — phải thêm `workforceDb.Database.MigrateAsync()` để tạo `workforce` schema + `vendors` table khi khởi động
3. **VendorConflict 409** — `UpdateVendorHandler` phải throw `ConflictException(msg, ToDto(vendor), ETagHelper.Generate(vendor.Version))` — không phải throw generic exception
4. **`IsActive` khác `IsDeleted`** — Vendor dùng `IsActive` cho trạng thái business, `IsDeleted` (từ AuditableEntity) KHÔNG được set
5. **createReducer không createFeature** — `createFeature` gây TypeScript error khi state extends `EntityState`
6. **Angular 17+ control flow** — Dùng `@if`, `@for` trong templates, không dùng `*ngIf`, `*ngFor`
7. **EF migration với nhiều DbContext** — chạy `dotnet ef migrations add Init_Workforce --context WorkforceDbContext --project ... --startup-project ...`

---

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

- Application handlers dùng `ETagHelper` từ Shared.Infrastructure — vi phạm layer boundary. Fix: inline ETag string `$"\"{version}\""` trong handlers.
- Host project thiếu `Microsoft.EntityFrameworkCore.Design` để chạy EF migrations. Fix: thêm package vào Host.csproj.
- EF migration tạo tại `Migrations/` (không phải `Persistence/Migrations/`) — đây là mặc định của EF CLI.

### Completion Notes List

- Tạo mới module `Workforce` với 4 layers: Domain, Application, Infrastructure, Api. Theo đúng pattern của Projects module.
- `Vendor` entity dùng `IsActive` (không phải `IsDeleted`) cho trạng thái active/inactive.
- CQRS: 3 Commands (Create, Update, Inactivate) + 2 Queries (GetById, GetList với optional activeOnly filter).
- 409 Conflict: `ConflictException` với `currentState` + inline ETag string — GlobalExceptionMiddleware map sang `extensions.current` và `extensions.eTag`.
- EF migration `Init_Workforce` tạo schema `workforce` + table `vendors` với unique index `uq_vendors_code`.
- Frontend: NgRx store đầy đủ (actions, reducer, selectors, effects), VendorListComponent với Material table, VendorFormComponent với MatDialog.
- `ng build` tạo lazy chunk `vendor-list` — routing lazy load hoạt động đúng.
- `dotnet build` 0 errors (5 pre-existing version warnings từ EF packages — không liên quan story này).

### File List

**Backend:**
- `src/Modules/Workforce/ProjectManagement.Workforce.Domain/Entities/Vendor.cs`
- `src/Modules/Workforce/ProjectManagement.Workforce.Domain/ProjectManagement.Workforce.Domain.csproj`
- `src/Modules/Workforce/ProjectManagement.Workforce.Application/ProjectManagement.Workforce.Application.csproj`
- `src/Modules/Workforce/ProjectManagement.Workforce.Application/Common/Interfaces/IWorkforceDbContext.cs`
- `src/Modules/Workforce/ProjectManagement.Workforce.Application/DTOs/VendorDto.cs`
- `src/Modules/Workforce/ProjectManagement.Workforce.Application/Vendors/Commands/CreateVendor/CreateVendorCommand.cs`
- `src/Modules/Workforce/ProjectManagement.Workforce.Application/Vendors/Commands/CreateVendor/CreateVendorHandler.cs`
- `src/Modules/Workforce/ProjectManagement.Workforce.Application/Vendors/Commands/UpdateVendor/UpdateVendorCommand.cs`
- `src/Modules/Workforce/ProjectManagement.Workforce.Application/Vendors/Commands/UpdateVendor/UpdateVendorHandler.cs`
- `src/Modules/Workforce/ProjectManagement.Workforce.Application/Vendors/Commands/InactivateVendor/InactivateVendorCommand.cs`
- `src/Modules/Workforce/ProjectManagement.Workforce.Application/Vendors/Commands/InactivateVendor/InactivateVendorHandler.cs`
- `src/Modules/Workforce/ProjectManagement.Workforce.Application/Vendors/Queries/GetVendorById/GetVendorByIdQuery.cs`
- `src/Modules/Workforce/ProjectManagement.Workforce.Application/Vendors/Queries/GetVendorById/GetVendorByIdHandler.cs`
- `src/Modules/Workforce/ProjectManagement.Workforce.Application/Vendors/Queries/GetVendorList/GetVendorListQuery.cs`
- `src/Modules/Workforce/ProjectManagement.Workforce.Application/Vendors/Queries/GetVendorList/GetVendorListHandler.cs`
- `src/Modules/Workforce/ProjectManagement.Workforce.Infrastructure/ProjectManagement.Workforce.Infrastructure.csproj`
- `src/Modules/Workforce/ProjectManagement.Workforce.Infrastructure/Persistence/WorkforceDbContext.cs`
- `src/Modules/Workforce/ProjectManagement.Workforce.Infrastructure/Persistence/Configurations/VendorConfiguration.cs`
- `src/Modules/Workforce/ProjectManagement.Workforce.Infrastructure/Migrations/20260426035456_Init_Workforce.cs` (generated)
- `src/Modules/Workforce/ProjectManagement.Workforce.Infrastructure/Migrations/20260426035456_Init_Workforce.Designer.cs` (generated)
- `src/Modules/Workforce/ProjectManagement.Workforce.Infrastructure/Migrations/WorkforceDbContextModelSnapshot.cs` (generated)
- `src/Modules/Workforce/ProjectManagement.Workforce.Infrastructure/Extensions/WorkforceInfrastructureExtensions.cs`
- `src/Modules/Workforce/ProjectManagement.Workforce.Api/ProjectManagement.Workforce.Api.csproj`
- `src/Modules/Workforce/ProjectManagement.Workforce.Api/Controllers/VendorsController.cs`
- `src/Modules/Workforce/ProjectManagement.Workforce.Api/Extensions/WorkforceModuleExtensions.cs`
- `src/Host/ProjectManagement.Host/ProjectManagement.Host.csproj` (thêm Workforce.Api reference + EF Design)
- `src/Host/ProjectManagement.Host/Program.cs` (thêm AddWorkforceModule + workforceDb.MigrateAsync)
- `ProjectManagement.slnx` (thêm Workforce folder)

**Frontend:**
- `frontend/project-management-web/src/app/features/vendors/models/vendor.model.ts`
- `frontend/project-management-web/src/app/features/vendors/store/vendors.actions.ts`
- `frontend/project-management-web/src/app/features/vendors/store/vendors.reducer.ts`
- `frontend/project-management-web/src/app/features/vendors/store/vendors.selectors.ts`
- `frontend/project-management-web/src/app/features/vendors/store/vendors.effects.ts`
- `frontend/project-management-web/src/app/features/vendors/services/vendors-api.service.ts`
- `frontend/project-management-web/src/app/features/vendors/components/vendor-list/vendor-list.ts`
- `frontend/project-management-web/src/app/features/vendors/components/vendor-list/vendor-list.html`
- `frontend/project-management-web/src/app/features/vendors/components/vendor-list/vendor-list.scss`
- `frontend/project-management-web/src/app/features/vendors/components/vendor-form/vendor-form.ts`
- `frontend/project-management-web/src/app/features/vendors/components/vendor-form/vendor-form.html`
- `frontend/project-management-web/src/app/features/vendors/vendors.routes.ts`
- `frontend/project-management-web/src/app/core/store/app.state.ts` (thêm vendors state)
- `frontend/project-management-web/src/app/app.config.ts` (thêm VendorsEffects)
- `frontend/project-management-web/src/app/app.routes.ts` (thêm /vendors route)
