# Story 10.1: Budget Report, Export & Deep-Link Sharing

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a PM,
I want to view the budget report with planned vs actual costs by project and vendor, and export it as PDF or Excel,
so that I can share accurate financial reports with leadership and vendors in minutes instead of hours.

## Acceptance Criteria

**AC-1: Report Shell Layout**
- **Given** PM navigate đến `/reports/budget`
- **When** trang load
- **Then** `ReportShellComponent` hiển thị layout KHÔNG có sidebar/navbar
- **And** header chỉ chứa: context label (project name/scope), "← Back to Dashboard" link, filter bar, và export buttons
- **And** trang load trong P95 < 3s

**AC-2: Budget Table — Planned vs Actual**
- **Given** PM chọn month filter "Tháng 4/2026" và project scope "All"
- **When** filter áp dụng (URL update → effect gọi API)
- **Then** server fetch hoàn thành < 800ms
- **And** budget table hiển thị mỗi project một section, mỗi section có breakdown theo vendor: vendor name, planned hours (Estimated), actual hours (VendorConfirmed + PmAdjusted), planned cost, actual cost

**AC-3: Vendor Anomaly Highlight**
- **Given** một vendor báo tổng giờ > working_days_in_month × 8 (ví dụ: 24 ngày công trong tháng có 22 ngày làm việc = 176h, nhưng vendor báo 192h)
- **When** budget table render
- **Then** dòng vendor đó được highlight màu warning (amber/orange)
- **And** tooltip hoặc icon giải thích: "Số giờ vượt ngày làm việc thực tế trong tháng"

**AC-4: Export PDF**
- **Given** PM click "Export PDF"
- **When** export bắt đầu
- **Then** PDF download tự động trong < 10s
- **And** PDF có footer: "Tài liệu này chứa thông tin tài chính nội bộ — không phân phối ra ngoài."
- **And** footer này được generate server-side — client không thể bypass

**AC-5: Export Excel**
- **Given** PM click "Export Excel"
- **When** export bắt đầu
- **Then** file Excel download trong < 3s (client-side)
- **And** file có đúng data như budget table đang hiển thị

**AC-6: Copy Link (Deep-Link)**
- **Given** PM click "Copy Link"
- **When** click xảy ra
- **Then** current URL (bao gồm filter params `month` và `projects`) được copy vào clipboard
- **And** toast notification: "Link đã được copy"

**AC-7: Deep-Link Hydration**
- **Given** authorized user nhận link `/reports/budget?month=2026-04&projects=id1,id2`
- **When** mở link (đã đăng nhập)
- **Then** report load với đúng filter state (month = tháng 4/2026, projects = id1 và id2)

**AC-8: Auth Guard + Return URL**
- **Given** user chưa đăng nhập mở link `/reports/budget?month=2026-04`
- **When** trang load
- **Then** redirect → `/login` với `returnUrl` encoded trong URL
- **And** sau login thành công, redirect về đúng report URL với filter params giữ nguyên

**AC-9: Print Layout**
- **Given** PM in report từ browser (Ctrl+P)
- **When** print dialog mở
- **Then** `@media print` CSS ẩn toolbar và export buttons
- **And** page-break-before: always giữa các project sections

---

## Dev Notes

### ⚠️ Brownfield Context — Đọc trước khi code

Project là Angular 21 SPA + .NET 10 Modular Monolith với nhiều features đã có. Story 10-1 là **extension**, không phải greenfield.

**Hệ thống hiện có (quan trọng):**
- `AppShellComponent` tại `frontend/.../core/shell/app-shell.ts` — đã có sidebar + navbar; wrap TẤT CẢ authenticated routes trong `app.routes.ts` dưới children của nó
- `features/dashboard/` — đã tạo ở Story 9-1; có `DashboardShellComponent`, NgRx store, routing
- `features/reporting/` — đã có, KHÁC với `features/reports/` cần tạo mới; chứa cost dashboard, cost breakdown, export trigger (route `/reporting/cost`, `/reporting/breakdown`, `/reporting/export`)
- `@ngrx/router-store` — **đã cài và configured** tại `app.config.ts` từ Story 9-1
- `Reporting` module backend — đã có `ReportingController` tại `/api/v1/reports`, `GetCostSummaryQuery`, `GetCostBreakdownQuery`, `PdfExportService` (dùng **QuestPDF**, KHÔNG phải Puppeteer)
- Auth guard: `authGuard` tại `core/auth/auth.guard.ts`

**CRITICAL: ReportShellComponent phải là top-level route — KHÔNG phải children của AppShellComponent:**
```
// app.routes.ts — cấu trúc hiện tại
{
  path: '',
  component: AppShellComponent,   // sidebar + navbar
  canActivate: [authGuard],
  children: [
    { path: 'dashboard', loadChildren: () => import('./features/dashboard/dashboard.routes')... },
    { path: 'reporting', loadChildren: () => import('./features/reporting/reporting.routes')... },
    // ... existing routes
  ]
},
// THÊM reports ở top-level — NGOÀI AppShellComponent để có clean layout
{
  path: 'reports',
  loadChildren: () => import('./features/reports/reports.routes').then(m => m.REPORTS_ROUTES)
}
```

### Architecture Compliance

| Rule | Requirement |
|---|---|
| DA-01 | Mở rộng module `Reporting` — KHÔNG tạo module backend mới |
| DA-02 | `features/reports/` là lazy-loaded module riêng — KHÔNG import vào AppModule hay DashboardModule |
| DA-03 | NgRx `reports` feature store độc lập — KHÔNG mở rộng `dashboard` hay `reporting` store |
| DA-04 | URL filter sync qua `@ngrx/router-store` — URL là single source of truth |
| AR15 | `ReportsModule` KHÔNG được import vào `AppModule` hay `DashboardModule` |
| 8.9 | `Cache-Control: max-age=300` cho `/api/v1/reports/*` |
| 8.9 | `ReportShellComponent` KHÔNG chứa sidebar/navbar |

### Backend — Critical Implementation Notes

**PDF thực tế dùng QuestPDF, không phải Puppeteer:**
Mặc dù architecture doc đề cập Puppeteer, implementation hiện có (`PdfExportService.cs`) dùng **QuestPDF**. Follow QuestPDF pattern.

**Không tạo endpoint mới tách rời — mở rộng `ReportingController`:**
Thêm endpoint `GET /api/v1/reports/budget` vào `ReportingController.cs` hiện có. Không tạo controller mới.

**Tái dụng cross-module data access pattern đã có:**
`GetCostBreakdownHandler` và `GetProjectsSummaryHandler` dùng `IProjectsDbContext` + `ITimeTrackingDbContext` + `IWorkforceDbContext` — follow same pattern.

**Anomaly detection logic:**
```
working_days_in_month = số ngày thứ 2–6 trong tháng (bỏ qua Sat/Sun)
anomaly = vendor totalHours > working_days_in_month × 8
```

### Backend — Exact File Locations

```
src/Modules/Reporting/
├── ProjectManagement.Reporting.Application/
│   └── Queries/
│       └── GetBudgetReport/                          ← MỚI thư mục
│           ├── GetBudgetReportQuery.cs               ← query + handler + DTOs
│           └── BudgetReportDto.cs
│
├── ProjectManagement.Reporting.Infrastructure/
│   └── Services/
│       └── PdfExportService.cs                       ← THÊM method GenerateBudgetReport()
│
└── ProjectManagement.Reporting.Api/
    └── Controllers/
        └── ReportingController.cs                    ← THÊM GetBudgetReport() + ExportBudgetPdf() endpoints
```

**BudgetReportDto design:**
```csharp
public sealed record BudgetVendorRow(
    Guid? VendorId,
    string VendorName,          // "Inhouse" nếu VendorId null
    decimal PlannedHours,       // EntryType = "Estimated"
    decimal ActualHours,        // EntryType = "PmAdjusted" + "VendorConfirmed"
    decimal PlannedCost,
    decimal ActualCost,
    decimal ConfirmedPct,
    bool HasAnomaly);           // actualHours > workingDays * 8

public sealed record BudgetProjectSection(
    Guid ProjectId,
    string ProjectName,
    decimal TotalPlannedCost,
    decimal TotalActualCost,
    IReadOnlyList<BudgetVendorRow> Vendors);

public sealed record BudgetReportDto(
    string Month,               // "YYYY-MM"
    int WorkingDaysInMonth,     // số ngày thứ 2–6
    decimal GrandTotalPlanned,
    decimal GrandTotalActual,
    IReadOnlyList<BudgetProjectSection> Projects);
```

**GetBudgetReportQuery:**
```csharp
public sealed record GetBudgetReportQuery(
    Guid CurrentUserId,
    string Month,               // "YYYY-MM"
    IReadOnlyList<Guid>? ProjectIds = null)
    : IRequest<BudgetReportDto>;
```

**Endpoint trong ReportingController:**
```csharp
// GET /api/v1/reports/budget?month=2026-04&projectIds=id1&projectIds=id2
[HttpGet("budget")]
[ResponseCache(Duration = 300)]
public async Task<IActionResult> GetBudgetReport(
    [FromQuery] string month,
    [FromQuery] Guid[]? projectIds,
    CancellationToken ct)
{
    var result = await _mediator.Send(
        new GetBudgetReportQuery(_currentUser.UserId, month, projectIds?.ToList()), ct);
    return Ok(result);
}

// POST /api/v1/reports/budget/export/pdf?month=2026-04&projectIds=...
[HttpPost("budget/export/pdf")]
public async Task<IActionResult> ExportBudgetPdf(
    [FromQuery] string month,
    [FromQuery] Guid[]? projectIds,
    CancellationToken ct)
{
    var report = await _mediator.Send(
        new GetBudgetReportQuery(_currentUser.UserId, month, projectIds?.ToList()), ct);
    var pdfBytes = _pdfExportService.GenerateBudgetReport(report);
    return File(pdfBytes, "application/pdf", $"budget-report-{month}.pdf");
}
```

**PdfExportService — thêm method GenerateBudgetReport:**
Footer hardcoded bằng QuestPDF — client không thể override:
```csharp
public byte[] GenerateBudgetReport(BudgetReportDto data)
{
    return Document.Create(container =>
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(1.5f, Unit.Centimetre);
            page.DefaultTextStyle(x => x.FontSize(10));

            page.Header()...    // title + month + filter info

            page.Content()...   // table by project → vendors

            page.Footer().AlignCenter()
                .Text("Tài liệu này chứa thông tin tài chính nội bộ — không phân phối ra ngoài.")
                .FontSize(8).Italic().FontColor(Colors.Grey.Darken1);
        });
    }).GeneratePdf();
}
```

**Working days calculation:**
```csharp
private static int CalculateWorkingDays(int year, int month)
{
    var daysInMonth = DateTime.DaysInMonth(year, month);
    var count = 0;
    for (var day = 1; day <= daysInMonth; day++)
    {
        var dow = new DateTime(year, month, day).DayOfWeek;
        if (dow != DayOfWeek.Saturday && dow != DayOfWeek.Sunday)
            count++;
    }
    return count;
}
```

### Frontend — Exact File Locations

```
frontend/project-management-web/src/app/
├── app.routes.ts                              ← THÊM top-level 'reports' lazy route
│
└── features/
    └── reports/                               ← MỚI (KHÁC features/reporting/ đã có)
        ├── reports.routes.ts
        ├── shells/
        │   └── report-shell/
        │       ├── report-shell.ts            ← clean layout: NO sidebar/navbar
        │       ├── report-shell.html          ← report-header + <router-outlet>
        │       └── report-shell.scss          ← @media print CSS
        ├── components/
        │   └── budget/
        │       ├── budget-report.ts           ← container: inject Store
        │       ├── budget-report.html
        │       ├── budget-filter-bar/
        │       │   ├── budget-filter-bar.ts   ← @Input() month + projectIds
        │       │   └── budget-filter-bar.html
        │       └── budget-table/
        │           ├── budget-table.ts        ← @Input() report data; anomaly highlight
        │           └── budget-table.html
        ├── store/
        │   ├── reports.actions.ts
        │   ├── reports.reducer.ts
        │   ├── reports.effects.ts             ← load report + URL sync
        │   └── reports.selectors.ts
        ├── services/
        │   └── reports-api.service.ts         ← GET /api/v1/reports/budget + SheetJS Excel
        └── models/
            └── budget-report.model.ts
```

**reports.routes.ts:**
```typescript
import { Routes } from '@angular/router';
import { authGuard } from '../../core/auth/auth.guard';

export const REPORTS_ROUTES: Routes = [
  {
    path: '',
    component: ReportShellComponent,
    canActivate: [authGuard],
    providers: [provideState(reportsFeature)],    // feature store scoped
    children: [
      { path: 'budget', component: BudgetReportComponent },
      { path: '', redirectTo: 'budget', pathMatch: 'full' }
    ]
  }
];
```

**ReportShellComponent:**
```typescript
// report-shell.ts — KHÔNG dispatch polling, KHÔNG inject DashboardFacade
// Chỉ là routing container + header + <router-outlet>
// Header: "<- Back to Dashboard" button + project context label
@Component({
  selector: 'app-report-shell',
  standalone: true,
  imports: [RouterOutlet, MatButtonModule, MatIconModule, RouterLink]
})
export class ReportShellComponent {}
```

**report-shell.html:**
```html
<div class="report-shell">
  <div class="report-header">
    <a mat-button routerLink="/dashboard">
      <mat-icon>arrow_back</mat-icon> Về Dashboard
    </a>
    <!-- report title/context passed via store or route data -->
    <router-outlet></router-outlet>
  </div>
</div>
```

**report-shell.scss:**
```scss
@media print {
  .report-header { display: none; }   // ẩn toolbar khi in
  .budget-filter-bar { display: none; }
  .export-buttons { display: none; }
}

.project-section { page-break-before: always; }
.project-section:first-child { page-break-before: avoid; }
```

**ReportsState (NgRx):**
```typescript
// budget-report.model.ts
export interface BudgetVendorRow {
  vendorId: string | null;
  vendorName: string;
  plannedHours: number;
  actualHours: number;
  plannedCost: number;
  actualCost: number;
  confirmedPct: number;
  hasAnomaly: boolean;
}

export interface BudgetProjectSection {
  projectId: string;
  projectName: string;
  totalPlannedCost: number;
  totalActualCost: number;
  vendors: BudgetVendorRow[];
}

export interface BudgetReport {
  month: string;
  workingDaysInMonth: number;
  grandTotalPlanned: number;
  grandTotalActual: number;
  projects: BudgetProjectSection[];
}

// reports.reducer.ts
export interface ReportsFilters {
  month: string;          // 'YYYY-MM', default = current month
  projectIds: string[];   // [] = all
}

export interface ReportsState {
  filters: ReportsFilters;
  budgetReport: BudgetReport | null;
  loading: boolean;
  error: string | null;
}
```

**Reports Effects — URL sync pattern:**
```typescript
// reports.effects.ts
// 1. URL → Store: khi navigate đến /reports/* parse params → dispatch SetFilters
syncFiltersFromUrl$ = createEffect(() =>
  this.actions$.pipe(
    ofType(routerNavigatedAction),
    filter(action => action.payload.routerState.url.startsWith('/reports')),
    map(action => {
      const params = action.payload.routerState.queryParams;
      return ReportsActions.setFilters({
        filters: {
          month: params['month'] ?? currentYearMonth(),
          projectIds: params['projects'] ? params['projects'].split(',') : []
        }
      });
    })
  )
);

// 2. Store → URL: khi filter thay đổi, navigate để update URL
updateUrl$ = createEffect(() =>
  this.store.select(selectReportsFilters).pipe(
    distinctUntilChanged(isEqual),
    skip(1),
    tap(filters => this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        month: filters.month,
        projects: filters.projectIds.length ? filters.projectIds.join(',') : null
      },
      queryParamsHandling: 'merge'
    }))
  ),
  { dispatch: false }
);

// 3. Load data khi filter thay đổi
loadBudgetReport$ = createEffect(() =>
  this.actions$.pipe(
    ofType(ReportsActions.setFilters),
    switchMap(({ filters }) =>
      this.reportsApi.getBudgetReport(filters.month, filters.projectIds).pipe(
        map(data => ReportsActions.loadBudgetReportSuccess({ data })),
        catchError(err => of(ReportsActions.loadBudgetReportFailure({
          error: err?.message ?? 'Lỗi tải budget report.'
        })))
      )
    )
  )
);
```

**ReportsApiService:**
```typescript
// reports-api.service.ts
@Injectable({ providedIn: 'root' })
export class ReportsApiService {
  private readonly http = inject(HttpClient);

  getBudgetReport(month: string, projectIds: string[]): Observable<BudgetReport> {
    const params = new HttpParams()
      .set('month', month)
      .appendAll({ projectIds });
    return this.http.get<BudgetReport>('/api/v1/reports/budget', { params });
  }

  exportBudgetPdf(month: string, projectIds: string[]): void {
    // POST → download via <a> click trick or direct navigation
    const params = new HttpParams()
      .set('month', month)
      .appendAll({ projectIds });
    // Trigger file download by navigating to the endpoint with blob response
    this.http.post('/api/v1/reports/budget/export/pdf', null, {
      params, responseType: 'blob'
    }).subscribe(blob => {
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `budget-report-${month}.pdf`;
      a.click();
      URL.revokeObjectURL(url);
    });
  }

  exportBudgetExcel(report: BudgetReport): void {
    // Client-side SheetJS
    import('xlsx').then(XLSX => {
      const wb = XLSX.utils.book_new();
      report.projects.forEach(project => {
        const rows = project.vendors.map(v => ({
          Vendor: v.vendorName,
          'Planned Hours': v.plannedHours,
          'Actual Hours': v.actualHours,
          'Planned Cost': v.plannedCost,
          'Actual Cost': v.actualCost,
          'Confirmed %': v.confirmedPct,
          Anomaly: v.hasAnomaly ? 'Vượt ngưỡng' : ''
        }));
        const ws = XLSX.utils.json_to_sheet(rows);
        XLSX.utils.book_append_sheet(wb, ws, project.projectName.slice(0, 31)); // max 31 chars
      });
      XLSX.writeFile(wb, `budget-report-${report.month}.xlsx`);
    });
  }
}
```

**BudgetReportComponent (container):**
```typescript
// budget-report.ts — inject Store; pass via @Input() to widgets
@Component({ standalone: true, ... })
export class BudgetReportComponent {
  private store = inject(Store);
  report$ = this.store.select(selectBudgetReport);
  loading$ = this.store.select(selectReportsLoading);
  error$ = this.store.select(selectReportsError);
  filters$ = this.store.select(selectReportsFilters);

  onFilterChange(filters: ReportsFilters) {
    this.store.dispatch(ReportsActions.setFilters({ filters }));
  }

  onCopyLink() {
    navigator.clipboard.writeText(window.location.href).then(() => {
      // dispatch toast action or use MatSnackBar
    });
  }
}
```

**BudgetTableComponent — anomaly highlight:**
```html
<!-- budget-table.html -->
<tr *ngFor="let vendor of section.vendors"
    [class.anomaly-row]="vendor.hasAnomaly"
    [matTooltip]="vendor.hasAnomaly ? 'Số giờ vượt ngày làm việc thực tế trong tháng' : ''">
  ...
</tr>
```
```scss
// budget-table.scss
.anomaly-row {
  background-color: var(--mat-sys-error-container, #FDECEA);
  td { color: var(--mat-sys-error, #B71C1C); }
}
```

**SheetJS installation:**
```bash
cd frontend/project-management-web
npm install xlsx
```

### NgRx Pattern từ Codebase Hiện Có

```typescript
// Pattern inject() — KHÔNG dùng constructor injection (theo codebase)
@Injectable()
export class ReportsEffects {
  private readonly actions$ = inject(Actions);
  private readonly store = inject(Store);
  private readonly reportsApi = inject(ReportsApiService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
}

// createFeature pattern (như dashboard store)
export const reportsFeature = createFeature({
  name: 'reports',
  reducer: createReducer(initialState, ...on() handlers)
});
```

### Previous Story Intelligence (Story 9-1)

- `@ngrx/router-store` đã được installed và `provideRouterStore()` đã được cấu hình ở root `app.config.ts` → KHÔNG install lại
- `DashboardEffects` đã registered trong `provideEffects([...])` tại `app.config.ts` → thêm `ReportsEffects` vào cùng list
- `AppShellComponent` wrap tất cả children routes — dashboard là child, **reports phải là top-level**
- Story 9-1 confirmed: Projects module KHÔNG có MediatR Notification events; dùng on-the-fly computation thay vì projector
- `IProjectsDbContext` cross-module access pattern đã validated và hoạt động
- Pattern: `inject(Actions)` + `inject(Store)` (không dùng constructor injection) đã confirmed

### Git Intelligence (Recent Commits)

Từ git log gần nhất, commit "comit" (52b732a) là commit mới nhất. Feature set đang ở sprint Epic 8 (task list, kanban). Features từ Epic 9 (dashboard) đã được implement nhưng ở trạng thái "review". Story này thuộc Epic 10 — build trên nền Dashboard infrastructure đã có từ Story 9-1.

### Project Structure Notes

**`features/reports/` vs `features/reporting/` — KHÔNG được nhầm:**
- `features/reporting/` — đã có, chứa cost dashboard (cũ), route `/reporting/*`, store `reporting`
- `features/reports/` — MỚI tạo trong story này, chứa budget report, route `/reports/*`, store `reports`

**Top-level route placement:**
Route `reports` PHẢI được đặt NGOÀI `AppShellComponent` children trong `app.routes.ts` để có clean layout không có sidebar/navbar. Nếu đặt nhầm vào children của AppShellComponent, sidebar sẽ xuất hiện trong report view — vi phạm AC-1 và UX-DR2 từ epics-dashboard.md.

**`provideState(reportsFeature)` trong route:**
Feature store `reports` chỉ cần khi route `/reports/*` được load. Dùng `providers: [provideState(reportsFeature)]` trong `REPORTS_ROUTES` (không phải root). Tham khảo cách `dashboard` store được registered (Story 9-1 dùng root registration; `reports` nên dùng feature-level registration để lazy).

**Excel export (SheetJS) là client-side:**
- Không cần backend endpoint riêng cho Excel
- SheetJS `xlsx` package phải được install: `npm install xlsx`
- Dynamic import `import('xlsx')` để lazy load, tránh ảnh hưởng initial bundle

### References

- Epic spec: `_bmad-output/planning-artifacts/epics-dashboard.md` — Story 10-1 (đầy đủ)
- Architecture: `_bmad-output/planning-artifacts/architecture.md` — Phần 8 (8.2–8.9)
- PRD Dashboard: `_bmad-output/planning-artifacts/prd-dashboard.md` — FR17–FR22, FR29–FR36
- Previous story: `_bmad-output/implementation-artifacts/9-1-dashboard-infrastructure-portfolio-health-cards.md`
- Existing pattern (ReportingController): `src/Modules/Reporting/ProjectManagement.Reporting.Api/Controllers/ReportingController.cs`
- Existing pattern (GetCostBreakdown): `src/Modules/Reporting/ProjectManagement.Reporting.Application/Queries/GetCostBreakdown/GetCostBreakdownQuery.cs`
- Existing pattern (PdfExportService): `src/Modules/Reporting/ProjectManagement.Reporting.Infrastructure/Services/PdfExportService.cs`
- Existing pattern (reporting effects): `frontend/.../features/reporting/store/reporting.effects.ts`
- Existing routes: `frontend/.../app.routes.ts`
- Existing app config: `frontend/.../app.config.ts`

---

## Tasks / Subtasks

### Backend Tasks

- [ ] **Task BE-1: GetBudgetReportQuery** (AC: 2, 3)
  - [ ] BE-1.1: Tạo `GetBudgetReport/GetBudgetReportQuery.cs` + Handler trong `Reporting.Application/Queries/`
  - [ ] BE-1.2: Tạo `BudgetReportDto.cs` với `BudgetProjectSection` + `BudgetVendorRow` (HasAnomaly field)
  - [ ] BE-1.3: Handler: membership-scope projectIds → group TimeEntries by project → by vendor → compute planned/actual hours + costs
  - [ ] BE-1.4: Implement `CalculateWorkingDays(year, month)` helper (count Mon–Fri)
  - [ ] BE-1.5: Set `HasAnomaly = actualHours > workingDays * 8` per vendor row
  - [ ] BE-1.6: Resolve vendor names via `IWorkforceDbContext` (pattern từ `GetCostBreakdownHandler`)
  - [ ] BE-1.7: Resolve project names via `IProjectsDbContext`

- [ ] **Task BE-2: PdfExportService — GenerateBudgetReport** (AC: 4)
  - [ ] BE-2.1: Thêm `GenerateBudgetReport(BudgetReportDto data)` vào `PdfExportService.cs`
  - [ ] BE-2.2: Hardcode footer: "Tài liệu này chứa thông tin tài chính nội bộ — không phân phối ra ngoài."
  - [ ] BE-2.3: Table structure: project section → vendor rows với anomaly highlight (background color)
  - [ ] BE-2.4: `page-break-before: always` giữa project sections (QuestPDF: `EnsureSpace` hoặc `.NewPage()`)

- [ ] **Task BE-3: ReportingController — Endpoints** (AC: 2, 4)
  - [ ] BE-3.1: Thêm `GetBudgetReport()` → `GET /api/v1/reports/budget?month=YYYY-MM&projectIds[]` với `[ResponseCache(Duration = 300)]`
  - [ ] BE-3.2: Thêm `ExportBudgetPdf()` → `POST /api/v1/reports/budget/export/pdf` → trả `File(pdfBytes, "application/pdf", ...)`
  - [ ] BE-3.3: Inject `PdfExportService` vào `ReportingController` (constructor injection)
  - [ ] BE-3.4: Test compile: `dotnet build src/`

### Frontend Tasks

- [ ] **Task FE-1: Install SheetJS** (AC: 5)
  - [ ] FE-1.1: `cd frontend/project-management-web && npm install xlsx`

- [ ] **Task FE-2: Models** (AC: 2, 3, 5)
  - [ ] FE-2.1: Tạo `features/reports/models/budget-report.model.ts` với `BudgetVendorRow`, `BudgetProjectSection`, `BudgetReport`, `ReportsFilters`, `ReportsState`

- [ ] **Task FE-3: NgRx Reports Store** (AC: 2, 6, 7)
  - [ ] FE-3.1: Tạo `reports.actions.ts` — `setFilters`, `loadBudgetReportSuccess`, `loadBudgetReportFailure`
  - [ ] FE-3.2: Tạo `reports.reducer.ts` dùng `createFeature` (pattern giống `dashboard.reducer.ts`)
  - [ ] FE-3.3: Tạo `reports.selectors.ts` — `selectBudgetReport`, `selectReportsFilters`, `selectReportsLoading`, `selectReportsError`
  - [ ] FE-3.4: Tạo `reports.effects.ts` — `syncFiltersFromUrl$`, `updateUrl$`, `loadBudgetReport$`
  - [ ] FE-3.5: Thêm `ReportsEffects` vào `provideEffects([...])` trong `app.config.ts`

- [ ] **Task FE-4: ReportsApiService** (AC: 2, 4, 5)
  - [ ] FE-4.1: Tạo `services/reports-api.service.ts`
  - [ ] FE-4.2: Implement `getBudgetReport(month, projectIds)` → `GET /api/v1/reports/budget`
  - [ ] FE-4.3: Implement `exportBudgetPdf(month, projectIds)` → `POST /api/v1/reports/budget/export/pdf` blob download
  - [ ] FE-4.4: Implement `exportBudgetExcel(report)` — dynamic `import('xlsx')` + SheetJS

- [ ] **Task FE-5: ReportShellComponent** (AC: 1, 8, 9)
  - [ ] FE-5.1: Tạo `shells/report-shell/report-shell.ts` — standalone, RouterOutlet, NO Store injection
  - [ ] FE-5.2: `report-shell.html` — header ("← Về Dashboard" routerLink="/dashboard" + project context) + `<router-outlet>`
  - [ ] FE-5.3: `report-shell.scss` — `@media print { toolbar hidden, export buttons hidden }` + `.project-section { page-break-before: always }`

- [ ] **Task FE-6: BudgetFilterBarComponent** (AC: 2, 6, 7)
  - [ ] FE-6.1: Tạo `components/budget/budget-filter-bar/budget-filter-bar.ts` — `@Input() filters`, `@Output() filtersChange`
  - [ ] FE-6.2: Angular Material month picker (MatDatepicker với granularity tháng hoặc MatSelect cho tháng/năm)
  - [ ] FE-6.3: Project multi-select (MatSelect multiple, danh sách từ store)
  - [ ] FE-6.4: "Copy Link" button → `navigator.clipboard.writeText(window.location.href)` + toast

- [ ] **Task FE-7: BudgetTableComponent** (AC: 2, 3, 9)
  - [ ] FE-7.1: Tạo `components/budget/budget-table/budget-table.ts` — `@Input() report: BudgetReport | null`, `@Input() loading: boolean`
  - [ ] FE-7.2: Table structure: project section header → vendor rows
  - [ ] FE-7.3: `[class.anomaly-row]="vendor.hasAnomaly"` + MatTooltip "Số giờ vượt ngày làm việc thực tế trong tháng"
  - [ ] FE-7.4: Empty state khi no data
  - [ ] FE-7.5: `.project-section { page-break-before: always }` SCSS class

- [ ] **Task FE-8: BudgetReportComponent (container)** (AC: 2, 3, 4, 5, 6)
  - [ ] FE-8.1: Tạo `components/budget/budget-report.ts` — inject Store, compose widgets
  - [ ] FE-8.2: Expose `report$`, `loading$`, `error$`, `filters$` từ store selectors
  - [ ] FE-8.3: Export PDF button → call `reportsApi.exportBudgetPdf()`
  - [ ] FE-8.4: Export Excel button → call `reportsApi.exportBudgetExcel(report)`
  - [ ] FE-8.5: MatSnackBar toast khi copy link thành công

- [ ] **Task FE-9: reports.routes.ts + app.routes.ts** (AC: 1, 7, 8)
  - [ ] FE-9.1: Tạo `features/reports/reports.routes.ts` với `REPORTS_ROUTES` (ReportShellComponent + authGuard + provideState(reportsFeature))
  - [ ] FE-9.2: Thêm `{ path: 'reports', loadChildren: () => import('./features/reports/reports.routes').then(m => m.REPORTS_ROUTES) }` vào `app.routes.ts` ở **top-level** (NGOÀI AppShellComponent children)

- [ ] **Task FE-10: Navigation Entry** (AC: 1)
  - [ ] FE-10.1: Thêm "Budget Report" link vào sidebar `AppShellComponent` nav items: `{ label: 'Budget Report', icon: 'assessment', route: '/reports/budget' }`

- [ ] **Task FE-11: Build & Smoke Test** (AC: 1–9)
  - [ ] FE-11.1: `ng build --configuration development` → 0 errors
  - [ ] FE-11.2: Navigate đến `/reports/budget` → sidebar không hiển thị, clean layout
  - [ ] FE-11.3: Thay đổi filter → URL update, data reload
  - [ ] FE-11.4: Copy link → toast xuất hiện, URL đúng
  - [ ] FE-11.5: Excel export → file download đúng data

---

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

### Completion Notes List

### File List
