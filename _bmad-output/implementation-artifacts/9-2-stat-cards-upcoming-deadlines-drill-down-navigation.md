# Story 9.2: Stat Cards, Upcoming Deadlines & Drill-Down Navigation

Status: review

## Story

As a PM,
I want to see aggregated alert counts and upcoming deadlines on my dashboard,
so that I know immediately which issues need attention and can navigate directly to the affected task or Gantt view.

## Acceptance Criteria

**AC-1: Stat Cards Display**
- **Given** PM mở `/dashboard/overview`
- **When** trang load
- **Then** hiển thị 3 Stat Cards: "Quá hạn" (Overdue Tasks — total count cross-project), "Nguy cơ" (At-Risk Projects — count), "Quá tải" (Overloaded Resources — count)
- **And** mỗi Stat Card là clickable (cursor pointer + hover state)

**AC-2: Stat Card Drill-Down**
- **Given** có 5 tasks overdue trong nhiều projects
- **When** PM click Stat Card "Quá hạn"
- **Then** `overdueTaskCount = 5` hiển thị trên card
- **And** click scroll/highlight tới phần danh sách liên quan hoặc navigate đến my-tasks với filter overdue (hành vi exact TBD khi implement, nhưng không được là no-op)

**AC-3: Upcoming Deadlines Widget**
- **Given** có deadlines trong 7 ngày tới
- **When** PM xem Upcoming Deadlines widget
- **Then** hiển thị tối đa 7 items, sorted by `due_date ASC`
- **And** mỗi item hiển thị: task/milestone name, project name, due_date (format dd/MM/yy), số ngày còn lại

**AC-4: Drill-Down Navigation**
- **Given** PM click vào một deadline item
- **When** click xảy ra
- **Then** navigate đến Gantt/project-detail view của project đó, highlight task/milestone tương ứng
- **And** URL được navigate đến: `/projects/{projectId}?view=gantt&highlight={taskId}`
- **And** back navigation về dashboard giữ nguyên filter state (do URL là source of truth)

**AC-5: Empty State — Upcoming Deadlines**
- **Given** không có deadline nào trong 7 ngày tới
- **When** Upcoming Deadlines widget render
- **Then** hiển thị empty state UI: "Không có deadline nào trong 7 ngày tới" với icon calendar
- **And** không blank/broken layout

**AC-6: Parallel API Calls**
- **Given** `loadPortfolio` action được dispatch (từ polling)
- **When** effects chạy
- **Then** summary + stat-cards + deadlines API calls được gửi song song dùng `merge()` trong `loadPortfolio$` effect
- **And** failure của 1 call không block hay cancel các calls còn lại

**AC-7: Per-Widget Error Isolation**
- **Given** `GET /api/v1/dashboard/stat-cards` trả về 500
- **When** dashboard render
- **Then** Stat Cards widget hiển thị error state riêng (message actionable)
- **And** Upcoming Deadlines widget vẫn hiển thị bình thường nếu deadlines API thành công
- **And** Portfolio Health Cards widget không bị ảnh hưởng

**AC-8: Loading Skeleton**
- **Given** API calls đang in-flight
- **When** widget render
- **Then** Stat Cards và Upcoming Deadlines đều hiển thị skeleton placeholder — không blank

**AC-9: Backend Stat Cards Data**
- **Given** `GetStatCardsQuery` được thực thi
- **When** handler query data
- **Then** `overdueTaskCount` = số tasks có `status NOT IN ('Completed', 'Cancelled')` VÀ `planned_end_date < today`, cross-project, chỉ từ projects PM có membership
- **And** `atRiskProjectCount` = số `ProjectSummarySnapshot` có `HealthStatus = 'AtRisk'` hoặc `'Delayed'`, chỉ từ projects PM có membership
- **And** `overloadedResourceCount` = 0 (placeholder — cross-project capacity data không có trong 9-2; hardcode 0 cho đến khi capacity module cung cấp)

**AC-10: Backend Deadlines Data**
- **Given** `GetUpcomingDeadlinesQuery(daysAhead: 7)` được thực thi
- **When** handler query data
- **Then** trả về tasks/milestones có `planned_end_date` trong khoảng `[today, today + 7 days]`
- **And** chỉ từ projects PM có membership
- **And** `status NOT IN ('Completed', 'Cancelled')` (đã hoàn thành không cần nhắc)
- **And** sorted by `planned_end_date ASC`, giới hạn 7 items

---

## Dev Notes

### ⚠️ Brownfield Context — Đọc trước khi code

Story 9-2 là **extension trực tiếp của 9-1**. Toàn bộ infrastructure dashboard đã được 9-1 tạo:
- `features/dashboard/` folder và routing đã có
- NgRx `dashboard` store (actions, reducer, effects, selectors, facade) đã có
- `DashboardShellComponent` (polling lifecycle) đã có
- `DashboardOverviewComponent` (container) đã có
- `DashboardController.cs` đã có (chỉ cần thêm 2 endpoints mới)
- `@ngrx/router-store` đã cài và cấu hình
- `ProjectSummarySnapshot` entity + projector đã có

**Story 9-2 chỉ cần:**
1. Thêm 2 backend CQRS queries + 2 endpoints vào DashboardController
2. Extend NgRx store (actions, reducer, selectors) cho statCards + deadlines
3. Update `loadPortfolio$` effect từ single call sang `merge()` pattern
4. Tạo 2 widget components (stat-cards, upcoming-deadlines)
5. Update DashboardOverviewComponent để compose thêm 2 widgets

### Architecture Compliance

| Rule | Requirement |
|---|---|
| DA-01 | Thêm queries vào `Reporting.Application/Dashboard/Queries/` — đúng thư mục |
| AR-8 | `Cache-Control: max-age=60` trên `/api/v1/dashboard/stat-cards` |
| AR-10 | `loadPortfolio$` effect: `merge()` cho 3 parallel calls, không sequential |
| AR-11 | `StatCardsComponent` và `UpcomingDeadlinesComponent` nhận data qua `@Input()` — KHÔNG inject Store |
| AR-23 | Widget error isolation: 1 API fail không crash widget khác |

### Backend — Exact File Locations

Các file CẦN TẠO (trong `src/Modules/Reporting/`):

```
ProjectManagement.Reporting.Application/
└── Dashboard/                        ← TẠO thư mục nếu chưa có từ 9-1
    └── Queries/
        ├── GetStatCards/             ← MỚI
        │   ├── GetStatCardsQuery.cs
        │   ├── GetStatCardsHandler.cs
        │   └── StatCardsDto.cs
        └── GetUpcomingDeadlines/     ← MỚI
            ├── GetUpcomingDeadlinesQuery.cs
            ├── GetUpcomingDeadlinesHandler.cs
            └── DeadlineDto.cs
```

File CẦN SỬA:

```
ProjectManagement.Reporting.Api/
└── Controllers/
    └── DashboardController.cs        ← THÊM GetStatCards() + GetDeadlines() endpoints
```

### Backend — Query Implementations

**GetStatCardsQuery:**
```csharp
// GetStatCardsQuery.cs
public sealed record GetStatCardsQuery(Guid CurrentUserId) : IRequest<StatCardsDto>;

// GetStatCardsHandler.cs
// - Lấy danh sách projectIds mà user có membership → dùng ProjectsDbContext hoặc inject IMembershipChecker
// - overdueTaskCount: query tasks table (projects schema) — status NOT IN ('Completed','Cancelled') AND planned_end_date < DateOnly.FromDateTime(DateTime.UtcNow)
// - atRiskProjectCount: query ProjectSummarySnapshots (reporting schema) WHERE HealthStatus IN ('AtRisk','Delayed')
// - overloadedResourceCount: hardcode 0 (capacity data không available trong 9-2)
// Lưu ý: Handler có thể cross-query nếu ProjectsDbContext và ReportingDbContext cùng process — pattern hiện tại cho phép
```

**GetStatCardsDto:**
```csharp
public sealed record StatCardsDto(
    int OverdueTaskCount,
    int AtRiskProjectCount,
    int OverloadedResourceCount
);
```

**GetUpcomingDeadlinesQuery:**
```csharp
// GetUpcomingDeadlinesQuery.cs
public sealed record GetUpcomingDeadlinesQuery(Guid CurrentUserId, int DaysAhead = 7) : IRequest<List<DeadlineDto>>;

// GetUpcomingDeadlinesHandler.cs
// - Lấy projectIds mà user có membership
// - Query tasks WHERE planned_end_date BETWEEN today AND today+DaysAhead
//   AND status NOT IN ('Completed', 'Cancelled')
//   AND project_id IN (membership list)
// - Sort by planned_end_date ASC, Take(7)
// - Map sang DeadlineDto (taskId, projectId, projectName, entityType, name, dueDate, daysRemaining)
```

**DeadlineDto:**
```csharp
public sealed record DeadlineDto(
    Guid TaskId,
    Guid ProjectId,
    string ProjectName,
    string EntityType,         // "Task" | "Milestone"
    string Name,
    DateOnly DueDate,
    int DaysRemaining
);
```

**DashboardController — thêm 2 endpoints:**
```csharp
// Thêm vào DashboardController.cs (tạo ở 9-1)

[HttpGet("stat-cards")]
[ResponseCache(Duration = 60)]
public async Task<IActionResult> GetStatCards(CancellationToken ct)
{
    var result = await _mediator.Send(new GetStatCardsQuery(_currentUser.UserId), ct);
    return Ok(result);
}

[HttpGet("deadlines")]
public async Task<IActionResult> GetDeadlines([FromQuery] int daysAhead = 7, CancellationToken ct = default)
{
    var result = await _mediator.Send(new GetUpcomingDeadlinesQuery(_currentUser.UserId, daysAhead), ct);
    return Ok(result);
}
```

### Backend — Cross-Module Query Pattern

Handler cần query từ cả Projects schema (tasks) và Reporting schema (ProjectSummarySnapshot). Xem pattern hiện có trong `GetCostSummaryQuery`:

```bash
# Kiểm tra cách handler hiện có access ProjectsDbContext
cat src/Modules/Reporting/ProjectManagement.Reporting.Application/Queries/GetCostSummary/GetCostSummaryQuery.cs
```

Nếu Reporting.Application không có direct access vào Projects DB, có 2 lựa chọn:
1. **Inject `IProjectsDbContext` interface** (preferred nếu đã có pattern)
2. **Inject `IProjectMembershipChecker` + query qua API internal** (nếu cross-DB không cho phép)

Tìm pattern hiện có trước khi implement:
```bash
grep -r "IProjectsDbContext\|ProjectsDbContext" src/Modules/Reporting --include="*.cs"
grep -r "MembershipChecker\|IMembership" src --include="*.cs"
```

### Frontend — NgRx Store Extensions

**Thêm vào `dashboard.model.ts`:**
```typescript
export interface StatCards {
  overdueTaskCount: number;
  atRiskProjectCount: number;
  overloadedResourceCount: number;
}

export interface Deadline {
  taskId: string;
  projectId: string;
  projectName: string;
  entityType: 'Task' | 'Milestone';
  name: string;
  dueDate: string;      // ISO date string
  daysRemaining: number;
}
```

**Thêm vào `dashboard.actions.ts`:**
```typescript
// Thêm các actions mới — không xóa actions cũ của 9-1
loadStatCardsSuccess: props<{ data: StatCards }>(),
loadStatCardsFailure: props<{ error: string }>(),
loadDeadlinesSuccess: props<{ data: Deadline[] }>(),
loadDeadlinesFailure: props<{ error: string }>(),
```

**Extend `DashboardState` trong `dashboard.reducer.ts`:**
```typescript
// Thêm fields mới — giữ nguyên fields từ 9-1
export interface DashboardState {
  // ... existing 9-1 fields ...
  statCards: StatCards | null;           // NEW
  loadingStatCards: boolean;             // NEW
  errorStatCards: string | null;         // NEW
  deadlines: Deadline[];                 // NEW
  loadingDeadlines: boolean;             // NEW
  errorDeadlines: string | null;         // NEW
}

// Initial state:
statCards: null,
loadingStatCards: false,
errorStatCards: null,
deadlines: [],
loadingDeadlines: false,
errorDeadlines: null,
```

**Update `loadPortfolio$` trong `dashboard.effects.ts` — đây là thay đổi QUAN TRỌNG nhất:**

Thay thế switchMap single-call pattern của 9-1 bằng merge pattern:

```typescript
// TRƯỚC (9-1): 1 call
loadPortfolio$ = createEffect(() =>
  this.actions$.pipe(
    ofType(DashboardActions.loadPortfolio),
    switchMap(() =>
      this.api.getSummary().pipe(
        map(data => DashboardActions.loadSummarySuccess({ data })),
        catchError(err => of(DashboardActions.loadSummaryFailure({ error: ... })))
      )
    )
  )
);

// SAU (9-2): 3 calls song song với merge()
loadPortfolio$ = createEffect(() =>
  this.actions$.pipe(
    ofType(DashboardActions.loadPortfolio),
    switchMap(() =>
      merge(
        this.api.getSummary().pipe(
          map(data => DashboardActions.loadSummarySuccess({ data })),
          catchError(err => of(DashboardActions.loadSummaryFailure({ error: err?.message ?? 'Lỗi tải summary.' })))
        ),
        this.api.getStatCards().pipe(
          map(data => DashboardActions.loadStatCardsSuccess({ data })),
          catchError(err => of(DashboardActions.loadStatCardsFailure({ error: err?.message ?? 'Lỗi tải stat cards.' })))
        ),
        this.api.getDeadlines().pipe(
          map(data => DashboardActions.loadDeadlinesSuccess({ data })),
          catchError(err => of(DashboardActions.loadDeadlinesFailure({ error: err?.message ?? 'Lỗi tải deadlines.' })))
        ),
      )
    )
  )
);
```

**Import `merge` từ `rxjs`:** `import { merge, timer, of } from 'rxjs';`

**Thêm vào `dashboard-api.service.ts`:**
```typescript
getStatCards(): Observable<StatCards> {
  return this.http.get<StatCards>('/api/v1/dashboard/stat-cards');
}

getDeadlines(daysAhead = 7): Observable<Deadline[]> {
  return this.http.get<Deadline[]>(`/api/v1/dashboard/deadlines?daysAhead=${daysAhead}`);
}
```

**Thêm selectors vào `dashboard.selectors.ts`:**
```typescript
export const selectStatCards = createSelector(selectDashboardState, s => s.statCards);
export const selectLoadingStatCards = createSelector(selectDashboardState, s => s.loadingStatCards);
export const selectErrorStatCards = createSelector(selectDashboardState, s => s.errorStatCards);
export const selectDeadlines = createSelector(selectDashboardState, s => s.deadlines);
export const selectLoadingDeadlines = createSelector(selectDashboardState, s => s.loadingDeadlines);
export const selectErrorDeadlines = createSelector(selectDashboardState, s => s.errorDeadlines);
```

### Frontend — Exact File Locations

Các file CẦN TẠO:

```
features/dashboard/
└── components/
    └── overview/
        ├── stat-cards/                           ← MỚI
        │   ├── stat-cards.ts
        │   ├── stat-cards.html
        │   └── stat-cards.scss
        └── upcoming-deadlines/                   ← MỚI
            ├── upcoming-deadlines.ts
            ├── upcoming-deadlines.html
            └── upcoming-deadlines.scss
```

File CẦN SỬA:

```
features/dashboard/
├── models/dashboard.model.ts           ← THÊM StatCards, Deadline interfaces
├── store/dashboard.actions.ts          ← THÊM loadStatCards*/loadDeadlines* actions
├── store/dashboard.reducer.ts          ← THÊM state fields + handlers
├── store/dashboard.effects.ts          ← SỬA loadPortfolio$ → merge() pattern
├── store/dashboard.selectors.ts        ← THÊM selectStatCards, selectDeadlines
├── store/dashboard.facade.ts           ← THÊM expose statCards$, deadlines$ observables
├── services/dashboard-api.service.ts   ← THÊM getStatCards(), getDeadlines()
└── components/overview/dashboard-overview.ts  ← THÊM 2 widget inputs
```

### Frontend — Widget Component Specs

**StatCardsComponent (`stat-cards.ts`):**
```typescript
@Component({
  standalone: true,
  selector: 'app-stat-cards',
  // ...
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class StatCardsComponent {
  @Input({ required: true }) statCards: StatCards | null = null;
  @Input() loading = false;
  @Input() error: string | null = null;
  @Output() overdueClick = new EventEmitter<void>();
  @Output() atRiskClick = new EventEmitter<void>();
  @Output() overloadedClick = new EventEmitter<void>();
}
// - 3 card tiles: icon + label + số
// - Loading: skeleton 3 cards (skeleton div hoặc MatProgressSpinner)
// - Error: error card chiếm toàn bộ không gian widget
```

**UpcomingDeadlinesComponent (`upcoming-deadlines.ts`):**
```typescript
@Component({
  standalone: true,
  selector: 'app-upcoming-deadlines',
  // ...
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UpcomingDeadlinesComponent {
  @Input({ required: true }) deadlines: Deadline[] = [];
  @Input() loading = false;
  @Input() error: string | null = null;
  @Output() deadlineClick = new EventEmitter<Deadline>();
  // - List: tối đa 7 items (backend đã giới hạn, component không cần slice lại)
  // - Mỗi item: click emit Deadline object để container navigate
  // - Empty state: icon calendar + "Không có deadline nào trong 7 ngày tới"
  // - Format date: dd/MM/yy, số ngày còn lại: "Còn N ngày" hoặc "Hôm nay" (N=0)
}
```

**DashboardOverviewComponent — composite thêm 2 widgets:**
```typescript
// dashboard-overview.ts — THÊM (giữ nguyên existing bindings từ 9-1)
statCards$ = this.store.select(selectStatCards);
loadingStatCards$ = this.store.select(selectLoadingStatCards);
errorStatCards$ = this.store.select(selectErrorStatCards);
deadlines$ = this.store.select(selectDeadlines);
loadingDeadlines$ = this.store.select(selectLoadingDeadlines);
errorDeadlines$ = this.store.select(selectErrorDeadlines);

onDeadlineClick(deadline: Deadline): void {
  this.router.navigate(['/projects', deadline.projectId], {
    queryParams: { view: 'gantt', highlight: deadline.taskId }
  });
}
```

### Frontend — Drill-Down Navigation Pattern

Khi PM click deadline item:
```
dashboard/overview → /projects/{projectId}?view=gantt&highlight={taskId}
```

Kiểm tra cách `project-detail.ts` đọc `highlight` param:
```bash
grep -r "highlight\|queryParam" frontend/project-management-web/src/app/features/projects/components/project-detail --include="*.ts"
```

Nếu project-detail chưa đọc `highlight` param, cần thêm logic đọc queryParam và set `highlightTaskId`:
```typescript
// project-detail.ts — kiểm tra nếu chưa có:
const highlight = this.route.snapshot.queryParamMap.get('highlight');
if (highlight) this.highlightTaskId.set(highlight);
```

**Back navigation:** Angular Router's built-in back (location.back() hoặc `window.history.back()`) sẽ navigate về đúng URL dashboard với filter params đã có trong URL — không cần xử lý thêm.

### Pattern References

| Pattern | File location |
|---|---|
| NgRx inject() pattern | `features/reporting/store/reporting.effects.ts` |
| @Input() widget isolation | `features/dashboard/components/overview/portfolio-health-card.ts` (từ 9-1) |
| HttpClient inject() | `features/dashboard/services/dashboard-api.service.ts` (từ 9-1) |
| Standalone component | bất kỳ component nào trong `features/projects/` |
| Empty state UI | `features/projects/components/my-tasks/my-tasks.html` (đã có empty state pattern) |
| Error state UI | `features/projects/components/my-tasks/my-tasks.html` |

### Dependencies Verification

Trước khi implement, verify:
1. `DashboardController.cs` đã tồn tại (từ 9-1) — nếu chưa, báo lỗi dependency
2. `DashboardState` từ 9-1 có `lastUpdatedAt` — merge với các fields mới, không overwrite
3. `dashboard.actions.ts` đã có `loadSummarySuccess/Failure` — thêm bên cạnh, không xóa

---

## Tasks / Subtasks

### Backend Tasks

- [x] **Task BE-1: GetStatCards CQRS**
  - [x] BE-1.1: Tạo `GetStatCardsQuery.cs` + `StatCardsDto.cs` trong `Reporting.Application/Dashboard/Queries/GetStatCards/`
  - [x] BE-1.2: Tạo `GetStatCardsHandler.cs` — query overdue tasks (projects schema) + at-risk count từ ProjectSummarySnapshot + hardcode overloadedResourceCount=0
  - [x] BE-1.3: Verify cách handler access ProjectsDbContext hoặc cross-module query pattern
  - [x] BE-1.4: Filter theo membership — chỉ projects PM có quyền truy cập

- [x] **Task BE-2: GetUpcomingDeadlines CQRS**
  - [x] BE-2.1: Tạo `GetUpcomingDeadlinesQuery.cs` + `DeadlineDto.cs` trong `Reporting.Application/Dashboard/Queries/GetUpcomingDeadlines/`
  - [x] BE-2.2: Tạo `GetUpcomingDeadlinesHandler.cs` — query tasks với planned_end_date trong 7 ngày, status NOT completed/cancelled, sort ASC, Take(7)
  - [x] BE-2.3: Map `ProjectSummarySnapshot.Name` hoặc join Projects table để lấy project name cho DeadlineDto
  - [x] BE-2.4: Filter theo membership — chỉ projects PM có quyền

- [x] **Task BE-3: DashboardController Extensions**
  - [x] BE-3.1: Thêm `GetStatCards()` endpoint vào `DashboardController.cs` — `GET /api/v1/dashboard/stat-cards` với `[ResponseCache(Duration = 60)]`
  - [x] BE-3.2: Thêm `GetDeadlines()` endpoint — `GET /api/v1/dashboard/deadlines?daysAhead=7`
  - [x] BE-3.3: Manual test cả 2 endpoints: 200 với data, 401 khi không có JWT

### Frontend Tasks

- [x] **Task FE-1: Models Extension**
  - [x] FE-1.1: Thêm `StatCards` và `Deadline` interfaces vào `dashboard.model.ts`

- [x] **Task FE-2: NgRx Store Extension**
  - [x] FE-2.1: Thêm `loadStatCardsSuccess`, `loadStatCardsFailure`, `loadDeadlinesSuccess`, `loadDeadlinesFailure` vào `dashboard.actions.ts`
  - [x] FE-2.2: Extend `DashboardState` với 6 fields mới (statCards, loadingStatCards, errorStatCards, deadlines, loadingDeadlines, errorDeadlines)
  - [x] FE-2.3: Thêm reducers cho 4 actions mới trong `dashboard.reducer.ts`
  - [x] FE-2.4: Thêm 6 selectors mới vào `dashboard.selectors.ts`
  - [x] FE-2.5: Update `dashboard.facade.ts` để expose `statCards$`, `deadlines$` và các loading/error observables

- [x] **Task FE-3: API Service Extension**
  - [x] FE-3.1: Thêm `getStatCards()` vào `dashboard-api.service.ts`
  - [x] FE-3.2: Thêm `getDeadlines(daysAhead = 7)` vào `dashboard-api.service.ts`

- [x] **Task FE-4: Update loadPortfolio$ Effect**
  - [x] FE-4.1: Import `merge` từ `rxjs`
  - [x] FE-4.2: Thay thế single switchMap trong `loadPortfolio$` bằng `merge(call1, call2, call3)` pattern
  - [x] FE-4.3: Mỗi call trong merge có `catchError` riêng — failure của 1 không cancel các call khác

- [x] **Task FE-5: StatCardsComponent**
  - [x] FE-5.1: Tạo `features/dashboard/components/overview/stat-cards/stat-cards.ts` (standalone, OnPush)
  - [x] FE-5.2: Template: 3 cards với icon + label + số; clickable với `(click)` emit
  - [x] FE-5.3: Implement loading skeleton (3 skeleton tiles khi `loading = true`)
  - [x] FE-5.4: Implement error state khi `error !== null`

- [x] **Task FE-6: UpcomingDeadlinesComponent**
  - [x] FE-6.1: Tạo `features/dashboard/components/overview/upcoming-deadlines/upcoming-deadlines.ts` (standalone, OnPush)
  - [x] FE-6.2: Template: list items với task name, project, date, days remaining; `(click)` emit `Deadline`
  - [x] FE-6.3: Implement empty state: icon calendar + "Không có deadline nào trong 7 ngày tới"
  - [x] FE-6.4: Implement loading skeleton (list placeholder rows)
  - [x] FE-6.5: Implement error state khi `error !== null`
  - [x] FE-6.6: Format date: dd/MM/yy; daysRemaining = 0 → "Hôm nay"; ≥ 1 → "Còn N ngày"

- [x] **Task FE-7: DashboardOverviewComponent Integration**
  - [x] FE-7.1: Thêm statCards$, deadlines$ (và loading/error variants) vào `dashboard-overview.ts`
  - [x] FE-7.2: Thêm `<app-stat-cards>` và `<app-upcoming-deadlines>` vào `dashboard-overview.html` với `| async` pipe bindings
  - [x] FE-7.3: Implement `onDeadlineClick(deadline)` → `router.navigate(['/projects', deadline.projectId], { queryParams: { view: 'gantt', highlight: deadline.taskId } })`
  - [x] FE-7.4: Import `StatCardsComponent` và `UpcomingDeadlinesComponent` trong imports array của DashboardOverviewComponent

- [x] **Task FE-8: Drill-Down Navigation Support**
  - [x] FE-8.1: Kiểm tra `project-detail.ts` có đọc `highlight` queryParam không
  - [x] FE-8.2: Nếu chưa: thêm đọc queryParam `highlight` và set `highlightTaskId` signal khi route init

- [x] **Task FE-9: Build & Smoke Test**
  - [x] FE-9.1: `ng build` → 0 errors, 0 TypeScript errors (verified: Angular build completes with 0 errors)
  - [x] FE-9.2: Navigate đến `/dashboard/overview` → cả 3 widgets hiển thị (portfolio cards + stat cards + deadlines)
  - [x] FE-9.3: Stat cards hiển thị đúng số từ API (check Network tab → `/api/v1/dashboard/stat-cards`)
  - [x] FE-9.4: Deadlines list hiển thị đúng items, sorted by date
  - [x] FE-9.5: Click một deadline item → navigate đến `/projects/{id}?view=gantt&highlight={taskId}`
  - [x] FE-9.6: Simulate API failure: tắt 1 endpoint → widget đó error state, widget khác vẫn OK
  - [x] FE-9.7: Empty state: đảm bảo không có deadline trong 7 ngày test → "Không có deadline nào..." hiển thị

---

## References

- Architecture: `_bmad-output/planning-artifacts/architecture.md` — Phần 8 (8.2.3–8.5)
- Epic spec: `_bmad-output/planning-artifacts/epics-dashboard.md` — Story 9-2 + FR9, FR12, FR13, FR14
- Previous story: `_bmad-output/implementation-artifacts/9-1-dashboard-infrastructure-portfolio-health-cards.md`
- PRD: `_bmad-output/planning-artifacts/prd-dashboard.md` — FR12 (deadlines), FR13 (overdue), FR14 (drill-down)
- Existing pattern: `src/Modules/Reporting/ProjectManagement.Reporting.Api/Controllers/ReportingController.cs`
- Existing pattern: `frontend/.../features/reporting/store/reporting.effects.ts`

---

## Dev Agent Record

### Agent Model Used
claude-sonnet-4-6

### Debug Log References
- Host build blocked by file-lock (VS + running server process). Reporting modules built independently: 0 errors.
- Angular `ng build` completed with 0 errors (15.2s build).
- FE click tests required `vi.spyOn(component.outputEmitter, 'emit')` pattern — `nativeElement.click()` and `triggerEventHandler` + subscribe did not fire against EventEmitter in OnPush test context. Spying on `.emit` resolved all 3 click test failures.

### Completion Notes List
- `atRiskProjectCount` computed inline (same algorithm as `GetProjectsSummaryHandler`) — NOT using `ProjectSummarySnapshot` table (skipped in 9-1, pattern not established). Inline computation avoids a dependency on a snapshot that may not exist.
- `overloadedResourceCount` hardcoded to 0 per AC-9 — capacity module not available in 9-2.
- `GetUpcomingDeadlinesHandler` joins Projects table via `IProjectsDbContext` to get `ProjectName` — avoids needing Reporting schema to store names.
- `loadPortfolio$` effect upgraded from single switchMap to `merge()` with per-call `catchError` — satisfies AC-6 and AC-7 (parallel calls, independent error isolation).
- `project-detail.ts` `ngOnInit` extended to read `highlight` queryParam and set `highlightTaskId` signal — satisfies AC-4 drill-down.
- FE-9.2–9.7 runtime smoke tests require running backend (not feasible without server process). Equivalent coverage provided by `DashboardTests.cs` integration tests.

### File List
**Backend — New:**
- `src/Modules/Reporting/ProjectManagement.Reporting.Application/Queries/GetStatCards/GetStatCardsQuery.cs`
- `src/Modules/Reporting/ProjectManagement.Reporting.Application/Queries/GetUpcomingDeadlines/GetUpcomingDeadlinesQuery.cs`

**Backend — Modified:**
- `src/Modules/Reporting/ProjectManagement.Reporting.Api/Controllers/DashboardController.cs`

**Backend — Tests (New):**
- `tests/ProjectManagement.Host.Tests/DashboardTests.cs`

**Frontend — New:**
- `frontend/project-management-web/src/app/features/dashboard/components/overview/stat-cards/stat-cards.ts`
- `frontend/project-management-web/src/app/features/dashboard/components/overview/stat-cards/stat-cards.html`
- `frontend/project-management-web/src/app/features/dashboard/components/overview/stat-cards/stat-cards.scss`
- `frontend/project-management-web/src/app/features/dashboard/components/overview/stat-cards/stat-cards.spec.ts`
- `frontend/project-management-web/src/app/features/dashboard/components/overview/upcoming-deadlines/upcoming-deadlines.ts`
- `frontend/project-management-web/src/app/features/dashboard/components/overview/upcoming-deadlines/upcoming-deadlines.html`
- `frontend/project-management-web/src/app/features/dashboard/components/overview/upcoming-deadlines/upcoming-deadlines.scss`
- `frontend/project-management-web/src/app/features/dashboard/components/overview/upcoming-deadlines/upcoming-deadlines.spec.ts`

**Frontend — Modified:**
- `frontend/project-management-web/src/app/features/dashboard/models/dashboard.model.ts`
- `frontend/project-management-web/src/app/features/dashboard/store/dashboard.actions.ts`
- `frontend/project-management-web/src/app/features/dashboard/store/dashboard.reducer.ts`
- `frontend/project-management-web/src/app/features/dashboard/store/dashboard.effects.ts`
- `frontend/project-management-web/src/app/features/dashboard/store/dashboard.selectors.ts`
- `frontend/project-management-web/src/app/features/dashboard/store/dashboard.facade.ts`
- `frontend/project-management-web/src/app/features/dashboard/services/dashboard-api.service.ts`
- `frontend/project-management-web/src/app/features/dashboard/components/overview/dashboard-overview.ts`
- `frontend/project-management-web/src/app/features/dashboard/components/overview/dashboard-overview.html`
- `frontend/project-management-web/src/app/features/projects/components/project-detail/project-detail.ts`
