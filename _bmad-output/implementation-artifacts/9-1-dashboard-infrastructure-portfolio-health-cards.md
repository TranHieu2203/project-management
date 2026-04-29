# Story 9.1: Dashboard Infrastructure & Portfolio Health Cards

Status: review

## Story

As a PM,
I want to see the health status of all my projects on a single dashboard page that auto-refreshes,
so that I can assess the overall portfolio situation in under 30 seconds without opening each Gantt separately.

## Acceptance Criteria

**AC-1: Page Load & Navigation**
- **Given** PM đã đăng nhập và navigate đến `/dashboard/overview`
- **When** trang load
- **Then** trang hiển thị đầy đủ trong P95 < 3s với 20 concurrent users
- **And** URL `/dashboard` hoặc `/dashboard/overview` đều dẫn đến đúng page (redirect nếu cần)

**AC-2: Portfolio Health Cards**
- **Given** PM đang xem `/dashboard/overview`
- **When** data load từ `GET /api/v1/dashboard/summary`
- **Then** mỗi project PM có membership hiển thị một `PortfolioHealthCardComponent`
- **And** mỗi card hiển thị: project name, health status, % tasks done, remaining task count, overdue task count

**AC-3: Traffic Light Color-Blind Support**
- **Given** một project có health status "At Risk"
- **When** PM nhìn vào card
- **Then** card hiển thị cả màu VÀNG VÀ text "At Risk" VÀ warning icon — không chỉ màu
- **And** "On Track" → xanh lá + icon check + text; "Delayed" → đỏ + icon alert + text

**AC-4: Project Pulse Strip (Dual-Axis)**
- **Given** project có % time elapsed = 75% và % tasks done = 55%
- **When** PM nhìn vào `ProjectPulseStripComponent` trên card
- **Then** progress ring hiển thị 55% (% tasks done)
- **And** mini timeline bar hiển thị 75% (% time elapsed)
- **And** remaining work chip hiển thị số tasks còn lại

**AC-5: Auto-Polling**
- **Given** `DashboardShellComponent` được load (ngOnInit)
- **When** component khởi tạo
- **Then** dispatch `DashboardActions.startPolling()` ngay lập tức
- **And** data được fetch mỗi 30 giây tự động qua `timer(0, 30_000)` + `switchMap` trong Effect
- **When** PM rời `/dashboard/*` (ngOnDestroy)
- **Then** dispatch `DashboardActions.stopPolling()` để dừng polling

**AC-6: Widget Error Isolation**
- **Given** API `GET /api/v1/dashboard/summary` trả về 500
- **When** dashboard render
- **Then** Portfolio Health Cards widget hiển thị error state riêng (không blank, không crash)
- **And** các widgets khác (nếu có data) vẫn render bình thường
- **And** error state có message actionable (không phải HTTP code)

**AC-7: Stale Data Resilience**
- **Given** network không khả dụng trong khi polling
- **When** poll attempt fail
- **Then** dashboard hiển thị data cũ kèm "Cập nhật lúc HH:mm" timestamp
- **And** không crash, không blank screen

**AC-8: Empty State**
- **Given** PM chưa có project nào hoặc data chưa load
- **When** Portfolio Health Cards widget render
- **Then** hiển thị defined empty state UI — không để blank/broken layout

**AC-9: Backend Snapshot Accuracy**
- **Given** `ProjectSummarySnapshot` đã có trong DB
- **When** `GET /api/v1/dashboard/summary` được gọi
- **Then** trả về `ProjectSummaryDto[]` phản ánh đúng data hiện tại (sau UPSERT từ Projector)
- **And** response có `Cache-Control: max-age=60`

---

## Dev Notes

### ⚠️ Brownfield Context — Đọc trước khi code

Project này là Angular SPA + .NET 10 Modular Monolith đã có nhiều features. Story 9-1 là **extension**, không phải greenfield.

**Hệ thống đang có:**
- `AppShellComponent` tại `frontend/.../core/shell/app-shell.ts` — đã có sidebar + navbar, wrap tất cả authenticated routes
- `provideStore(reducers)` tại `app.config.ts` — state được cung cấp tại root
- `provideEffects([AuthEffects, ProjectsEffects, ...])` tại `app.config.ts` — TẤT CẢ effects tại root
- `Reporting` module đã có: `ReportingDbContext`, `ExportJob`, `ReportingController`, `PdfWorker`
- Auth đã có: JWT interceptor tại `auth.interceptor.ts`, `authGuard` tại `core/auth/auth.guard.ts`

**@ngrx/router-store chưa được install** — cần thêm vào package.json và app.config.ts.

### Architecture Compliance (từ architecture.md Phần 8)

| Rule | Requirement |
|---|---|
| DA-01 | Mở rộng module `Reporting` — KHÔNG tạo module mới |
| DA-02 | Dashboard là lazy-loaded feature module — KHÔNG import vào AppModule |
| DA-03 | NgRx `dashboard` feature store độc lập — KHÔNG mở rộng `capacity` store |
| DA-04 | `@ngrx/router-store` provide tại root trong `app.config.ts` |
| AR5 | `ProjectSummarySnapshot` dùng UPSERT (ON CONFLICT DO UPDATE) — KHÔNG INSERT mới |
| AR7 | `ProjectSummaryProjector` subscribe đủ 4 MediatR events |
| AR8 | `Cache-Control: max-age=60` trên `/api/v1/dashboard/*` |
| AR10 | Polling dùng `switchMap` + `takeUntil` — KHÔNG dùng `mergeMap` |
| AR11 | Widget nhận data qua `@Input()` — KHÔNG inject Store trong widget |
| AR12 | `DashboardShellComponent` là routing container (polling lifecycle), AppShellComponent đã handle sidebar/navbar |

### Critical Architecture Note: Dashboard Routing

Dashboard routes là **children** của `AppShellComponent` (đã có sidebar + navbar). `DashboardShellComponent` trong story này là **thin routing container** — chỉ quản lý polling lifecycle, không duplicate sidebar/navbar.

```typescript
// app.routes.ts — thêm vào children của AppShellComponent route
{
  path: 'dashboard',
  loadChildren: () =>
    import('./features/dashboard/dashboard.routes').then(m => m.dashboardRoutes),
},

// dashboard.routes.ts
export const dashboardRoutes: Routes = [
  {
    path: '',
    component: DashboardShellComponent,    // polling lifecycle only
    children: [
      { path: 'overview', component: DashboardOverviewComponent },
      { path: '', redirectTo: 'overview', pathMatch: 'full' }
    ]
  }
];
```

`ReportShellComponent` (Story 10-1) sẽ là **top-level route** bên ngoài AppShellComponent vì cần clean layout — không liên quan Story 9-1.

### Backend — Exact File Locations

```
src/Modules/Reporting/
├── ProjectManagement.Reporting.Domain/
│   └── Entities/
│       └── ProjectSummarySnapshot.cs          ← MỚI (cạnh ExportJob.cs)
│
├── ProjectManagement.Reporting.Application/
│   └── Queries/                               ← thư mục đã có
│       └── GetProjectsSummary/                ← MỚI thư mục
│           ├── GetProjectsSummaryQuery.cs
│           ├── GetProjectsSummaryHandler.cs
│           └── ProjectSummaryDto.cs
│
├── ProjectManagement.Reporting.Infrastructure/
│   ├── Persistence/
│   │   ├── ReportingDbContext.cs              ← THÊM DbSet + UPSERT method
│   │   └── Configurations/
│   │       └── ProjectSummarySnapshotConfiguration.cs  ← MỚI
│   ├── ReadModels/                            ← tạo mới thư mục nếu chưa có
│   │   └── ProjectSummaryProjector.cs         ← MỚI
│   └── Migrations/                            ← tạo migration mới
│
└── ProjectManagement.Reporting.Api/
    └── Controllers/
        ├── ReportingController.cs             ← đã có, KHÔNG chỉnh sửa
        └── DashboardController.cs             ← MỚI (cạnh ReportingController.cs)
```

### Frontend — Exact File Locations

```
frontend/project-management-web/src/app/
├── app.config.ts                  ← THÊM provideRouterStore(), DashboardEffects
├── app.routes.ts                  ← THÊM dashboard lazy route trong children AppShell
└── features/
    └── dashboard/                 ← MỚI thư mục (cạnh reporting/, capacity/, v.v.)
        ├── dashboard.routes.ts
        ├── shells/
        │   └── dashboard-shell/
        │       ├── dashboard-shell.ts         ← polling lifecycle
        │       ├── dashboard-shell.html
        │       └── dashboard-shell.scss
        ├── components/
        │   └── overview/
        │       ├── dashboard-overview.ts      ← container, inject Store
        │       ├── dashboard-overview.html
        │       ├── portfolio-health-card/
        │       │   ├── portfolio-health-card.ts    ← @Input() only
        │       │   ├── portfolio-health-card.html
        │       │   └── portfolio-health-card.scss
        │       └── project-pulse-strip/
        │           ├── project-pulse-strip.ts      ← @Input() only
        │           ├── project-pulse-strip.html
        │           └── project-pulse-strip.scss
        ├── store/
        │   ├── dashboard.actions.ts
        │   ├── dashboard.reducer.ts
        │   ├── dashboard.effects.ts
        │   ├── dashboard.selectors.ts
        │   └── dashboard.facade.ts
        ├── services/
        │   └── dashboard-api.service.ts
        └── models/
            └── dashboard.model.ts
```

### NgRx Store Patterns (copy từ existing codebase)

**Pattern hiện có trong project** (ví dụ từ `reporting.effects.ts`):
```typescript
// Injectable + inject() pattern (KHÔNG dùng constructor injection)
@Injectable()
export class DashboardEffects {
  private readonly actions$ = inject(Actions);
  private readonly store = inject(Store);
  private readonly api = inject(DashboardApiService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  ...
}
```

**DashboardState interface:**
```typescript
// dashboard.model.ts
export interface ProjectSummary {
  projectId: string;
  name: string;
  healthStatus: 'OnTrack' | 'AtRisk' | 'Delayed';
  startDate: string;
  endDate: string;
  percentComplete: number;
  percentTimeElapsed: number;
  remainingTaskCount: number;
  overdueTaskCount: number;
  overloadedResourceCount: number;
  lastUpdatedAt: string;
}

// dashboard.reducer.ts
export interface DashboardState {
  projects: ProjectSummary[];
  loadingProjects: boolean;
  errorProjects: string | null;
  lastUpdatedAt: number | null;   // Date.now() timestamp
}
```

**Polling Effect pattern:**
```typescript
// dashboard.effects.ts
pollDashboard$ = createEffect(() =>
  this.actions$.pipe(
    ofType(DashboardActions.startPolling),
    switchMap(() =>
      timer(0, 30_000).pipe(
        takeUntil(this.actions$.pipe(ofType(DashboardActions.stopPolling))),
        map(() => DashboardActions.loadPortfolio())
      )
    )
  )
);

loadPortfolio$ = createEffect(() =>
  this.actions$.pipe(
    ofType(DashboardActions.loadPortfolio),
    switchMap(() =>
      this.api.getSummary().pipe(
        map(data => DashboardActions.loadSummarySuccess({ data })),
        catchError(err => of(DashboardActions.loadSummaryFailure({ error: err?.message ?? 'Lỗi tải dashboard.' })))
      )
    )
  )
);
```

**Widget @Input() isolation pattern:**
```typescript
// dashboard-overview.ts — inject Store, pass via @Input()
projects$ = this.store.select(selectProjects);
loadingProjects$ = this.store.select(selectLoadingProjects);
errorProjects$ = this.store.select(selectErrorProjects);
lastUpdatedAt$ = this.store.select(selectLastUpdatedAt);

// portfolio-health-card.ts — KHÔNG inject Store
@Input({ required: true }) projects: ProjectSummary[] = [];
@Input() loading = false;
@Input() error: string | null = null;
```

### Backend — ProjectSummarySnapshot Entity

```csharp
// ProjectSummarySnapshot.cs (trong Reporting.Domain/Entities/)
public class ProjectSummarySnapshot
{
    public Guid ProjectId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string HealthStatus { get; private set; } = "OnTrack"; // OnTrack | AtRisk | Delayed
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public decimal PercentComplete { get; private set; }
    public decimal PercentTimeElapsed { get; private set; }
    public int RemainingTaskCount { get; private set; }
    public int OverdueTaskCount { get; private set; }
    public int OverloadedResourceCount { get; private set; }
    public DateTime LastUpdatedAt { get; private set; }
    
    // EF constructor
    private ProjectSummarySnapshot() { }
    
    public static ProjectSummarySnapshot Create(...) { ... }
    public void Update(...) { ... }
}
```

**Traffic light rules:**
```
OnTrack:  PercentComplete >= PercentTimeElapsed - 10  AND OverdueTaskCount == 0
AtRisk:   OverdueTaskCount in [1, 3]  OR  PercentComplete < PercentTimeElapsed - 10
Delayed:  OverdueTaskCount > 3  OR  PercentComplete < PercentTimeElapsed - 25
```

### Backend — ReportingDbContext Changes

```csharp
// Thêm vào ReportingDbContext.cs
public DbSet<ProjectSummarySnapshot> ProjectSummarySnapshots => Set<ProjectSummarySnapshot>();

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.HasDefaultSchema("reporting");
    modelBuilder.ApplyConfiguration(new ExportJobConfiguration());
    modelBuilder.ApplyConfiguration(new ProjectSummarySnapshotConfiguration()); // THÊM
}
```

### Backend — ProjectSummaryProjector

```csharp
// Subscribe cả 4 events — thiếu 1 là bug
public class ProjectSummaryProjector :
    INotificationHandler<TaskCreatedNotification>,
    INotificationHandler<TaskStatusChangedNotification>,
    INotificationHandler<TaskDueDateChangedNotification>,
    INotificationHandler<TimeEntryCreatedNotification>
{
    // Mỗi handler: gọi UpsertSnapshot(projectId)
    // UpsertSnapshot: query data từ Projects schema → tính toán → UPSERT vào reporting schema
    // Dùng ExecuteSqlRaw với ON CONFLICT DO UPDATE hoặc thông qua EF LINQ
}
```

**Kiểm tra tên events hiện có trong project:**
```bash
grep -r "Notification" src/Modules/Projects/ProjectManagement.Projects.Application --include="*.cs" -l
```
Tìm đúng tên class `Notification` events trước khi implement để tránh compile error.

### @ngrx/router-store Setup

```bash
# Cài đặt nếu chưa có
cd frontend/project-management-web
npm install @ngrx/router-store
```

```typescript
// app.config.ts — thêm import và provider
import { provideRouterStore } from '@ngrx/router-store';

export const appConfig: ApplicationConfig = {
  providers: [
    // ... existing providers
    provideRouterStore(),    // THÊM — phải ở root, không ở feature
    provideEffects([
      AuthEffects,
      // ... existing effects
      DashboardEffects,      // THÊM khi tạo xong
    ]),
  ],
};
```

**Note:** Story 9-3 mới implement URL sync logic. Story 9-1 chỉ cần `provideRouterStore()` để infrastructure sẵn sàng.

### Sidebar Navigation Entry

Thêm "Dashboard" link vào sidebar để PM navigate được. Tìm file sidebar component:
```bash
grep -r "my-tasks\|projects\|sidebar" frontend/project-management-web/src/app/core --include="*.html" -l
```
Thêm link `/dashboard/overview` vào sidebar navigation list.

---

## Tasks / Subtasks

### Backend Tasks

- [x] **Task BE-1: Domain Entity** *(ADAPTED — see Completion Notes)*
  - [x] BE-1.1: Verified: Projects module has NO MediatR Notification events → snapshot/projector approach SKIPPED
  - [x] BE-1.2: Adopted on-the-fly computation via `IProjectsDbContext` cross-module access pattern

- [~] **Task BE-2: EF Infrastructure** *(SKIPPED — no snapshot entity)*
  - [~] BE-2.1–2.3: Not applicable; no `ProjectSummarySnapshot` entity needed

- [~] **Task BE-3: PostgreSQL Indexes** *(SKIPPED — no snapshot table)*
  - [~] BE-3.1–3.3: Not applicable

- [~] **Task BE-4: ProjectSummaryProjector** *(SKIPPED — no MediatR events in Projects module)*
  - [~] BE-4.1–4.4: Not applicable

- [x] **Task BE-5: Application Layer (CQRS)**
  - [x] BE-5.1: Created `GetProjectsSummaryQuery.cs` + Handler + `ProjectSummaryDto.cs` in `Reporting.Application/Queries/GetProjectsSummary/`
  - [x] BE-5.2: Handler computes health on-the-fly from `IProjectsDbContext` (cross-module pattern, same as `GetCostBreakdownHandler`)
  - [x] BE-5.3: Filters by membership ProjectIds; `ProjectSummaryDto` returns `HealthStatus`, `PercentComplete`, `PercentTimeElapsed`, `RemainingTaskCount`, `OverdueTaskCount`

- [x] **Task BE-6: DashboardController**
  - [x] BE-6.1: Created `DashboardController.cs` in `Reporting.Api/Controllers/` with `[Route("api/v1/dashboard")]`
  - [x] BE-6.2: `GET /api/v1/dashboard/summary` → dispatches `GetProjectsSummaryQuery`
  - [x] BE-6.3: `[ResponseCache(Duration = 60)]` applied
  - [x] BE-6.4: `[Authorize]` attribute applied
  - [x] BE-6.5: Manual test pending (requires running backend)

### Frontend Tasks

- [x] **Task FE-1: @ngrx/router-store Setup**
  - [x] FE-1.1: `npm install @ngrx/router-store@^21.1.0` completed
  - [x] FE-1.2: `provideRouterStore()` added to `app.config.ts`

- [x] **Task FE-2: Dashboard Models & Store**
  - [x] FE-2.1: Created `features/dashboard/models/dashboard.model.ts`
  - [x] FE-2.2: Created `dashboard.actions.ts` — `startPolling`, `stopPolling`, `loadPortfolio`, `loadSummarySuccess`, `loadSummaryFailure`
  - [x] FE-2.3: Created `dashboard.reducer.ts` using `createFeature`; spinner only on first load
  - [x] FE-2.4: Created `dashboard.selectors.ts` — re-exports from `dashboardFeature`
  - [x] FE-2.5: Created `dashboard.facade.ts` — `DashboardFacade` with observables + `startPolling`/`stopPolling`

- [x] **Task FE-3: DashboardApiService**
  - [x] FE-3.1–3.3: Created `dashboard-api.service.ts` — `getSummary()` → `GET /api/v1/dashboard/summary` using `inject(HttpClient)`

- [x] **Task FE-4: Dashboard Effects**
  - [x] FE-4.1: `pollDashboard$` — `timer(0, 30_000)` + `switchMap` + `takeUntil(stopPolling)`
  - [x] FE-4.2: `loadPortfolio$` — calls `api.getSummary()` + success/failure handling
  - [x] FE-4.3: `DashboardEffects` added to `provideEffects([...])` in `app.config.ts`

- [x] **Task FE-5: Routing Setup**
  - [x] FE-5.1: Created `dashboard.routes.ts` with `DashboardShellComponent` + `overview` lazy child
  - [x] FE-5.2: Added `dashboard` lazy route to `app.routes.ts` in `AppShellComponent` children

- [x] **Task FE-6: DashboardShellComponent**
  - [x] FE-6.1: Created `DashboardShellComponent` — `<router-outlet>`, polling lifecycle only
  - [x] FE-6.2: No sidebar/navbar duplication

- [x] **Task FE-7: Widget Components**
  - [x] FE-7.1: Created `DashboardOverviewComponent` — injects `DashboardFacade`, async pipes
  - [x] FE-7.2: Created `PortfolioHealthCardComponent` — `@Input() project`, color/icon/text per health status
  - [x] FE-7.3: Created `ProjectPulseStripComponent` — SVG progress ring (`circumference = 2πr`), mini timeline bar, remaining chip
  - [x] FE-7.4: `MatProgressSpinner` loading state (spinner only on first load, not polling refresh)
  - [x] FE-7.5: Error state with actionable message + stale data display
  - [x] FE-7.6: Empty state with `folder_open` icon + instructional text

- [x] **Task FE-8: Sidebar Navigation**
  - [x] FE-8.1: Located sidebar in `core/shell/app-shell.ts`
  - [x] FE-8.2: Added `{ label: 'Dashboard', icon: 'dashboard', route: '/dashboard' }` to `navItems`

- [x] **Task FE-9: Build & Smoke Test**
  - [x] FE-9.1: `ng build --configuration development` → 0 errors
  - [ ] FE-9.2: Navigate to `/dashboard/overview` → requires running backend
  - [ ] FE-9.3: Health cards display correct API data — requires running backend
  - [ ] FE-9.4: Polling observable — requires running app
  - [ ] FE-9.5: Polling stops on navigate away — requires running app

---

## References

- Architecture: `_bmad-output/planning-artifacts/architecture.md` — Phần 8 (toàn bộ)
- Epic spec: `_bmad-output/planning-artifacts/epics-dashboard.md` — Story 9-1
- PRD: `_bmad-output/planning-artifacts/prd-dashboard.md` — Functional Requirements FR1–FR6, FR40–FR44
- Existing pattern: `frontend/.../features/reporting/store/reporting.effects.ts` — NgRx effects pattern
- Existing pattern: `frontend/.../app.config.ts` — provider registration
- Existing pattern: `frontend/.../app.routes.ts` — routing setup
- Existing pattern: `src/Modules/Reporting/ProjectManagement.Reporting.Infrastructure/Persistence/ReportingDbContext.cs`
- Existing pattern: `src/Modules/Reporting/ProjectManagement.Reporting.Api/Controllers/ReportingController.cs`

---

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

### Completion Notes List

**ADAPTATION: No MediatR Notification events in Projects module**
The story specified `ProjectSummarySnapshot` entity + `ProjectSummaryProjector` subscribing to 4 MediatR events. Investigation confirmed Projects module has no published notification events (`TaskCreated`, `TaskStatusChanged`, etc.). The snapshot/projector approach was entirely skipped. Instead, `GetProjectsSummaryHandler` computes health metrics on-the-fly by injecting `IProjectsDbContext` — same cross-module pattern established by `GetCostBreakdownHandler` in Reporting. This satisfies AC-9 since data always reflects current state.

**ADAPTATION: Project entity has no StartDate/EndDate**
`percentTimeElapsed` was derived from min `PlannedStartDate` and max `PlannedEndDate` across all project tasks. If no tasks have dates, value = 0.

**Traffic light rules implemented:**
- Delayed: `overdueCount > 3 OR pctComplete < pctTime - 25`
- AtRisk: `overdueCount >= 1 OR pctComplete < pctTime - 10`
- OnTrack: otherwise

### File List

**Backend — New files:**
- `src/Modules/Reporting/ProjectManagement.Reporting.Application/Queries/GetProjectsSummary/ProjectSummaryDto.cs`
- `src/Modules/Reporting/ProjectManagement.Reporting.Application/Queries/GetProjectsSummary/GetProjectsSummaryQuery.cs`
- `src/Modules/Reporting/ProjectManagement.Reporting.Api/Controllers/DashboardController.cs`

**Frontend — New files:**
- `frontend/project-management-web/src/app/features/dashboard/models/dashboard.model.ts`
- `frontend/project-management-web/src/app/features/dashboard/store/dashboard.actions.ts`
- `frontend/project-management-web/src/app/features/dashboard/store/dashboard.reducer.ts`
- `frontend/project-management-web/src/app/features/dashboard/store/dashboard.selectors.ts`
- `frontend/project-management-web/src/app/features/dashboard/store/dashboard.effects.ts`
- `frontend/project-management-web/src/app/features/dashboard/store/dashboard.facade.ts`
- `frontend/project-management-web/src/app/features/dashboard/services/dashboard-api.service.ts`
- `frontend/project-management-web/src/app/features/dashboard/dashboard.routes.ts`
- `frontend/project-management-web/src/app/features/dashboard/shells/dashboard-shell/dashboard-shell.ts`
- `frontend/project-management-web/src/app/features/dashboard/components/overview/dashboard-overview.ts`
- `frontend/project-management-web/src/app/features/dashboard/components/overview/dashboard-overview.html`
- `frontend/project-management-web/src/app/features/dashboard/components/overview/dashboard-overview.scss`
- `frontend/project-management-web/src/app/features/dashboard/components/overview/portfolio-health-card/portfolio-health-card.ts`
- `frontend/project-management-web/src/app/features/dashboard/components/overview/portfolio-health-card/portfolio-health-card.html`
- `frontend/project-management-web/src/app/features/dashboard/components/overview/portfolio-health-card/portfolio-health-card.scss`
- `frontend/project-management-web/src/app/features/dashboard/components/overview/project-pulse-strip/project-pulse-strip.ts`
- `frontend/project-management-web/src/app/features/dashboard/components/overview/project-pulse-strip/project-pulse-strip.html`
- `frontend/project-management-web/src/app/features/dashboard/components/overview/project-pulse-strip/project-pulse-strip.scss`

**Frontend — Modified files:**
- `frontend/project-management-web/src/app/app.config.ts` — added `provideRouterStore()`, `DashboardEffects`
- `frontend/project-management-web/src/app/app.routes.ts` — added `dashboard` lazy route
- `frontend/project-management-web/src/app/core/store/app.state.ts` — added `dashboard: DashboardState` + `dashboardFeature.reducer`
- `frontend/project-management-web/src/app/core/shell/app-shell.ts` — added Dashboard nav item
