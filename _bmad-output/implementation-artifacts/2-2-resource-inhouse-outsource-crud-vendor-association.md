# Story 2.2: Resource (Inhouse/Outsource) CRUD + Vendor Association

Status: review

**Story ID:** 2.2
**Epic:** Epic 2 — Workforce (People/Vendor) + Rate Model + Audit Foundation
**Sprint:** Sprint 3
**Date Created:** 2026-04-26

---

## Story

As a PM,
I want quản lý danh sách nhân sự (inhouse/outsource) và gắn vendor nếu outsource,
So that tôi có thể phân bổ người và tính cost đúng theo nguồn lực.

## Acceptance Criteria

1. **Given** resource có loại Outsource
   **When** tạo resource outsource
   **Then** bắt buộc có `vendorId` hợp lệ (vendor tồn tại và active); thiếu → `400 ValidationError`

2. **Given** resource có loại Inhouse
   **When** tạo resource inhouse
   **Then** `vendorId` phải null; nếu truyền → `400 ValidationError`

3. **Given** code resource phải unique
   **When** tạo resource với code đã tồn tại
   **Then** trả `409 Conflict ProblemDetails`

4. **Given** resource bị inactivate
   **When** truy vấn `GET /api/v1/resources/{id}`
   **Then** resource vẫn trả về (isActive=false) — data không bị xoá

5. **Given** update/inactivate resource yêu cầu version
   **When** thiếu `If-Match`
   **Then** trả `412 PreconditionFailed`
   **When** version mismatch
   **Then** trả `409 Conflict` với `extensions.current`

6. **Given** tìm resource không tồn tại
   **When** GET /api/v1/resources/{id}
   **Then** trả `404 Not Found ProblemDetails`

## Tasks / Subtasks

- [x] **Task 1: Domain Entity + Enum (BE)**
  - [x] 1.1 Tạo `ResourceType.cs` enum trong `Workforce.Domain/Enums/`
  - [x] 1.2 Tạo `Resource.cs` entity trong `Workforce.Domain/Entities/`

- [x] **Task 2: Application Layer (BE)**
  - [x] 2.1 Cập nhật `IWorkforceDbContext` — thêm `DbSet<Resource> Resources`
  - [x] 2.2 Tạo `ResourceDto.cs` record
  - [x] 2.3 Tạo `CreateResourceCommand` + Handler (validate vendorId theo type)
  - [x] 2.4 Tạo `UpdateResourceCommand` + Handler (optimistic lock)
  - [x] 2.5 Tạo `InactivateResourceCommand` + Handler (optimistic lock)
  - [x] 2.6 Tạo `GetResourceListQuery` + Handler (filter by type, vendorId, isActive)
  - [x] 2.7 Tạo `GetResourceByIdQuery` + Handler

- [x] **Task 3: Infrastructure Layer (BE)**
  - [x] 3.1 Tạo `ResourceConfiguration.cs` EF config (FK → vendors)
  - [x] 3.2 Cập nhật `WorkforceDbContext` — thêm `Resources` DbSet + ApplyConfiguration
  - [x] 3.3 Tạo EF migration `AddResource_Workforce`

- [x] **Task 4: API Controller (BE)**
  - [x] 4.1 Tạo `ResourcesController.cs` tại `/api/v1/resources`

- [x] **Task 5: Frontend NgRx Store (FE)**
  - [x] 5.1 Tạo `resource.model.ts`
  - [x] 5.2 Tạo `resources.actions.ts`
  - [x] 5.3 Tạo `resources.reducer.ts`
  - [x] 5.4 Tạo `resources.selectors.ts`
  - [x] 5.5 Tạo `resources.effects.ts`
  - [x] 5.6 Đăng ký ResourcesState trong `app.state.ts` + ResourcesEffects trong `app.config.ts`

- [x] **Task 6: Frontend Service + Components (FE)**
  - [x] 6.1 Tạo `resources-api.service.ts`
  - [x] 6.2 Tạo `resource-list` component
  - [x] 6.3 Tạo `resource-form` component (MatDialog, vendor select khi Outsource)
  - [x] 6.4 Tạo `resources.routes.ts` + đăng ký trong `app.routes.ts`

- [x] **Task 7: Build verification**
  - [x] 7.1 `dotnet build` → 0 errors (6 warnings pre-existing EF version conflicts)
  - [x] 7.2 `ng build` → 0 errors

---

## Dev Notes

### Workforce Module đã có (Story 2.1) — KHÔNG tạo lại

| Đã có | Ghi chú |
|---|---|
| `Vendor.cs` domain entity | Đã seed, có IsActive, Version |
| `WorkforceDbContext` | Cần thêm `Resources` DbSet |
| `IWorkforceDbContext` | Cần thêm `DbSet<Resource>` |
| `WorkforceInfrastructureExtensions` | Đã đăng ký, không cần sửa |
| `WorkforceModuleExtensions` | Đã đăng ký, cần thêm ResourcesController assembly |
| `VendorsController` | Pattern mẫu cho ResourcesController |
| `VendorDto.cs` + `CreateVendorHandler.ToDto()` pattern | Áp dụng cho ResourceDto |

### Task 1 Detail: Resource Entity + Enum

```csharp
// Workforce.Domain/Enums/ResourceType.cs
namespace ProjectManagement.Workforce.Domain.Enums;

public enum ResourceType
{
    Inhouse,
    Outsource
}
```

```csharp
// Workforce.Domain/Entities/Resource.cs
using ProjectManagement.Shared.Domain.Entities;
using ProjectManagement.Workforce.Domain.Enums;

namespace ProjectManagement.Workforce.Domain.Entities;

public class Resource : AuditableEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public ResourceType Type { get; private set; }
    public Guid? VendorId { get; private set; }
    public bool IsActive { get; private set; }
    public int Version { get; private set; }

    public Vendor? Vendor { get; private set; }

    public static Resource Create(
        string code, string name, string? email,
        ResourceType type, Guid? vendorId, string createdBy)
        => new()
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            Email = email,
            Type = type,
            VendorId = vendorId,
            IsActive = true,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };

    public void Update(string name, string? email, string updatedBy)
    {
        Name = name;
        Email = email;
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

**Lưu ý:** `Type` và `VendorId` KHÔNG thể thay đổi sau khi tạo — phản ánh identity của nhân sự.

### Task 2 Detail: Application Layer

**IWorkforceDbContext — thêm Resources:**
```csharp
DbSet<Resource> Resources { get; }
```

**ResourceDto:**
```csharp
public sealed record ResourceDto(
    Guid Id,
    string Code,
    string Name,
    string? Email,
    string Type,          // "Inhouse" | "Outsource"
    Guid? VendorId,
    string? VendorName,   // từ navigation property
    bool IsActive,
    int Version,
    DateTime CreatedAt,
    string CreatedBy,
    DateTime? UpdatedAt,
    string? UpdatedBy
);
```

**CreateResourceCommand validation logic:**
```csharp
// 1. Code uniqueness → 409
// 2. Type = Outsource → VendorId phải có giá trị → else 400
// 3. Type = Inhouse → VendorId phải null → else 400
// 4. Nếu VendorId có giá trị → verify vendor tồn tại và IsActive → else 400 DomainException
// 5. Tạo Resource.Create(...) và save
```

**CreateResourceCommand:**
```csharp
public sealed record CreateResourceCommand(
    string Code,
    string Name,
    string? Email,
    string Type,       // "Inhouse" | "Outsource"
    Guid? VendorId,
    string CreatedBy
) : IRequest<ResourceDto>;
```

**UpdateResourceCommand (chỉ Name + Email, không thay đổi Type/VendorId):**
```csharp
public sealed record UpdateResourceCommand(
    Guid ResourceId,
    string Name,
    string? Email,
    int ExpectedVersion,
    string UpdatedBy
) : IRequest<ResourceDto>;
```

**InactivateResourceCommand:**
```csharp
public sealed record InactivateResourceCommand(
    Guid ResourceId,
    int ExpectedVersion,
    string UpdatedBy
) : IRequest<ResourceDto>;
```

**GetResourceListQuery:**
```csharp
public sealed record GetResourceListQuery(
    string? Type = null,       // filter by "Inhouse"/"Outsource"
    Guid? VendorId = null,     // filter by vendor
    bool? ActiveOnly = null
) : IRequest<List<ResourceDto>>;
```

**Handler — Include Vendor để lấy VendorName:**
```csharp
var q = _db.Resources.AsNoTracking().Include(r => r.Vendor);
// Apply filters...
// ToDto: new ResourceDto(..., r.Vendor?.Name, ...)
```

**ToDto helper:**
```csharp
internal static ResourceDto ToDto(Resource r) => new(
    r.Id, r.Code, r.Name, r.Email, r.Type.ToString(),
    r.VendorId, r.Vendor?.Name, r.IsActive, r.Version,
    r.CreatedAt, r.CreatedBy, r.UpdatedAt, r.UpdatedBy);
```

**Validate type conversion:**
```csharp
if (!Enum.TryParse<ResourceType>(cmd.Type, out var resourceType))
    throw new DomainException($"Loại resource không hợp lệ: '{cmd.Type}'. Chỉ chấp nhận 'Inhouse' hoặc 'Outsource'.");
```

### Task 3 Detail: Infrastructure

**ResourceConfiguration:**
```csharp
b.ToTable("resources");  // trong workforce schema (HasDefaultSchema ở DbContext)
b.HasKey(x => x.Id);
b.Property(x => x.Id).HasColumnName("id");
b.Property(x => x.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
b.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
b.Property(x => x.Email).HasColumnName("email").HasMaxLength(256).IsRequired(false);
b.Property(x => x.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(20);
b.Property(x => x.VendorId).HasColumnName("vendor_id").IsRequired(false);
b.Property(x => x.IsActive).HasColumnName("is_active");
b.Property(x => x.Version).HasColumnName("version");
b.Property(x => x.CreatedAt).HasColumnName("created_at");
b.Property(x => x.CreatedBy).HasColumnName("created_by").HasMaxLength(256);
b.Property(x => x.UpdatedAt).HasColumnName("updated_at");
b.Property(x => x.UpdatedBy).HasColumnName("updated_by").HasMaxLength(256);
b.Property(x => x.IsDeleted).HasColumnName("is_deleted");

b.HasIndex(x => x.Code).IsUnique().HasDatabaseName("uq_resources_code");

b.HasOne(x => x.Vendor)
 .WithMany()
 .HasForeignKey(x => x.VendorId)
 .IsRequired(false)
 .OnDelete(DeleteBehavior.Restrict);
```

**WorkforceDbContext — thêm:**
```csharp
public DbSet<Resource> Resources => Set<Resource>();
// + modelBuilder.ApplyConfiguration(new ResourceConfiguration());
```

**Migration:** `dotnet ef migrations add AddResource_Workforce --context WorkforceDbContext --project ... --startup-project ...`

### Task 4 Detail: API Controller

```csharp
[Authorize]
[ApiController]
[Route("api/v1/resources")]
public sealed class ResourcesController : ControllerBase
{
    // GET /api/v1/resources?type=Outsource&vendorId=...&activeOnly=true
    // GET /api/v1/resources/{resourceId}  → 200 + ETag
    // POST /api/v1/resources              → 201 + Location + ETag
    // PUT /api/v1/resources/{resourceId}  → 200 + ETag (If-Match required)
    // DELETE /api/v1/resources/{resourceId} → 204 (If-Match required, inactivate)
}

public sealed record CreateResourceRequest(
    string Code, string Name, string? Email, string Type, Guid? VendorId);
public sealed record UpdateResourceRequest(string Name, string? Email);
```

**Lưu ý quan trọng:**
- `ResourcesController` nằm trong assembly `Workforce.Api` — `WorkforceModuleExtensions.AddApplicationPart` đã dùng `typeof(VendorsController).Assembly`. Vì cả hai cùng assembly, KHÔNG cần gọi `AddApplicationPart` lần nữa. Controller sẽ tự động được register.

### Task 5-6 Detail: Frontend

**resource.model.ts:**
```typescript
export interface Resource {
  id: string;
  code: string;
  name: string;
  email?: string;
  type: 'Inhouse' | 'Outsource';
  vendorId?: string;
  vendorName?: string;
  isActive: boolean;
  version: number;
  createdAt: string;
  createdBy: string;
  updatedAt?: string;
  updatedBy?: string;
}
```

**resources-api.service.ts:**
```typescript
@Injectable({ providedIn: 'root' })
export class ResourcesApiService {
  private readonly baseUrl = '/api/v1/resources';

  getResources(type?: string, vendorId?: string, activeOnly?: boolean): Observable<Resource[]>
  getResourceById(resourceId: string): Observable<Resource>
  createResource(code: string, name: string, email: string|undefined, type: string, vendorId?: string): Observable<Resource>
  updateResource(resourceId: string, name: string, email: string|undefined, version: number): Observable<Resource>
  inactivateResource(resourceId: string, version: number): Observable<void>
}
```

**resource-form component:**
- Dropdown `type`: Inhouse / Outsource
- Khi chọn Outsource: hiện field `vendor` (MatSelect, load từ selectAllVendors)
- Khi chọn Inhouse: ẩn vendor field, reset vendorId
- Dùng `@if` và `@for` cho conditional rendering

**NgRx store pattern:** Giống hệt vendors store. Dùng `createReducer` (không `createFeature`).

**app.state.ts — thêm:**
```typescript
import { resourcesReducer, ResourcesState } from '../../features/resources/store/resources.reducer';
// AppState: resources: ResourcesState
// reducers: resources: resourcesReducer
```

**app.config.ts — thêm:**
```typescript
import { ResourcesEffects } from './features/resources/store/resources.effects';
// provideEffects([..., ResourcesEffects])
```

**app.routes.ts — thêm:**
```typescript
{
  path: 'resources',
  loadChildren: () => import('./features/resources/resources.routes').then(m => m.resourcesRoutes),
},
```

### Patterns đã có — KHÔNG viết lại

| Pattern | Source | Ghi chú |
|---|---|---|
| `Vendor.cs` entity pattern | Story 2.1 | Áp dụng y chang cho Resource |
| `createReducer` (không createFeature) | Story 1.6, 2.1 | Tránh TypeScript error với EntityState |
| `ETagHelper.Generate/ParseIfMatch` | `Shared.Infrastructure` | Dùng trong controller |
| `ConflictException(msg, currentState, eTagStr)` | `Shared.Domain` | Handler: `$"\"{r.Version}\""` |
| `NotFoundException` / `DomainException` | `Shared.Domain` | 404 / 422 |
| `[Authorize]` | ASP.NET | 401 cho unauthenticated |
| Angular 17+ `@if`, `@for` | Story 1.5+ | Không dùng `*ngIf`, `*ngFor` |
| `ChangeDetectionStrategy.OnPush` | Pattern | Tất cả components |

### Lỗi cần tránh

1. **VendorId validation trong handler, không FluentValidation** — logic phụ thuộc DB (load vendor) phải trong handler, không validator
2. **`ResourcesController` không cần AddApplicationPart riêng** — cùng assembly với `VendorsController`, đã được register
3. **EF navigation Include khi query** — `GetResourceListHandler` phải `.Include(r => r.Vendor)` để lấy `VendorName`
4. **Type enum conversion** — EF config `HasConversion<string>()` để lưu "Inhouse"/"Outsource" dạng string trong DB
5. **Không thay đổi Type/VendorId sau khi tạo** — UpdateResourceCommand chỉ cho phép sửa Name + Email

---

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

_(Trống)_

### Completion Notes List

- `type` is a reserved NgRx action property — renamed to `resourceType` in action props and all call sites
- `ResourcesController` is in the same assembly as `VendorsController` — no additional `AddApplicationPart` call needed
- EF migration `AddResource_Workforce` generated with FK `vendor_id → workforce.vendors(id)` + `uq_resources_code` unique index
- Pre-existing EF version conflict warnings (10.0.4 vs 10.0.7) in Host build — not caused by this story, 0 errors

### File List

**Backend:**
- `src/Modules/Workforce/ProjectManagement.Workforce.Domain/Enums/ResourceType.cs`
- `src/Modules/Workforce/ProjectManagement.Workforce.Domain/Entities/Resource.cs`
- `src/Modules/Workforce/ProjectManagement.Workforce.Application/Common/Interfaces/IWorkforceDbContext.cs` _(updated)_
- `src/Modules/Workforce/ProjectManagement.Workforce.Application/DTOs/ResourceDto.cs`
- `src/Modules/Workforce/ProjectManagement.Workforce.Application/Resources/Commands/CreateResource/CreateResourceCommand.cs`
- `src/Modules/Workforce/ProjectManagement.Workforce.Application/Resources/Commands/CreateResource/CreateResourceHandler.cs`
- `src/Modules/Workforce/ProjectManagement.Workforce.Application/Resources/Commands/UpdateResource/UpdateResourceCommand.cs`
- `src/Modules/Workforce/ProjectManagement.Workforce.Application/Resources/Commands/UpdateResource/UpdateResourceHandler.cs`
- `src/Modules/Workforce/ProjectManagement.Workforce.Application/Resources/Commands/InactivateResource/InactivateResourceCommand.cs`
- `src/Modules/Workforce/ProjectManagement.Workforce.Application/Resources/Commands/InactivateResource/InactivateResourceHandler.cs`
- `src/Modules/Workforce/ProjectManagement.Workforce.Application/Resources/Queries/GetResourceList/GetResourceListQuery.cs`
- `src/Modules/Workforce/ProjectManagement.Workforce.Application/Resources/Queries/GetResourceList/GetResourceListHandler.cs`
- `src/Modules/Workforce/ProjectManagement.Workforce.Application/Resources/Queries/GetResourceById/GetResourceByIdQuery.cs`
- `src/Modules/Workforce/ProjectManagement.Workforce.Application/Resources/Queries/GetResourceById/GetResourceByIdHandler.cs`
- `src/Modules/Workforce/ProjectManagement.Workforce.Infrastructure/Persistence/Configurations/ResourceConfiguration.cs`
- `src/Modules/Workforce/ProjectManagement.Workforce.Infrastructure/Persistence/WorkforceDbContext.cs` _(updated)_
- `src/Modules/Workforce/ProjectManagement.Workforce.Infrastructure/Migrations/<timestamp>_AddResource_Workforce.cs`
- `src/Modules/Workforce/ProjectManagement.Workforce.Api/Controllers/ResourcesController.cs`

**Frontend:**
- `frontend/project-management-web/src/app/features/resources/models/resource.model.ts`
- `frontend/project-management-web/src/app/features/resources/store/resources.actions.ts`
- `frontend/project-management-web/src/app/features/resources/store/resources.reducer.ts`
- `frontend/project-management-web/src/app/features/resources/store/resources.selectors.ts`
- `frontend/project-management-web/src/app/features/resources/store/resources.effects.ts`
- `frontend/project-management-web/src/app/features/resources/services/resources-api.service.ts`
- `frontend/project-management-web/src/app/features/resources/components/resource-list/resource-list.ts`
- `frontend/project-management-web/src/app/features/resources/components/resource-list/resource-list.html`
- `frontend/project-management-web/src/app/features/resources/components/resource-list/resource-list.scss`
- `frontend/project-management-web/src/app/features/resources/components/resource-form/resource-form.ts`
- `frontend/project-management-web/src/app/features/resources/components/resource-form/resource-form.html`
- `frontend/project-management-web/src/app/features/resources/resources.routes.ts`
- `frontend/project-management-web/src/app/core/store/app.state.ts` _(updated)_
- `frontend/project-management-web/src/app/app.config.ts` _(updated)_
- `frontend/project-management-web/src/app/app.routes.ts` _(updated)_
