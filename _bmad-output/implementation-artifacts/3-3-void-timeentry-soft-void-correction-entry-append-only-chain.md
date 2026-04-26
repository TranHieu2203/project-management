# Story 3.3: Void TimeEntry (soft void) + Correction entry (append-only chain)

Status: review

**Story ID:** 3.3
**Epic:** Epic 3 — TimeEntry & Timesheet (2-tier: Grid + Vendor Import) + Status/Lock + Corrections
**Sprint:** Sprint 4
**Date Created:** 2026-04-26

---

## Story

As a PM,
I want sửa sai giờ công mà không overwrite lịch sử,
So that audit trail bất biến vẫn đúng và dữ liệu có thể đối soát.

## Acceptance Criteria

1. **Given** TimeEntry tồn tại
   **When** user void entry với reason
   **Then** entry chuyển sang `IsVoided = true` (soft void) và vẫn giữ trong lịch sử
   **And** bắt buộc có `VoidReason` và `VoidedBy` + `VoidedAt`; không hard-delete

2. **Given** entry đã bị void
   **When** user cố void lại
   **Then** trả `409 ProblemDetails` ("Entry đã bị void")

3. **Given** cần chỉnh số giờ
   **When** user tạo correction entry (POST với `supersededEntryId`)
   **Then** tạo TimeEntry mới liên kết entry gốc qua `SupersedesId`, không UPDATE record cũ
   **And** correction phải có `Note` (reason/description) bắt buộc

4. **Given** list entries
   **When** gọi `GET /api/v1/time-entries`
   **Then** response bao gồm `isVoided`, `voidReason`, `voidedBy`, `voidedAt`, `supersedesId` cho mỗi entry

---

## Tasks / Subtasks

- [x] **Task 1: Domain — cập nhật TimeEntry entity**
  - [x] 1.1 Thêm fields: `IsVoided`, `VoidReason`, `VoidedBy`, `VoidedAt`, `SupersedesId`
  - [x] 1.2 Thêm method `Void(string reason, string voidedBy)` trên entity
  - [x] 1.3 Cập nhật `TimeEntry.Create(...)` nhận thêm `Guid? supersededEntryId`

- [x] **Task 2: Application Layer — VoidTimeEntry command**
  - [x] 2.1 Tạo `VoidTimeEntryCommand(Guid EntryId, string Reason, string VoidedBy)`
  - [x] 2.2 Tạo `VoidTimeEntryHandler`
  - [x] 2.3 Cập nhật `TimeEntryDto` thêm void fields + `SupersedesId`
  - [x] 2.4 Cập nhật `CreateTimeEntryCommand` thêm `Guid? SupersedesEntryId`
  - [x] 2.5 Cập nhật `CreateTimeEntryHandler` + `ToDto` để include void fields

- [x] **Task 3: Infrastructure — EF Migration**
  - [x] 3.1 Cập nhật `TimeEntryConfiguration` map các fields mới
  - [x] 3.2 Tạo EF migration `AddVoidAndCorrection_TimeTracking` (manual, Host locked)

- [x] **Task 4: API Layer — void endpoint**
  - [x] 4.1 Thêm `POST /api/v1/time-entries/{entryId}/void` vào `TimeEntriesController`
  - [x] 4.2 Cập nhật `CreateTimeEntryRequest` body thêm `Guid? SupersedesEntryId`

- [x] **Task 5: Frontend**
  - [x] 5.1 Cập nhật `TimeEntry` model thêm void fields + `supersedesId`
  - [x] 5.2 Thêm NgRx actions: `voidEntry`, `voidEntrySuccess`, `voidEntryFailure`
  - [x] 5.3 Thêm `voidTimeEntry(entryId, reason)` trong `TimeTrackingApiService`
  - [x] 5.4 Thêm `voidEntry$` effect
  - [x] 5.5 Cập nhật reducer để mark entry voided sau success
  - [x] 5.6 Thêm "Void" button trong `time-entry-list` component (dialog confirm + reason input)
  - [x] 5.7 Cập nhật `time-entry-form` nhận `supersededEntryId` optional

- [x] **Task 6: Build verification**
  - [x] 6.1 `dotnet build TimeTracking.Api.csproj` → 0 errors
  - [x] 6.2 `ng build` → 0 errors, 0 warnings

---

## Dev Notes

### Nguyên tắc thiết kế — CRITICAL

- **TimeEntry "immutable" = không được UPDATE hours/date/rate** — void là meta-operation thêm trạng thái, không phải thay đổi dữ liệu gốc
- **Void = UPDATE duy nhất cho phép**: chỉ set `is_voided`, `void_reason`, `voided_by`, `void_at` — không thay đổi `hours`, `rate_at_time`, `cost_at_time`
- **Correction = new TimeEntry** với `supersedes_id` trỏ về entry gốc — hoàn toàn INSERT, không UPDATE
- **Không hard-delete** — voided entry vẫn trả trong list, client filter by `isVoided` nếu cần

### Task 1 Detail: TimeEntry entity

```csharp
// Thêm fields:
public bool IsVoided { get; private set; }
public string? VoidReason { get; private set; }
public string? VoidedBy { get; private set; }
public DateTime? VoidedAt { get; private set; }
public Guid? SupersedesId { get; private set; }  // correction entry → original entry

// Thêm method:
public void Void(string reason, string voidedBy)
{
    if (IsVoided)
        throw new DomainException("Entry đã bị void.");
    IsVoided = true;
    VoidReason = reason;
    VoidedBy = voidedBy;
    VoidedAt = DateTime.UtcNow;
}

// Cập nhật Create — thêm param supersededEntryId:
public static TimeEntry Create(
    Guid resourceId, Guid projectId, Guid? taskId,
    DateOnly date, decimal hours, string entryType,
    string? note, decimal rateAtTime, string enteredBy,
    Guid? supersededEntryId = null)
    => new()
    {
        // ... existing fields ...
        SupersedesId = supersededEntryId,
        IsVoided = false,
    };
```

### Task 2 Detail: VoidTimeEntry

**VoidTimeEntryCommand:**
```csharp
public sealed record VoidTimeEntryCommand(
    Guid EntryId,
    string Reason,
    string VoidedBy
) : IRequest<TimeEntryDto>;
```

**VoidTimeEntryHandler:**
```csharp
public async Task<TimeEntryDto> Handle(VoidTimeEntryCommand cmd, CancellationToken ct)
{
    var entry = await _db.TimeEntries.FindAsync([cmd.EntryId], ct)
        ?? throw new NotFoundException($"TimeEntry {cmd.EntryId} không tồn tại.");
    
    entry.Void(cmd.Reason, cmd.VoidedBy);  // throws DomainException if already voided
    await _db.SaveChangesAsync(ct);
    return ToDto(entry);
}
```

**TimeEntryDto — cập nhật thêm fields (phải giữ backward compat với handler hiện tại):**
```csharp
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
    DateTime CreatedAt,
    bool IsVoided,
    string? VoidReason,
    string? VoidedBy,
    DateTime? VoidedAt,
    Guid? SupersedesId
);
```

**Cập nhật ToDto** trong `CreateTimeEntryHandler` và mọi `.Select(e => new TimeEntryDto(...))` — thêm các void fields và SupersedesId.

**CreateTimeEntryCommand — thêm param:**
```csharp
public sealed record CreateTimeEntryCommand(
    Guid ResourceId, Guid ProjectId, Guid? TaskId,
    DateOnly Date, decimal Hours, string EntryType,
    string Role, string Level, string? Note, string EnteredBy,
    Guid? SupersedesEntryId = null   // ← NEW, optional
) : IRequest<TimeEntryDto>;
```

**CreateTimeEntryHandler — khi correction, validate SupersedesEntryId tồn tại:**
```csharp
if (cmd.SupersedesEntryId.HasValue)
{
    var original = await _db.TimeEntries.FindAsync([cmd.SupersedesEntryId.Value], ct)
        ?? throw new NotFoundException($"Entry gốc {cmd.SupersedesEntryId.Value} không tồn tại.");
    // correction không bắt buộc phải void entry gốc trước — nhưng yêu cầu Note có giá trị
    if (string.IsNullOrWhiteSpace(cmd.Note))
        throw new DomainException("Correction entry bắt buộc phải có Note (reason).");
}
```

### Task 3 Detail: EF Configuration + Migration

**TimeEntryConfiguration — thêm:**
```csharp
builder.Property(e => e.IsVoided).HasColumnName("is_voided").HasDefaultValue(false);
builder.Property(e => e.VoidReason).HasColumnName("void_reason").HasMaxLength(500);
builder.Property(e => e.VoidedBy).HasColumnName("voided_by").HasMaxLength(100);
builder.Property(e => e.VoidedAt).HasColumnName("voided_at");
builder.Property(e => e.SupersedesId).HasColumnName("supersedes_id");
builder.HasIndex(e => e.SupersedesId).HasDatabaseName("ix_time_entries_supersedes_id");
```

**EF Migration — chạy từ thư mục TimeTracking.Infrastructure:**
```
dotnet ef migrations add AddVoidAndCorrection_TimeTracking --context TimeTrackingDbContext --project src/Modules/TimeTracking/ProjectManagement.TimeTracking.Infrastructure --startup-project src/Host/ProjectManagement.Host
```

### Task 4 Detail: API Endpoints

```csharp
// Thêm vào TimeEntriesController:
[HttpPost("{entryId:guid}/void")]
public async Task<IActionResult> VoidTimeEntry(
    Guid entryId,
    [FromBody] VoidTimeEntryRequest body,
    CancellationToken ct)
{
    var cmd = new VoidTimeEntryCommand(entryId, body.Reason, _currentUser.UserId.ToString());
    var result = await _mediator.Send(cmd, ct);
    return Ok(result);
}

// Thêm request record:
public sealed record VoidTimeEntryRequest(string Reason);

// Cập nhật CreateTimeEntryRequest — thêm field optional:
public sealed record CreateTimeEntryRequest(
    Guid ResourceId, Guid ProjectId, Guid? TaskId,
    DateOnly Date, decimal Hours, string EntryType,
    string Role, string Level, string? Note,
    Guid? SupersedesEntryId = null   // ← NEW
);
```

### Task 5 Detail: Frontend

**time-entry.model.ts — cập nhật:**
```typescript
export interface TimeEntry {
  id: string;
  resourceId: string;
  projectId: string;
  taskId?: string;
  date: string;
  hours: number;
  entryType: string;
  note?: string;
  rateAtTime: number;
  costAtTime: number;
  enteredBy: string;
  createdAt: string;
  // NEW void fields:
  isVoided: boolean;
  voidReason?: string;
  voidedBy?: string;
  voidedAt?: string;
  supersedesId?: string;
}
```

**NgRx actions — thêm vào `time-tracking.actions.ts`:**
```typescript
voidEntry: props<{ entryId: string; reason: string }>(),
voidEntrySuccess: props<{ entry: TimeEntry }>(),
voidEntryFailure: props<{ error: string }>(),
```

**time-tracking-api.service.ts — thêm:**
```typescript
voidTimeEntry(entryId: string, reason: string): Observable<TimeEntry> {
  return this.http.post<TimeEntry>(`${this.baseUrl}/${entryId}/void`, { reason });
}

createTimeEntry(body: CreateTimeEntryRequest): Observable<TimeEntry>
// Cập nhật CreateTimeEntryRequest interface thêm: supersedesEntryId?: string
```

**time-tracking.effects.ts — thêm:**
```typescript
voidEntry$ = createEffect(() =>
  this.actions$.pipe(
    ofType(TimeTrackingActions.voidEntry),
    switchMap(action =>
      this.api.voidTimeEntry(action.entryId, action.reason).pipe(
        map(entry => TimeTrackingActions.voidEntrySuccess({ entry })),
        catchError(err =>
          of(TimeTrackingActions.voidEntryFailure({
            error: err?.error?.detail ?? err?.message ?? 'Lỗi không xác định',
          }))
        )
      )
    )
  )
);
```

**Reducer — xử lý voidEntrySuccess** (cập nhật entry trong adapter):
```typescript
on(TimeTrackingActions.voidEntrySuccess, (state, { entry }) =>
  adapter.updateOne({ id: entry.id, changes: entry }, state)
),
```

**time-entry-list component** — thêm "Void" button:
- Dialog confirm hiển thị entry info + input reason (required)
- Dispatch `TimeTrackingActions.voidEntry({ entryId, reason })`
- Hiển thị icon/badge cho voided entries

### Patterns đã có — KHÔNG viết lại

| Pattern | Source |
|---|---|
| `NotFoundException` | `ProjectManagement.Shared.Domain.Exceptions` (đã dùng Story 3.1) |
| `DomainException` | `ProjectManagement.Shared.Domain.Exceptions` |
| NgRx `adapter.updateOne` | time-tracking reducer từ Story 3.1 |
| `_currentUser.UserId.ToString()` | Controller pattern từ Story 2.5 |
| `[HttpPost("{id:guid}/void")]` | Route pattern từ tất cả controller hiện tại |

### File lock workaround

Nếu Host.exe đang chạy, build sẽ gặp MSB3027. Dùng:
```
dotnet build src/Modules/TimeTracking/ProjectManagement.TimeTracking.Api/ProjectManagement.TimeTracking.Api.csproj
```

---

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

_(Trống)_

### Completion Notes List

- Domain: TimeEntry.Void() method updates only void meta-fields (IsVoided/VoidReason/VoidedBy/VoidedAt); original hours/rate immutable
- Correction chain: TimeEntry.Create() accepts optional SupersedesId; CreateTimeEntryHandler validates original exists and Note required
- EF migration created manually (20260426120000) due to Host.exe file lock; ModelSnapshot updated to match
- VoidTimeEntryHandler reuses CreateTimeEntryHandler.ToDto() (internal static method)
- GetTimeEntryListHandler Select projection updated to include all void fields and SupersedesId
- Frontend: voidEntry action/effect/reducer; time-entry-list shows VOIDED badge + block button; time-entry-form supports supersededEntryId pass-through
- dotnet build: 0 errors; ng build: 0 errors, 0 warnings

### File List

- src/Modules/TimeTracking/ProjectManagement.TimeTracking.Domain/Entities/TimeEntry.cs
- src/Modules/TimeTracking/ProjectManagement.TimeTracking.Application/DTOs/TimeEntryDto.cs
- src/Modules/TimeTracking/ProjectManagement.TimeTracking.Application/TimeEntries/Commands/CreateTimeEntry/CreateTimeEntryCommand.cs
- src/Modules/TimeTracking/ProjectManagement.TimeTracking.Application/TimeEntries/Commands/CreateTimeEntry/CreateTimeEntryHandler.cs
- src/Modules/TimeTracking/ProjectManagement.TimeTracking.Application/TimeEntries/Commands/VoidTimeEntry/VoidTimeEntryCommand.cs (new)
- src/Modules/TimeTracking/ProjectManagement.TimeTracking.Application/TimeEntries/Commands/VoidTimeEntry/VoidTimeEntryHandler.cs (new)
- src/Modules/TimeTracking/ProjectManagement.TimeTracking.Application/TimeEntries/Queries/GetTimeEntryList/GetTimeEntryListHandler.cs
- src/Modules/TimeTracking/ProjectManagement.TimeTracking.Infrastructure/Persistence/Configurations/TimeEntryConfiguration.cs
- src/Modules/TimeTracking/ProjectManagement.TimeTracking.Infrastructure/Migrations/20260426120000_AddVoidAndCorrection_TimeTracking.cs (new)
- src/Modules/TimeTracking/ProjectManagement.TimeTracking.Infrastructure/Migrations/20260426120000_AddVoidAndCorrection_TimeTracking.Designer.cs (new)
- src/Modules/TimeTracking/ProjectManagement.TimeTracking.Infrastructure/Migrations/TimeTrackingDbContextModelSnapshot.cs
- src/Modules/TimeTracking/ProjectManagement.TimeTracking.Api/Controllers/TimeEntriesController.cs
- frontend/project-management-web/src/app/features/time-tracking/models/time-entry.model.ts
- frontend/project-management-web/src/app/features/time-tracking/services/time-tracking-api.service.ts
- frontend/project-management-web/src/app/features/time-tracking/store/time-tracking.actions.ts
- frontend/project-management-web/src/app/features/time-tracking/store/time-tracking.effects.ts
- frontend/project-management-web/src/app/features/time-tracking/store/time-tracking.reducer.ts
- frontend/project-management-web/src/app/features/time-tracking/components/time-entry-list/time-entry-list.ts
- frontend/project-management-web/src/app/features/time-tracking/components/time-entry-list/time-entry-list.html
- frontend/project-management-web/src/app/features/time-tracking/components/time-entry-form/time-entry-form.ts
