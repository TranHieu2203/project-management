# Story 2.5: Audit Events for Master Data Changes (Append-Only)

Status: review

**Story ID:** 2.5
**Epic:** Epic 2 — Workforce (People/Vendor) + Rate Model + Audit Foundation
**Sprint:** Sprint 3
**Date Created:** 2026-04-26

---

## Story

As a PM,
I want mọi thay đổi vendor/resource/rate đều có audit trail append-only,
So that có thể đối soát "ai đổi gì, khi nào" như yêu cầu thay Excel.

## Acceptance Criteria

1. **Given** create/update/inactivate vendor/resource
   **When** mutation thành công
   **Then** hệ thống ghi audit event gồm: `actor`, `timestamp`, `entityType`, `entityId`, `action`, `summary`

2. **Given** create/delete rate
   **When** mutation thành công
   **Then** hệ thống ghi audit event tương tự

3. **Given** audit events đã được ghi
   **When** gọi `GET /api/v1/audit?entityType=Vendor&entityId=...`
   **Then** trả danh sách audit events, sắp xếp mới nhất trước

4. **Given** audit event không thể bị sửa/xoá
   **When** không có DELETE/PUT endpoint cho `/api/v1/audit`
   **Then** API chỉ đọc — không có mutation endpoint

## Tasks / Subtasks

- [x] **Task 1: Domain Entity (BE)**
  - [x] 1.1 Tạo `AuditEvent.cs` trong `Workforce.Domain/Entities/`

- [x] **Task 2: Application Layer (BE)**
  - [x] 2.1 Cập nhật `IWorkforceDbContext` — thêm `DbSet<AuditEvent> AuditEvents`
  - [x] 2.2 Tạo `WorkforceMutatedNotification` (MediatR `INotification`)
  - [x] 2.3 Tạo `AuditEventDto.cs` record
  - [x] 2.4 Tạo `GetAuditListQuery` + Handler

- [x] **Task 3: Infrastructure Layer (BE)**
  - [x] 3.1 Tạo `AuditEventConfiguration.cs` EF config
  - [x] 3.2 Tạo `WorkforceMutatedEventHandler` (`INotificationHandler<WorkforceMutatedNotification>`)
  - [x] 3.3 Cập nhật `WorkforceDbContext` — thêm AuditEvents DbSet + ApplyConfiguration
  - [x] 3.4 Tạo EF migration `AddAuditEvent_Workforce`

- [x] **Task 4: Publish notification từ existing handlers (BE)**
  - [x] 4.1 Inject `IMediator` vào `CreateVendorHandler` + publish sau SaveChanges
  - [x] 4.2 Inject `IMediator` vào `UpdateVendorHandler` + publish
  - [x] 4.3 Inject `IMediator` vào `InactivateVendorHandler` + publish
  - [x] 4.4 Inject `IMediator` vào `CreateResourceHandler` + publish
  - [x] 4.5 Inject `IMediator` vào `UpdateResourceHandler` + publish
  - [x] 4.6 Inject `IMediator` vào `InactivateResourceHandler` + publish
  - [x] 4.7 Inject `IMediator` vào `CreateRateHandler` + publish
  - [x] 4.8 Inject `IMediator` vào `DeleteRateHandler` + publish (thêm `DeletedBy` vào DeleteRateCommand)

- [x] **Task 5: API Controller (BE)**
  - [x] 5.1 Tạo `AuditController.cs` tại `/api/v1/audit` (read-only, GET only)

- [x] **Task 6: Frontend (FE)**
  - [x] 6.1 Tạo `audit-event.model.ts`
  - [x] 6.2 Tạo `audit-api.service.ts` (getAuditEvents)
  - [x] 6.3 Tạo `audit-log` component (simple list, no NgRx — dùng service trực tiếp)
  - [x] 6.4 Tạo `audit.routes.ts` + đăng ký trong `app.routes.ts`

- [x] **Task 7: Build verification**
  - [x] 7.1 `dotnet build` → 0 errors
  - [x] 7.2 `ng build` → 0 errors

---

## Dev Notes

### Workforce Module đã có — KHÔNG tạo lại

| Đã có | Ghi chú |
|---|---|
| `CreateVendorHandler`, `UpdateVendorHandler`, `InactivateVendorHandler` | Task 4: thêm IMediator + Publish |
| `CreateResourceHandler`, `UpdateResourceHandler`, `InactivateResourceHandler` | Task 4: thêm IMediator + Publish |
| `CreateRateHandler`, `DeleteRateHandler` | Task 4: thêm IMediator + Publish |
| `WorkforceDbContext`, `IWorkforceDbContext` | Cần thêm AuditEvents DbSet |
| `WorkforceInfrastructureExtensions` | Đã đăng ký MediatR, handler tự được resolve |

### Task 1 Detail: AuditEvent Entity

```csharp
// Workforce.Domain/Entities/AuditEvent.cs
namespace ProjectManagement.Workforce.Domain.Entities;

public class AuditEvent
{
    public Guid Id { get; private set; }
    public string EntityType { get; private set; } = string.Empty;  // "Vendor", "Resource", "Rate"
    public Guid EntityId { get; private set; }
    public string Action { get; private set; } = string.Empty;     // "Create", "Update", "Inactivate", "Delete"
    public string Actor { get; private set; } = string.Empty;       // UserId
    public string Summary { get; private set; } = string.Empty;     // "Created vendor 'ABC Corp'"
    public DateTime CreatedAt { get; private set; }

    public static AuditEvent Create(
        string entityType, Guid entityId, string action, string actor, string summary)
        => new()
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            Actor = actor,
            Summary = summary,
            CreatedAt = DateTime.UtcNow,
        };
}
```

**Lưu ý thiết kế:**
- `AuditEvent` KHÔNG kế thừa `AuditableEntity` hay `BaseEntity` — đây là immutable log, không cần UpdatedAt/IsDeleted/CreatedBy riêng
- Không có `Version` — audit events không bao giờ bị update
- `CreatedAt` là timestamp thực sự của event, không phải của entity

### Task 2 Detail: Application Layer

**WorkforceMutatedNotification:**
```csharp
// Workforce.Application/Notifications/WorkforceMutatedNotification.cs
using MediatR;

public sealed record WorkforceMutatedNotification(
    string EntityType,
    Guid EntityId,
    string Action,
    string Actor,
    string Summary
) : INotification;
```

**AuditEventDto:**
```csharp
public sealed record AuditEventDto(
    Guid Id,
    string EntityType,
    Guid EntityId,
    string Action,
    string Actor,
    string Summary,
    DateTime CreatedAt
);
```

**GetAuditListQuery:**
```csharp
public sealed record GetAuditListQuery(
    string? EntityType = null,
    Guid? EntityId = null,
    int PageSize = 50       // default 50, max 200
) : IRequest<List<AuditEventDto>>;
```

**GetAuditListHandler:**
```csharp
var q = _db.AuditEvents.AsNoTracking().AsQueryable();
if (!string.IsNullOrEmpty(query.EntityType)) q = q.Where(e => e.EntityType == query.EntityType);
if (query.EntityId.HasValue) q = q.Where(e => e.EntityId == query.EntityId.Value);
var pageSize = Math.Min(query.PageSize, 200);
return await q.OrderByDescending(e => e.CreatedAt).Take(pageSize)
              .Select(e => new AuditEventDto(e.Id, e.EntityType, e.EntityId, e.Action, e.Actor, e.Summary, e.CreatedAt))
              .ToListAsync(ct);
```

### Task 3 Detail: Infrastructure

**AuditEventConfiguration:**
```csharp
b.ToTable("audit_events");
b.HasKey(x => x.Id);
b.Property(x => x.Id).HasColumnName("id");
b.Property(x => x.EntityType).HasColumnName("entity_type").HasMaxLength(50).IsRequired();
b.Property(x => x.EntityId).HasColumnName("entity_id");
b.Property(x => x.Action).HasColumnName("action").HasMaxLength(50).IsRequired();
b.Property(x => x.Actor).HasColumnName("actor").HasMaxLength(256).IsRequired();
b.Property(x => x.Summary).HasColumnName("summary").HasMaxLength(1000);
b.Property(x => x.CreatedAt).HasColumnName("created_at");

b.HasIndex(x => new { x.EntityType, x.EntityId }).HasDatabaseName("ix_audit_events_entity");
b.HasIndex(x => x.CreatedAt).HasDatabaseName("ix_audit_events_created_at");
```

**WorkforceMutatedEventHandler:**
```csharp
// Workforce.Infrastructure/Notifications/WorkforceMutatedEventHandler.cs
public sealed class WorkforceMutatedEventHandler
    : INotificationHandler<WorkforceMutatedNotification>
{
    private readonly IWorkforceDbContext _db;

    public WorkforceMutatedEventHandler(IWorkforceDbContext db) => _db = db;

    public async Task Handle(WorkforceMutatedNotification notification, CancellationToken ct)
    {
        var auditEvent = AuditEvent.Create(
            notification.EntityType,
            notification.EntityId,
            notification.Action,
            notification.Actor,
            notification.Summary);
        _db.AuditEvents.Add(auditEvent);
        await _db.SaveChangesAsync(ct);
    }
}
```

**WorkforceDbContext — thêm:**
```csharp
public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
// + modelBuilder.ApplyConfiguration(new AuditEventConfiguration());
```

### Task 4 Detail: Publish từ existing handlers

**Pattern chuẩn — áp dụng cho mọi handler:**
```csharp
// Ví dụ CreateVendorHandler:
public sealed class CreateVendorHandler : IRequestHandler<CreateVendorCommand, VendorDto>
{
    private readonly IWorkforceDbContext _db;
    private readonly IMediator _mediator;       // ← THÊM

    public CreateVendorHandler(IWorkforceDbContext db, IMediator mediator)
    {
        _db = db;
        _mediator = mediator;                   // ← THÊM
    }

    public async Task<VendorDto> Handle(CreateVendorCommand cmd, CancellationToken ct)
    {
        // ... existing logic ...
        _db.Vendors.Add(vendor);
        await _db.SaveChangesAsync(ct);         // save entity

        // Publish audit notification AFTER successful save
        await _mediator.Publish(new WorkforceMutatedNotification(
            "Vendor", vendor.Id, "Create", cmd.CreatedBy,
            $"Created vendor '{vendor.Name}' (code: {vendor.Code})"), ct);

        return ToDto(vendor);
    }
}
```

**Summary strings cho từng handler:**

| Handler | Summary |
|---|---|
| CreateVendorHandler | `$"Created vendor '{vendor.Name}' (code: {vendor.Code})"` |
| UpdateVendorHandler | `$"Updated vendor '{vendor.Name}'"` |
| InactivateVendorHandler | `$"Inactivated vendor '{vendor.Name}'"` |
| CreateResourceHandler | `$"Created resource '{resource.Name}' (code: {resource.Code}, type: {resource.Type})"` |
| UpdateResourceHandler | `$"Updated resource '{resource.Name}'"` |
| InactivateResourceHandler | `$"Inactivated resource '{resource.Name}'"` |
| CreateRateHandler | `$"Created rate: {vendor.Name} / {cmd.Role} / {cmd.Level} / {cmd.Month}/{cmd.Year} = {cmd.MonthlyAmount}"` |
| DeleteRateHandler | `$"Deleted rate id: {cmd.RateId}"` |

**Lưu ý quan trọng về handler patterns:**
- `UpdateVendorHandler`, `InactivateVendorHandler` đã có vendor object (load từ DB) — dùng trực tiếp
- `UpdateResourceHandler`, `InactivateResourceHandler` đã có resource object + Vendor navigation — dùng `resource.Name`
- `DeleteRateHandler` KHÔNG có `actor` vì DeleteRateCommand chỉ có `RateId`. Cần thêm `DeletedBy` vào command
- Actor cho DeleteRateHandler: cần sửa `DeleteRateCommand` để có `DeletedBy` string, và `RatesController.DeleteRate` truyền `_currentUser.UserId.ToString()`

### Task 5 Detail: API Controller

```csharp
[Authorize]
[ApiController]
[Route("api/v1/audit")]
public sealed class AuditController : ControllerBase
{
    // GET /api/v1/audit?entityType=Vendor&entityId=...&pageSize=50
    [HttpGet]
    public async Task<IActionResult> GetAuditEvents(
        [FromQuery] string? entityType,
        [FromQuery] Guid? entityId,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetAuditListQuery(entityType, entityId, pageSize), ct);
        return Ok(result);
    }
    // NO POST, PUT, DELETE endpoints
}
```

### Task 6 Detail: Frontend (Simple — không dùng NgRx)

Audit log chỉ cần đọc, không cần state management phức tạp. Dùng service trực tiếp trong component.

**audit-event.model.ts:**
```typescript
export interface AuditEvent {
  id: string;
  entityType: string;
  entityId: string;
  action: string;
  actor: string;
  summary: string;
  createdAt: string;
}
```

**audit-api.service.ts:**
```typescript
@Injectable({ providedIn: 'root' })
export class AuditApiService {
  getAuditEvents(entityType?: string, entityId?: string, pageSize = 50): Observable<AuditEvent[]>
}
```

**audit-log component:**
```typescript
@Component({ ..., changeDetection: ChangeDetectionStrategy.OnPush })
export class AuditLogComponent implements OnInit {
  auditEvents$!: Observable<AuditEvent[]>;

  ngOnInit(): void {
    this.auditEvents$ = this.auditApi.getAuditEvents();
  }
}
```

**Template:** MatTable với columns: `entityType`, `entityId`, `action`, `actor`, `summary`, `createdAt`.

**Vị trí files:**
```
frontend/src/app/features/audit/
├── models/audit-event.model.ts
├── services/audit-api.service.ts
├── components/audit-log/
│   ├── audit-log.ts
│   └── audit-log.html
└── audit.routes.ts
```

**app.routes.ts — thêm:**
```typescript
{
  path: 'audit',
  loadChildren: () => import('./features/audit/audit.routes').then(m => m.auditRoutes),
}
```

**Lưu ý:** Audit feature KHÔNG dùng NgRx — chỉ dùng `Observable` từ service trực tiếp. Không cần thêm vào `app.state.ts` hay `app.config.ts` providers.

### Patterns đã có — KHÔNG viết lại

| Pattern | Source | Ghi chú |
|---|---|---|
| `MediatR INotification + INotificationHandler` | Architecture doc | Đã dùng trong TimeTracking pattern |
| `createReducer` (không `createFeature`) | Story 2.1-2.4 | Chỉ cần nếu thêm NgRx — không cần ở đây |
| `[Authorize]` controller | Story 2.1 | Tất cả audit endpoints cần auth |
| `AsNoTracking()` trong query handler | Story 2.2 | Read-only queries |

### Lỗi cần tránh

1. **`DeleteRateCommand` cần thêm `DeletedBy`** — cần sửa command + handler + controller để có actor
2. **AuditEvent KHÔNG extends AuditableEntity** — tự quản lý Id + CreatedAt, không cần IsDeleted
3. **Publish AFTER SaveChanges** — không phải before. Nếu entity save thất bại, không ghi audit (đúng)
4. **Handler không cần IWorkforceDbContext** cho `WorkforceMutatedEventHandler` nếu dùng IWorkforceDbContext — handler ở Infrastructure, được inject IWorkforceDbContext bình thường
5. **Không có NgRx cho audit** — feature đơn giản chỉ cần service + Observable

---

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

- `DeleteRateCommand` không có `DeletedBy` — thêm field vào command và cập nhật `RatesController.DeleteRate` để truyền `_currentUser.UserId.ToString()`.
- `WorkforceMutatedEventHandler` ở Infrastructure layer — inject `IWorkforceDbContext` bình thường, gọi `SaveChangesAsync` riêng để lưu audit event.

### Completion Notes List

- Tạo `AuditEvent` entity (không extends AuditableEntity — immutable log)
- Tạo `WorkforceMutatedNotification : INotification` (MediatR)
- Tạo `AuditEventDto`, `GetAuditListQuery`, `GetAuditListHandler` với filter EntityType/EntityId, pageSize tối đa 200
- Tạo `AuditEventConfiguration` (table `audit_events`, indexes entity + created_at)
- Tạo `WorkforceMutatedEventHandler : INotificationHandler<WorkforceMutatedNotification>` ghi audit sau mỗi mutation
- Cập nhật `WorkforceDbContext` thêm `DbSet<AuditEvent>` + `ApplyConfiguration`
- Tạo EF migration `AddAuditEvent_Workforce`
- Inject `IMediator` + Publish vào tất cả 8 handlers (Create/Update/Inactivate Vendor, Create/Update/Inactivate Resource, Create/Delete Rate)
- Sửa `DeleteRateCommand` thêm `DeletedBy` + cập nhật `RatesController`
- Tạo `AuditController` GET /api/v1/audit (read-only)
- Frontend: `audit-event.model.ts`, `audit-api.service.ts`, `AuditLogComponent` (service trực tiếp, không NgRx), `audit.routes.ts`, đăng ký route `/audit` trong `app.routes.ts`
- `dotnet build` → 0 errors, `ng build` → 0 errors

### File List

**Backend (BE):**
- `src/Modules/Workforce/ProjectManagement.Workforce.Domain/Entities/AuditEvent.cs` (new)
- `src/Modules/Workforce/ProjectManagement.Workforce.Application/Common/Interfaces/IWorkforceDbContext.cs` (modified — thêm DbSet<AuditEvent>)
- `src/Modules/Workforce/ProjectManagement.Workforce.Application/Notifications/WorkforceMutatedNotification.cs` (new)
- `src/Modules/Workforce/ProjectManagement.Workforce.Application/DTOs/AuditEventDto.cs` (new)
- `src/Modules/Workforce/ProjectManagement.Workforce.Application/Audit/Queries/GetAuditList/GetAuditListQuery.cs` (new)
- `src/Modules/Workforce/ProjectManagement.Workforce.Application/Audit/Queries/GetAuditList/GetAuditListHandler.cs` (new)
- `src/Modules/Workforce/ProjectManagement.Workforce.Application/Vendors/Commands/CreateVendor/CreateVendorHandler.cs` (modified)
- `src/Modules/Workforce/ProjectManagement.Workforce.Application/Vendors/Commands/UpdateVendor/UpdateVendorHandler.cs` (modified)
- `src/Modules/Workforce/ProjectManagement.Workforce.Application/Vendors/Commands/InactivateVendor/InactivateVendorHandler.cs` (modified)
- `src/Modules/Workforce/ProjectManagement.Workforce.Application/Resources/Commands/CreateResource/CreateResourceHandler.cs` (modified)
- `src/Modules/Workforce/ProjectManagement.Workforce.Application/Resources/Commands/UpdateResource/UpdateResourceHandler.cs` (modified)
- `src/Modules/Workforce/ProjectManagement.Workforce.Application/Resources/Commands/InactivateResource/InactivateResourceHandler.cs` (modified)
- `src/Modules/Workforce/ProjectManagement.Workforce.Application/Rates/Commands/CreateRate/CreateRateHandler.cs` (modified)
- `src/Modules/Workforce/ProjectManagement.Workforce.Application/Rates/Commands/DeleteRate/DeleteRateCommand.cs` (modified — thêm DeletedBy)
- `src/Modules/Workforce/ProjectManagement.Workforce.Application/Rates/Commands/DeleteRate/DeleteRateHandler.cs` (modified)
- `src/Modules/Workforce/ProjectManagement.Workforce.Infrastructure/Persistence/Configurations/AuditEventConfiguration.cs` (new)
- `src/Modules/Workforce/ProjectManagement.Workforce.Infrastructure/Notifications/WorkforceMutatedEventHandler.cs` (new)
- `src/Modules/Workforce/ProjectManagement.Workforce.Infrastructure/Persistence/WorkforceDbContext.cs` (modified)
- `src/Modules/Workforce/ProjectManagement.Workforce.Infrastructure/Migrations/[timestamp]_AddAuditEvent_Workforce.cs` (new)
- `src/Modules/Workforce/ProjectManagement.Workforce.Infrastructure/Migrations/[timestamp]_AddAuditEvent_Workforce.Designer.cs` (new)
- `src/Modules/Workforce/ProjectManagement.Workforce.Infrastructure/Migrations/WorkforceDbContextModelSnapshot.cs` (modified)
- `src/Modules/Workforce/ProjectManagement.Workforce.Api/Controllers/AuditController.cs` (new)
- `src/Modules/Workforce/ProjectManagement.Workforce.Api/Controllers/RatesController.cs` (modified — truyền DeletedBy)

**Frontend (FE):**
- `frontend/project-management-web/src/app/features/audit/models/audit-event.model.ts` (new)
- `frontend/project-management-web/src/app/features/audit/services/audit-api.service.ts` (new)
- `frontend/project-management-web/src/app/features/audit/components/audit-log/audit-log.ts` (new)
- `frontend/project-management-web/src/app/features/audit/components/audit-log/audit-log.html` (new)
- `frontend/project-management-web/src/app/features/audit/audit.routes.ts` (new)
- `frontend/project-management-web/src/app/app.routes.ts` (modified — thêm /audit route)
