# Story 2.4: Monthly Rate Model (Vendor × Role × Level × Month) + Non-Overlap Rule

Status: review

**Story ID:** 2.4
**Epic:** Epic 2 — Workforce (People/Vendor) + Rate Model + Audit Foundation
**Sprint:** Sprint 3
**Date Created:** 2026-04-26

---

## Story

As a PM,
I want cấu hình monthly rate theo vendor × role × level với hiệu lực theo tháng,
So that hệ thống tính Hourly Rate và cost đúng, có thể reconstruct lịch sử.

## Acceptance Criteria

1. **Given** rate có hiệu lực theo tháng
   **When** tạo rate mới cho cùng vendor/role/level/tháng đã tồn tại
   **Then** bị chặn (non-overlap) và trả `409 Conflict ProblemDetails`

2. **Given** `Hourly Rate = Monthly Rate ÷ 176`
   **When** tạo/lấy rate record
   **Then** `hourlyRate` được tính và trả về trong DTO (không lưu riêng, computed on-the-fly)

3. **Given** vendor không tồn tại hoặc inactive
   **When** tạo rate với vendorId đó
   **Then** trả `400 DomainException`

4. **Given** role hoặc level không nằm trong catalog
   **When** tạo rate
   **Then** trả `400 DomainException` (validate bằng `Enum.TryParse`)

5. **Given** user muốn xem rates hiện tại
   **When** gọi `GET /api/v1/rates?vendorId=...&year=...&month=...`
   **Then** trả danh sách rates phù hợp filter

6. **Given** rate đã tồn tại và cần xóa (sai nhập)
   **When** gọi `DELETE /api/v1/rates/{rateId}`
   **Then** rate bị xóa vĩnh viễn (hard delete — rate là config, không phải log)

## Tasks / Subtasks

- [x] **Task 1: Domain Entity (BE)**
  - [x] 1.1 Tạo `MonthlyRate.cs` trong `Workforce.Domain/Entities/`

- [x] **Task 2: Application Layer (BE)**
  - [x] 2.1 Cập nhật `IWorkforceDbContext` — thêm `DbSet<MonthlyRate> MonthlyRates`
  - [x] 2.2 Tạo `MonthlyRateDto.cs` record
  - [x] 2.3 Tạo `CreateRateCommand` + Handler (non-overlap + vendor validation)
  - [x] 2.4 Tạo `DeleteRateCommand` + Handler
  - [x] 2.5 Tạo `GetRateListQuery` + Handler (filter by vendorId, year, month)
  - [x] 2.6 Tạo `GetRateByIdQuery` + Handler

- [x] **Task 3: Infrastructure Layer (BE)**
  - [x] 3.1 Tạo `MonthlyRateConfiguration.cs` EF config
  - [x] 3.2 Cập nhật `WorkforceDbContext` — thêm `MonthlyRates` DbSet + ApplyConfiguration
  - [x] 3.3 Tạo EF migration `AddMonthlyRate_Workforce`

- [x] **Task 4: API Controller (BE)**
  - [x] 4.1 Tạo `RatesController.cs` tại `/api/v1/rates`

- [x] **Task 5: Frontend NgRx Store + Service (FE)**
  - [x] 5.1 Tạo `monthly-rate.model.ts`
  - [x] 5.2 Tạo `rates.actions.ts`
  - [x] 5.3 Tạo `rates.reducer.ts`
  - [x] 5.4 Tạo `rates.selectors.ts`
  - [x] 5.5 Tạo `rates.effects.ts`
  - [x] 5.6 Tạo `rates-api.service.ts`
  - [x] 5.7 Đăng ký trong `app.state.ts` + `app.config.ts`

- [x] **Task 6: Frontend Components + Routes (FE)**
  - [x] 6.1 Tạo `rate-list` component (MatTable, filter by vendor)
  - [x] 6.2 Tạo `rate-form` component (MatDialog, dropdown role/level từ lookups catalog)
  - [x] 6.3 Tạo `rates.routes.ts` + đăng ký trong `app.routes.ts`

- [x] **Task 7: Build verification**
  - [x] 7.1 `dotnet build` → 0 errors
  - [x] 7.2 `ng build` → 0 errors (fix: thêm `DecimalPipe` vào imports của `RateListComponent`)

---

## Dev Notes

### Workforce Module đã có — KHÔNG tạo lại

| Đã có | Ghi chú |
|---|---|
| `Vendor.cs` entity + `VendorDto` | FK source cho MonthlyRate |
| `WorkforceDbContext` | Cần thêm MonthlyRates DbSet |
| `IWorkforceDbContext` | Cần thêm `DbSet<MonthlyRate>` |
| `ResourceRole.cs`, `ResourceLevel.cs` enums | Story 2.3 — validate bằng Enum.TryParse |
| `LookupItemDto` | Dùng cho dropdown data |
| `LookupsEffects` + `selectRoles`, `selectLevels` | Story 2.3 — dùng trong rate-form |
| Pattern `CreateVendorHandler.ToDto()` | Áp dụng cho ToDto trong CreateRateHandler |
| `[Authorize]` + `ETagHelper` pattern | Từ VendorsController/ResourcesController |

### Task 1 Detail: MonthlyRate Entity

```csharp
// Workforce.Domain/Entities/MonthlyRate.cs
using ProjectManagement.Shared.Domain.Entities;
using ProjectManagement.Workforce.Domain.Enums;

namespace ProjectManagement.Workforce.Domain.Entities;

public class MonthlyRate : AuditableEntity
{
    public Guid VendorId { get; private set; }
    public string Role { get; private set; } = string.Empty;    // stored as string from enum
    public string Level { get; private set; } = string.Empty;   // stored as string from enum
    public int Year { get; private set; }
    public int Month { get; private set; }       // 1-12
    public decimal MonthlyAmount { get; private set; }

    public Vendor? Vendor { get; private set; }

    // Computed — not persisted
    public decimal HourlyRate => MonthlyAmount / 176m;

    public static MonthlyRate Create(
        Guid vendorId, string role, string level,
        int year, int month, decimal monthlyAmount,
        string createdBy)
        => new()
        {
            Id = Guid.NewGuid(),
            VendorId = vendorId,
            Role = role,
            Level = level,
            Year = year,
            Month = month,
            MonthlyAmount = monthlyAmount,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
}
```

**Lưu ý thiết kế quan trọng:**
- `Role` và `Level` lưu dạng **string** (không phải enum) — dễ migrate về sau nếu thêm giá trị mới
- Validate qua `Enum.TryParse<ResourceRole>` trong handler, không ép buộc DB enum
- `HourlyRate` là **computed property**, không có cột DB tương ứng — EF sẽ ignore hoặc cần `[NotMapped]`
- `MonthlyRate` **KHÔNG có** `Version`/optimistic lock — đây là config, không phải entity cần concurrent edit
- `MonthlyRate` **KHÔNG có** `IsActive`/`IsDeleted` — delete là hard delete

### Task 2 Detail: Application Layer

**IWorkforceDbContext — thêm:**
```csharp
DbSet<MonthlyRate> MonthlyRates { get; }
```

**MonthlyRateDto:**
```csharp
public sealed record MonthlyRateDto(
    Guid Id,
    Guid VendorId,
    string? VendorName,
    string Role,
    string Level,
    int Year,
    int Month,
    decimal MonthlyAmount,
    decimal HourlyRate,       // = MonthlyAmount / 176
    DateTime CreatedAt,
    string CreatedBy
);
```

**CreateRateCommand:**
```csharp
public sealed record CreateRateCommand(
    Guid VendorId,
    string Role,
    string Level,
    int Year,
    int Month,
    decimal MonthlyAmount,
    string CreatedBy
) : IRequest<MonthlyRateDto>;
```

**CreateRateHandler — validation logic:**
```csharp
// 1. Validate Role enum → 400 DomainException
if (!Enum.TryParse<ResourceRole>(cmd.Role, out _))
    throw new DomainException($"Role không hợp lệ: '{cmd.Role}'.");

// 2. Validate Level enum → 400 DomainException
if (!Enum.TryParse<ResourceLevel>(cmd.Level, out _))
    throw new DomainException($"Level không hợp lệ: '{cmd.Level}'.");

// 3. Validate Year/Month range
if (cmd.Month < 1 || cmd.Month > 12)
    throw new DomainException("Month phải từ 1 đến 12.");

// 4. Validate Vendor exists + IsActive → 400 DomainException
var vendor = await _db.Vendors.FirstOrDefaultAsync(v => v.Id == cmd.VendorId, ct)
    ?? throw new DomainException($"Vendor không tồn tại.");
if (!vendor.IsActive)
    throw new DomainException($"Vendor '{vendor.Name}' đã inactive.");

// 5. Non-overlap check → 409 ConflictException (plain, no currentState)
var exists = await _db.MonthlyRates.AnyAsync(r =>
    r.VendorId == cmd.VendorId &&
    r.Role == cmd.Role &&
    r.Level == cmd.Level &&
    r.Year == cmd.Year &&
    r.Month == cmd.Month, ct);
if (exists)
    throw new ConflictException(
        $"Rate cho vendor/role/level/tháng này đã tồn tại.");

// 6. Create + save
var rate = MonthlyRate.Create(cmd.VendorId, cmd.Role, cmd.Level,
    cmd.Year, cmd.Month, cmd.MonthlyAmount, cmd.CreatedBy);
_db.MonthlyRates.Add(rate);
await _db.SaveChangesAsync(ct);
return ToDto(rate, vendor.Name);
```

**ToDto helper:**
```csharp
internal static MonthlyRateDto ToDto(MonthlyRate r, string? vendorName = null) => new(
    r.Id, r.VendorId, vendorName ?? r.Vendor?.Name,
    r.Role, r.Level, r.Year, r.Month,
    r.MonthlyAmount, r.HourlyRate,
    r.CreatedAt, r.CreatedBy);
```

**DeleteRateCommand:**
```csharp
public sealed record DeleteRateCommand(Guid RateId) : IRequest;
```

**DeleteRateHandler:**
```csharp
var rate = await _db.MonthlyRates.FindAsync([cmd.RateId], ct)
    ?? throw new NotFoundException("Rate không tồn tại.");
_db.MonthlyRates.Remove(rate);
await _db.SaveChangesAsync(ct);
```

**GetRateListQuery:**
```csharp
public sealed record GetRateListQuery(
    Guid? VendorId = null,
    int? Year = null,
    int? Month = null
) : IRequest<List<MonthlyRateDto>>;
```

**GetRateListHandler:**
```csharp
var q = _db.MonthlyRates.AsNoTracking().Include(r => r.Vendor).AsQueryable();
if (query.VendorId.HasValue) q = q.Where(r => r.VendorId == query.VendorId.Value);
if (query.Year.HasValue)     q = q.Where(r => r.Year == query.Year.Value);
if (query.Month.HasValue)    q = q.Where(r => r.Month == query.Month.Value);
var rates = await q.OrderBy(r => r.Year).ThenBy(r => r.Month)
                   .ThenBy(r => r.Role).ThenBy(r => r.Level).ToListAsync(ct);
return rates.Select(r => CreateRateHandler.ToDto(r)).ToList();
```

**Lưu ý ConflictException:** Rate không có `currentState` để truyền vào (không có optimistic lock). Dùng `ConflictException(string message)` — overload 1 tham số — không phải overload với `(msg, currentState, eTag)`.

### Task 3 Detail: Infrastructure

**MonthlyRateConfiguration:**
```csharp
b.ToTable("monthly_rates");
b.HasKey(x => x.Id);
b.Property(x => x.Id).HasColumnName("id");
b.Property(x => x.VendorId).HasColumnName("vendor_id");
b.Property(x => x.Role).HasColumnName("role").HasMaxLength(50).IsRequired();
b.Property(x => x.Level).HasColumnName("level").HasMaxLength(50).IsRequired();
b.Property(x => x.Year).HasColumnName("year");
b.Property(x => x.Month).HasColumnName("month");
b.Property(x => x.MonthlyAmount).HasColumnName("monthly_amount").HasColumnType("decimal(18,2)");
b.Property(x => x.CreatedAt).HasColumnName("created_at");
b.Property(x => x.CreatedBy).HasColumnName("created_by").HasMaxLength(256);
// Audit columns từ AuditableEntity
b.Property(x => x.UpdatedAt).HasColumnName("updated_at");
b.Property(x => x.UpdatedBy).HasColumnName("updated_by").HasMaxLength(256);
b.Property(x => x.IsDeleted).HasColumnName("is_deleted");

// HourlyRate là computed, EF phải ignore
b.Ignore(x => x.HourlyRate);

// Unique constraint: non-overlap rule
b.HasIndex(x => new { x.VendorId, x.Role, x.Level, x.Year, x.Month })
 .IsUnique()
 .HasDatabaseName("uq_monthly_rates_key");

b.HasOne(x => x.Vendor)
 .WithMany()
 .HasForeignKey(x => x.VendorId)
 .IsRequired()
 .OnDelete(DeleteBehavior.Restrict);
```

**WorkforceDbContext — thêm:**
```csharp
public DbSet<MonthlyRate> MonthlyRates => Set<MonthlyRate>();
// + modelBuilder.ApplyConfiguration(new MonthlyRateConfiguration());
```

**Migration:** `dotnet ef migrations add AddMonthlyRate_Workforce --context WorkforceDbContext --project ... --startup-project ...`

### Task 4 Detail: API Controller

```csharp
[Authorize]
[ApiController]
[Route("api/v1/rates")]
public sealed class RatesController : ControllerBase
{
    // GET /api/v1/rates?vendorId=...&year=...&month=...
    // GET /api/v1/rates/{rateId}
    // POST /api/v1/rates → 201 Created + Location
    // DELETE /api/v1/rates/{rateId} → 204 (hard delete, NO If-Match needed)
}

public sealed record CreateRateRequest(
    Guid VendorId, string Role, string Level,
    int Year, int Month, decimal MonthlyAmount);
```

**Lưu ý:** Rate không có update (immutable). Chỉ Create + Delete + Queries.

### Task 5-6 Detail: Frontend

**monthly-rate.model.ts:**
```typescript
export interface MonthlyRate {
  id: string;
  vendorId: string;
  vendorName?: string;
  role: string;
  level: string;
  year: number;
  month: number;
  monthlyAmount: number;
  hourlyRate: number;
  createdAt: string;
  createdBy: string;
}
```

**NgRx store pattern (giống vendors/resources):**
- `createReducer` (không `createFeature`)
- Actions: Load/LoadSuccess/LoadFailure, Create/CreateSuccess/CreateFailure, Delete/DeleteSuccess/DeleteFailure
- Không có Update (immutable), không có Conflict (không có optimistic lock)

**rate-form component:**
- Dropdown Vendor (dùng `selectAllVendors` hoặc `selectAllVendors` filter activeOnly)
- Dropdown Role — dùng `selectRoles` từ **lookups store** (Story 2.3)
- Dropdown Level — dùng `selectLevels` từ **lookups store** (Story 2.3)
- Dispatch `LookupsActions.loadCatalog()` nếu `!lookupsLoaded` khi form mở

```typescript
// rate-form.ts — ngOnInit
ngOnInit(): void {
  this.store.select(selectLookupsLoaded).pipe(take(1)).subscribe(loaded => {
    if (!loaded) this.store.dispatch(LookupsActions.loadCatalog());
  });
  this.store.dispatch(VendorsActions.loadVendors({ activeOnly: true }));
}
```

**rates-api.service.ts:**
```typescript
getRates(vendorId?: string, year?: number, month?: number): Observable<MonthlyRate[]>
getRateById(rateId: string): Observable<MonthlyRate>
createRate(body: CreateRateRequest): Observable<MonthlyRate>
deleteRate(rateId: string): Observable<void>
```

**app.state.ts — thêm:**
```typescript
import { ratesReducer, RatesState } from '../../features/rates/store/rates.reducer';
// AppState: rates: RatesState
// reducers: rates: ratesReducer
```

### Patterns đã có — KHÔNG viết lại

| Pattern | Source | Ghi chú |
|---|---|---|
| `createReducer` (không `createFeature`) | Story 2.1, 2.2, 2.3 | Tránh TypeScript error |
| `Enum.TryParse` validate | Story 2.2, 2.3 | validate role/level |
| Include(r => r.Vendor) trong query handler | Story 2.2 | Lấy VendorName |
| `ConflictException(message)` | Shared.Domain | 1-arg overload khi không có currentState |
| `[Authorize]` controller | Story 2.1 | Tất cả endpoints cần auth |
| `selectRoles`, `selectLevels` selectors | Story 2.3 | Dùng trong rate-form |
| `LookupsActions.loadCatalog()` | Story 2.3 | Trigger load catalog cho dropdown |

### Lỗi cần tránh

1. **Không dùng HasConversion<string>() cho Role/Level** — đã lưu dạng string, không phải enum property
2. **Không cần b.Ignore(x => x.IsActive)** — MonthlyRate không có IsActive, nhưng AuditableEntity có IsDeleted — để EF map bình thường
3. **b.Ignore(x => x.HourlyRate) là BẮT BUỘC** — computed property không có cột DB
4. **ConflictException 1-arg** — rate không có optimistic lock, không truyền currentState
5. **Không có If-Match cho DELETE** — rate không có Version, hard delete trực tiếp
6. **`type` là reserved prop trong NgRx actions** — không dùng field tên `type` (bài học từ Story 2.2)
7. **VendorId là required (không nullable)** — khác Resource.VendorId. Rate luôn gắn với vendor

---

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

_(Trống)_

### Completion Notes List

- `HourlyRate` là computed property — `b.Ignore(x => x.HourlyRate)` bắt buộc trong EF config
- `ConflictException(message)` 1-arg được dùng vì rate không có optimistic lock
- `DeleteRateHandler` dùng hard delete (`Remove`), không soft-delete
- `DecimalPipe` cần import tường minh trong standalone component để dùng `| number` pipe
- dotnet build: 0 errors; ng build: 0 errors

### File List

**Backend:**
- `src/Modules/Workforce/ProjectManagement.Workforce.Domain/Entities/MonthlyRate.cs`
- `src/Modules/Workforce/ProjectManagement.Workforce.Application/Common/Interfaces/IWorkforceDbContext.cs` _(updated)_
- `src/Modules/Workforce/ProjectManagement.Workforce.Application/DTOs/MonthlyRateDto.cs`
- `src/Modules/Workforce/ProjectManagement.Workforce.Application/Rates/Commands/CreateRate/CreateRateCommand.cs`
- `src/Modules/Workforce/ProjectManagement.Workforce.Application/Rates/Commands/CreateRate/CreateRateHandler.cs`
- `src/Modules/Workforce/ProjectManagement.Workforce.Application/Rates/Commands/DeleteRate/DeleteRateCommand.cs`
- `src/Modules/Workforce/ProjectManagement.Workforce.Application/Rates/Commands/DeleteRate/DeleteRateHandler.cs`
- `src/Modules/Workforce/ProjectManagement.Workforce.Application/Rates/Queries/GetRateList/GetRateListQuery.cs`
- `src/Modules/Workforce/ProjectManagement.Workforce.Application/Rates/Queries/GetRateList/GetRateListHandler.cs`
- `src/Modules/Workforce/ProjectManagement.Workforce.Application/Rates/Queries/GetRateById/GetRateByIdQuery.cs`
- `src/Modules/Workforce/ProjectManagement.Workforce.Application/Rates/Queries/GetRateById/GetRateByIdHandler.cs`
- `src/Modules/Workforce/ProjectManagement.Workforce.Infrastructure/Persistence/Configurations/MonthlyRateConfiguration.cs`
- `src/Modules/Workforce/ProjectManagement.Workforce.Infrastructure/Persistence/WorkforceDbContext.cs` _(updated)_
- `src/Modules/Workforce/ProjectManagement.Workforce.Infrastructure/Migrations/<timestamp>_AddMonthlyRate_Workforce.cs`
- `src/Modules/Workforce/ProjectManagement.Workforce.Api/Controllers/RatesController.cs`

**Frontend:**
- `frontend/project-management-web/src/app/features/rates/models/monthly-rate.model.ts`
- `frontend/project-management-web/src/app/features/rates/store/rates.actions.ts`
- `frontend/project-management-web/src/app/features/rates/store/rates.reducer.ts`
- `frontend/project-management-web/src/app/features/rates/store/rates.selectors.ts`
- `frontend/project-management-web/src/app/features/rates/store/rates.effects.ts`
- `frontend/project-management-web/src/app/features/rates/services/rates-api.service.ts`
- `frontend/project-management-web/src/app/features/rates/components/rate-list/rate-list.ts`
- `frontend/project-management-web/src/app/features/rates/components/rate-list/rate-list.html`
- `frontend/project-management-web/src/app/features/rates/components/rate-list/rate-list.scss`
- `frontend/project-management-web/src/app/features/rates/components/rate-form/rate-form.ts`
- `frontend/project-management-web/src/app/features/rates/components/rate-form/rate-form.html`
- `frontend/project-management-web/src/app/features/rates/rates.routes.ts`
- `frontend/project-management-web/src/app/core/store/app.state.ts` _(updated)_
- `frontend/project-management-web/src/app/app.config.ts` _(updated)_
- `frontend/project-management-web/src/app/app.routes.ts` _(updated)_
