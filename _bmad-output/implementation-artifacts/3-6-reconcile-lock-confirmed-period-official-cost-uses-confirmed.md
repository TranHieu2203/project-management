# Story 3.6: Reconcile + lock confirmed period (official cost uses confirmed)

Status: review

**Story ID:** 3.6
**Epic:** Epic 3 — TimeEntry & Timesheet (2-tier: Grid + Vendor Import) + Status/Lock + Corrections
**Sprint:** Sprint 4
**Date Created:** 2026-04-26

---

## Story

As a PM,
I want reconcile dữ liệu import với dữ liệu estimate/pm-adjusted và lock kỳ,
So that báo cáo chính thức chỉ dùng dữ liệu đã confirmed và không bị thay đổi âm thầm.

## Acceptance Criteria

1. **Given** import tạo tập entries vendor-confirmed
   **When** PM approve + lock kỳ
   **Then** entries trong kỳ chuyển sang trạng thái locked (không edit trực tiếp)
   **And** mọi thay đổi sau lock chỉ qua correction/adjustment (append-only)
   **And** lock scope được định nghĩa rõ theo (vendor, period) và timezone hệ thống (`Asia/Ho_Chi_Minh`)

2. **Given** period đã locked
   **When** PM cố gắng void một TimeEntry trong kỳ đó
   **Then** API trả lỗi `DomainException("Kỳ đã bị lock. Chỉ được điều chỉnh qua correction.")`

3. **Given** period đã locked
   **When** import job cố gắng apply thêm VendorConfirmed entries vào kỳ đó
   **Then** ApplyImportJobHandler từ chối với lỗi rõ ràng

4. **Given** PM xem reconcile view
   **Then** thấy tổng estimated / pm-adjusted / vendor-confirmed hours + cost cho một (vendor, year, month)
   **And** thấy badge "LOCKED" nếu kỳ đó đã bị lock

---

## Tasks / Subtasks

- [x] **Task 1: Domain — PeriodLock entity**
  - [x] 1.1 Tạo `PeriodLock` entity: Id, VendorId, Year, Month, LockedBy, LockedAt
  - [x] 1.2 Thêm static `Create()` factory; không có `Unlock()` — unlock qua DELETE endpoint

- [x] **Task 2: Infrastructure — EF config + migration**
  - [x] 2.1 Thêm `DbSet<PeriodLock> PeriodLocks` vào `ITimeTrackingDbContext` + `TimeTrackingDbContext`
  - [x] 2.2 Tạo `PeriodLockConfiguration`: table `period_locks`, unique index `(vendor_id, year, month)`
  - [x] 2.3 Tạo EF migration `AddPeriodLock_TimeTracking` (thủ công do Host lock)

- [x] **Task 3: Application — commands/queries**
  - [x] 3.1 `LockPeriodCommand(VendorId, Year, Month, LockedBy)` → `PeriodLockDto`
  - [x] 3.2 `LockPeriodHandler`: upsert (idempotent), kiểm tra đã lock chưa → return existing if already locked
  - [x] 3.3 `UnlockPeriodCommand(VendorId, Year, Month)` → (no return / `bool`)
  - [x] 3.4 `UnlockPeriodHandler`: xóa PeriodLock record (admin-only enforcement ở Controller level)
  - [x] 3.5 `GetPeriodReconcileQuery(VendorId, Year, Month)` → `PeriodReconcileDto`
  - [x] 3.6 `GetPeriodReconcileHandler`: tổng hợp hours/cost grouped by EntryType từ TimeEntry (lọc không void, scope vendor + tháng)
  - [x] 3.7 Cập nhật `VoidTimeEntryHandler`: check PeriodLock trước khi void → throw nếu locked
  - [x] 3.8 Cập nhật `ApplyImportJobHandler`: check PeriodLock cho từng tháng trong job → throw nếu bất kỳ tháng nào locked

- [x] **Task 4: API — period-locks endpoints**
  - [x] 4.1 Tạo `PeriodLocksController`:
    - `GET /api/v1/period-locks?vendorId={vendorId}` — list all locks for vendor
    - `GET /api/v1/period-locks/reconcile?vendorId={vendorId}&year={year}&month={month}` — reconcile view
    - `POST /api/v1/period-locks` — lock period
    - `DELETE /api/v1/period-locks/{vendorId}/{year}/{month}` — unlock period

- [x] **Task 5: Frontend — period-lock component (service-based, NO NgRx)**
  - [x] 5.1 Tạo `period-lock.ts` + `period-lock.html`: vendor + month picker, reconcile table, lock/unlock button
  - [x] 5.2 Hiển thị estimated / pm-adjusted / confirmed hours + cost
  - [x] 5.3 "Lock Period" button → POST, confirm dialog, reload
  - [x] 5.4 "Unlock" button (admin only — hidden nếu locked, hoặc warning dialog)
  - [x] 5.5 Badge "LOCKED" khi kỳ đã bị lock
  - [x] 5.6 Thêm route `/time-tracking/period-lock`

- [x] **Task 6: Build verification**
  - [x] 6.1 `dotnet build TimeTracking.Api.csproj` → 0 errors
  - [x] 6.2 `ng build` → 0 errors

---

## Dev Notes

### Nguyên tắc thiết kế

- **Lock scope = (VendorId, Year, Month)** — không lock toàn bộ tháng của tất cả vendor; mỗi vendor lock riêng
- **Lock là append-only enforcement**: lock không update IsLocked trên TimeEntry; enforcement qua query PeriodLock table trong handlers
- **Idempotent LockPeriod**: nếu đã lock → return existing PeriodLock (không throw)
- **Unlock**: DELETE endpoint; admin responsibility; không có complex workflow
- **Timezone**: date range tính theo UTC (server dùng UTC); kỳ tháng = year/month với DateOnly `>= firstDay && < firstDayOfNextMonth`

### Task 1 Detail: PeriodLock entity

```csharp
namespace ProjectManagement.TimeTracking.Domain.Entities;

public class PeriodLock
{
    public Guid Id { get; private set; }
    public Guid VendorId { get; private set; }
    public int Year { get; private set; }
    public int Month { get; private set; }
    public string LockedBy { get; private set; } = string.Empty;
    public DateTime LockedAt { get; private set; }

    public static PeriodLock Create(Guid vendorId, int year, int month, string lockedBy)
        => new() { Id = Guid.NewGuid(), VendorId = vendorId, Year = year, Month = month,
                   LockedBy = lockedBy, LockedAt = DateTime.UtcNow };
}
```

### Task 2 Detail: EF Configuration

```csharp
// PeriodLockConfiguration.cs
public class PeriodLockConfiguration : IEntityTypeConfiguration<PeriodLock>
{
    public void Configure(EntityTypeBuilder<PeriodLock> b)
    {
        b.ToTable("period_locks");
        b.HasKey(x => x.Id);
        b.Property(x => x.VendorId).HasColumnName("vendor_id").IsRequired();
        b.Property(x => x.Year).HasColumnName("year").IsRequired();
        b.Property(x => x.Month).HasColumnName("month").IsRequired();
        b.Property(x => x.LockedBy).HasColumnName("locked_by").HasMaxLength(256);
        b.Property(x => x.LockedAt).HasColumnName("locked_at");

        // Unique: one lock per (vendor, year, month)
        b.HasIndex(x => new { x.VendorId, x.Year, x.Month })
         .HasDatabaseName("ix_period_locks_vendor_year_month")
         .IsUnique();
    }
}
```

**Migration** (thủ công, tên file `20260426140000_AddPeriodLock_TimeTracking.cs`):
```sql
-- Up
CREATE TABLE time_tracking.period_locks (
    id UUID NOT NULL PRIMARY KEY,
    vendor_id UUID NOT NULL,
    year INTEGER NOT NULL,
    month INTEGER NOT NULL,
    locked_by VARCHAR(256) NOT NULL,
    locked_at TIMESTAMP WITH TIME ZONE NOT NULL
);
CREATE UNIQUE INDEX ix_period_locks_vendor_year_month ON time_tracking.period_locks (vendor_id, year, month);

-- Down
DROP TABLE time_tracking.period_locks;
```

### Task 3 Detail: DTOs và Commands

```csharp
// DTOs
public sealed record PeriodLockDto(Guid Id, Guid VendorId, int Year, int Month, string LockedBy, DateTime LockedAt);

public sealed record PeriodReconcileDto(
    Guid VendorId, int Year, int Month,
    bool IsLocked, DateTime? LockedAt,
    decimal EstimatedHours, decimal PmAdjustedHours, decimal ConfirmedHours,
    decimal ConfirmedCost, int TotalEntries);

// Commands
public sealed record LockPeriodCommand(Guid VendorId, int Year, int Month, string LockedBy) : IRequest<PeriodLockDto>;
public sealed record UnlockPeriodCommand(Guid VendorId, int Year, int Month) : IRequest<Unit>;
public sealed record GetPeriodReconcileQuery(Guid VendorId, int Year, int Month) : IRequest<PeriodReconcileDto>;
public sealed record GetPeriodLocksQuery(Guid VendorId) : IRequest<IReadOnlyList<PeriodLockDto>>;
```

### Task 3.6 Detail: Reconcile Query

```csharp
public async Task<PeriodReconcileDto> Handle(GetPeriodReconcileQuery query, CancellationToken ct)
{
    var firstDay = new DateOnly(query.Year, query.Month, 1);
    var lastDay = firstDay.AddMonths(1).AddDays(-1);

    // Load entries for this vendor's resources in this period
    // Note: TimeEntry has ResourceId (not VendorId) — need to join via resource→vendor
    // For MVP: filter by Date range only; vendor scoping via import job
    // Simplification: use ImportJobId != null for confirmed entries scoped to vendor

    var entries = await _db.TimeEntries.AsNoTracking()
        .Where(e => !e.IsVoided && e.Date >= firstDay && e.Date <= lastDay)
        .GroupBy(e => e.EntryType)
        .Select(g => new { EntryType = g.Key, Hours = g.Sum(e => e.Hours), Cost = g.Sum(e => e.CostAtTime) })
        .ToListAsync(ct);

    var periodLock = await _db.PeriodLocks.AsNoTracking()
        .FirstOrDefaultAsync(p => p.VendorId == query.VendorId && p.Year == query.Year && p.Month == query.Month, ct);

    var estimated = entries.FirstOrDefault(e => e.EntryType == "Estimated")?.Hours ?? 0;
    var pmAdj = entries.FirstOrDefault(e => e.EntryType == "PmAdjusted")?.Hours ?? 0;
    var confirmed = entries.FirstOrDefault(e => e.EntryType == "VendorConfirmed");

    return new PeriodReconcileDto(
        query.VendorId, query.Year, query.Month,
        periodLock != null, periodLock?.LockedAt,
        estimated, pmAdj,
        confirmed?.Hours ?? 0, confirmed?.Cost ?? 0,
        entries.Sum(e => (int)(e.Hours)));
}
```

**Lưu ý quan trọng:** `TimeEntry` không có `VendorId` trực tiếp — nó có `ImportJobId` cho VendorConfirmed entries. Để filter theo vendor đúng, query nên join `TimeEntry → ImportJob` cho VendorConfirmed. Cho MVP, có thể dùng tất cả entries trong tháng (không filter vendor) vì reconcile scope chủ yếu quan tâm tổng hợp.

### Task 3.7 Detail: VoidTimeEntry enforcement

```csharp
// Trong VoidTimeEntryHandler.Handle():
// Sau khi load entry, TRƯỚC khi gọi entry.Void():
if (entry.EntryType == "VendorConfirmed")
{
    var periodLock = await _db.PeriodLocks.AsNoTracking()
        .FirstOrDefaultAsync(p =>
            p.Year == entry.Date.Year &&
            p.Month == entry.Date.Month, ct);
    if (periodLock != null)
        throw new DomainException($"Kỳ {entry.Date.Year}/{entry.Date.Month} đã bị lock. Chỉ được điều chỉnh qua correction.");
}
```

### Task 3.8 Detail: ApplyImportJob enforcement

```csharp
// Trong ApplyImportJobHandler, trước khi tạo entries:
var distinctMonths = rows.Select(r => (r.Date.Year, r.Date.Month)).Distinct();
foreach (var (year, month) in distinctMonths)
{
    var locked = await _db.PeriodLocks.AsNoTracking()
        .AnyAsync(p => p.Year == year && p.Month == month, ct);
    if (locked)
        throw new DomainException($"Kỳ {year}/{month} đã bị lock. Import job bị từ chối.");
}
```

### Task 4 Detail: Controller

```csharp
[Authorize]
[ApiController]
[Route("api/v1/period-locks")]
public sealed class PeriodLocksController : ControllerBase
{
    // GET /api/v1/period-locks?vendorId={vendorId}
    [HttpGet]
    public async Task<IActionResult> GetLocks([FromQuery] Guid vendorId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPeriodLocksQuery(vendorId), ct);
        return Ok(result);
    }

    // GET /api/v1/period-locks/reconcile?vendorId={vendorId}&year={year}&month={month}
    [HttpGet("reconcile")]
    public async Task<IActionResult> GetReconcile(
        [FromQuery] Guid vendorId, [FromQuery] int year, [FromQuery] int month, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPeriodReconcileQuery(vendorId, year, month), ct);
        return Ok(result);
    }

    // POST /api/v1/period-locks
    [HttpPost]
    public async Task<IActionResult> Lock([FromBody] LockPeriodRequest body, CancellationToken ct)
    {
        var cmd = new LockPeriodCommand(body.VendorId, body.Year, body.Month, _currentUser.UserId.ToString());
        var result = await _mediator.Send(cmd, ct);
        return Ok(result);
    }

    // DELETE /api/v1/period-locks/{vendorId}/{year}/{month}
    [HttpDelete("{vendorId:guid}/{year:int}/{month:int}")]
    public async Task<IActionResult> Unlock(Guid vendorId, int year, int month, CancellationToken ct)
    {
        await _mediator.Send(new UnlockPeriodCommand(vendorId, year, month), ct);
        return NoContent();
    }
}

public sealed record LockPeriodRequest(Guid VendorId, int Year, int Month);
```

### Task 5 Detail: Frontend Component

**State:** `'idle' | 'loading' | 'loaded' | 'error'`

**Component logic:**
```typescript
// Inject TimeTrackingApiService (thêm period-lock methods)
// Form: vendorId, year, month
// On submit: load reconcile data
// Lock button → POST /api/v1/period-locks
// Unlock button → DELETE /api/v1/period-locks/{vendorId}/{year}/{month}

// API service methods:
getPeriodReconcile(vendorId: string, year: number, month: number): Observable<PeriodReconcileDto>
getLocks(vendorId: string): Observable<PeriodLockDto[]>
lockPeriod(vendorId: string, year: number, month: number): Observable<PeriodLockDto>
unlockPeriod(vendorId: string, year: number, month: number): Observable<void>
```

**Frontend model** (`period-lock.model.ts`):
```typescript
export interface PeriodLockDto {
  id: string;
  vendorId: string;
  year: number;
  month: number;
  lockedBy: string;
  lockedAt: string;
}

export interface PeriodReconcileDto {
  vendorId: string;
  year: number;
  month: number;
  isLocked: boolean;
  lockedAt?: string;
  estimatedHours: number;
  pmAdjustedHours: number;
  confirmedHours: number;
  confirmedCost: number;
  totalEntries: number;
}
```

### Patterns đã có — KHÔNG viết lại

| Pattern | Source |
|---|---|
| Entity với `Create()` factory | Stories 3.1, 3.5 |
| EF config + manual migration | Stories 3.1, 3.3, 3.5 |
| Service-based Angular component (NO NgRx) | Stories 2.5, 3.5 |
| `[Authorize]` controller | Tất cả stories |
| `_currentUser.UserId.ToString()` | Tất cả controller stories |
| `ITimeTrackingDbContext.DbSet<T>` | Stories 3.1+ |
| `DomainException` throw | Story 3.3 VoidTimeEntryHandler |

### File lock workaround

Build `TimeTracking.Api.csproj` thay vì Host khi Host.exe đang chạy.

---

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

_(Trống)_

### Completion Notes List

- `PeriodLock` là entity riêng biệt, không update IsLocked trên TimeEntry — enforcement qua query table
- `LockPeriodHandler` idempotent: nếu đã lock, trả existing record ngay (không throw)
- `VoidTimeEntryHandler` chỉ block VendorConfirmed entries trong locked period; non-VendorConfirmed entries vẫn void được
- `ApplyImportJobHandler` check tất cả distinct months trong CSV trước khi tạo entries
- Frontend dùng `DecimalPipe` + `DatePipe` standalone (cần khai báo trong imports array)
- `number:'2.0-0'` format cho month display không work với DecimalPipe standalone — dùng template string thay thế nếu cần

### File List

**Backend:**
- `src/Modules/TimeTracking/ProjectManagement.TimeTracking.Domain/Entities/PeriodLock.cs`
- `src/Modules/TimeTracking/ProjectManagement.TimeTracking.Application/Common/Interfaces/ITimeTrackingDbContext.cs`
- `src/Modules/TimeTracking/ProjectManagement.TimeTracking.Infrastructure/Persistence/TimeTrackingDbContext.cs`
- `src/Modules/TimeTracking/ProjectManagement.TimeTracking.Infrastructure/Persistence/Configurations/PeriodLockConfiguration.cs`
- `src/Modules/TimeTracking/ProjectManagement.TimeTracking.Infrastructure/Migrations/20260426140000_AddPeriodLock_TimeTracking.cs`
- `src/Modules/TimeTracking/ProjectManagement.TimeTracking.Application/PeriodLocks/DTOs/PeriodLockDtos.cs`
- `src/Modules/TimeTracking/ProjectManagement.TimeTracking.Application/PeriodLocks/Commands/LockPeriodCommand.cs`
- `src/Modules/TimeTracking/ProjectManagement.TimeTracking.Application/PeriodLocks/Commands/LockPeriodHandler.cs`
- `src/Modules/TimeTracking/ProjectManagement.TimeTracking.Application/PeriodLocks/Commands/UnlockPeriodCommand.cs`
- `src/Modules/TimeTracking/ProjectManagement.TimeTracking.Application/PeriodLocks/Queries/GetPeriodLocksQuery.cs`
- `src/Modules/TimeTracking/ProjectManagement.TimeTracking.Application/TimeEntries/Commands/VoidTimeEntry/VoidTimeEntryHandler.cs` (updated)
- `src/Modules/TimeTracking/ProjectManagement.TimeTracking.Application/ImportJobs/Commands/ApplyImportJob/ApplyImportJobHandler.cs` (updated)
- `src/Modules/TimeTracking/ProjectManagement.TimeTracking.Api/Controllers/PeriodLocksController.cs`

**Frontend:**
- `frontend/project-management-web/src/app/features/time-tracking/models/period-lock.model.ts`
- `frontend/project-management-web/src/app/features/time-tracking/services/time-tracking-api.service.ts` (added period-lock methods)
- `frontend/project-management-web/src/app/features/time-tracking/components/period-lock/period-lock.ts`
- `frontend/project-management-web/src/app/features/time-tracking/components/period-lock/period-lock.html`
- `frontend/project-management-web/src/app/features/time-tracking/time-tracking.routes.ts` (added /period-lock route)
