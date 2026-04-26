# Story 6.2: Cost reporting APIs (vendor/project/resource/month) + filters

Status: review

**Story ID:** 6.2
**Epic:** Epic 6 — Cost Tracking & Official Reporting (Confirmed vs Estimated) + Export
**Sprint:** Sprint 7
**Date Created:** 2026-04-26

---

## Story

As a PM,
I want xem báo cáo chi phí theo nhiều chiều và filter,
So that tôi tổng hợp nhanh thay vì Excel.

## Acceptance Criteria

1. **Given** filter tùy chọn: `month` (YYYY-MM), `vendorId`, `projectId`, `resourceId`, `groupBy` (vendor|project|resource|month)
   **When** gọi `GET /api/v1/reports/cost/breakdown`
   **Then** trả về data nhóm theo chiều đã chọn với `estimatedCost`, `officialCost`, `confirmedPct`, `totalHours`
   **And** kết quả chỉ chứa data từ projects user là member (membership-scope)

2. **Given** paging params `page`, `pageSize` (max 200)
   **When** số lượng rows lớn
   **Then** trả `totalCount`, `page`, `pageSize` chuẩn + items cho trang đó

3. **Given** params không hợp lệ (groupBy không thuộc enum, pageSize > 200)
   **When** gọi endpoint
   **Then** trả `ProblemDetails` 400 theo chuẩn .NET (không throw 500)

4. **Given** UI breakdown view
   **When** render cost-breakdown component
   **Then** hiển thị dimension selector (vendor/project/resource/month) + optional filters + paginated table

---

## Tasks / Subtasks

- [x] **Task 1: Mở rộng Reporting.Application.csproj**
  - [x] 1.1 Thêm reference `ProjectManagement.Workforce.Application` vào `Reporting.Application.csproj` (cần cho IWorkforceDbContext)

- [x] **Task 2: GetCostBreakdownQuery**
  - [x] 2.1 Tạo `Queries/GetCostBreakdown/GetCostBreakdownQuery.cs` + handler
  - [x] 2.2 Validate `groupBy` — nếu không thuộc ["vendor","project","resource","month"] throw `ArgumentException` (→ 400)
  - [x] 2.3 Membership-scope: lấy projectIds từ `IProjectsDbContext.ProjectMemberships` (CurrentUserId)
  - [x] 2.4 Query TimeEntries (cross-module) + join Resources (IWorkforceDbContext) cho ResourceName/VendorId
  - [x] 2.5 Group by dimension + tính estimatedCost/officialCost/confirmedPct/totalHours
  - [x] 2.6 Apply paging (page/pageSize, max 200)

- [x] **Task 3: ReportingController — thêm endpoint**
  - [x] 3.1 Thêm `GET /api/v1/reports/cost/breakdown` vào `ReportingController.cs`

- [x] **Task 4: Frontend — models + service**
  - [x] 4.1 Thêm `CostBreakdownItem`, `CostBreakdownResult` vào `cost-report.model.ts`
  - [x] 4.2 Thêm `getCostBreakdown(...)` vào `reporting-api.service.ts`

- [x] **Task 5: Frontend — NgRx store mở rộng**
  - [x] 5.1 Thêm 3 actions: `loadCostBreakdown`, `loadCostBreakdownSuccess`, `loadCostBreakdownFailure` vào `reporting.actions.ts`
  - [x] 5.2 Mở rộng `ReportingState` + reducer: `costBreakdown: CostBreakdownResult | null`, `breakdownLoading: boolean`
  - [x] 5.3 Thêm effect `loadCostBreakdown$` vào `reporting.effects.ts`

- [x] **Task 6: Frontend — CostBreakdownComponent**
  - [x] 6.1 Tạo `features/reporting/components/cost-breakdown/cost-breakdown.ts` + `cost-breakdown.html`
  - [x] 6.2 Thêm route `{ path: 'breakdown', ... }` vào `reporting.routes.ts`

- [x] **Task 7: Build verification**
  - [x] 7.1 `dotnet build` → 0 errors (10 pre-existing MSB3277 warnings only)
  - [x] 7.2 `ng build` → 0 errors

---

## Dev Notes

### Story 6-1 đã tạo Reporting module — 6-2 mở rộng, không tạo module mới

Module structure đã có sẵn. Story 6-2 chỉ:
1. Thêm 1 cross-module reference (Workforce)
2. Thêm 1 query file mới
3. Thêm 1 endpoint trong ReportingController (đã có sẵn)
4. Mở rộng FE store + thêm component mới

---

### Task 1 — Cập nhật Reporting.Application.csproj

**File:** `src/Modules/Reporting/ProjectManagement.Reporting.Application/ProjectManagement.Reporting.Application.csproj`

Thêm vào `<ItemGroup>` project references:
```xml
<!-- Cross-module: read Workforce resources/vendors for display names in cost breakdown -->
<ProjectReference Include="..\..\Workforce\ProjectManagement.Workforce.Application\ProjectManagement.Workforce.Application.csproj" />
```

---

### Task 2 — GetCostBreakdownQuery

**File:** `src/Modules/Reporting/ProjectManagement.Reporting.Application/Queries/GetCostBreakdown/GetCostBreakdownQuery.cs`

**GroupBy values (string, case-insensitive):** `"vendor"`, `"project"`, `"resource"`, `"month"`

**`CostBreakdownItem`** — flexible DTO, nullable fields ngoài dimension key:
```csharp
public sealed record CostBreakdownItem(
    string DimensionKey,     // primary grouping identifier
    string DimensionLabel,   // display name
    string? VendorId,
    string? VendorName,
    string? ResourceId,
    string? ResourceName,
    string? ProjectId,
    string? Month,           // "YYYY-MM" khi groupBy=month
    decimal EstimatedCost,
    decimal OfficialCost,
    decimal ConfirmedPct,
    decimal TotalHours);

public sealed record CostBreakdownResult(
    string GroupBy,
    int TotalCount,
    int Page,
    int PageSize,
    IReadOnlyList<CostBreakdownItem> Items);

public sealed record GetCostBreakdownQuery(
    Guid CurrentUserId,
    string GroupBy,          // "vendor"|"project"|"resource"|"month"
    string? Month,           // "YYYY-MM" — optional filter
    Guid? VendorId,
    Guid? ProjectId,
    Guid? ResourceId,
    int Page = 1,
    int PageSize = 50)
    : IRequest<CostBreakdownResult>;
```

**Handler logic:**

```csharp
public async Task<CostBreakdownResult> Handle(GetCostBreakdownQuery query, CancellationToken ct)
{
    var groupBy = query.GroupBy.ToLowerInvariant();
    if (!new[] { "vendor", "project", "resource", "month" }.Contains(groupBy))
        throw new ArgumentException($"GroupBy '{query.GroupBy}' không hợp lệ. Chấp nhận: vendor, project, resource, month.");

    var pageSize = Math.Min(Math.Max(query.PageSize, 1), 200);
    var page = Math.Max(query.Page, 1);

    // 1. Membership-scope
    var projectIds = await _projectsDb.ProjectMemberships
        .Where(m => m.UserId == query.CurrentUserId)
        .Select(m => m.ProjectId).Distinct().ToListAsync(ct);

    if (query.ProjectId.HasValue)
    {
        if (!projectIds.Contains(query.ProjectId.Value)) return EmptyResult(query, groupBy, pageSize);
        projectIds = [query.ProjectId.Value];
    }
    if (projectIds.Count == 0) return EmptyResult(query, groupBy, pageSize);

    // 2. Parse month filter → DateOnly range
    DateOnly? dateFrom = null, dateTo = null;
    if (!string.IsNullOrWhiteSpace(query.Month) &&
        DateOnly.TryParseExact(query.Month, "yyyy-MM", out var parsedMonth))
    {
        dateFrom = parsedMonth;
        dateTo = parsedMonth.AddMonths(1).AddDays(-1);
    }

    // 3. Query TimeEntries + join Resources (in-memory join after separate fetches)
    var entryQuery = _timeTrackingDb.TimeEntries.AsNoTracking()
        .Where(e => projectIds.Contains(e.ProjectId) && !e.IsVoided);

    if (dateFrom.HasValue) entryQuery = entryQuery.Where(e => e.Date >= dateFrom.Value);
    if (dateTo.HasValue)   entryQuery = entryQuery.Where(e => e.Date <= dateTo.Value);
    if (query.ResourceId.HasValue) entryQuery = entryQuery.Where(e => e.ResourceId == query.ResourceId.Value);

    var entries = await entryQuery
        .Select(e => new { e.ResourceId, e.ProjectId, e.Date, e.EntryType, e.CostAtTime, e.Hours })
        .ToListAsync(ct);

    // 4. Resolve vendor filter if set: get all resourceIds under that vendor
    HashSet<Guid>? vendorResourceIds = null;
    if (query.VendorId.HasValue)
    {
        var rids = await _workforceDb.Resources.AsNoTracking()
            .Where(r => r.VendorId == query.VendorId.Value)
            .Select(r => r.Id).ToListAsync(ct);
        vendorResourceIds = rids.ToHashSet();
        entries = entries.Where(e => vendorResourceIds.Contains(e.ResourceId)).ToList();
    }

    // 5. Load resource map (Id → Name, VendorId) for labelling
    var resourceIds = entries.Select(e => e.ResourceId).Distinct().ToList();
    var resources = await _workforceDb.Resources.AsNoTracking()
        .Where(r => resourceIds.Contains(r.Id))
        .Select(r => new { r.Id, r.Name, r.VendorId })
        .ToListAsync(ct);
    var resourceMap = resources.ToDictionary(r => r.Id);

    // 6. Load vendor map
    var vendorIds = resources.Where(r => r.VendorId.HasValue).Select(r => r.VendorId!.Value).Distinct().ToList();
    var vendors = await _workforceDb.Vendors.AsNoTracking()
        .Where(v => vendorIds.Contains(v.Id))
        .Select(v => new { v.Id, v.Name })
        .ToListAsync(ct);
    var vendorMap = vendors.ToDictionary(v => v.Id);

    // 7. Helper: build item per group
    CostBreakdownItem BuildItem(
        string dimKey, string dimLabel,
        string? vendorId, string? vendorName,
        string? resourceId, string? resourceName,
        string? projectId, string? month,
        IEnumerable<dynamic> group)
    {
        var list = group.ToList();
        var estimated  = list.Where(e => e.EntryType == "Estimated").Sum(e => (decimal)e.CostAtTime);
        var pmAdj      = list.Where(e => e.EntryType == "PmAdjusted").Sum(e => (decimal)e.CostAtTime);
        var vendorConf = list.Where(e => e.EntryType == "VendorConfirmed").Sum(e => (decimal)e.CostAtTime);
        var official   = pmAdj + vendorConf;
        var total      = official + estimated;
        var pct        = total == 0m ? 0m : Math.Round(official / total * 100m, 1);
        var hours      = list.Sum(e => (decimal)e.Hours);
        return new CostBreakdownItem(dimKey, dimLabel, vendorId, vendorName, resourceId, resourceName, projectId, month,
            estimated, official, pct, hours);
    }

    // 8. Group and build items
    List<CostBreakdownItem> allItems = groupBy switch
    {
        "vendor" => entries
            .GroupBy(e => resourceMap.TryGetValue(e.ResourceId, out var r) && r.VendorId.HasValue ? r.VendorId!.Value.ToString() : "__inhouse__")
            .Select(g =>
            {
                var vid = g.Key == "__inhouse__" ? (Guid?)null : Guid.Parse(g.Key);
                var vname = vid.HasValue && vendorMap.TryGetValue(vid.Value, out var v) ? v.Name : "Inhouse";
                return BuildItem(g.Key, vname, g.Key == "__inhouse__" ? null : g.Key, vname, null, null, null, null, g);
            }).ToList(),

        "project" => entries
            .GroupBy(e => e.ProjectId.ToString())
            .Select(g => BuildItem(g.Key, g.Key, null, null, null, null, g.Key, null, g))
            .ToList(),

        "resource" => entries
            .GroupBy(e => e.ResourceId.ToString())
            .Select(g =>
            {
                var rid = Guid.Parse(g.Key);
                var rName = resourceMap.TryGetValue(rid, out var r) ? r.Name : g.Key;
                var vId = resourceMap.TryGetValue(rid, out var r2) && r2.VendorId.HasValue ? r2.VendorId!.Value.ToString() : null;
                var vName = vId != null && vendorMap.TryGetValue(Guid.Parse(vId), out var v) ? v.Name : null;
                return BuildItem(g.Key, rName, vId, vName, g.Key, rName, null, null, g);
            }).ToList(),

        "month" => entries
            .GroupBy(e => $"{e.Date.Year:D4}-{e.Date.Month:D2}")
            .OrderBy(g => g.Key)
            .Select(g => BuildItem(g.Key, g.Key, null, null, null, null, null, g.Key, g))
            .ToList(),

        _ => []
    };

    // 9. Sort: by officialCost desc (except month: already sorted)
    if (groupBy != "month")
        allItems = allItems.OrderByDescending(i => i.OfficialCost).ToList();

    var totalCount = allItems.Count;
    var paged = allItems.Skip((page - 1) * pageSize).Take(pageSize).ToList();
    return new CostBreakdownResult(groupBy, totalCount, page, pageSize, paged);
}

private static CostBreakdownResult EmptyResult(GetCostBreakdownQuery q, string groupBy, int pageSize) =>
    new(groupBy, 0, Math.Max(q.Page, 1), pageSize, []);
```

**Lưu ý triển khai:**
- Sử dụng `dynamic` cho group items gây type-safety issues. Thay bằng local record:
  ```csharp
  private record EntryData(Guid ResourceId, Guid ProjectId, DateOnly Date, string EntryType, decimal CostAtTime, decimal Hours);
  ```
  Rồi project `entries` sang `List<EntryData>`.
- `BuildItem` sẽ nhận `IEnumerable<EntryData>` thay vì `IEnumerable<dynamic>`.

---

### Task 3 — ReportingController endpoint

**Thêm vào `ReportingController.cs`:**

```csharp
using ProjectManagement.Reporting.Application.Queries.GetCostBreakdown;

/// <summary>
/// Cost breakdown by dimension: vendor, project, resource, or month.
/// </summary>
[HttpGet("cost/breakdown")]
public async Task<IActionResult> GetCostBreakdown(
    [FromQuery] string groupBy,
    [FromQuery] string? month,
    [FromQuery] Guid? vendorId,
    [FromQuery] Guid? projectId,
    [FromQuery] Guid? resourceId,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 50,
    CancellationToken ct = default)
{
    var result = await _mediator.Send(
        new GetCostBreakdownQuery(
            _currentUser.UserId, groupBy, month,
            vendorId, projectId, resourceId, page, pageSize), ct);
    return Ok(result);
}
```

**Lưu ý:** `ArgumentException` từ handler sẽ được map sang 400 bởi `GlobalExceptionMiddleware` — kiểm tra middleware hiện có.

---

### Task 4 — Frontend models + service

**Thêm vào `cost-report.model.ts`:**
```typescript
export interface CostBreakdownItem {
  dimensionKey: string;
  dimensionLabel: string;
  vendorId: string | null;
  vendorName: string | null;
  resourceId: string | null;
  resourceName: string | null;
  projectId: string | null;
  month: string | null;
  estimatedCost: number;
  officialCost: number;
  confirmedPct: number;
  totalHours: number;
}

export interface CostBreakdownResult {
  groupBy: string;
  totalCount: number;
  page: number;
  pageSize: number;
  items: CostBreakdownItem[];
}
```

**Thêm vào `reporting-api.service.ts`:**
```typescript
import { CostBreakdownResult, CostSummaryResult } from '../models/cost-report.model';

getCostBreakdown(
  groupBy: string,
  month?: string,
  vendorId?: string,
  projectId?: string,
  resourceId?: string,
  page = 1,
  pageSize = 50
): Observable<CostBreakdownResult> {
  let params = new HttpParams().set('groupBy', groupBy).set('page', page).set('pageSize', pageSize);
  if (month)      params = params.set('month', month);
  if (vendorId)   params = params.set('vendorId', vendorId);
  if (projectId)  params = params.set('projectId', projectId);
  if (resourceId) params = params.set('resourceId', resourceId);
  return this.http.get<CostBreakdownResult>('/api/v1/reports/cost/breakdown', { params });
}
```

---

### Task 5 — NgRx store mở rộng

**`reporting.actions.ts`** — thêm vào events:
```typescript
'Load Cost Breakdown': props<{ groupBy: string; month?: string; vendorId?: string; projectId?: string; resourceId?: string; page?: number; pageSize?: number }>(),
'Load Cost Breakdown Success': props<{ result: CostBreakdownResult }>(),
'Load Cost Breakdown Failure': props<{ error: string }>(),
```

**`reporting.reducer.ts`** — mở rộng state:
```typescript
// State interface:
costBreakdown: CostBreakdownResult | null;
breakdownLoading: boolean;

// initialState:
costBreakdown: null,
breakdownLoading: false,

// Reducer cases:
on(ReportingActions.loadCostBreakdown, state => ({ ...state, breakdownLoading: true })),
on(ReportingActions.loadCostBreakdownSuccess, (state, { result }) => ({ ...state, breakdownLoading: false, costBreakdown: result })),
on(ReportingActions.loadCostBreakdownFailure, state => ({ ...state, breakdownLoading: false })),
```

Selectors được `createFeature` tự generate: `selectCostBreakdown`, `selectBreakdownLoading`.

**`reporting.effects.ts`** — thêm effect:
```typescript
loadCostBreakdown$ = createEffect(() =>
  this.actions$.pipe(
    ofType(ReportingActions.loadCostBreakdown),
    switchMap(({ groupBy, month, vendorId, projectId, resourceId, page, pageSize }) =>
      this.api.getCostBreakdown(groupBy, month, vendorId, projectId, resourceId, page, pageSize).pipe(
        map(result => ReportingActions.loadCostBreakdownSuccess({ result })),
        catchError(err => of(ReportingActions.loadCostBreakdownFailure({ error: err?.message ?? 'Lỗi tải breakdown.' })))
      )
    )
  )
);
```

---

### Task 6 — CostBreakdownComponent + routing

**`cost-breakdown.ts`** — standalone OnPush component:
```typescript
@Component({
  selector: 'app-cost-breakdown',
  standalone: true,
  imports: [AsyncPipe, DecimalPipe, NgFor, NgIf, ReactiveFormsModule,
    MatButtonModule, MatCardModule, MatProgressSpinnerModule,
    MatInputModule, MatFormFieldModule, MatSelectModule],
  templateUrl: './cost-breakdown.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CostBreakdownComponent implements OnInit {
  private readonly store = inject(Store);
  private readonly fb = inject(FormBuilder);

  readonly breakdown$ = this.store.select(selectCostBreakdown);
  readonly loading$ = this.store.select(selectBreakdownLoading);

  readonly dimensions = ['vendor', 'project', 'resource', 'month'];

  readonly form = this.fb.nonNullable.group({
    groupBy: ['vendor'],
    month: [''],
  });

  ngOnInit(): void { this.load(); }

  load(): void {
    const { groupBy, month } = this.form.getRawValue();
    this.store.dispatch(ReportingActions.loadCostBreakdown({
      groupBy, month: month || undefined
    }));
  }

  loadPage(page: number): void {
    const { groupBy, month } = this.form.getRawValue();
    this.store.dispatch(ReportingActions.loadCostBreakdown({
      groupBy, month: month || undefined, page
    }));
  }
}
```

**`cost-breakdown.html`** — minimal template với dimension selector + table:
- `<mat-select formControlName="groupBy">` với 4 options
- Date input cho `month` (type="month" — HTML month picker, value format "YYYY-MM")
- Bảng hiển thị: `dimensionLabel`, `estimatedCost`, `officialCost`, `confirmedPct`, `totalHours`
- Pagination controls (previous/next page) dựa trên `breakdown.totalCount`, `breakdown.page`, `breakdown.pageSize`

**`reporting.routes.ts`** — thêm route:
```typescript
{
  path: 'breakdown',
  loadComponent: () =>
    import('./components/cost-breakdown/cost-breakdown').then(m => m.CostBreakdownComponent),
},
```

---

### Kiểm tra GlobalExceptionMiddleware — ArgumentException → 400

Cần verify `GlobalExceptionMiddleware` handle `ArgumentException` thành 400. Nếu không, thêm case:

```csharp
// Trong GlobalExceptionMiddleware
ArgumentException => StatusCodes.Status400BadRequest
```

---

### Patterns từ Stories trước

1. **In-memory join pattern**: Query TimeEntries và Resources vào separate lists rồi join in-memory — giống TriggerForecastComputeHandler. Không dùng EF join cross-DbContext.
2. **Local record thay dynamic**: Dùng `private record EntryData(...)` thay vì anonymous type khi cần truyền qua methods.
3. **Phân trang chuẩn**: `pageSize = Math.Min(Math.Max(query.PageSize, 1), 200)` — giống `GetTimeEntryListHandler`.
4. **Membership-scope bắt buộc**: Luôn filter projectIds trước, rồi mới query TimeEntries.
5. **`!IsVoided` bắt buộc**: Không bao giờ tính void entries.
6. **createFeature selector naming**: `selectCostBreakdown`, `selectBreakdownLoading` được auto-generate bởi `createFeature`. Import trực tiếp từ reducer file, không cần tạo selectors file riêng.

---

### Anti-patterns cần tránh

- **KHÔNG** dùng EF join across 2 DbContexts trong 1 LINQ query — dùng in-memory join
- **KHÔNG** leak data ra ngoài membership scope — luôn check projectIds trước
- **KHÔNG** trả 500 khi `groupBy` invalid — validate và throw `ArgumentException` (→ 400)
- **KHÔNG** cho phép pageSize vượt 200 — clamp như `GetTimeEntryListHandler`
- **KHÔNG** quên include `MatSelectModule` trong component imports list

---

## Completion Notes

- Mở rộng `GlobalExceptionMiddleware` để map `ArgumentException` → 400 (trước đây fall-through → 500)
- Handler dùng `private sealed record EntryData` thay vì anonymous type / `dynamic` để đảm bảo type-safety khi truyền qua `BuildItem()`
- In-memory join pattern: load TimeEntries + Resources vào separate `List<>` rồi join in-memory (không thể dùng EF LINQ cross-DbContext)
- GroupBy "month" sort ascending by key; các dimension khác sort descending by officialCost
- `dotnet build`: 0 errors, 10 pre-existing MSB3277 warnings (EF Core Relational version conflict — pre-existing)
- `ng build`: 0 errors, bundle generation complete

## Files Created/Modified

**Backend:**
- `src/Modules/Reporting/ProjectManagement.Reporting.Application/ProjectManagement.Reporting.Application.csproj` — thêm Workforce.Application reference
- `src/Modules/Reporting/ProjectManagement.Reporting.Application/Queries/GetCostBreakdown/GetCostBreakdownQuery.cs` — mới (query + handler + records)
- `src/Modules/Reporting/ProjectManagement.Reporting.Api/Controllers/ReportingController.cs` — thêm `GET /api/v1/reports/cost/breakdown`
- `src/Host/ProjectManagement.Host/GlobalExceptionMiddleware.cs` — thêm `ArgumentException` → 400 case

**Frontend:**
- `frontend/.../features/reporting/models/cost-report.model.ts` — thêm `CostBreakdownItem`, `CostBreakdownResult`
- `frontend/.../features/reporting/services/reporting-api.service.ts` — thêm `getCostBreakdown()`
- `frontend/.../features/reporting/store/reporting.actions.ts` — thêm 3 breakdown actions
- `frontend/.../features/reporting/store/reporting.reducer.ts` — mở rộng state + selectors
- `frontend/.../features/reporting/store/reporting.effects.ts` — thêm `loadCostBreakdown$`
- `frontend/.../features/reporting/components/cost-breakdown/cost-breakdown.ts` — mới
- `frontend/.../features/reporting/components/cost-breakdown/cost-breakdown.html` — mới
- `frontend/.../features/reporting/reporting.routes.ts` — thêm `breakdown` route
