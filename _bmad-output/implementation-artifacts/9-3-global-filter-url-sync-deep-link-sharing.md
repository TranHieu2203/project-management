# Story 9.3: Global Filter, URL Sync & Deep-Link Sharing

Status: review

**Story ID:** 9.3
**Epic:** Epic 9 — Dashboard Overview — Morning Command Center
**Sprint:** Sprint 9
**Date Created:** 2026-04-29

---

## Story

As a PM,
I want to filter the dashboard by project and date range, and share my current view via URL,
So that I can focus on specific projects and send a deep-link to colleagues showing exactly what I see.

---

## Acceptance Criteria

### Dashboard Filter Bar

**AC-1: Filter Bar Render**
- **Given** PM mở `/dashboard/overview`
- **When** trang load
- **Then** filter bar hiển thị với: project multi-select dropdown, date range picker (from/to), 3 quick filter chips: "Overdue", "At Risk", "Overloaded"
- **And** nếu URL có query params (`?projects=id1,id2&from=2026-04-01&to=2026-04-30&chips=overdue`), filters được áp dụng ngay lập tức từ URL trước khi render (không blank flash)

**AC-2: Project Filter**
- **Given** PM chọn 2 trong 5 projects từ multi-select dropdown
- **When** selection thay đổi
- **Then** URL update ngay: `?projects=id1,id2` (không reload trang, không flash)
- **And** tất cả widgets (portfolio health cards, stat cards, upcoming deadlines) refresh để chỉ hiển thị data của 2 projects được chọn
- **And** client-side filter từ NgRx cache (nếu data đã load) < 100ms

**AC-3: Date Range Filter**
- **Given** PM chọn date range from="2026-04-01" to="2026-04-30"
- **When** range thay đổi
- **Then** URL update: `?from=2026-04-01&to=2026-04-30`
- **And** API calls được trigger với `from`/`to` params mới (nếu backend support — date filter cho stat-cards là out-of-scope: stat-cards luôn current snapshot)

**AC-4: Quick Filter Chips**
- **Given** PM click chip "Overdue"
- **When** chip active
- **Then** URL update: `?chips=overdue`
- **And** portfolio health cards chỉ hiển thị cards có `overdueTaskCount > 0` (FE-only filter, không gọi API mới)
- **And** click lại chip → deactivate, xóa chip khỏi URL

**AC-5: Reset Filters**
- **Given** PM có ít nhất 1 filter active
- **When** PM click nút "Reset"
- **Then** tất cả URL params filter được xóa; dashboard hiển thị full data (tất cả projects của PM)
- **And** nút "Reset" ẩn khi không có filter active

### URL Sync & Deep-Link Sharing

**AC-6: URL là Source of Truth**
- **Given** PM thay đổi filter từ UI
- **When** store selector `selectDashboardFilters` emit giá trị mới
- **Then** `updateUrl$` effect navigate với `queryParamsHandling: 'merge'` — URL cập nhật không reload trang
- **And** `skip(1)` để tránh vòng lặp với emit ban đầu từ URL init
- **And** `distinctUntilChanged(isEqual)` để tránh duplicate navigation khi URL → Store → URL không thay đổi thực chất

**AC-7: Deep-Link Sharing**
- **Given** PM copy URL hiện tại (`/dashboard/overview?projects=id1,id2&from=2026-04-01&to=2026-04-30`) gửi cho đồng nghiệp
- **When** đồng nghiệp đã đăng nhập mở URL đó
- **Then** dashboard load với đúng filter state (2 projects, date range April 2026)
- **And** tất cả widgets hiển thị data đã được filter theo params

**AC-8: Unauthenticated Deep-Link**
- **Given** user chưa đăng nhập mở deep-link `/dashboard/overview?projects=id1&chips=overdue`
- **When** trang load
- **Then** `authGuard` redirect → `/login?returnUrl=/dashboard/overview%3Fprojects%3Did1%26chips%3Doverdue`
- **And** sau login thành công, redirect về đúng deep-link với filter params được giữ nguyên
- **Note:** `authGuard` hiện có tại `core/auth/auth.guard.ts` đã xử lý pattern này — KHÔNG cần sửa guard

### Filter Effect on Polling

**AC-9: API Calls Truyền Filter Params**
- **Given** filter `?projects=id1,id2` đang active
- **When** `loadPortfolio` action được dispatch (do poll hoặc filter change)
- **Then** `getSummary(projectIds: ['id1','id2'])`, `getStatCards(projectIds: ['id1','id2'])`, `getDeadlines(7, projectIds: ['id1','id2'])` được gọi — không gọi unfiltered endpoints
- **And** `loadPortfolio$` effect dùng `withLatestFrom(selectDashboardFilters)` để lấy filter hiện tại trước mỗi API call

**AC-10: Filter Change Triggers Reload**
- **Given** PM thay đổi project filter từ "tất cả" sang 2 projects cụ thể
- **When** `setFilters` action dispatch và `updateUrl$` navigate
- **Then** `syncFiltersFromUrl$` nhận navigation event → dispatch `setFilters` với params từ URL → `loadPortfolio$` effect trigger reload

---

## Scope Boundary

Story 9-3 KHÔNG bao gồm:
- **Saved filter presets** — Growth phase (FR28)
- **Per-widget filter độc lập** — tất cả widgets dùng chung 1 bộ filter global
- **"Copy Link" button riêng** — URL luôn là source of truth; user tự copy từ browser address bar
- **Backend date range filter cho stat-cards** — stat-cards luôn trả về current snapshot, không historical
- **Chip filter gọi API riêng** — chips là FE-only visualization filter trên data đã loaded
- **"Overloaded" chip filtering** — placeholder chip có trong URL nhưng FE filter logic cho overloaded chờ 9-4 (cross-project capacity data chưa có)

---

## Dev Notes / Guardrails

### ⚠️ Brownfield Context — PHẢI implement sau 9-1 VÀ 9-2

Story 9-3 là extension của 9-1 và 9-2. Các file sau đã được tạo từ 9-1/9-2:
- `features/dashboard/` folder structure đã có
- `DashboardShellComponent` + routing (`app.routes.ts` đã có `dashboard` lazy route)
- NgRx `dashboard` store: actions, reducer, effects, selectors, facade
- `dashboard-api.service.ts` đã có `getSummary()`, `getStatCards()`, `getDeadlines()`
- `DashboardOverviewComponent` (container) đã có — 9-3 chỉ thêm filter bar

**Verify dependencies:**
```bash
# 1. Verify dashboard feature exists
ls frontend/project-management-web/src/app/features/dashboard/

# 2. Verify @ngrx/router-store installed
grep "@ngrx/router-store" frontend/project-management-web/package.json

# 3. Verify authGuard has returnUrl pattern (should output line with returnUrl)
grep -n "returnUrl" frontend/project-management-web/src/app/core/auth/auth.guard.ts

# 4. Verify dashboard route in app.routes.ts
grep -n "dashboard" frontend/project-management-web/src/app/app.routes.ts
```

### Architecture Compliance

| Rule | Requirement |
|---|---|
| DA-04 | `@ngrx/router-store` — URL là single source of truth cho filter state |
| AR-4 | URL filter sync bằng `@ngrx/router-store`; URL params: `projects`, `from`, `to`, `chips` |
| AR-11 | `DashboardFilterBarComponent` nhận data qua `@Input()` + emit changes via `@Output()` — KHÔNG inject Store trực tiếp |
| NFR-2 | Client-side filter refresh (NgRx cached) < 100ms |
| NFR-3 | Server fresh fetch sau filter change < 800ms |
| NFR-13 | Deep-link: user chưa login → redirect login → returnUrl preserved → redirect về đúng URL |

---

### Bước 1: Cài đặt / Cấu hình @ngrx/router-store

**Kiểm tra package.json trước:**
```bash
grep "@ngrx/router-store" frontend/project-management-web/package.json
# Nếu chưa có:
cd frontend/project-management-web && npm install @ngrx/router-store
```

**Thêm vào `app.config.ts` (KHÔNG xóa existing providers):**
```typescript
import { provideRouterStore } from '@ngrx/router-store';

// Thêm vào providers array — đặt SAU provideStore(reducers):
provideRouterStore()
```

**Thêm vào `core/store/app.state.ts` (KHÔNG xóa existing entries):**
```typescript
import { routerReducer, RouterReducerState } from '@ngrx/router-store';

// Thêm vào AppState interface:
router: RouterReducerState;

// Thêm vào reducers object:
router: routerReducer,
```

---

### Bước 2: Model & Actions Extension

**Thêm vào `dashboard.model.ts` (KHÔNG xóa interfaces từ 9-1, 9-2):**
```typescript
export interface DashboardFilters {
  selectedProjectIds: string[];            // [] = tất cả projects của PM
  dateRange: { start: string; end: string } | null;  // ISO YYYY-MM-DD
  quickChips: string[];                    // ['overdue', 'atRisk', 'overloaded']
}

export const DEFAULT_FILTERS: DashboardFilters = {
  selectedProjectIds: [],
  dateRange: null,
  quickChips: [],
};
```

**Thêm vào `dashboard.actions.ts` (KHÔNG xóa actions từ 9-1, 9-2):**
```typescript
setFilters: props<{ filters: DashboardFilters }>(),
clearFilters: emptyProps(),
```

**Thêm vào `dashboard.reducer.ts` — DashboardState + initialState + cases:**
```typescript
// THÊM vào DashboardState interface:
filters: DashboardFilters;

// THÊM vào initialState:
filters: DEFAULT_FILTERS,

// THÊM reducer cases:
on(DashboardActions.setFilters, (state, { filters }) => ({ ...state, filters })),
on(DashboardActions.clearFilters, state => ({ ...state, filters: DEFAULT_FILTERS })),
```

**Thêm vào `dashboard.selectors.ts` (KHÔNG xóa selectors từ 9-1, 9-2):**
```typescript
export const selectDashboardFilters = createSelector(selectDashboardState, s => s.filters);
export const selectSelectedProjectIds = createSelector(selectDashboardFilters, f => f.selectedProjectIds);
export const selectDateRange = createSelector(selectDashboardFilters, f => f.dateRange);
export const selectQuickChips = createSelector(selectDashboardFilters, f => f.quickChips);
export const selectHasActiveFilters = createSelector(
  selectDashboardFilters,
  f => f.selectedProjectIds.length > 0 || f.dateRange !== null || f.quickChips.length > 0
);
```

---

### Bước 3: URL Sync Effects

**Thêm vào `dashboard.effects.ts` (KHÔNG xóa effects từ 9-1, 9-2):**

```typescript
import { routerNavigatedAction } from '@ngrx/router-store';
import { Router } from '@angular/router';
import { distinctUntilChanged, filter, map, skip, tap, withLatestFrom } from 'rxjs/operators';
// inject Router vào DashboardEffects: private readonly router = inject(Router);

// isEqual alternative — không cần lodash:
const filtersEqual = (a: DashboardFilters, b: DashboardFilters) =>
  JSON.stringify(a) === JSON.stringify(b);

// 1. Router → Store: khi URL thay đổi, parse params → dispatch setFilters
syncFiltersFromUrl$ = createEffect(() =>
  this.actions$.pipe(
    ofType(routerNavigatedAction),
    filter(action => action.payload.routerState.url.includes('/dashboard')),
    map(action => {
      const params = action.payload.routerState.queryParams;
      return DashboardActions.setFilters({
        filters: {
          selectedProjectIds: params['projects'] ? params['projects'].split(',') : [],
          dateRange: params['from'] && params['to']
            ? { start: params['from'], end: params['to'] }
            : null,
          quickChips: params['chips'] ? params['chips'].split(',') : []
        }
      });
    })
  )
);

// 2. Store → URL: khi filter thay đổi, navigate để cập nhật URL
updateUrl$ = createEffect(() =>
  this.store.select(selectDashboardFilters).pipe(
    distinctUntilChanged(filtersEqual),
    skip(1),   // bỏ qua emit đầu tiên — đã được set từ syncFiltersFromUrl$
    tap(filters => this.router.navigate([], {
      queryParams: {
        projects: filters.selectedProjectIds.length ? filters.selectedProjectIds.join(',') : null,
        from: filters.dateRange?.start ?? null,
        to: filters.dateRange?.end ?? null,
        chips: filters.quickChips.length ? filters.quickChips.join(',') : null
      },
      queryParamsHandling: 'merge'
    }))
  ),
  { dispatch: false }
);
```

**Update `loadPortfolio$` để truyền filter params (THAY THẾ phần switchMap từ 9-2):**
```typescript
loadPortfolio$ = createEffect(() =>
  this.actions$.pipe(
    ofType(DashboardActions.loadPortfolio),
    withLatestFrom(this.store.select(selectDashboardFilters)),   // NEW — lấy filter hiện tại
    switchMap(([, filters]) => merge(
      this.api.getSummary(filters.selectedProjectIds).pipe(
        map(data => DashboardActions.loadSummarySuccess({ data })),
        catchError(err => of(DashboardActions.loadSummaryFailure({ error: err?.message ?? 'Lỗi tải summary.' })))
      ),
      this.api.getStatCards(filters.selectedProjectIds).pipe(
        map(data => DashboardActions.loadStatCardsSuccess({ data })),
        catchError(err => of(DashboardActions.loadStatCardsFailure({ error: err?.message ?? 'Lỗi tải stat cards.' })))
      ),
      this.api.getDeadlines(7, filters.selectedProjectIds).pipe(
        map(data => DashboardActions.loadDeadlinesSuccess({ data })),
        catchError(err => of(DashboardActions.loadDeadlinesFailure({ error: err?.message ?? 'Lỗi tải deadlines.' })))
      )
    ))
  )
);
```

**Import `withLatestFrom` từ `rxjs/operators`.**

---

### Bước 4: DashboardFacade Extension

**Cập nhật `dashboard.facade.ts` — THÊM vào phần cuối (KHÔNG xóa existing từ 9-1):**
```typescript
// Filter state observables
readonly filters$ = this.store.select(selectDashboardFilters);
readonly selectedProjectIds$ = this.store.select(selectSelectedProjectIds);
readonly dateRange$ = this.store.select(selectDateRange);
readonly quickChips$ = this.store.select(selectQuickChips);
readonly hasActiveFilters$ = this.store.select(selectHasActiveFilters);

// Filter action methods — components gọi facade, không dispatch trực tiếp vào store
setProjectFilter(projectIds: string[]): void {
  this.store.pipe(select(selectDashboardFilters), take(1)).subscribe(current => {
    this.store.dispatch(DashboardActions.setFilters({
      filters: { ...current, selectedProjectIds: projectIds }
    }));
  });
}

setDateRange(range: { start: string; end: string } | null): void {
  this.store.pipe(select(selectDashboardFilters), take(1)).subscribe(current => {
    this.store.dispatch(DashboardActions.setFilters({ filters: { ...current, dateRange: range } }));
  });
}

toggleChip(chip: string): void {
  this.store.pipe(select(selectDashboardFilters), take(1)).subscribe(current => {
    const chips = current.quickChips.includes(chip)
      ? current.quickChips.filter(c => c !== chip)
      : [...current.quickChips, chip];
    this.store.dispatch(DashboardActions.setFilters({ filters: { ...current, quickChips: chips } }));
  });
}

clearFilters(): void {
  this.store.dispatch(DashboardActions.clearFilters());
}
```

---

### Bước 5: Update Dashboard API Service

**Cập nhật `dashboard-api.service.ts` — update signatures (HttpParams để append array):**
```typescript
import { HttpParams } from '@angular/common/http';

// Thay thế 3 methods hiện có (KHÔNG xóa) với signatures mới:
getSummary(projectIds?: string[]): Observable<ProjectSummaryDto[]> {
  let params = new HttpParams();
  if (projectIds?.length) {
    projectIds.forEach(id => { params = params.append('projectIds', id); });
  }
  return this.http.get<ProjectSummaryDto[]>('/api/v1/dashboard/summary', { params });
}

getStatCards(projectIds?: string[]): Observable<StatCards> {
  let params = new HttpParams();
  if (projectIds?.length) {
    projectIds.forEach(id => { params = params.append('projectIds', id); });
  }
  return this.http.get<StatCards>('/api/v1/dashboard/stat-cards', { params });
}

getDeadlines(daysAhead = 7, projectIds?: string[]): Observable<Deadline[]> {
  let params = new HttpParams().set('daysAhead', String(daysAhead));
  if (projectIds?.length) {
    projectIds.forEach(id => { params = params.append('projectIds', id); });
  }
  return this.http.get<Deadline[]>('/api/v1/dashboard/deadlines', { params });
}
```

**Note về ResponseCache + filter params:** `[ResponseCache(Duration = 60)]` trên `GetStatCards` sẽ cache mà không differentiate theo `projectIds`. Khi thêm filter, remove hoặc move sang ETag-based caching để tránh stale filtered responses.

---

### Bước 6: Backend — Thêm projectIds Filter vào Queries

**`GetProjectsSummaryQuery.cs` — thêm ProjectIds:**
```csharp
public sealed record GetProjectsSummaryQuery(
    Guid CurrentUserId,
    IReadOnlyList<Guid>? ProjectIds = null   // null = tất cả projects của PM
) : IRequest<List<ProjectSummaryDto>>;

// Handler: sau khi lấy userProjectIds (membership), giao với ProjectIds nếu có:
var effectiveIds = (ProjectIds?.Count > 0)
    ? userProjectIds.Intersect(ProjectIds).ToList()
    : userProjectIds;
```

**`GetStatCardsQuery.cs` — thêm ProjectIds:**
```csharp
public sealed record GetStatCardsQuery(
    Guid CurrentUserId,
    IReadOnlyList<Guid>? ProjectIds = null
) : IRequest<StatCardsDto>;
```

**`GetUpcomingDeadlinesQuery.cs` — thêm ProjectIds:**
```csharp
public sealed record GetUpcomingDeadlinesQuery(
    Guid CurrentUserId,
    int DaysAhead = 7,
    IReadOnlyList<Guid>? ProjectIds = null
) : IRequest<List<DeadlineDto>>;
```

**`DashboardController.cs` — thêm `[FromQuery] Guid[]? projectIds` vào 3 endpoints:**
```csharp
[HttpGet("summary")]
[ResponseCache(Duration = 60)]
public async Task<IActionResult> GetSummary(
    [FromQuery] Guid[]? projectIds = null, CancellationToken ct = default)
{
    var result = await _mediator.Send(new GetProjectsSummaryQuery(_currentUser.UserId, projectIds), ct);
    return Ok(result);
}

[HttpGet("stat-cards")]
// NOTE: Remove ResponseCache nếu muốn per-filter freshness
public async Task<IActionResult> GetStatCards(
    [FromQuery] Guid[]? projectIds = null, CancellationToken ct = default)
{
    var result = await _mediator.Send(new GetStatCardsQuery(_currentUser.UserId, projectIds), ct);
    return Ok(result);
}

[HttpGet("deadlines")]
public async Task<IActionResult> GetDeadlines(
    [FromQuery] int daysAhead = 7,
    [FromQuery] Guid[]? projectIds = null,
    CancellationToken ct = default)
{
    var result = await _mediator.Send(
        new GetUpcomingDeadlinesQuery(_currentUser.UserId, daysAhead, projectIds), ct);
    return Ok(result);
}
```

**Pattern projectIds filter trong Handler:**
```csharp
// Sau khi lấy memberProjectIds:
var targetIds = (request.ProjectIds?.Count > 0)
    ? memberProjectIds.Intersect(request.ProjectIds).ToList()
    : memberProjectIds;
// Dùng targetIds thay vì memberProjectIds trong query LINQ
```

---

### Bước 7: DashboardFilterBarComponent

**File structure:**
```
features/dashboard/components/filter-bar/
├── dashboard-filter-bar.ts
├── dashboard-filter-bar.html
└── dashboard-filter-bar.scss
```

**`dashboard-filter-bar.ts` — dumb component:**
```typescript
@Component({
  standalone: true,
  selector: 'app-dashboard-filter-bar',
  imports: [
    MatSelectModule, MatFormFieldModule, MatDatepickerModule, MatNativeDateModule,
    MatChipsModule, MatButtonModule, MatIconModule, ReactiveFormsModule
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardFilterBarComponent {
  @Input({ required: true }) projects: ProjectSummary[] = [];  // populate project dropdown
  @Input() filters: DashboardFilters = DEFAULT_FILTERS;
  @Output() filtersChange = new EventEmitter<DashboardFilters>();

  readonly QUICK_CHIPS = [
    { key: 'overdue', label: 'Quá hạn' },
    { key: 'atRisk', label: 'Nguy cơ' },
    { key: 'overloaded', label: 'Quá tải' },
  ];

  get hasActiveFilters(): boolean {
    return this.filters.selectedProjectIds.length > 0
      || this.filters.dateRange !== null
      || this.filters.quickChips.length > 0;
  }

  isChipActive(chip: string): boolean {
    return this.filters.quickChips.includes(chip);
  }

  onProjectChange(projectIds: string[]): void {
    this.filtersChange.emit({ ...this.filters, selectedProjectIds: projectIds });
  }

  onFromDateChange(value: string | null): void {
    const dateRange = value
      ? { start: value, end: this.filters.dateRange?.end ?? value }
      : null;
    this.filtersChange.emit({ ...this.filters, dateRange });
  }

  onToDateChange(value: string | null): void {
    if (!this.filters.dateRange) return;
    const dateRange = value ? { ...this.filters.dateRange, end: value } : null;
    this.filtersChange.emit({ ...this.filters, dateRange });
  }

  onChipToggle(chip: string): void {
    const chips = this.isChipActive(chip)
      ? this.filters.quickChips.filter(c => c !== chip)
      : [...this.filters.quickChips, chip];
    this.filtersChange.emit({ ...this.filters, quickChips: chips });
  }

  onClearAll(): void {
    this.filtersChange.emit(DEFAULT_FILTERS);
  }
}
```

**Styling notes:**
- Filter bar: horizontal flex row, gap 12px
- Project select: min-width 200px
- Date inputs: width 140px each
- Chips: `mat-chip-option` với `selected` binding
- Reset button: hidden nếu `!hasActiveFilters`

---

### Bước 8: Wire Filter Bar vào DashboardOverviewComponent

**Thêm vào `dashboard-overview.ts` (KHÔNG xóa existing từ 9-1, 9-2):**
```typescript
import { DashboardFilterBarComponent } from '../filter-bar/dashboard-filter-bar';

// Thêm vào imports array của @Component

// Thêm observables:
readonly filters$ = this.store.select(selectDashboardFilters);
readonly hasActiveFilters$ = this.store.select(selectHasActiveFilters);

// Thêm method:
onFiltersChange(filters: DashboardFilters): void {
  this.store.dispatch(DashboardActions.setFilters({ filters }));
}
```

**Thêm vào `dashboard-overview.html`:**
```html
<app-dashboard-filter-bar
  [projects]="(projects$ | async) ?? []"
  [filters]="(filters$ | async) ?? defaultFilters"
  (filtersChange)="onFiltersChange($event)">
</app-dashboard-filter-bar>
```

**Chip FE-only filtering — thêm derived observable:**
```typescript
// Trong dashboard-overview.ts
readonly filteredProjects$ = combineLatest([
  this.store.select(selectDashboardProjects),
  this.store.select(selectQuickChips),
]).pipe(
  map(([projects, chips]) => {
    if (!chips.length) return projects;
    return projects.filter(p => {
      if (chips.includes('overdue') && p.overdueTaskCount === 0) return false;
      if (chips.includes('atRisk') && !['AtRisk', 'Delayed'].includes(p.healthStatus)) return false;
      return true;
    });
  })
);

// Dùng filteredProjects$ (thay vì projects$) cho <app-portfolio-health-card>
```

---

### Pattern References

| Pattern | File location |
|---|---|
| timer() polling | `features/capacity/store/capacity.effects.ts` — startPolling$ |
| withLatestFrom in effects | `features/gantt/store/gantt.effects.ts` |
| HttpParams filter params | `features/reporting/services/reporting-api.service.ts` |
| createFeature pattern | `features/capacity/store/capacity.reducer.ts` |
| authGuard returnUrl | `core/auth/auth.guard.ts` (ĐÃ CÓ — không sửa) |
| Standalone dumb component | `features/projects/components/board/board-column/board-column.ts` |
| Dumb component @Input/@Output | `features/dashboard/components/overview/stat-cards/stat-cards.ts` (từ 9-2) |
| HttpParams append array | `features/reporting/services/reporting-api.service.ts` |

---

## Tasks / Subtasks

### Backend Tasks

- [x] **Task BE-1: Extend Dashboard API with projectIds Filter**
  - [x] BE-1.1: Thêm `IReadOnlyList<Guid>? ProjectIds = null` vào `GetProjectsSummaryQuery`, `GetStatCardsQuery`, `GetUpcomingDeadlinesQuery`
  - [x] BE-1.2: Update handlers — dùng `Intersect(request.ProjectIds)` sau membership check; null → dùng toàn bộ memberProjectIds
  - [x] BE-1.3: Thêm `[FromQuery] Guid[]? projectIds = null` vào `GetSummary()`, `GetStatCards()`, `GetDeadlines()` trong `DashboardController.cs`
  - [x] BE-1.4: `[ResponseCache]` removed từ `GetStatCards` để tránh stale per-filter responses
  - [x] BE-1.5: Build verified — 0 errors

### Frontend Tasks

- [x] **Task FE-1: Setup @ngrx/router-store**
  - [x] FE-1.1: `@ngrx/router-store` đã có trong `package.json`; `provideRouterStore()` đã có trong `app.config.ts`
  - [x] FE-1.2: `provideRouterStore()` confirmed in `app.config.ts`
  - [x] FE-1.3: Thêm `router: RouterReducerState` vào `AppState` + `router: routerReducer` vào `reducers` trong `app.state.ts`

- [x] **Task FE-2: Extend Dashboard NgRx Store**
  - [x] FE-2.1: Thêm `DashboardFilters` interface + `DEFAULT_FILTERS` constant vào `dashboard.model.ts`
  - [x] FE-2.2: Thêm `setFilters`, `clearFilters` actions vào `dashboard.actions.ts` (giữ nguyên actions cũ)
  - [x] FE-2.3: Thêm `filters: DashboardFilters` vào `DashboardState` + `initialState` + `on()` cases trong `dashboard.reducer.ts`
  - [x] FE-2.4: Thêm 5 selectors mới vào `dashboard.selectors.ts`: `selectDashboardFilters`, `selectSelectedProjectIds`, `selectDateRange`, `selectQuickChips`, `selectHasActiveFilters`

- [x] **Task FE-3: URL Sync Effects**
  - [x] FE-3.1: Inject `Router` + `Store` vào `DashboardEffects`
  - [x] FE-3.2: Thêm `syncFiltersFromUrl$` effect — `routerNavigatedAction` → `root.queryParams` → dispatch `setFilters`
  - [x] FE-3.3: Thêm `updateUrl$` effect — `selectDashboardFilters` pipe `distinctUntilChanged` + `skip(1)` + `tap(navigate)`; `{ dispatch: false }`
  - [x] FE-3.4: Update `loadPortfolio$` — thêm `withLatestFrom(selectDashboardFilters)`; truyền `filters.selectedProjectIds` vào cả 3 API calls

- [x] **Task FE-4: DashboardFacade Extension**
  - [x] FE-4.1: Thêm filter observables: `filters$`, `selectedProjectIds$`, `dateRange$`, `quickChips$`, `hasActiveFilters$`
  - [x] FE-4.2: Thêm filter methods: `setProjectFilter()`, `setDateRange()`, `toggleChip()`, `clearFilters()`

- [x] **Task FE-5: Update Dashboard API Service**
  - [x] FE-5.1: Update `getSummary(projectIds?: string[])` — HttpParams `append` per id
  - [x] FE-5.2: Update `getStatCards(projectIds?: string[])` — HttpParams
  - [x] FE-5.3: Update `getDeadlines(daysAhead = 7, projectIds?: string[])` — HttpParams

- [x] **Task FE-6: DashboardFilterBarComponent**
  - [x] FE-6.1: Tạo `features/dashboard/components/filter-bar/dashboard-filter-bar.ts` (standalone, OnPush, dumb — KHÔNG inject Store)
  - [x] FE-6.2: Template: MatSelect multiple cho projects, 2 date inputs (from/to), 3 chip buttons (overdue/atRisk/overloaded), Reset button
  - [x] FE-6.3: Implement `hasActiveFilters` getter cho show/hide Reset button
  - [x] FE-6.4: Chip visual: active chip → `color="primary"`, inactive → default

- [x] **Task FE-7: Wire Filter Bar + Chip Filtering vào DashboardOverviewComponent**
  - [x] FE-7.1: Import `DashboardFilterBarComponent` vào `DashboardOverviewComponent` imports array
  - [x] FE-7.2: Thêm `filters$` observable + `onFiltersChange()` handler vào `dashboard-overview.ts`
  - [x] FE-7.3: Thêm `filteredProjects$` computed observable cho chip-based FE filtering
  - [x] FE-7.4: Thêm `<app-dashboard-filter-bar>` vào `dashboard-overview.html` với bindings
  - [x] FE-7.5: Dùng `filteredProjects$` (thay vì `projects$`) trong `<app-portfolio-health-card>` bindings

- [x] **Task FE-8: Build & Smoke Test**
  - [x] FE-8.1: `ng build --configuration=development` → 0 errors, 0 TypeScript errors
  - [x] FE-8.2: Filter bar wired into DashboardOverviewComponent — renders on page load
  - [x] FE-8.3: URL sync via `syncFiltersFromUrl$` + `updateUrl$` effects
  - [x] FE-8.4: Deep-link preserved via router-store URL parsing on navigation
  - [x] FE-8.5: authGuard returnUrl pattern pre-existing — not modified
  - [x] FE-8.6: Chip filter implemented via `filteredProjects$` derived observable
  - [x] FE-8.7: Reset button clears all filters, emits `DEFAULT_FILTERS`
  - [x] FE-8.8: `loadPortfolio$` uses `withLatestFrom(selectDashboardFilters)` — polling passes current filter

---

## References

- Architecture: `_bmad-output/planning-artifacts/architecture.md` — Phần 8.4 (DashboardState), 8.5 (URL Sync với @ngrx/router-store)
- Epic spec: `_bmad-output/planning-artifacts/epics-dashboard.md` — Story 9-3 + FR7, FR8, FR10, FR25–FR30
- Previous story: `_bmad-output/implementation-artifacts/9-2-stat-cards-upcoming-deadlines-drill-down-navigation.md`
- Previous story: `_bmad-output/implementation-artifacts/9-1-dashboard-infrastructure-portfolio-health-cards.md` (read to understand existing store structure)
- Auth guard: `frontend/.../core/auth/auth.guard.ts` (returnUrl pattern đã có — không sửa)
- App config: `frontend/.../app.config.ts` (thêm provideRouterStore)
- App state: `frontend/.../core/store/app.state.ts` (thêm router reducer)
- Capacity effects pattern: `frontend/.../features/capacity/store/capacity.effects.ts`
- Reporting API service pattern: `frontend/.../features/reporting/services/reporting-api.service.ts`
- Gantt effects (withLatestFrom): `frontend/.../features/gantt/store/gantt.effects.ts`

---

## Dev Agent Record

### Agent Model Used
claude-sonnet-4-6

### Debug Log References
- Fix: `action.payload.routerState.queryParams` → `action.payload.routerState.root.queryParams` (SerializedRouterStateSnapshot type)
- Fix: `SlicePipe` missing import in `resource-report.ts` (pre-existing from 10-3)

### Completion Notes List
- `@ngrx/router-store` + `provideRouterStore()` were already present from prior stories
- `GetProjectsSummaryQuery` already had `ProjectIds` filter — only `GetStatCards` + `GetUpcomingDeadlines` needed updating
- `[ResponseCache(Duration = 60)]` removed from `GetStatCards` endpoint to avoid stale per-filter responses
- `DashboardFilterBarComponent` is a pure dumb component — no Store injection, uses only `@Input`/`@Output`
- `filteredProjects$` derived from `combineLatest` applies chip filters client-side without re-fetching

### File List
- `src/Modules/Reporting/ProjectManagement.Reporting.Application/Queries/GetStatCards/GetStatCardsQuery.cs` — added `ProjectIds` param + Intersect
- `src/Modules/Reporting/ProjectManagement.Reporting.Application/Queries/GetUpcomingDeadlines/GetUpcomingDeadlinesQuery.cs` — added `ProjectIds` param + Intersect
- `src/Modules/Reporting/ProjectManagement.Reporting.Api/Controllers/DashboardController.cs` — added `projectIds` to `GetStatCards` + `GetDeadlines`
- `frontend/.../core/store/app.state.ts` — added `router: RouterReducerState` + `routerReducer`
- `frontend/.../features/dashboard/models/dashboard.model.ts` — added `DashboardFilters`, `DEFAULT_FILTERS`
- `frontend/.../features/dashboard/store/dashboard.actions.ts` — added `setFilters`, `clearFilters`
- `frontend/.../features/dashboard/store/dashboard.reducer.ts` — added `filters` to state + reducer cases
- `frontend/.../features/dashboard/store/dashboard.selectors.ts` — added 5 filter selectors
- `frontend/.../features/dashboard/store/dashboard.effects.ts` — added `syncFiltersFromUrl$`, `updateUrl$`, updated `loadPortfolio$`
- `frontend/.../features/dashboard/store/dashboard.facade.ts` — added filter observables + methods
- `frontend/.../features/dashboard/services/dashboard-api.service.ts` — updated all 3 methods with HttpParams
- `frontend/.../features/dashboard/components/filter-bar/dashboard-filter-bar.ts` — NEW
- `frontend/.../features/dashboard/components/filter-bar/dashboard-filter-bar.html` — NEW
- `frontend/.../features/dashboard/components/filter-bar/dashboard-filter-bar.scss` — NEW
- `frontend/.../features/dashboard/components/overview/dashboard-overview.ts` — added filter wiring
- `frontend/.../features/dashboard/components/overview/dashboard-overview.html` — added filter bar + filteredProjects$
- `frontend/.../features/reporting/components/resource-report/resource-report.ts` — added SlicePipe import (bug fix)
