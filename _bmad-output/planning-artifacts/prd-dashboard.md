---
stepsCompleted: ['step-01-init', 'step-02-discovery', 'step-02b-vision', 'step-02c-executive-summary', 'step-03-success', 'step-04-journeys', 'step-05-domain', 'step-06-innovation', 'step-07-project-type', 'step-08-scoping', 'step-09-functional', 'step-10-nonfunctional', 'step-11-polish']
inputDocuments: ['_bmad-output/planning-artifacts/prd.md', 'docs/DEMO-SCRIPT.md']
workflowType: 'prd'
briefCount: 0
researchCount: 0
brainstormingCount: 0
projectDocsCount: 1
outputFile: '_bmad-output/planning-artifacts/prd-dashboard.md'
classification:
  projectType: web_app
  domain: enterprise_project_management
  complexity: medium
  projectContext: brownfield
  angularModules:
    - DashboardModule
    - ReportsModule
  routingDecision:
    daily: ['/dashboard/overview', '/dashboard/my-tasks']
    reports: ['/reports/budget', '/reports/resources', '/reports/milestones', '/reports/vendor']
  filterStrategy: 'NgRx Store + URL params, Global scope (Project + DateRange + QuickChips)'
---

# Product Requirements Document - Dashboard & Reporting Module

**Author:** HieuTV-Team-Project-Management
**Date:** 2026-04-28

## Executive Summary

**Dashboard & Reporting Module** là lớp intelligence tổng hợp được bổ sung vào hệ thống quản lý dự án hiện có — biến dữ liệu phân tán trong từng Gantt chart, assignment engine, và cost tracker thành quyết định có thể hành động ngay trong một màn hình duy nhất.

**Vấn đề cốt lõi:** PM quản lý 3–5 dự án đồng thời với đội gồm inhouse và outsource từ nhiều vendor phải mở nhiều Gantt chart riêng lẻ, hỏi thủ công từng người, hoặc tổng hợp Excel để trả lời hai câu hỏi cơ bản mỗi sáng: *"Dự án nào đang có vấn đề?"* và *"Ai đang overload — ai còn capacity?"* Hệ thống hiện tại đã thu thập đủ dữ liệu; thiếu duy nhất lớp aggregation và visualization đứng trên toàn bộ dữ liệu đó.

**Trạng thái mục tiêu:** PM mở `/dashboard/overview` trong 30 giây — thấy ngay trạng thái tất cả dự án, overload alerts, và deadline nguy hiểm tuần này — rồi tiếp tục làm việc. Cuối tháng, `/reports/budget` xuất báo cáo chi phí theo vendor/dự án/nhân sự trong vài giây, không tổng hợp thủ công.

### Điểm khác biệt cốt lõi

Jira dashboard phục vụ sprint tracking trong một project đơn. Module này xây trên dữ liệu **richer và operational hơn**: vendor rate theo role/level, actual vs planned hours cross-project, multi-role allocation với overload detection, và immutable audit trail về mọi thay đổi chi phí.

**Hai insight kiến trúc phân biệt module này:**

1. **Operational vs Reporting split:** `/dashboard/*` — daily command center, live data, không export; `/reports/*` — lazy-loaded, print-optimized layout, bookmark/share/export cho stakeholders. Mỗi loại có shell component riêng, không pha trộn.

2. **Global filter state (NgRx + URL params):** Tất cả widgets phản ứng đồng bộ với một filter duy nhất (Project Scope + Date Range + Quick Chips). URL shareable — PM có thể gửi link report đã filter cho Finance team mà không cần hướng dẫn thêm.

**Core differentiator so với Jira:** Biết được vendor nào đang tốn bao nhiêu tiền, ai đang overload cross-project, và deadline nào sẽ kéo theo cascade failures — tất cả trong cùng một nơi.

## Project Classification

| Thuộc tính | Giá trị |
|---|---|
| **Loại sản phẩm** | Web Application — Angular SPA, Brownfield Extension |
| **Domain** | Enterprise Project Management — Internal Tool |
| **Độ phức tạp** | Medium — cross-project aggregation, NgRx filter state, lazy-loaded ReportsModule |
| **Ngữ cảnh** | Brownfield — thêm `DashboardModule` + `ReportsModule` vào app hiện có |
| **Angular Modules mới** | `DashboardModule` (daily operational) + `ReportsModule` (lazy, print-optimized) |
| **Routes** | `/dashboard/overview`, `/dashboard/my-tasks` + `/reports/budget`, `/reports/resources`, `/reports/milestones`, `/reports/vendor` |
| **Filter Strategy** | NgRx Store + URL params sync, Global scope (Project + DateRange + QuickChips) |

## Success Criteria

### User Success

- **30-giây Morning Check:** PM mở `/dashboard/overview` → biết trạng thái tất cả dự án trong 30 giây, không cần click thêm: dự án nào On Track / At Risk / Delayed, ai overload, deadline nào nguy hiểm trong 7 ngày tới
- **Overload detection proactive:** Cảnh báo overload hiển thị trực tiếp trên dashboard — không cần PM mở từng Gantt để phát hiện
- **Báo cáo tức thì:** Export báo cáo chi phí tháng (theo vendor/dự án/nhân sự) trong < 2 phút
- **Deep-link sharing:** PM gửi link report đã filter cho Finance/leadership — người nhận mở thẳng đúng report, không cần điều hướng thêm

### Business Success

- **Adoption:** ≥80% PM login Week 1 → 100% sử dụng ≥3x/week sau Week 4 *(đo từ JWT session logs)*
- **Loại bỏ Excel reporting:** Zero file Excel báo cáo tạo ra sau go-live
- **Thời gian báo cáo tháng:** 2–4 giờ → < 5 phút *(đo bằng export timestamp tracking)*
- **Leading Indicators:** Week 2 ≥50% PM dùng filter; Week 3 ≥3 reports exported; Week 4 ≥1 deep-link shared với stakeholder

### Technical Success

- **Performance:** Dashboard P95 load < 3s / 20 concurrent users / ≤200 projects
- **Filter:** Client-side (NgRx cached) < 100ms; Server fresh fetch < 800ms
- **Export:** PDF hoàn thành < 10s + pixel-accurate layout vs print preview
- **Resilience:** Widget-level error isolation — 1 widget lỗi không crash toàn trang
- **Freshness:** Data freshness timestamp hiển thị trên mỗi widget; polling interval 30–60s
- **Empty states:** Defined cho mọi widget khi no data
- **Caching:** HTTP `Cache-Control: max-age=300` + PostgreSQL composite indexes (no application cache layer)
- **Alert data model:** Tables `alerts` + `alert_preferences` thiết kế ở MVP, UI ship ở Growth

### Measurable Outcomes

| Chỉ số | Trước | Mục tiêu |
|---|---|---|
| Thời gian check trạng thái buổi sáng | 30–45 phút | < 2 phút |
| Thời gian tổng hợp báo cáo chi phí tháng | 2–4 giờ | < 5 phút |
| Thời gian phát hiện overload | Ngẫu nhiên, > 1 ngày | Real-time (dashboard) |
| File Excel báo cáo hàng tháng | N file | 0 file |

## Product Scope

### MVP — Week 1–2: The Morning Brief

**DashboardModule — `/dashboard/overview`:**
- Portfolio Health Cards: mỗi project một card (tên, %, traffic light Green/Yellow/Red)
- Stat Cards: 3 số — Overdue Tasks | At-Risk Projects | Overloaded Resources
- Upcoming Deadlines: top 7 deadlines trong 7 ngày, click → drill-down sang Gantt/Task view hiện có
- Project dropdown filter đơn giản (no full NgRx infrastructure yet)

### MVP — Week 3–4: Data-Driven Expansion

*Quyết định dựa trên behavior data từ Week 1–2:*
- Nếu user click "overdue tasks" nhiều → ship `/dashboard/my-tasks` (cross-project task list)
- Nếu stakeholder hỏi budget → ship `/reports/budget` (Planned vs Actual per project/vendor, Export PDF+Excel)
- Global filter NgRx + URL params sync (full infrastructure)
- Alert Center data model: tables `alerts` + `alert_preferences` (UI chưa có)

### Growth — Week 5–8

- `/reports/resources`: Resource Utilization Heatmap (người × tuần, màu theo % capacity)
- `/reports/milestones`: Cross-project Milestone Timeline (reuse Gantt component, read-only)
- `/reports/vendor`: Vendor Performance Report (on-time rate, cost trend), Export PDF
- Alert Center UI: alert feed trên dashboard, background job evaluate rules, email digest weekly
- Saved Filter Presets

### Vision — Tương lai

- Predictive alerts: "Dự án X có 70% khả năng trễ deadline nếu trend hiện tại tiếp tục"
- Executive PDF digest tự động gửi email hàng tuần cho leadership
- Mobile-responsive dashboard view
- Embedded Gantt mini trong `/dashboard/overview`

*→ Xem resource estimates, decision gates và risk mitigation tại [Project Scoping & Phased Development](#project-scoping--phased-development).*

## User Journeys

### Journey 1 — PM: Morning Command Center (Happy Path)

**Nhân vật:** Minh — PM quản lý 3 dự án, 12 người (5 inhouse + 7 outsource từ 2 vendor).

**Hoàn cảnh:** 8:15 sáng thứ Hai. Trước đây Minh mất 30–45 phút mở 3 Gantt chart riêng lẻ, hỏi Slack từng lead. Hôm nay lần đầu dùng Dashboard.

**Hành trình:**

- **Mở app:** Click "Dashboard" trên sidebar → `/dashboard/overview` load trong < 2 giây.
- **Scan Health Cards:** 3 cards — Project Alpha 🟢 78%, Project Beta 🟡 52%, Project Gamma 🔴 31%.
- **Scan Project Pulse Strip:** Beta timeline bar đã qua 75% thời gian nhưng progress ring chỉ 60% → nhận ra đang chậm mà không cần mở Gantt. Gamma: `5 overdue 🔴`.
- **Stat Cards:** 5 tasks overdue, 1 project at risk, 2 người overload.
- **Upcoming Deadlines:** "Sprint Demo — Project Beta — 01/05 — 3 ngày nữa".
- **Drill-down:** Click vào deadline → app mở Gantt view của Beta đúng task cần xem. Minh thấy Hùng đang bị assign 3 tasks song song.
- **Hành động:** Reassign 1 task từ Hùng sang Lan (đang có 16h trống). Đóng laptop lúc 8:35.

**Giá trị cốt lõi:** Từ 30–45 phút check thủ công xuống 20 phút — bao gồm cả xử lý vấn đề thực sự. Project Pulse Strip cho phép phát hiện "chậm tiến độ" mà không cần mở Gantt.

---

### Journey 2 — PM: Resource Crisis Mid-Sprint (Edge Case)

**Nhân vật:** Minh — thứ Tư 2:00 chiều, nhận Slack từ Hùng: "em bị kẹt task, Lan chưa xong phần API".

**Hoàn cảnh:** Dependency cascade — task Hùng block vì Lan chưa xong; Lan đang overload từ Project Alpha.

**Hành trình:**

- **Mở Dashboard:** Stat Card "2 người overload" đang hiển thị. Minh dùng Project dropdown → chọn "Project Alpha" → thấy các task của Lan đang due.
- **Cross-project impact:** Project Pulse Strip của Alpha: timeline bar đã qua 60% thời gian, progress ring 65% — tạm ổn. Nhưng Resource Snapshot dưới cùng cho thấy Lan đang ở 44h/tuần.
- **Quyết định:** Minh click "Xem Gantt đầy đủ →" trên card Lan → thấy task cụ thể chiếm thời gian → tìm Duy (Vendor B, 12h trống, đúng role Backend) để hỗ trợ.
- **Xác nhận:** Stat Card "Overloaded" giảm từ 2 xuống 1. Upcoming Deadlines của Beta vẫn giữ nguyên.

**Giá trị cốt lõi:** Cross-project visibility — phát hiện và xử lý resource conflict span qua 2 dự án mà không cần mở 2 Gantt riêng lẻ.

---

### Journey 3 — PM: Gửi Report cho Stakeholder (Reporting Path)

**Nhân vật:** Minh — ngày 30 hàng tháng, cần báo cáo chi phí cho Giám đốc và 2 vendor outsource.

**Hoàn cảnh:** Trước đây mất 2–3 giờ tổng hợp Excel. Năm ngoái sai số 15 triệu vì nhầm sheet.

**Hành trình:**

- **Mở report:** Sidebar → Reports → Budget → `/reports/budget`. Clean layout, không có sidebar/navbar.
- **Filter:** Chọn tháng 4/2026, tất cả projects. Widgets refresh < 800ms.
- **Phát hiện bất thường:** Vendor B báo 24 ngày công — system đánh dấu bất thường (tháng 4 chỉ 22 ngày làm việc). Có căn cứ để hỏi lại vendor.
- **Share với sếp:** "Copy Link" → `/reports/budget?month=2026-04&projects=all`. Paste vào email — sếp mở thẳng đúng report.
- **Export:** "Export PDF" → hoàn thành < 10 giây, pixel-accurate. Attach gửi vendor.
- **Tổng thời gian:** 8 phút từ mở app đến gửi email xong.

**Giá trị cốt lõi:** Từ 2–3 giờ tổng hợp thủ công xuống 8 phút; bất thường được phát hiện tự động với bằng chứng cụ thể.

---

### Journey 4 — Giám đốc: Xem Portfolio Status (Secondary User)

**Nhân vật:** Anh Tuấn — Giám đốc, không dùng app hàng ngày. Thứ Sáu nhận link từ Minh.

**Hoàn cảnh:** Anh Tuấn không biết Gantt. Chỉ cần biết: dự án nào ổn, dự án nào cần vào cuộc.

**Hành trình:**

- **Nhận link:** Email từ Minh kèm `/dashboard/overview`. Mở link — đã có session hoặc login redirect đúng URL.
- **Scan ngay:** Portfolio Health Cards + Project Pulse Strip — Alpha 🟢, Beta 🟡 (timeline bar 75% thời gian đã qua, chỉ 60% done), Gamma 🔴 `5 overdue`.
- **Hỏi thêm:** Click vào Gamma card → Upcoming Deadlines của Gamma: "Client Handoff — 03/05 — 5 ngày nữa". Gọi Minh: "Gamma đang delay, cần plan B không?"
- **Không cần training:** Toàn bộ interaction là đọc + click. Không cần biết Gantt, không thể sửa data.

**Giá trị cốt lõi:** Stakeholder dùng được ngay không cần training — dashboard đủ intuitive và đủ read-only để leadership ra quyết định từ xa.

---

### Journey Requirements Summary

| Capability cần có | Journey liên quan |
|---|---|
| Portfolio Health Cards (traffic light, %) | J1, J4 |
| **Project Pulse Strip** (Progress Ring + Mini Timeline Bar + Remaining Work Chip) | J1, J2, J4 |
| Stat Cards (overdue / at-risk / overload counts) | J1, J2 |
| Upcoming Deadlines với drill-down → Gantt/Task | J1, J2, J4 |
| Project dropdown filter | J2 |
| Resource Load Snapshot (cross-project) | J2 |
| `GET /api/projects/summary` endpoint mới | J1, J2, J4 |
| Budget Report với filter tháng/project | J3 |
| Bất thường detection (ngày công vs lịch) | J3 |
| Export PDF + Copy Link (shareable URL) | J3, J4 |
| Clean report layout (no sidebar/navbar) | J3 |
| Session persistence + redirect sau login | J4 |
| Read-only access cho secondary user | J4 |

## Domain-Specific Requirements

Domain: Enterprise Project Management (internal tool) — low compliance complexity. Không có HIPAA, PCI-DSS, hay regulatory certification yêu cầu.

**Data Sensitivity:** Vendor rates, employee hours, project costs là dữ liệu tài chính nhạy cảm nội bộ — chỉ truy cập được sau khi authenticate, không expose qua public endpoint.

**Audit Trail Integrity:** Dashboard và Reports chỉ được phép *đọc* từ audit trail — không có cơ chế chỉnh sửa hay xóa log qua UI. Mọi NFR về immutable audit trail từ PRD gốc được kế thừa nguyên vẹn.

**Export Security:** PDF/Excel báo cáo chứa vendor rates nhạy cảm — cần cảnh báo "Tài liệu này chứa thông tin tài chính nội bộ" trong footer của mọi export.

**Inherited NFRs từ PRD gốc:** JWT 8h session, bcrypt password, HTTPS, tất cả API endpoints yêu cầu valid token.

## Innovation & Novel Patterns

### Detected Innovation Areas

**1. Project Pulse Strip — Dual-Axis Progress Visualization**

Widget kết hợp hai chiều tiến độ trong một view duy nhất: **% thời gian đã trôi qua** (mini timeline bar) vs **% công việc hoàn thành** (progress ring). Khoảng cách giữa hai giá trị là tín hiệu cảnh báo tức thì — không cần tính toán, không cần mở Gantt.

Ví dụ: Bar hiển thị 75% thời gian đã qua, ring hiển thị 60% done → PM nhận ra ngay "đang chậm" chỉ bằng mắt nhìn. Hầu hết dashboard tools (kể cả Jira) chỉ hiển thị một trong hai — không đặt chúng cạnh nhau để so sánh trực quan.

**2. Operational vs Reporting — Two-Shell Architecture**

Hai Angular shell component hoàn toàn độc lập:
- `DashboardShellComponent`: app shell đầy đủ (sidebar, navbar, live polling) — daily operational use
- `ReportShellComponent`: clean layout không có UI chrome, `@media print` CSS riêng — print/export cho Finance/Leadership

URL `/reports/*` shareable và printable bởi người không cần biết UI của app. Không cần CSS hack hay conditional rendering phức tạp.

### Market Context & Competitive Landscape

Jira yêu cầu user tự cấu hình gadgets — không có opinionated defaults. Linear đẹp nhưng thiếu vendor cost tracking. Asana có portfolio view nhưng không có dual-axis progress visualization hay print-optimized report shell.

Project Pulse Strip là execution innovation — giải quyết pain point thực sự: PM cần biết "chậm bao nhiêu" không chỉ là "đang ở bao nhiêu %".

### Validation Approach

- **Pulse Strip:** Đo số lần PM mở Gantt sau khi đã nhìn Pulse Strip. Nếu < 50% lần check buổi sáng cần mở Gantt → strip đang làm tốt việc.
- **Two-Shell:** Thời gian từ vào `/reports/budget` đến click "Export PDF" < 3 phút → report shell đủ intuitive.

### Risk Mitigation

- **Pulse Strip accuracy:** Nếu `endDate` chưa cập nhật đúng → bar misleading. Mitigation: hiển thị "last updated" timestamp; cảnh báo khi project chưa có milestone cuối.
- **Report shell navigation:** User bị lạc khi ở `/reports/*`. Mitigation: `ReportShellComponent` luôn có header với project name + "← Back to Dashboard" link.

## Web Application — Yêu Cầu Kỹ Thuật

### Project-Type Overview

Dashboard & Reporting Module là extension của Angular SPA hiện có — hai lazy-loaded modules mới (`DashboardModule`, `ReportsModule`) được thêm vào app shell hiện tại. Không build app mới, không thay đổi routing cơ bản hiện có.

### Browser Matrix

Kế thừa từ PRD gốc — Chrome 100+ và Edge 100+ (Chromium) là primary, bắt buộc hoạt động hoàn hảo. Firefox, Safari không hỗ trợ chính thức.

### Responsive Design

Desktop-first, viewport tối thiểu 1280×768px. Dashboard được tối ưu tại 1440px+. Scroll ngang được phép trên `/reports/*` khi bảng dữ liệu rộng. Không có yêu cầu mobile responsive.

### Technical Architecture Considerations

**DashboardModule** (daily operational):
- Routes: `/dashboard/overview` → `DashboardOverviewComponent`, `/dashboard/my-tasks` → `MyTasksComponent`
- NgRx feature store slice: `dashboard.filters`, `dashboard.portfolioData`
- Polling service: interval 30–60s configurable
- `DashboardFilterFacade`: NgRx Store ↔ URL params sync

**ReportsModule** (lazy-loaded, print-optimized):
- Routes: `/reports/budget`, `/reports/resources` *(Growth)*, `/reports/milestones` *(Growth)*, `/reports/vendor` *(Growth)*
- `ReportShellComponent` riêng — không có app sidebar/navbar
- `@media print` CSS: ẩn toolbar, `page-break-before: always` giữa sections
- Export: PDF server-side (Puppeteer), Excel client-side (SheetJS)

**Shared `GET /api/projects/summary` endpoint** (mới):
```json
[{ "id", "name", "status", "startDate", "endDate",
   "percentComplete", "remainingTaskCount", "overdueTaskCount", "lastUpdatedAt" }]
```
Powers `ProjectSummaryCardComponent` (Project Pulse Strip) — không tái sử dụng Gantt component.

**Caching:** HTTP `Cache-Control: max-age=300` trên aggregation endpoints. PostgreSQL composite indexes: `(project_id, status, due_date)`, `(assignee_id, week_start)`.

### Implementation Considerations

**Alert Center Data Model** (thiết kế ở MVP, UI ở Growth):
```sql
alerts (id, project_id, user_id, type, entity_type, entity_id,
        title, description, is_read BOOLEAN, created_at, read_at NULLABLE)
alert_preferences (id, user_id, alert_type VARCHAR(50),
                   enabled BOOLEAN DEFAULT TRUE, threshold_days INT NULLABLE)
```

**Export footer:** Mọi PDF export có footer: *"Tài liệu này chứa thông tin tài chính nội bộ — không phân phối ra ngoài."*

**Deployment:** Web server nội bộ (inherited từ PRD gốc) — không thay đổi infrastructure.

## Project Scoping & Phased Development

### MVP Strategy & Philosophy

**MVP Approach:** Problem-Solving MVP — ship điều nhỏ nhất giải quyết pain point lớn nhất: biến 30–45 phút morning check thành < 5 phút. Không build platform; validate giá trị của cross-project visibility trước, mở rộng theo usage signal.

**Resource Requirements (MVP Week 1–2):**
- 1 backend engineer: 1 endpoint mới (`GET /api/projects/summary`) + composite indexes PostgreSQL
- 1 frontend engineer: `DashboardModule` setup + 4 components (Health Cards, Stat Cards, Pulse Strip, Upcoming Deadlines)
- 0.5 thời gian UX (từ PM feedback trực tiếp): 1 design iteration

**Resource Requirements (MVP Week 3–4):**
- 1 backend: 1 report endpoint (budget aggregation) hoặc `/dashboard/my-tasks` endpoint
- 1 frontend: `ReportsModule` setup + BudgetReport component hoặc MyTasksComponent + NgRx filter infrastructure
- Alert Center data model (migration only, no UI code)

### MVP Feature Set (Phase 1 — Week 1–2)

**Core User Journeys được hỗ trợ:** Journey 1 (PM Morning Command Center) + Journey 4 (Stakeholder read-only view)

**Must-Have Capabilities:**
- Portfolio Health Cards: mỗi project 1 card, traffic light (Green/Yellow/Red), % completion, remaining tasks
- Project Pulse Strip: dual-axis visualization — progress ring (% done) + mini timeline bar (% time elapsed) + remaining work chip
- Stat Cards: 3 chỉ số tổng hợp — Overdue Tasks | At-Risk Projects | Overloaded Resources
- Upcoming Deadlines: top 7 deadlines trong 7 ngày tới, click → drill-down Gantt/task view hiện có
- Project dropdown filter đơn giản (không cần full NgRx infrastructure)
- `GET /api/projects/summary` endpoint + `ProjectSummaryCardComponent`
- Alert Center data model: tables `alerts` + `alert_preferences` (schema migration only, UI ở Growth)

**Data-Driven Expansion (Week 3–4) — Quyết định dựa trên usage data từ Week 1–2:**
- *Nếu user click "overdue tasks" nhiều* → ship `/dashboard/my-tasks` (cross-project task list)
- *Nếu stakeholder hỏi budget* → ship `/reports/budget` (Planned vs Actual per project/vendor)
- Full NgRx filter infrastructure + URL params sync (áp dụng cho cả hai option)

### Growth Features (Phase 2 — Week 5–8)

- `/reports/resources`: Resource Utilization visualization (người × tuần, theo % capacity)
- `/reports/milestones`: Cross-project Milestone Timeline (read-only)
- `/reports/vendor`: Vendor Performance Report (on-time rate, cost trend)
- Alert Center UI: alert feed trên dashboard + email digest weekly
- Saved Filter Presets
- Export PDF (server-side) + Excel (client-side) cho tất cả report pages

### Vision Features (Phase 3 — Tương lai)

- Predictive alerts: "Dự án X có 70% khả năng trễ deadline nếu trend hiện tại tiếp tục"
- Automated executive email digest — tự động gửi cho leadership hàng tuần
- Mobile-responsive dashboard view
- Embedded mini-Gantt trong portfolio cards

### Risk Mitigation Strategy

**Technical Risks:**
- *Rủi ro:* `GET /api/projects/summary` aggregation query chậm khi > 50 projects/user
- *Giảm thiểu:* PostgreSQL composite indexes `(project_id, status, due_date)` + HTTP `Cache-Control: max-age=300` đủ cho scale 20 users/200 projects; không cần application-level cache layer

**Adoption Risks:**
- *Rủi ro:* PM tiếp tục dùng Excel do thói quen, không chuyển sang dashboard
- *Giảm thiểu:* MVP Week 1–2 chỉ giải quyết 1 pain point rõ ràng (morning check); measure adoption từ ngày đầu qua JWT session logs; nếu adoption < 50% sau Week 2 → investigate trước khi build thêm

**Resource Risks:**
- *Rủi ro:* Thiếu engineer → Week 3–4 slip hoặc không ship được
- *Giảm thiểu:* Week 1–2 đứng độc lập và deliver giá trị ngay; Week 3–4 là decision gate dựa trên data, không phải hard commitment — nếu resource không đủ, dừng ở Week 2 vẫn có sản phẩm hoàn chỉnh

## Functional Requirements

### Portfolio Overview

- FR1: PM có thể xem trạng thái tổng hợp của tất cả dự án trong danh mục quản lý trên một màn hình duy nhất
- FR2: PM có thể xem tình trạng sức khỏe (On Track / At Risk / Delayed) của từng dự án dưới dạng chỉ báo trực quan
- FR3: PM có thể xem tỷ lệ hoàn thành công việc (% tasks done) của từng dự án
- FR4: PM có thể xem đồng thời tiến độ công việc (% done) VÀ tiến độ thời gian (% time elapsed) của từng dự án để nhận diện độ lệch
- FR5: PM có thể xem số lượng task còn lại và số task đã quá hạn của từng dự án
- FR6: Stakeholder (Giám đốc/leadership) có thể xem tổng quan danh mục dự án mà không cần kiến thức về Gantt chart hay task management detail

### Resource Monitoring

- FR7: PM có thể xem danh sách thành viên đang bị overload (vượt capacity) cross-project
- FR8: PM có thể xem tải công việc của từng thành viên tính theo giờ hoặc ngày công trong kỳ hiện tại
- FR9: PM có thể xem chỉ số tổng hợp về số lượng thành viên đang overload (summary indicator)
- FR10: PM có thể xem thành viên nào đang có capacity trống để tiếp nhận thêm công việc
- FR11: PM có thể xem tải công việc cross-project theo chiều người × tuần (resource utilization view)

### Task & Deadline Tracking

- FR12: PM có thể xem danh sách các deadline quan trọng sắp đến trong 7 ngày tới, cross-project
- FR13: PM có thể xem danh sách tasks đã quá hạn cross-project
- FR14: PM có thể điều hướng từ một deadline hoặc task trên dashboard trực tiếp đến Gantt view hoặc task detail của project tương ứng
- FR15: PM có thể xem danh sách tasks được assign cho mình cross-project với trạng thái và deadline
- FR16: PM có thể lọc danh sách task/deadline theo project, assignee, hoặc trạng thái

### Budget & Cost Reporting

- FR17: PM có thể xem báo cáo chi phí tổng hợp theo từng dự án (planned vs actual)
- FR18: PM có thể xem chi phí breakdown theo từng vendor (hours billed, rate, total amount)
- FR19: PM có thể xem chi phí breakdown theo từng nhân sự hoặc nhóm trong dự án
- FR20: Hệ thống tự động phát hiện và đánh dấu dữ liệu vendor bất thường (số ngày công billed vượt số ngày làm việc thực tế trong kỳ)
- FR21: PM có thể lọc báo cáo chi phí theo kỳ (tháng/quý) và theo phạm vi dự án
- FR22: PM có thể xem báo cáo hiệu suất vendor (on-time delivery rate, cost trend theo thời gian)
- FR23: PM có thể xem milestone cross-project trên một timeline duy nhất dưới dạng read-only

### Priority Alerts

- FR24: PM có thể xem danh sách ưu tiên các vấn đề cần hành động ngay (deadline trong 48h, thành viên overload, budget burn cao) dưới dạng ranked list

### Filtering & Navigation

- FR25: PM có thể lọc toàn bộ dashboard theo project scope (một hoặc nhiều dự án)
- FR26: PM có thể lọc dashboard và reports theo khoảng thời gian (date range)
- FR27: PM có thể áp dụng quick filter chips để chọn nhanh các view preset thường dùng
- FR28: PM có thể lưu các bộ filter thường dùng dưới dạng preset để truy cập lại
- FR29: PM có thể chia sẻ đúng view dashboard/report (bao gồm filter state) với người khác thông qua URL
- FR30: PM có thể quay trở lại Dashboard với filter state được giữ nguyên sau khi drill-down vào Gantt hay task detail

### Export & Sharing

- FR31: PM có thể export report dưới định dạng PDF, bao gồm thông tin phân loại tài liệu tài chính nội bộ
- FR32: PM có thể export report dưới định dạng Excel (spreadsheet)
- FR33: Authorized users có thể mở deep-link report và xem đúng data/filter state được chia sẻ

### Report Presentation

- FR34: Report pages hiển thị trong layout riêng không chứa navigation chrome của app (sidebar, navbar)
- FR35: PM có thể in report trực tiếp từ trình duyệt với layout tự động tối ưu cho in ấn
- FR36: Report pages luôn hiển thị tên project/context và có link điều hướng quay lại Dashboard chính

### Alert Center

- FR37: Hệ thống lưu trữ cảnh báo về các sự kiện quan trọng (deadline nguy hiểm, overload, budget vượt ngưỡng) cho từng user
- FR38: PM có thể xem và quản lý danh sách cảnh báo (xem chưa đọc, đánh dấu đã đọc)
- FR39: PM có thể cấu hình loại cảnh báo muốn nhận và ngưỡng kích hoạt (threshold) cho từng loại cảnh báo

### Data Freshness & Reliability

- FR40: Users có thể biết mức độ freshness của data đang xem để ra quyết định chính xác
- FR41: Dashboard luôn hiển thị data gần với real-time mà không cần người dùng thao tác thủ công
- FR42: Khi một widget xảy ra lỗi, các widget còn lại vẫn hoạt động bình thường (không crash toàn trang)
- FR43: Mỗi widget hiển thị trạng thái rõ ràng khi không có dữ liệu (empty state)
- FR44: PM có thể kích hoạt cập nhật dữ liệu thủ công khi cần

## Non-Functional Requirements

### Performance

| Tiêu chí | Mục tiêu |
|---|---|
| `/dashboard/overview` initial load | P95 < 3s / 20 concurrent users / ≤200 projects |
| Client-side filter refresh (NgRx cached) | < 100ms |
| Server fresh fetch sau filter change | < 800ms |
| `/reports/budget` page load | P95 < 3s |
| Dashboard polling cycle | 30–60s configurable; mỗi poll hoàn thành < 800ms |
| PDF export (server-side Puppeteer) | < 10s từ click đến file download |
| Excel export (client-side SheetJS) | < 3s |
| `GET /api/projects/summary` | < 800ms với ≤200 projects + composite indexes |

### Security

- Tất cả `/dashboard/*` và `/reports/*` endpoints yêu cầu valid JWT token — không public access
- Vendor rates, employee hours, project costs chỉ truy cập được bởi authenticated users có project membership tương ứng
- Mọi data truyền qua HTTPS (inherited từ PRD gốc)
- PDF export financial classification footer được enforce server-side — client không thể bypass
- Deep-link report: user chưa login redirect → login → redirect về đúng report URL (session preserved)
- Alert Center: mỗi user chỉ thấy alerts của chính mình — không có cross-user data leakage
- Không cache sensitive data (vendor rates, cost breakdown) trong browser localStorage hoặc sessionStorage

### Scalability

- System hỗ trợ ≤20 concurrent users / ≤200 projects tại các performance targets đã định
- Không cần application-level cache: HTTP `Cache-Control: max-age=300` + PostgreSQL composite indexes `(project_id, status, due_date)` và `(assignee_id, week_start)` đủ cho quy mô này
- `ReportsModule` lazy-loaded — không ảnh hưởng initial bundle size của app hiện có

### Accessibility

Basic level — WCAG compliance không bắt buộc:

- Keyboard navigation đầy đủ cho filter dropdowns, date range pickers, export buttons
- ARIA labels trên tất cả icon-only buttons (export, refresh, copy link, close)
- Traffic light indicators dùng cả màu VÀ icon/text label — không dùng màu đơn thuần (color-blind support)
- Color contrast ratio ≥ 4.5:1 trên tất cả text elements

### Reliability

- **Widget error isolation:** 1 widget API fail không crash hoặc blank toàn trang; widget đó hiển thị error state riêng
- **Stale data resilience:** Nếu network không khả dụng, dashboard hiển thị data cũ kèm "last updated" timestamp — không crash
- **Empty states:** Tất cả widgets định nghĩa empty state UI — không có blank/broken layout khi no data
- **Export errors:** PDF/Excel generation failure hiển thị error message có thể hành động — không silent fail
- **Browser support:** Chrome 100+ và Edge 100+ (Chromium) bắt buộc; Firefox/Safari không hỗ trợ chính thức
