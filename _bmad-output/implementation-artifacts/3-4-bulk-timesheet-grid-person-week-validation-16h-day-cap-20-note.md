# Story 3.4: Bulk Timesheet Grid (person × week) + validation (16h/day cap, >20% note)

Status: review

**Story ID:** 3.4
**Epic:** Epic 3 — TimeEntry & Timesheet (2-tier: Grid + Vendor Import) + Status/Lock + Corrections
**Sprint:** Sprint 4
**Date Created:** 2026-04-26

---

## Story

As a PM,
I want nhập giờ theo dạng grid nhanh cho nhiều người/nhiều ngày,
So that tôi cập nhật vận hành giữa tháng mà không tốn công như Excel.

## Acceptance Criteria

1. **Given** PM nhập giờ trong grid
   **When** nhập >16h/ngày cho 1 người (tổng tất cả rows của resource đó trong ngày đó)
   **Then** bị chặn với lỗi inline và `400 ProblemDetails` khi submit

2. **Given** giờ pm-adjusted lệch >20% so với estimate hiện có cho cùng resource+project+date
   **When** submit mà không có note/reason cho row đó
   **Then** server trả `400` với danh sách warning items; frontend yêu cầu note trước khi lưu

3. **Given** grid có dữ liệu
   **When** API lỗi (network/5xx)
   **Then** không mất dữ liệu người dùng đã nhập, hiển thị trạng thái error với retry

4. **Given** user thao tác cell trong grid
   **When** nhấn Tab/Enter/Esc
   **Then** Tab/Enter chuyển sang cell tiếp theo; Esc clear cell hiện tại

---

## Tasks / Subtasks

- [x] **Task 1: Application Layer — BulkCreateTimeEntries command (BE)**
  - [x] 1.1 Tạo `BulkTimesheetRowDto` (input row)
  - [x] 1.2 Tạo `BulkCreateTimeEntriesCommand` + `BulkCreateResult`
  - [x] 1.3 Tạo `BulkCreateTimeEntriesHandler` với validation logic:
    - Hard: sum hours per (resourceId, date) > 16 → error
    - Soft: PmAdjusted >20% vs Estimated cho cùng resource+project+date + no note → warning
    - Nếu no errors: tạo tất cả entries trong 1 transaction

- [x] **Task 2: API Layer — bulk endpoint (BE)**
  - [x] 2.1 Thêm `POST /api/v1/time-entries/bulk` vào `TimeEntriesController`

- [x] **Task 3: Frontend — TimesheetGridComponent**
  - [x] 3.1 Tạo `timesheet-grid` component (standalone)
  - [x] 3.2 Grid layout: rows = resources, columns = ngày trong tuần đã chọn
  - [x] 3.3 State: loading/empty/error (retry button)
  - [x] 3.4 Keyboard: Tab/Enter → next cell; Esc → clear cell
  - [x] 3.5 Inline validation: đánh dấu cell lỗi >16h/day; warning >20%
  - [x] 3.6 Thêm NgRx actions + effect + reducer cho bulk submit

- [x] **Task 4: Routing — timesheet page**
  - [x] 4.1 Thêm route `/time-tracking/timesheet` → `TimesheetGridComponent`
  - [x] 4.2 Route lazy-loaded qua `time-tracking.routes.ts`

- [x] **Task 5: Build verification**
  - [x] 5.1 `dotnet build TimeTracking.Api.csproj` → 0 errors
  - [x] 5.2 `ng build` → 0 errors, 0 warnings

---

## Dev Notes

### Nguyên tắc thiết kế

- **16h/day cap là hard block** — server trả `400 ProblemDetails` với danh sách row errors; KHÔNG tạo bất kỳ entry nào (all-or-nothing per request)
- **>20% warning** — server trả `400` với `type: "validation_warning"` và danh sách warning rows; frontend phải thêm note cho các row đó rồi resubmit
- **All-or-nothing transaction** — nếu có bất kỳ hard error nào, không entry nào được tạo
- **Frontend data preservation** — khi API lỗi, form data phải giữ nguyên (không reset grid)
- **Keyboard navigation** — chỉ cần Tab/Enter/Esc (đủ theo acceptance criteria)

### Task 1 Detail: BulkCreateTimeEntries

**DTOs:**
```csharp
public sealed record BulkTimesheetRowDto(
    Guid ResourceId,
    Guid ProjectId,
    Guid? TaskId,
    DateOnly Date,
    decimal Hours,
    string EntryType,  // Estimated | PmAdjusted
    string Role,
    string Level,
    string? Note
);

public sealed record BulkCreateResult(
    bool Success,
    IReadOnlyList<TimeEntryDto> CreatedEntries,
    IReadOnlyList<BulkValidationError> Errors
);

public sealed record BulkValidationError(
    int RowIndex,
    string ErrorType,  // "hard" | "warning"
    string Message
);
```

**BulkCreateTimeEntriesCommand:**
```csharp
public sealed record BulkCreateTimeEntriesCommand(
    IReadOnlyList<BulkTimesheetRowDto> Rows,
    string EnteredBy
) : IRequest<BulkCreateResult>;
```

**BulkCreateTimeEntriesHandler — validation logic:**
```csharp
public async Task<BulkCreateResult> Handle(BulkCreateTimeEntriesCommand cmd, CancellationToken ct)
{
    var errors = new List<BulkValidationError>();

    // 1. Hard validation: 16h/day cap per (resourceId, date)
    var dayTotals = cmd.Rows
        .GroupBy(r => (r.ResourceId, r.Date))
        .ToDictionary(g => g.Key, g => g.Sum(r => r.Hours));
    
    for (int i = 0; i < cmd.Rows.Count; i++)
    {
        var row = cmd.Rows[i];
        var key = (row.ResourceId, row.Date);
        if (dayTotals[key] > 16)
        {
            errors.Add(new BulkValidationError(i, "hard",
                $"Tổng giờ ngày {row.Date:yyyy-MM-dd} cho resource {row.ResourceId} vượt 16h (= {dayTotals[key]}h)."));
        }
    }

    // 2. Soft warning: PmAdjusted >20% vs Estimated (check DB for existing estimates)
    if (!errors.Any(e => e.ErrorType == "hard"))
    {
        var pmRows = cmd.Rows
            .Select((r, i) => (Row: r, Index: i))
            .Where(x => x.Row.EntryType == "PmAdjusted" && string.IsNullOrWhiteSpace(x.Row.Note))
            .ToList();

        foreach (var (row, index) in pmRows)
        {
            var estimatedHours = await _db.TimeEntries.AsNoTracking()
                .Where(e => e.ResourceId == row.ResourceId
                    && e.ProjectId == row.ProjectId
                    && e.Date == row.Date
                    && e.EntryType == "Estimated"
                    && !e.IsVoided)
                .SumAsync(e => (decimal?)e.Hours, ct) ?? 0m;

            if (estimatedHours > 0 && Math.Abs(row.Hours - estimatedHours) / estimatedHours > 0.20m)
            {
                errors.Add(new BulkValidationError(index, "warning",
                    $"Row {index}: PmAdjusted {row.Hours}h lệch >20% so với Estimated {estimatedHours}h. Cần thêm Note."));
            }
        }
    }

    if (errors.Any())
        return new BulkCreateResult(false, [], errors);

    // 3. Create all entries
    var created = new List<TimeEntryDto>();
    foreach (var row in cmd.Rows)
    {
        var hourlyRate = await _rateService.GetHourlyRateAsync(
            row.ResourceId, row.Role, row.Level, row.Date, ct);
        var entry = TimeEntry.Create(
            row.ResourceId, row.ProjectId, row.TaskId,
            row.Date, row.Hours, row.EntryType,
            row.Note, hourlyRate, cmd.EnteredBy);
        _db.TimeEntries.Add(entry);
        created.Add(CreateTimeEntryHandler.ToDto(entry));
    }
    await _db.SaveChangesAsync(ct);
    return new BulkCreateResult(true, created, []);
}
```

### Task 2 Detail: Bulk endpoint

```csharp
// TimeEntriesController:
[HttpPost("bulk")]
public async Task<IActionResult> BulkCreateTimeEntries(
    [FromBody] BulkTimesheetRequest body,
    CancellationToken ct)
{
    var cmd = new BulkCreateTimeEntriesCommand(
        body.Rows.Select(r => new BulkTimesheetRowDto(
            r.ResourceId, r.ProjectId, r.TaskId,
            r.Date, r.Hours, r.EntryType,
            r.Role, r.Level, r.Note)).ToList().AsReadOnly(),
        _currentUser.UserId.ToString());

    var result = await _mediator.Send(cmd, ct);

    if (!result.Success)
        return BadRequest(new { errors = result.Errors });

    return Ok(result.CreatedEntries);
}

// Request record:
public sealed record BulkTimesheetRequest(IReadOnlyList<BulkTimesheetRowRequest> Rows);
public sealed record BulkTimesheetRowRequest(
    Guid ResourceId, Guid ProjectId, Guid? TaskId,
    DateOnly Date, decimal Hours, string EntryType,
    string Role, string Level, string? Note);
```

### Task 3 Detail: Frontend TimesheetGridComponent

**NgRx actions — thêm vào `time-tracking.actions.ts`:**
```typescript
'Submit Bulk': props<{ rows: BulkTimesheetRow[] }>(),
'Submit Bulk Success': props<{ entries: TimeEntry[] }>(),
'Submit Bulk Failure': props<{ error: string; validationErrors?: BulkValidationError[] }>(),
```

**BulkTimesheetRow model** — tạo `models/bulk-timesheet.model.ts`:
```typescript
export interface BulkTimesheetRow {
  resourceId: string;
  projectId: string;
  taskId?: string;
  date: string;  // ISO date string YYYY-MM-DD
  hours: number;
  entryType: string;
  role: string;
  level: string;
  note?: string;
}

export interface BulkValidationError {
  rowIndex: number;
  errorType: 'hard' | 'warning';
  message: string;
}
```

**TimesheetGridComponent** — key behaviors:
- State management: `gridState: 'idle' | 'loading' | 'error'`; lưu data vào FormArray khi error (không reset)
- Grid: rows = danh sách resources (hardcode hoặc từ store); columns = 7 ngày của tuần đang chọn
- Inline error: `[class.cell-error]="hasError(rowIdx, colIdx)"` dùng ValidationError list
- Keyboard: `(keydown.Tab)`, `(keydown.Enter)`, `(keydown.Escape)` handlers

**Không cần EF migration** — không có domain/DB thay đổi ở task này.

### Patterns đã có — KHÔNG viết lại

| Pattern | Source |
|---|---|
| `ITimeTrackingRateService.GetHourlyRateAsync` | Story 3.1 |
| `CreateTimeEntryHandler.ToDto(entry)` | Story 3.1 (internal static) |
| `[Authorize]` controller + `_currentUser` | Story 3.1 |
| NgRx actions `createActionGroup` | Story 3.1 |
| `switchMap` + `catchError` effect pattern | Story 3.2 |
| `.AsNoTracking()` + LINQ queries | Story 3.2 |

### File lock workaround

Build `TimeTracking.Api.csproj` trực tiếp nếu Host.exe đang chạy.

---

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

_(Trống)_

### Completion Notes List

- BulkCreateTimeEntriesHandler: upfront EntryType + Hours validation, then 16h/day cap (hard), then >20% deviation vs DB estimates (soft warning) — all-or-nothing per request
- Handler reuses `CreateTimeEntryHandler.ToDto(entry)` (internal static) for DTO mapping
- Frontend grid uses ReactiveFormsModule FormArray-of-FormArrays (rows × cells); state preserved on API error via `gridState`
- Keyboard: Tab/Enter focus next cell by DOM id `cell-{row}-{col}`; Esc clears via setValue(null)
- `submitBulk$` effect catches 400 errors and passes `err?.error?.errors` as `validationErrors` to action
- Resources in grid are currently demo stubs — will connect to real resource store in Epic 4+ work
- dotnet build: 0 errors; ng build: 0 errors, 0 warnings

### File List

- src/Modules/TimeTracking/ProjectManagement.TimeTracking.Application/TimeEntries/Commands/BulkCreateTimeEntries/BulkCreateTimeEntriesCommand.cs (new)
- src/Modules/TimeTracking/ProjectManagement.TimeTracking.Application/TimeEntries/Commands/BulkCreateTimeEntries/BulkCreateTimeEntriesHandler.cs (new)
- src/Modules/TimeTracking/ProjectManagement.TimeTracking.Api/Controllers/TimeEntriesController.cs
- frontend/project-management-web/src/app/features/time-tracking/models/bulk-timesheet.model.ts (new)
- frontend/project-management-web/src/app/features/time-tracking/services/time-tracking-api.service.ts
- frontend/project-management-web/src/app/features/time-tracking/store/time-tracking.actions.ts
- frontend/project-management-web/src/app/features/time-tracking/store/time-tracking.effects.ts
- frontend/project-management-web/src/app/features/time-tracking/store/time-tracking.reducer.ts
- frontend/project-management-web/src/app/features/time-tracking/components/timesheet-grid/timesheet-grid.ts (new)
- frontend/project-management-web/src/app/features/time-tracking/components/timesheet-grid/timesheet-grid.html (new)
- frontend/project-management-web/src/app/features/time-tracking/time-tracking.routes.ts
