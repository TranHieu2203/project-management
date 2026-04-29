---
stepsCompleted: ['step-01-validate-prerequisites', 'step-02-design-epics', 'step-03-create-stories', 'step-04-final-validation']
inputDocuments:
  - '_bmad-output/planning-artifacts/prd-dashboard.md'
  - '_bmad-output/planning-artifacts/architecture.md'
  - '_bmad-output/planning-artifacts/ux-design-specification.md'
---

# project-management Dashboard & Reporting Module - Epic Breakdown

## Overview

This document provides the complete epic and story breakdown for the Dashboard & Reporting Module extension, decomposing requirements from prd-dashboard.md and architecture.md (Phần 8) into implementable stories.

## Requirements Inventory

### Functional Requirements

FR1: PM có thể xem trạng thái tổng hợp của tất cả dự án trong danh mục quản lý trên một màn hình duy nhất
FR2: PM có thể xem tình trạng sức khỏe (On Track / At Risk / Delayed) của từng dự án dưới dạng chỉ báo trực quan
FR3: PM có thể xem tỷ lệ hoàn thành công việc (% tasks done) của từng dự án
FR4: PM có thể xem đồng thời tiến độ công việc (% done) VÀ tiến độ thời gian (% time elapsed) của từng dự án để nhận diện độ lệch (Project Pulse Strip — dual-axis)
FR5: PM có thể xem số lượng task còn lại và số task đã quá hạn của từng dự án
FR6: Stakeholder (Giám đốc/leadership) có thể xem tổng quan danh mục dự án mà không cần kiến thức về Gantt chart hay task management detail
FR7: PM có thể xem danh sách thành viên đang bị overload (vượt capacity) cross-project
FR8: PM có thể xem tải công việc của từng thành viên tính theo giờ hoặc ngày công trong kỳ hiện tại
FR9: PM có thể xem chỉ số tổng hợp về số lượng thành viên đang overload (summary indicator)
FR10: PM có thể xem thành viên nào đang có capacity trống để tiếp nhận thêm công việc
FR11: PM có thể xem tải công việc cross-project theo chiều người × tuần (resource utilization view)
FR12: PM có thể xem danh sách các deadline quan trọng sắp đến trong 7 ngày tới, cross-project
FR13: PM có thể xem danh sách tasks đã quá hạn cross-project
FR14: PM có thể điều hướng từ một deadline hoặc task trên dashboard trực tiếp đến Gantt view hoặc task detail của project tương ứng
FR15: PM có thể xem danh sách tasks được assign cho mình cross-project với trạng thái và deadline
FR16: PM có thể lọc danh sách task/deadline theo project, assignee, hoặc trạng thái
FR17: PM có thể xem báo cáo chi phí tổng hợp theo từng dự án (planned vs actual)
FR18: PM có thể xem chi phí breakdown theo từng vendor (hours billed, rate, total amount)
FR19: PM có thể xem chi phí breakdown theo từng nhân sự hoặc nhóm trong dự án
FR20: Hệ thống tự động phát hiện và đánh dấu dữ liệu vendor bất thường (số ngày công billed vượt số ngày làm việc thực tế trong kỳ)
FR21: PM có thể lọc báo cáo chi phí theo kỳ (tháng/quý) và theo phạm vi dự án
FR22: PM có thể xem báo cáo hiệu suất vendor (on-time delivery rate, cost trend theo thời gian)
FR23: PM có thể xem milestone cross-project trên một timeline duy nhất dưới dạng read-only
FR24: PM có thể xem danh sách ưu tiên các vấn đề cần hành động ngay (deadline trong 48h, thành viên overload, budget burn cao) dưới dạng ranked list
FR25: PM có thể lọc toàn bộ dashboard theo project scope (một hoặc nhiều dự án)
FR26: PM có thể lọc dashboard và reports theo khoảng thời gian (date range)
FR27: PM có thể áp dụng quick filter chips để chọn nhanh các view preset thường dùng
FR28: PM có thể lưu các bộ filter thường dùng dưới dạng preset để truy cập lại
FR29: PM có thể chia sẻ đúng view dashboard/report (bao gồm filter state) với người khác thông qua URL
FR30: PM có thể quay trở lại Dashboard với filter state được giữ nguyên sau khi drill-down vào Gantt hay task detail
FR31: PM có thể export report dưới định dạng PDF, bao gồm thông tin phân loại tài liệu tài chính nội bộ
FR32: PM có thể export report dưới định dạng Excel (spreadsheet)
FR33: Authorized users có thể mở deep-link report và xem đúng data/filter state được chia sẻ
FR34: Report pages hiển thị trong layout riêng không chứa navigation chrome của app (sidebar, navbar)
FR35: PM có thể in report trực tiếp từ trình duyệt với layout tự động tối ưu cho in ấn
FR36: Report pages luôn hiển thị tên project/context và có link điều hướng quay lại Dashboard chính
FR37: Hệ thống lưu trữ cảnh báo về các sự kiện quan trọng (deadline nguy hiểm, overload, budget vượt ngưỡng) cho từng user
FR38: PM có thể xem và quản lý danh sách cảnh báo (xem chưa đọc, đánh dấu đã đọc)
FR39: PM có thể cấu hình loại cảnh báo muốn nhận và ngưỡng kích hoạt (threshold) cho từng loại cảnh báo
FR40: Users có thể biết mức độ freshness của data đang xem để ra quyết định chính xác
FR41: Dashboard luôn hiển thị data gần với real-time mà không cần người dùng thao tác thủ công
FR42: Khi một widget xảy ra lỗi, các widget còn lại vẫn hoạt động bình thường (không crash toàn trang)
FR43: Mỗi widget hiển thị trạng thái rõ ràng khi không có dữ liệu (empty state)
FR44: PM có thể kích hoạt cập nhật dữ liệu thủ công khi cần

### NonFunctional Requirements

NFR1: /dashboard/overview initial load P95 < 3s với 20 concurrent users và ≤200 projects
NFR2: Client-side filter refresh (NgRx cached) < 100ms
NFR3: Server fresh fetch sau filter change < 800ms
NFR4: /reports/budget page load P95 < 3s
NFR5: Dashboard polling cycle 30–60s configurable; mỗi poll hoàn thành < 800ms
NFR6: PDF export (server-side Puppeteer) < 10s từ click đến file download
NFR7: Excel export (client-side SheetJS) < 3s
NFR8: GET /api/v1/dashboard/summary < 800ms với ≤200 projects khi có composite indexes
NFR9: Tất cả /dashboard/* và /reports/* endpoints yêu cầu valid JWT token — không public access
NFR10: Vendor rates, employee hours, project costs chỉ truy cập bởi authenticated users có project membership
NFR11: Mọi data truyền qua HTTPS (inherited từ PRD gốc)
NFR12: PDF export financial classification footer enforce server-side — client không thể bypass
NFR13: Deep-link report: user chưa login redirect → login → redirect về đúng report URL (session preserved)
NFR14: Alert Center: mỗi user chỉ thấy alerts của chính mình — không cross-user data leakage
NFR15: Không cache sensitive data (vendor rates, cost breakdown) trong browser localStorage hoặc sessionStorage
NFR16: System hỗ trợ ≤20 concurrent users / ≤200 projects tại performance targets đã định
NFR17: HTTP Cache-Control: max-age=60 cho /dashboard/*, max-age=300 cho /reports/* — không cần application cache
NFR18: ReportsModule lazy-loaded — không ảnh hưởng initial bundle size của app
NFR19: Keyboard navigation đầy đủ cho filter dropdowns, date range pickers, export buttons
NFR20: ARIA labels trên tất cả icon-only buttons (export, refresh, copy link, close)
NFR21: Traffic light indicators dùng cả màu VÀ icon/text label — không dùng màu đơn thuần (color-blind support)
NFR22: Color contrast ratio ≥ 4.5:1 trên tất cả text elements
NFR23: Widget error isolation — 1 widget API fail không crash hoặc blank toàn trang; widget đó hiển thị error state riêng
NFR24: Stale data resilience: nếu network không khả dụng, dashboard hiển thị data cũ kèm "last updated" timestamp
NFR25: Empty states: tất cả widgets định nghĩa empty state UI — không có blank/broken layout khi no data
NFR26: Export errors hiển thị error message có thể hành động — không silent fail
NFR27: Chrome 100+ và Edge 100+ (Chromium) bắt buộc; Firefox/Safari không hỗ trợ chính thức

### Additional Requirements

- AR1: Backend — Mở rộng module Reporting hiện có; không tạo module Dashboard mới
- AR2: Frontend — 2 lazy-loaded Angular modules riêng: features/dashboard/ và features/reports/
- AR3: NgRx feature store dashboard độc lập; không mở rộng capacity store
- AR4: URL filter sync bằng @ngrx/router-store; URL là single source of truth cho filter state
- AR5: ProjectSummarySnapshot — denormalized read model trong reporting schema; UPSERT pattern (ON CONFLICT DO UPDATE)
- AR6: Alert + AlertPreference entities trong reporting schema; Alert UI ship ở Growth phase
- AR7: ProjectSummaryProjector subscribe 4 MediatR events: TaskCreated, TaskStatusChanged, TaskDueDateChanged, TimeEntryCreated
- AR8: DashboardController → /api/v1/dashboard/* với Cache-Control: max-age=60; 4 endpoints: summary, deadlines, stat-cards, my-tasks
- AR9: AlertsController → /api/v1/alerts/*; GET list + PATCH mark-read
- AR10: Polling via timer(0, 30_000) + switchMap + takeUntil(stopPolling) trong NgRx Effect; 3 API calls phân tách độc lập
- AR11: Widget error isolation — widget nhận @Input() không inject Store trực tiếp; container component inject Store
- AR12: DashboardShellComponent (layout với sidebar + navbar) + ReportShellComponent (clean layout, @media print CSS); 2 shell riêng biệt
- AR13: PostgreSQL composite indexes bắt buộc: ix_tasks_project_status_due, ix_assignments_assignee_week_start, ix_alerts_user_read
- AR14: PDF export server-side Puppeteer (mở rộng Reporting worker hiện có); Excel client-side SheetJS
- AR15: ReportsModule không được import vào AppModule hay DashboardModule — hoàn toàn lazy

### UX Design Requirements

UX-DR1: DashboardShellComponent — layout có full app chrome (sidebar, navbar, breadcrumb); consistent với layout hiện có của app
UX-DR2: ReportShellComponent — clean layout không có sidebar/navbar; chỉ có report-header (project name + "← Back to Dashboard") và router-outlet
UX-DR3: ReportShellComponent có @media print CSS: ẩn header toolbar, export buttons; page-break-before: always giữa các sections
UX-DR4: ProjectPulseStripComponent — dual-axis visualization: progress ring (% tasks done) bên trái + mini timeline bar (% time elapsed) bên phải + remaining work chip
UX-DR5: PortfolioHealthCardComponent — traffic light indicator dùng cả màu (green/yellow/red) VÀ icon + text label (On Track / At Risk / Delayed) cho color-blind support
UX-DR6: StatCardsComponent — 3 summary numbers: Overdue Tasks | At-Risk Projects | Overloaded Resources; số click được để drill-down
UX-DR7: UpcomingDeadlinesComponent — list tối đa 7 items, sorted by due_date ASC; mỗi item click → navigate đến Gantt/task view của project tương ứng
UX-DR8: Widget loading skeleton — mỗi widget hiển thị skeleton placeholder trong khi loading (không để blank)
UX-DR9: Widget error state — mỗi widget hiển thị error card riêng khi API fail; không ảnh hưởng widgets khác
UX-DR10: Widget empty state — mỗi widget có empty state UI riêng khi no data (không để blank)
UX-DR11: "Last updated" timestamp hiển thị trên mỗi widget (format: "Cập nhật lúc HH:mm")
UX-DR12: Manual refresh button trên dashboard header (icon refresh); dispatch loadPortfolio action
UX-DR13: Budget report — anomaly highlight: dòng vendor có số ngày công vượt ngày làm việc thực tế được đánh dấu màu warning
UX-DR14: Report header có "Copy Link" button → copy current URL (bao gồm filter params) vào clipboard
UX-DR15: Filter bar trên dashboard — project dropdown (multi-select) + date range picker + quick chips; responsive layout

### FR Coverage Map

| FR | Epic | Story |
|---|---|---|
| FR1, FR2, FR3, FR4, FR5 | Epic 9 | 9-1 |
| FR6 | Epic 9 | 9-1 |
| FR9, FR12, FR13 | Epic 9 | 9-2 |
| FR14 | Epic 9 | 9-2 |
| FR40, FR41, FR42, FR43, FR44 | Epic 9 | 9-1, 9-2 |
| FR7, FR8, FR10, FR25, FR26, FR27, FR29, FR30 | Epic 9 | 9-3 |
| FR15, FR16 | Epic 9 | 9-4 |
| FR17, FR18, FR19, FR20, FR21, FR31, FR32, FR34, FR35, FR36 | Epic 10 | 10-1, 10-2 |
| FR33 | Epic 10 | 10-1 |
| FR11, FR22, FR23 | Epic 10 | 10-3 (Growth) |
| FR24 | Epic 10 | 10-3 (Growth) |
| FR28 | Epic 10 | 10-3 (Growth) |
| FR37, FR38, FR39 | Epic 10 | 10-4 (Growth) |

## Epic List

- **Epic 9**: Dashboard Overview — Morning Command Center (MVP Week 1–2 → Week 3–4)
- **Epic 10**: Reports & Growth Features (MVP Week 3–4 → Growth)

---

## Epic 9: Dashboard Overview — Morning Command Center

**Goal:** PM mở `/dashboard/overview` trong < 30 giây thấy trạng thái tất cả dự án, overload alerts, và upcoming deadlines — biến 30–45 phút morning check thành < 5 phút. Bao gồm toàn bộ infrastructure (NgRx store, polling, URL sync, 2 shell components) cần thiết cho cả Epic 10.

---

### Story 9-1: Dashboard Infrastructure & Portfolio Health Cards

As a PM,
I want to see the health status of all my projects on a single dashboard page,
So that I can assess the overall portfolio situation in under 30 seconds without opening each Gantt separately.

**Acceptance Criteria:**

**Given** tôi đã đăng nhập và navigate đến `/dashboard/overview`
**When** trang load
**Then** trang hiển thị đầy đủ trong P95 < 3s với 20 concurrent users
**And** mỗi project tôi có membership hiển thị một PortfolioHealthCard
**And** mỗi card hiển thị: project name, health status (On Track / At Risk / Delayed), % tasks done, remaining task count, overdue task count

**Given** một project có health status "At Risk"
**When** PM nhìn vào card
**Then** card hiển thị cả màu yellow VÀ text label "At Risk" VÀ warning icon (không chỉ màu)

**Given** project có % time elapsed = 75% và % tasks done = 55%
**When** PM nhìn vào ProjectPulseStrip
**Then** progress ring hiển thị 55%, mini timeline bar hiển thị 75%, PM có thể thấy ngay độ lệch mà không cần tính toán

**Given** DashboardShellComponent được load
**When** component khởi tạo
**Then** dispatch `DashboardActions.startPolling()`; polling xảy ra mỗi 30s qua `timer(0, 30_000)` + `switchMap`
**And** khi rời `/dashboard/*`, dispatch `DashboardActions.stopPolling()`

**Given** một widget API call fail (ví dụ stat-cards API trả 500)
**When** dashboard render
**Then** widget đó hiển thị error state riêng với message actionable
**And** các widgets khác vẫn hiển thị bình thường

**Given** network không khả dụng trong khi polling
**When** poll attempt fail
**Then** dashboard hiển thị data cũ kèm "last updated" timestamp — không crash, không blank

**Given** PM navigate đến dashboard khi chưa có data
**When** widget chưa có data
**Then** mỗi widget hiển thị defined empty state UI (không blank/broken)

**Technical Notes:**
- Tạo `features/dashboard/` lazy-loaded module với `dashboard.routes.ts`
- Tạo `DashboardShellComponent` (layout có sidebar + navbar)
- Tạo NgRx feature store `dashboard`: `DashboardState`, actions, reducer, effects, selectors, facade
- `@ngrx/router-store` provide ở root `app.config.ts`; filter sync via `routerNavigatedAction`
- `portfolio-health-card`, `project-pulse-strip`, `stat-cards` nhận data qua `@Input()` — không inject Store trực tiếp
- Backend: `ProjectSummarySnapshot` entity + `ProjectSummaryProjector` trong Reporting module
- Backend: `DashboardController.GetSummary()` → `GET /api/v1/dashboard/summary` với `Cache-Control: max-age=60`
- EF migration: bảng `project_summary_snapshots` trong `reporting` schema
- PostgreSQL indexes: `ix_tasks_project_status_due`, `ix_project_summary_snapshots_project_id`

---

### Story 9-2: Stat Cards, Upcoming Deadlines & Drill-Down Navigation

As a PM,
I want to see aggregated alert counts and upcoming deadlines on my dashboard,
So that I know immediately which issues need attention and can navigate directly to the affected task or Gantt view.

**Acceptance Criteria:**

**Given** tôi mở `/dashboard/overview`
**When** trang load
**Then** hiển thị 3 Stat Cards: "Overdue Tasks" (total count cross-project), "At-Risk Projects" (count), "Overloaded Resources" (count)
**And** mỗi Stat Card là clickable để drill-down

**Given** có 5 tasks overdue trong 3 projects
**When** PM nhìn Stat Card "Overdue Tasks"
**Then** hiển thị số 5; click → scroll/highlight danh sách overdue tasks bên dưới (hoặc navigate đến my-tasks filter)

**Given** có deadlines trong 7 ngày tới
**When** PM xem Upcoming Deadlines widget
**Then** hiển thị tối đa 7 items, sorted by due_date ASC
**And** mỗi item hiển thị: task/milestone name, project name, due_date, days remaining

**Given** PM click vào một deadline item
**When** click xảy ra
**Then** navigate đến Gantt view của project đó, focus vào task/milestone tương ứng
**And** back navigation về dashboard giữ nguyên scroll position và filter state

**Given** không có deadline trong 7 ngày tới
**When** Upcoming Deadlines widget render
**Then** hiển thị empty state UI: "Không có deadline nào trong 7 ngày tới" với icon calendar

**Given** stat-cards API call và deadlines API call được dispatch song song
**When** effects chạy
**Then** cả 2 calls dùng `merge()` trong loadPortfolio$ effect — không sequential, không block nhau
**And** failure của 1 call không affect call còn lại

**Technical Notes:**
- Backend: `GetStatCardsQuery` + `GetUpcomingDeadlinesQuery` trong `Reporting.Application/Dashboard/Queries/`
- Backend: `DashboardController.GetStatCards()` → `GET /api/v1/dashboard/stat-cards`
- Backend: `DashboardController.GetDeadlines()` → `GET /api/v1/dashboard/deadlines?daysAhead=7`
- Frontend: `stat-cards` và `upcoming-deadlines` components trong `features/dashboard/components/overview/`
- `loadPortfolio$` effect dùng `merge()` để gọi summary + deadlines + stat-cards song song
- Drill-down navigation dùng Angular Router với `queryParams` để truyền `taskId` hoặc `milestoneId`

---

### Story 9-3: Global Filter, URL Sync & Deep-Link Sharing

As a PM,
I want to filter the dashboard by project and date range, and share my current view via URL,
So that I can focus on specific projects and send a deep-link to colleagues showing exactly what I see.

**Acceptance Criteria:**

**Given** tôi mở `/dashboard/overview`
**When** trang load
**Then** filter bar hiển thị với: project multi-select dropdown, date range picker, quick filter chips
**And** nếu URL có query params (`?projects=id1,id2&from=2026-04-01&to=2026-04-30`), filters được áp dụng ngay lập tức

**Given** PM chọn project filter (chọn 2 trong 5 projects)
**When** filter thay đổi
**Then** URL update ngay: `?projects=id1,id2` (không reload trang)
**And** tất cả widgets trên dashboard refresh để chỉ hiển thị data của 2 projects được chọn
**And** client-side filter từ NgRx cache < 100ms; server fetch nếu cache miss < 800ms

**Given** PM copy URL hiện tại và gửi cho đồng nghiệp
**When** đồng nghiệp (đã đăng nhập) mở URL đó
**Then** dashboard load với đúng filter state như PM đã set

**Given** PM chưa đăng nhập và mở deep-link `/dashboard/overview?projects=id1`
**When** trang load
**Then** redirect → `/login` với `returnUrl` encoded trong URL
**And** sau login thành công, redirect về đúng deep-link với filter params được giữ nguyên

**Given** PM click quick chip "Overdue"
**When** chip active
**Then** dashboard filter để chỉ hiển thị projects/tasks có overdue items
**And** URL update: `?chips=overdue`

**Given** PM thay đổi filter từ UI
**When** store selector `selectDashboardFilters` emit giá trị mới
**Then** `updateUrl$` effect navigate với `queryParamsHandling: 'merge'`; `skip(1)` để tránh vòng lặp với init từ URL

**Technical Notes:**
- `@ngrx/router-store` provide tại root; `routerNavigatedAction` trigger `syncFiltersFromUrl$` effect
- `updateUrl$` effect dùng `distinctUntilChanged(isEqual)` + `skip(1)` + `{ dispatch: false }`
- URL params: `projects` (comma-separated IDs), `from` (YYYY-MM-DD), `to` (YYYY-MM-DD), `chips` (comma-separated)
- `AuthGuard` handle redirect + returnUrl preservation cho deep-link
- `DashboardFilterFacade` expose public API: `setProjectFilter()`, `setDateRange()`, `toggleChip()`
- Quick chips MVP: "Overdue", "At Risk", "Overloaded" — stored in URL as `chips=overdue,atRisk,overloaded`

---

### Story 9-4: My Tasks — Cross-Project Task List

As a PM,
I want to see all tasks assigned to me across all projects in one list with filters,
So that I can manage my personal workload without switching between project views.

**Acceptance Criteria:**

**Given** PM navigate đến `/dashboard/my-tasks`
**When** trang load
**Then** hiển thị danh sách tasks được assign cho PM hiện tại, cross-project, sorted by due_date ASC
**And** mỗi task hiển thị: task name, project name, status, due_date, % complete

**Given** PM filter tasks theo status "In Progress"
**When** filter thay đổi
**Then** chỉ hiển thị tasks có status = In Progress
**And** filter state sync với URL: `?status=in-progress`

**Given** PM filter theo project cụ thể
**When** project filter thay đổi
**Then** chỉ hiển thị tasks thuộc project được chọn

**Given** PM click vào một task trong list
**When** click xảy ra
**Then** navigate đến task detail view (hoặc Gantt với task highlighted)

**Given** PM không có task nào được assign
**When** my-tasks component render
**Then** hiển thị empty state: "Bạn chưa được assign task nào" với CTA "Xem tất cả Projects"

**Given** my-tasks list có > 20 items
**When** render
**Then** hiển thị pagination với page size = 20; URL sync với `?page=2`

**Technical Notes:**
- Backend: `GetMyTasksCrossProjectQuery` trong `Reporting.Application/Dashboard/Queries/`
- Backend: `DashboardController.GetMyTasks()` → `GET /api/v1/dashboard/my-tasks?page=1&pageSize=20&status[]&projectIds[]`
- Frontend: `my-tasks.ts` component trong `features/dashboard/components/my-tasks/`
- Reuse `DashboardFilterFacade` cho project filter; thêm status filter local vào MyTasksComponent
- Query đọc trực tiếp từ `ProjectsDbContext` thông qua `ProjectSummaryProjector` context — hoặc query riêng nếu cần join

---

## Epic 10: Reports & Growth Features

**Goal:** PM và stakeholder có thể xem báo cáo chi phí chi tiết, export PDF/Excel, và chia sẻ deep-link report. Growth features (resource heatmap, milestone timeline, alert center UI) mở rộng theo usage signal từ MVP Dashboard.

---

### Story 10-1: Budget Report, Export & Deep-Link Sharing

As a PM,
I want to view the budget report with planned vs actual costs by project and vendor, and export it as PDF or Excel,
So that I can share accurate financial reports with leadership and vendors in minutes instead of hours.

**Acceptance Criteria:**

**Given** PM navigate đến `/reports/budget`
**When** trang load
**Then** `ReportShellComponent` hiển thị layout clean (không có sidebar/navbar)
**And** header chỉ có: project name/scope + "← Back to Dashboard" link + filter bar + export buttons
**And** trang load trong P95 < 3s

**Given** PM chọn month filter "Tháng 4/2026" và project scope "All"
**When** filter áp dụng
**Then** server fetch < 800ms
**And** budget table hiển thị: mỗi project một section, breakdown theo vendor, planned hours, actual hours, planned cost, actual cost

**Given** một vendor báo 24 ngày công trong tháng có 22 ngày làm việc
**When** budget table render
**Then** dòng vendor đó được highlight màu warning với tooltip "Số ngày công vượt ngày làm việc thực tế trong tháng"

**Given** PM click "Export PDF"
**When** export bắt đầu
**Then** PDF hoàn thành trong < 10s và download tự động
**And** PDF có footer: "Tài liệu này chứa thông tin tài chính nội bộ — không phân phối ra ngoài."
**And** footer này được generate server-side (Puppeteer) — không thể bị bỏ qua từ client

**Given** PM click "Export Excel"
**When** export bắt đầu
**Then** Excel file download trong < 3s (client-side SheetJS)
**And** file có đúng data như budget table đang hiển thị

**Given** PM click "Copy Link"
**When** click xảy ra
**Then** current URL (bao gồm filter params) được copy vào clipboard
**And** toast notification: "Link đã được copy"

**Given** authorized user nhận link `/reports/budget?month=2026-04&projects=all`
**When** mở link (đã đăng nhập)
**Then** report load với đúng filter state
**And** nếu chưa đăng nhập, redirect → login → redirect về đúng report URL

**Given** PM in report từ browser (Ctrl+P)
**When** print dialog mở
**Then** @media print CSS ẩn toolbar và export buttons; page-break-before: always giữa project sections

**Technical Notes:**
- Tạo `features/reports/` lazy-loaded module với `reports.routes.ts`
- Tạo `ReportShellComponent` với `@media print` SCSS
- Backend: mở rộng `GetCostSummaryQuery` trong `Reporting.Application` để support month/projectIds filter
- Backend: `ReportingController` — thêm endpoint `GET /api/v1/reports/budget?month=YYYY-MM&projectIds[]` với `Cache-Control: max-age=300`
- PDF: mở rộng `PdfExportWorker` hiện có; footer hardcoded server-side trong Puppeteer template
- Excel: client-side SheetJS trong `reports-api.service.ts`; không gọi API riêng cho Excel
- Vendor anomaly detection: query so sánh `sum(hours)` với `working_days_in_month * 8`; flag trong DTO
- `budget-filter-bar.ts` component với month picker (Angular Material) + project multi-select

---

### Story 10-2: Alert Center Data Model & Schema Migration

As a developer,
I want the Alert and AlertPreference database tables to be created,
So that the Alert Center UI (Growth phase) can be built without database schema changes.

**Acceptance Criteria:**

**Given** migration được apply
**When** database inspect
**Then** bảng `alerts` tồn tại trong `reporting` schema với đúng columns: id, project_id, user_id, type, entity_type, entity_id, title, description, is_read, created_at, read_at
**And** bảng `alert_preferences` tồn tại với: id, user_id, alert_type, enabled, threshold_days + UNIQUE constraint (user_id, alert_type)
**And** index `ix_alerts_user_read` tồn tại trên `alerts(user_id, is_read, created_at DESC)`

**Given** `AlertsController` được call
**When** `GET /api/v1/alerts` với valid JWT
**Then** trả về `AlertDto[]` của user hiện tại — không có data của user khác (per-user isolation)

**Given** PM call `PATCH /api/v1/alerts/{id}/read`
**When** alert thuộc về PM đó
**Then** trả về 204; `is_read = true`, `read_at = now()` trong DB
**And** nếu alert không thuộc về PM đó → 403 Forbidden

**Given** `Alert` entity được tạo
**When** code trong Reporting module
**Then** `Alert` và `AlertPreference` được map trong `ReportingDbContext` với EF Fluent configuration
**And** KHÔNG có UpdatedAt field — alerts là append-only, chỉ is_read/read_at được update

**Technical Notes:**
- Tạo `Alert.cs` + `AlertPreference.cs` trong `Reporting.Domain/Entities/`
- Tạo EF configuration `AlertConfiguration.cs` + `AlertPreferenceConfiguration.cs`
- Add `DbSet<Alert> Alerts` + `DbSet<AlertPreference> AlertPreferences` vào `ReportingDbContext`
- Add migration: `AddAlertCenterSchema`
- Tạo `AlertsController` với `GetMyAlerts` + `MarkAlertRead` actions
- Tạo `GetMyAlertsQuery` + `MarkAlertReadCommand` trong `Reporting.Application/Alerts/`
- Authorization: `MarkAlertReadHandler` phải verify `alert.UserId == currentUserId` trước khi update

---

### Story 10-3: Resource Heatmap & Milestone Timeline (Growth)

As a PM,
I want to see resource utilization as a person × week heatmap and milestones across all projects on one timeline,
So that I can identify capacity bottlenecks and cross-project scheduling conflicts at a glance.

**Acceptance Criteria:**

**Given** PM navigate đến `/reports/resources`
**When** trang load
**Then** hiển thị heatmap với rows = team members, columns = tuần, màu cell theo % capacity (green/yellow/orange/red)
**And** legend giải thích các mức màu: < 80% / 80–95% / 95–105% / > 105%

**Given** PM click vào một cell trong heatmap (person × week)
**When** click xảy ra
**Then** drill-down hiển thị danh sách tasks của người đó trong tuần đó

**Given** PM navigate đến `/reports/milestones`
**When** trang load
**Then** hiển thị timeline read-only với milestones của tất cả projects PM có membership
**And** mỗi milestone hiển thị: name, project, due_date, status
**And** timeline sort theo due_date

**Technical Notes:**
- Backend: `GET /api/v1/reports/resources?from=&to=` — query từ `ProjectSummarySnapshot` + assignments
- Backend: `GET /api/v1/reports/milestones?from=&to=` — query milestones từ `ProjectsDbContext`
- Frontend: `resource-report.ts` + `milestone-report.ts` trong `features/reports/components/`
- Heatmap: dùng Angular Material table với custom cell background based on capacity %
- Milestone timeline: reuse `gantt` rendering logic nếu available, hoặc custom SVG timeline

---

### Story 10-4: Alert Center UI & Email Digest (Growth)

As a PM,
I want to see prioritized alerts on my dashboard and receive weekly email digests,
So that I never miss critical issues even when I'm not actively monitoring the dashboard.

**Acceptance Criteria:**

**Given** PM có alerts chưa đọc
**When** mở dashboard
**Then** alert badge hiển thị unread count trên dashboard header
**And** PM có thể mở alert panel để xem danh sách alerts sorted by created_at DESC

**Given** PM xem alert list
**When** click vào một alert
**Then** alert được mark as read (PATCH /api/v1/alerts/{id}/read)
**And** navigate đến entity liên quan (task, project, vendor) nếu có entity_id

**Given** background job evaluate alert rules
**When** deadline của task < 48h và chưa có alert cho task đó hôm nay
**Then** tạo Alert record với type = 'deadline', title = "{task name} — deadline trong {N}h"

**Given** PM có email preferences enabled cho loại "overload"
**When** weekly digest job chạy (ví dụ mỗi thứ Hai 8:00)
**Then** PM nhận email summary các overload alerts từ tuần trước

**Technical Notes:**
- Frontend: alert badge + panel trong `DashboardShellComponent`
- Backend: background job evaluate alert rules — dùng `IHostedService` pattern tương tự hiện có
- Email: dùng SMTP hoặc SendGrid — cấu hình trong appsettings; template đơn giản (plain text + HTML)
- Alert rules: deadline < 48h, overload detected, budget > 90% planned
- `AlertPreference` table (từ Story 10-2) điều khiển loại alert và threshold của từng user
