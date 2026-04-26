# Story 3.2: TimeEntry List/Filter (Range, Resource, Project, Status) + Paging

Status: ready-for-dev

**Story ID:** 3.2
**Epic:** Epic 3 — TimeEntry & Timesheet (2-tier: Grid + Vendor Import) + Status/Lock + Corrections
**Sprint:** Sprint 4
**Date Created:** 2026-04-26

---

## Story

As a PM,
I want xem danh sách TimeEntry theo kỳ và filter,
So that tôi kiểm tra nhanh dữ liệu đã confirmed vs chưa confirmed.

## Acceptance Criteria

1. **Given** query theo range ngày/tuần/tháng
   **When** gọi `GET /api/v1/time-entries`
   **Then** hỗ trợ filter `dateFrom`, `dateTo` (inclusive), trả entries trong range đó

2. **Given** filter theo resource hoặc project
   **When** gọi `GET /api/v1/time-entries?resourceId=...&projectId=...`
   **Then** chỉ trả entries match đúng filter, có thể combine nhiều filter

3. **Given** filter theo status (entryType)
   **When** gọi `GET /api/v1/time-entries?entryType=PmAdjusted`
   **Then** chỉ trả entries có entryType khớp

4. **Given** paging
   **When** gọi với `page` và `pageSize`
   **Then** trả `PagedResult<TimeEntryDto>` với `totalCount`, `page`, `pageSize`, `items`

## Tasks / Subtasks

- [ ] **Task 1: Application Layer — GetTimeEntryListQuery (BE)**
  - [ ] 1.1 Tạo `PagedResult<T>` record (Shared hoặc TimeTracking.Application)
  - [ ] 1.2 Tạo `GetTimeEntryListQuery` + Handler

- [ ] **Task 2: API Layer — GET list endpoint (BE)**
  - [ ] 2.1 Thêm `GET /api/v1/time-entries` endpoint vào `TimeEntriesController`

- [ ] **Task 3: Frontend — load entries list (FE)**
  - [ ] 3.1 Cập nhật `time-tracking-api.service.ts`: thêm `getTimeEntries(filter)` method
  - [ ] 3.2 Cập nhật `time-tracking.effects.ts`: implement `loadEntries$` effect
  - [ ] 3.3 Cập nhật `time-entry-list` component: dispatch `loadEntries` với filter params, hiển thị danh sách

- [ ] **Task 4: Build verification**
  - [ ] 4.1 `dotnet build` → 0 errors
  - [ ] 4.2 `ng build` → 0 errors

---

## Dev Notes

### Module đã có — KHÔNG tạo lại

Story 3.1 đã tạo đầy đủ TimeTracking module. Story này CHỈ thêm:
- `GetTimeEntryListQuery` + handler
- GET list endpoint trong controller
- Frontend load effect

### Task 1 Detail: GetTimeEntryListQuery

**PagedResult<T>** — tạo trong `TimeTracking.Application.DTOs`:
```csharp
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize
)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
```

**GetTimeEntryListQuery:**
```csharp
public sealed record GetTimeEntryListQuery(
    DateOnly? DateFrom = null,
    DateOnly? DateTo = null,
    Guid? ResourceId = null,
    Guid? ProjectId = null,
    string? EntryType = null,
    int Page = 1,
    int PageSize = 50
) : IRequest<PagedResult<TimeEntryDto>>;
```

**GetTimeEntryListHandler** — dùng `AsNoTracking()` + `IQueryable` chaining:
```csharp
var q = _db.TimeEntries.AsNoTracking().AsQueryable();

if (query.DateFrom.HasValue) q = q.Where(e => e.Date >= query.DateFrom.Value);
if (query.DateTo.HasValue) q = q.Where(e => e.Date <= query.DateTo.Value);
if (query.ResourceId.HasValue) q = q.Where(e => e.ResourceId == query.ResourceId.Value);
if (query.ProjectId.HasValue) q = q.Where(e => e.ProjectId == query.ProjectId.Value);
if (!string.IsNullOrEmpty(query.EntryType)) q = q.Where(e => e.EntryType == query.EntryType);

var totalCount = await q.CountAsync(ct);
var pageSize = Math.Min(query.PageSize, 200);
var items = await q
    .OrderByDescending(e => e.Date)
    .ThenByDescending(e => e.CreatedAt)
    .Skip((query.Page - 1) * pageSize)
    .Take(pageSize)
    .Select(e => new TimeEntryDto(e.Id, e.ResourceId, e.ProjectId, e.TaskId,
        e.Date, e.Hours, e.EntryType, e.Note,
        e.RateAtTime, e.CostAtTime, e.EnteredBy, e.CreatedAt))
    .ToListAsync(ct);

return new PagedResult<TimeEntryDto>(items, totalCount, query.Page, pageSize);
```

### Task 2 Detail: GET list endpoint

```csharp
// Thêm vào TimeEntriesController
[HttpGet]
public async Task<IActionResult> GetTimeEntries(
    [FromQuery] DateOnly? dateFrom,
    [FromQuery] DateOnly? dateTo,
    [FromQuery] Guid? resourceId,
    [FromQuery] Guid? projectId,
    [FromQuery] string? entryType,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 50,
    CancellationToken ct = default)
{
    var result = await _mediator.Send(
        new GetTimeEntryListQuery(dateFrom, dateTo, resourceId, projectId, entryType, page, pageSize), ct);
    return Ok(result);
}
```

### Task 3 Detail: Frontend

**time-tracking-api.service.ts** — thêm method:
```typescript
getTimeEntries(filter?: {
  dateFrom?: string; dateTo?: string;
  resourceId?: string; projectId?: string;
  entryType?: string; page?: number; pageSize?: number;
}): Observable<PagedResult<TimeEntry>>
```

**PagedResult model:**
```typescript
export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}
```

**loadEntries$ effect** (đã khai báo action trong 3.1, chỉ cần implement):
```typescript
loadEntries$ = createEffect(() =>
  this.actions$.pipe(
    ofType(TimeTrackingActions.loadEntries),
    switchMap(action =>
      this.api.getTimeEntries({ projectId: action.projectId, resourceId: action.resourceId })
        .pipe(
          map(result => TimeTrackingActions.loadEntriesSuccess({ entries: result.items })),
          catchError(err => of(TimeTrackingActions.loadEntriesFailure({ error: err?.error?.detail ?? 'Lỗi' })))
        )
    )
  )
);
```

### Patterns đã có — KHÔNG viết lại

| Pattern | Source |
|---|---|
| `AsNoTracking()` + IQueryable chaining | Story 2.2 GetVendorList |
| `[Authorize]` controller | Story 2.1 |
| NgRx effects pattern | Story 2.1-2.4 |
| `createEffect` + `switchMap` | Time-tracking effects từ 3.1 |

---

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

_(Trống)_

### Completion Notes List

_(Trống)_

### File List

_(Trống — điền sau khi implement)_
