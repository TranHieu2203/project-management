---
stepsCompleted: ['step-01-validate-prerequisites', 'step-02-design-epics', 'step-03-create-stories']
inputDocuments:
  - '_bmad-output/planning-artifacts/prd.md'
  - '_bmad-output/planning-artifacts/architecture.md'
---

# project-management - Epic Breakdown

## Overview

This document provides the complete epic and story breakdown for project-management, decomposing the requirements from the PRD, UX Design if it exists, and Architecture requirements into implementable stories.

## Requirements Inventory

### Functional Requirements

FR1: Hệ thống là web application nội bộ thay thế Excel để quản lý mixed workforce (inhouse + outsource multi-vendor) trên nhiều dự án, cung cấp cái nhìn tập trung theo thời gian thực cho PM và quản lý.
FR2: Cung cấp Microsoft Project-style Gantt view split-panel (task tree trái, calendar phải) với hierarchy Dự Án → Phase → Milestone → Task và các cột dữ liệu task đầy đủ.
FR3: Hỗ trợ effort tracking theo giờ cho task và tổng hợp giờ theo ngày/tuần; cảnh báo khi vượt ngưỡng theo lịch làm việc chuẩn.
FR4: Hỗ trợ multi-role allocation: một nhân sự có thể giữ nhiều vai trò trên nhiều dự án đồng thời; phát hiện overload tự động.
FR5: Hỗ trợ per-vendor cost tracking: vendor có rate theo role/level; tổng hợp chi phí thực tế theo vendor và đối chiếu ngân sách.
FR6: Cung cấp unified multi-project view (tất cả dự án trên một giao diện thống nhất).
FR7: Cung cấp 1-click reporting: dashboard tiến độ và chi phí sẵn sàng chia sẻ.
FR8: Có audit trail đầy đủ, immutable: mọi thay đổi (task/effort/assignment/rate) ghi log ai sửa, sửa gì, khi nào; không retroactive adjustment.
FR9: (MVP) Quản lý dự án: tạo/sửa/xóa dự án với phân cấp Dự Án → Phase → Milestone → Task.
FR10: (MVP) Task hỗ trợ các trường: VBS, tên, loại, effort KH/TT (giờ), ngày KH & TT, % hoàn thành, predecessor, assignee (1 người), ưu tiên, trạng thái, ghi chú.
FR11: (MVP) Gantt chart view: split-panel layout và phân màu theo trạng thái (planned/actual/in-progress/late/milestone/today).
FR12: (MVP) Quản lý nhân sự: danh sách inhouse + outsource; phân bổ multi-role cross-project theo tỷ lệ.
FR13: (MVP) Quản lý vendor: danh sách vendor; vendor có nhiều nhân sự; đơn giá theo role/level; lưu lịch sử rate immutable.
FR14: (MVP) Cảnh báo overload tự động khi vượt 8h/ngày hoặc 40h/tuần.
FR15: (MVP) Cost tracking planned vs actual; tổng hợp theo vendor/dự án/nhân sự.
FR16: (MVP) Dashboard tổng quan: trạng thái dự án, overload alerts, tiến độ, chi phí.
FR17: (MVP) Báo cáo chi phí theo vendor/dự án/nhân sự; export PDF hoặc Excel.
FR18: (MVP) Multi-project view: tất cả dự án trong một giao diện.
FR19: (MVP) Data access: PM truy cập toàn bộ dữ liệu (rates/chi phí/giờ); data model sẵn sàng cho phân quyền giai đoạn sau.
FR20: Implement công thức tính chi phí: Hourly Rate = Monthly Rate ÷ 176h; chi phí = giờ thực tế (confirmed) × hourly rate; rate thay đổi theo ranh giới tháng; reconstruct được lịch sử.
FR21: Actual hours trên task là computed field (tổng hợp từ TimeEntry), không nhập trực tiếp; mọi thay đổi giờ thực tế tạo TimeEntry mới (immutable log).
FR22: Hỗ trợ 2 tầng ghi nhận actual hours: (1) Monthly vendor CSV import + mapping template + reconcile + lock; (2) Mid-month bulk timesheet grid (người × ngày/tuần × task).
FR23: TimeEntry có data status tracking: estimated | pm-adjusted | vendor-confirmed.
FR24: Quy tắc hiển thị báo cáo theo status; dashboard/forecast dùng mọi status; báo cáo chính thức dùng vendor-confirmed (hoặc pm-adjusted cho inhouse); hiển thị % confirmed vs estimated.
FR25: Validation rules cho TimeEntry: cảnh báo chênh lệch >20% so với estimate nếu thiếu note; hard cap 16h/ngày; tách entered_by (PM) và resource_id (người làm).
FR26: Holiday calendar configurable: admin CRUD ngày lễ; loại khỏi tính overload/capacity; hiển thị trên calendar; tự động shift deadline khi trùng/span qua ngày lễ.
FR27: Long-term retention: dữ liệu không tự xóa; entities có thể inactive nhưng giữ lịch sử.
FR28: Innovation areas: Predictive overload warning (traffic light), capacity-first assignment view (heatmap), smart assignment suggestion (rule-based), 4-week capacity forecast.
FR29: Validation/metrics: predictive overload (undo/reassign count), smart suggestion (accept vs override rate), forecast (proactive vs reactive bottleneck detection).
FR30: SPA Angular; dữ liệu qua REST API; overload calc client-side để phản hồi tức thì.
FR31: Data refresh polling-based (configurable 30–60s); không dùng websocket.
FR32: Overload check phản hồi <200ms client-side khi thay đổi assignment.
FR33: RESTful API (JSON).
FR34: Token-based authentication (JWT); pagination cho danh sách lớn.
FR35: Auth MVP: local username/password (hashed) + JWT 8h; admin quản lý tài khoản; thiết kế sẵn sàng thêm SSO (OAuth2/OIDC) ở Phase 2 không cần refactor authz.
FR36: PDF export phải server-side generation (Puppeteer hoặc tương đương), hướng tới schedule/batch.
FR37: Gantt rendering: ưu tiên thư viện Gantt thương mại (theo Architecture) + adapter layer; nếu dataset lớn (>500 tasks) cần tối ưu (canvas/virtualization theo quyết định kỹ thuật).
FR38: State management: NgRx store/effects; cache client-side cho overload check & polling.
FR39: Deployment nội bộ (on-prem/private cloud); không yêu cầu CDN/multi-region.
FR40: MVP strategy: Full product delivery + staged rollout theo sprint để feedback liên tục.
FR41: Resource requirements: team 3–4 dev (2 FE, 1–2 BE), 1 PM, 1 QA.
FR42: Staged rollout plan: Sprint 1–2 core gantt + assignment; Sprint 3–4 overload + heatmap; Sprint 5–6 forecast + reports + export; Sprint 7+ smart suggestion.
FR43: Phase 1 must-have: quản lý dự án & task + carry-over balance khi onboard (hours spent to date + remaining estimate).
FR44: Phase 1 must-have: Gantt dual-bar planned/actual; drag-drop reschedule & reassign; dependency arrows; holiday overlay.
FR45: Phase 1 must-have: people/vendor mgmt + immutable rate history.
FR46: Phase 1 must-have: actual hours logging 2-tier + status.
FR47: Phase 1 must-have: holiday calendar + auto shift deadline.
FR48: Phase 1 must-have: overload warning + predictive traffic light trước khi confirm assign.
FR49: Phase 1 must-have: capacity-first assignment heatmap (person × week) kết hợp cost.
FR50: Phase 1 must-have: 4-week rolling forecast (server-side precompute).
FR51: Smart assignment suggestion enable Sprint 7+: top 3 candidates + reasoning; track acceptance rate; tune nếu <40%.
FR52: Cost tracking/reporting: planned vs actual; anomaly detection; export PDF/Excel; report label % confirmed.
FR53: Notifications cơ bản: weekly email digest overload + tasks sắp trễ.
FR54: Auth & audit: local auth; audit tách entered_by vs resource_id; mọi mutation ghi log.

### NonFunctional Requirements

NFR1: Performance: Dashboard chính tải trong < 3 giây với 20 người dùng đồng thời.
NFR2: Độ chính xác: Tính toán giờ, chi phí, overload chính xác theo lịch Thứ 2–Thứ 6, 8h/ngày.
NFR3: Availability: hoạt động ổn định giờ làm việc; không mất dữ liệu.
NFR4: Trình duyệt: hoạt động đúng trên Chrome/Edge (hiện đại).
NFR5: Browser support: Chrome 100+ và Edge 100+ là primary; không hỗ trợ chính thức Firefox/Safari.
NFR6: Overload check latency: < 200ms (client-side).
NFR7: Performance targets: Gantt render (100 task) < 2s; cost report < 3s; PDF export < 10s server-side.
NFR8: Giả định đo lường: 20 users; ~10 dự án; ~100 task/dự án; ~50 nhân sự.
NFR9: Desktop-only viewport tối thiểu 1280×768; khuyến nghị 1440px+ cho Gantt; hỗ trợ scroll ngang.
NFR10: SEO không áp dụng; không cần SSR.
NFR11: Accessibility: không yêu cầu WCAG đặc biệt; đảm bảo dùng cơ bản (tab navigation).
NFR12: Real-time constraint: polling 30–60s; không dùng WebSocket.
NFR13: Gantt performance constraint: nếu >500 tasks cần chiến lược rendering phù hợp để đảm bảo performance.

### Additional Requirements

- Stack mục tiêu (Starter template): Angular CLI 21.2.10 + Angular Material 21.x + NgRx 21.1.0; Backend ASP.NET Core .NET 10 + EF Core 10.x; Database PostgreSQL; FE test Vitest; BE test xUnit.
- Kiến trúc backend: modular monolith nhiều module, mỗi module có DbContext/migrations riêng; host `ProjectManagement.Host` tham chiếu các `*.Api` module.
- FE structure: Angular standalone components, lazy-loaded feature routes; naming style Angular 21 (file ngắn gọn).
- CQRS/MediatR pattern bắt buộc cho Application layer; query dùng DTO, command trả `Result<Guid>`/`Result<Dto>`.
- Gantt: quyết định dùng thư viện thương mại (Bryntum) thay vì tự build; bắt buộc có Adapter Layer + Event Bridge + NgZone wrapper; cần chốt budget license trước Sprint 1 (nếu không có budget: fallback MIT libraries + scope reduction).
- State management: quyết định dùng NgRx từ Sprint 1 (tránh refactor về sau).
- TimeEntry & audit: TimeEntry immutable (không UPDATE), điều chỉnh qua bản ghi mới (`supersedes_id`); `rate_at_time` snapshot không null; `cost_at_time = hours × rate_at_time`; không có `updated_at`.
- Polling sync + conflict: optimistic locking với `version`/ETag; mutation dùng `If-Match`; server trả `409 Conflict` khi lệch version; UI cần inline reconciliation dialog; audit phải ghi cả 2 phiên bản.
- Overload traffic-light thresholds: Green <80%, Yellow 80–95%, Orange 95–105%, Red >105%; cần log override để tuning.
- API/DB naming conventions: DB `snake_case` (tables plural), API endpoints `/api/v1/{resource}` kebab-case plural; JSON fields `camelCase`; chuẩn hóa `HttpJsonOptions` trong .NET.
- Error responses theo chuẩn ProblemDetails (.NET built-in); 409 phải trigger inline reconciliation (không chỉ toast).
- Logging: structured logging (Serilog) theo chuẩn; phân mức `Information` cho business events (TimeEntry created, import completed),…
- Các điểm cần làm rõ trước Sprint 1: cross-project visibility; SSO (OIDC/SAML + IdP); Gantt license budget; overload partial allocation (0.5 FTE); exception cho immutability (xóa TimeEntry/GDPR?).
- Sprint 1–2 deliverable bắt buộc: Auth end-to-end + Projects (list/detail → CRUD) + deployable staging + demo stakeholder.
- **Sprint 1 decision gates (đã chốt):**
  - **Visibility**: membership-only (user chỉ thấy projects mình là member)
  - **SSO**: optional Sprint 1 (có local login cho staging/demo)
  - **Overload**: cho phép vượt capacity nhưng bắt buộc cảnh báo rõ ràng cho user
  - **TimeEntry**: append-only (không edit/delete trực tiếp); chỉ void + correction

### UX Design Requirements

Chưa có tài liệu UX trong `{planning_artifacts}` tại thời điểm hiện tại, nên chưa trích xuất được UX-DR cụ thể.

### FR Coverage Map

FR1: Epic 1 - Core planning trên Gantt interactive (mục tiêu thay Excel)
FR2: Epic 1 - Gantt split-panel + hierarchy + cột dữ liệu task
FR3: Epic 4 - Overload warning theo giờ (ngày/tuần)
FR4: Epic 2 - Workforce multi-role allocation nền tảng
FR5: Epic 2 - Vendor cost tracking theo role/level
FR6: Epic 1 - Unified multi-project view (portfolio/project navigation)
FR7: Epic 6 - Reporting 1-click (progress/cost) + export
FR8: Epic 2 - Audit trail immutable nền tảng
FR9: Epic 1 - Project CRUD + hierarchy scaffolding
FR10: Epic 1 - Task fields & editing cơ bản
FR11: Epic 1 - Gantt view màu trạng thái planned/actual/late/milestone/today
FR12: Epic 2 - Resource management (inhouse + outsource)
FR13: Epic 2 - Vendor management + rate model + history
FR14: Epic 4 - Overload alert 8h/day, 40h/week
FR15: Epic 6 - Cost tracking planned vs actual
FR16: Epic 6 - Dashboard tổng quan (progress/cost/alerts)
FR17: Epic 6 - Cost report theo vendor/project/resource + export
FR18: Epic 1 - Multi-project view (portfolio)
FR19: Epic 2 - Data model sẵn sàng phân quyền (MVP PM full access)
FR20: Epic 2 - Cost formula + monthly rate boundary + reconstruct history
FR21: Epic 3 - Actual hours computed từ TimeEntry (append-only)
FR22: Epic 3 - 2-tier actual hours: bulk grid + vendor CSV import
FR23: Epic 3 - TimeEntry status tracking (estimated/pm-adjusted/vendor-confirmed)
FR24: Epic 3 & Epic 6 - Rule dùng status cho dashboard vs báo cáo chính thức + % confirmed
FR25: Epic 3 - TimeEntry validation rules (20% note, 16h cap, entered_by vs resource_id)
FR26: Epic 6 - Holiday calendar ảnh hưởng hiển thị/reporting (deadline shift/overlay)
FR27: Epic 2 - Retention dài hạn + inactive marking
FR28: Epic 4 & Epic 5 - Predictive overload + heatmap + forecast (phân rã theo epic)
FR29: Epic 7 - Metrics/validation cho innovation areas (track accept/override, proactive/reactive)
FR30: Epic 1 - Angular SPA + REST integration baseline
FR31: Epic 4 - Polling-based refresh 30–60s
FR32: Epic 4 - Overload check <200ms client-side UX/logic
FR33: Epic 1 - RESTful API baseline
FR34: Epic 1 - JWT auth + pagination conventions (baseline)
FR35: Epic 1 - Auth model (local) + SSO-ready
FR36: Epic 6 - Server-side PDF export (Puppeteer, async)
FR37: Epic 1 - Gantt rendering strategy (Bryntum + adapter)
FR38: Epic 1 - State management (NgRx) + cache strategy
FR39: Epic 1 - Deployment nội bộ assumptions (staging demo)
FR40: Epic 1 - Staged rollout framing cho delivery
FR41: Epic 1 - Team/resource assumptions (planning)
FR42: Epic 1 - Sprint staging rollout plan anchor
FR43: Epic 1 - Project/task mgmt + carry-over balance onboarding
FR44: Epic 1 - Gantt dual bar + drag-drop + dependencies + holiday overlay (core planning UX)
FR45: Epic 2 - People/vendor mgmt + immutable rate history
FR46: Epic 3 - Actual logging 2-tier + status
FR47: Epic 6 - Holiday calendar capability (admin-config + effects)
FR48: Epic 4 - Predictive overload traffic-light
FR49: Epic 5 - Capacity-first heatmap view
FR50: Epic 5 - 4-week rolling forecast
FR51: Epic 5 & Epic 7 - Smart suggestion enablement + transparency + acceptance tracking
FR52: Epic 6 - Cost tracking/reporting + anomaly detection + export + confirmed label
FR53: Epic 7 - Weekly email digest notifications
FR54: Epic 1 & Epic 2 - Auth/audit foundation xuyên core flows

## Epic List

### Epic 1: Authentication + Portfolio/Project Setup + Gantt Interactive (Core Planning)
PM đăng nhập, tạo dự án/cấu trúc task, thao tác Gantt interactive để lập kế hoạch và cập nhật tiến độ cơ bản.
**FRs covered:** FR1, FR2, FR6, FR9, FR10, FR11, FR18, FR30, FR33, FR34, FR35, FR37, FR38, FR39, FR40, FR41, FR42, FR43, FR44

### Epic 2: Workforce (People/Vendor) + Rate Model + Audit Foundation
PM quản lý vendor + nhân sự, cấu hình rate theo role/level và nền audit/immutability để làm cơ sở cost/time tracking.
**FRs covered:** FR4, FR5, FR8, FR12, FR13, FR19, FR20, FR27, FR45, FR54

### Epic 3: TimeEntry & Timesheet (2-tier: Grid + Vendor Import) + Status/Lock + Corrections
PM ghi nhận giờ (bulk grid) và import timesheet vendor (mapping + reconcile + lock), có status tracking và correction/void theo audit.
**FRs covered:** FR21, FR22, FR23, FR24, FR25, FR46

### Epic 4: Overload Warning (Standard + Predictive) + Cross-project Aggregation
PM thấy và xử lý cảnh báo overload theo ngày/tuần, có predictive traffic-light trước khi xác nhận phân công; aggregation cross-project.
**FRs covered:** FR3, FR14, FR28 (predictive part), FR31, FR32, FR48

### Epic 5: Capacity Planning Suite (Heatmap + 4-week Forecast)
PM xem heatmap capacity-first và forecast 4 tuần để phát hiện bottleneck sớm, phục vụ vận hành.
**FRs covered:** FR28 (heatmap/forecast part), FR49, FR50, FR51 (enablement/track)

### Epic 6: Cost Tracking & Official Reporting (Confirmed vs Estimated) + Export
PM xem planned vs actual cost đa chiều, anomaly detection, xuất báo cáo PDF/Excel (server-side).
**FRs covered:** FR7, FR15, FR16, FR17, FR24, FR26, FR36, FR47, FR52

### Epic 7: Operations Layer (Notifications + In-product transparency metrics)
PM nhận weekly email digest, per-event notifications (assign/comment/transition/@mention), in-app Notification Center, và hệ thống ghi nhận các metrics (override predictive, accept/override suggestion, proactive vs reactive) để cải tiến vận hành/thuật toán. Stories 7.4–7.5 added.
**FRs covered:** FR29, FR53; updated with FR161-FR200

### Epic 9: Agile Board (Scrum + Kanban)
Cung cấp Board view tương tác (Kanban và Scrum) với drag-drop, Sprint CRUD, Backlog management, Sprint Planning UI tích hợp capacity từ Epic 5, swimlanes theo Assignee/Epic/Label, quick filters, và Sprint goal field. Stories 9.7–9.9 added.
**FRs covered:** FR9, FR10, FR11, FR28 (capacity integration); updated with FR161-FR200

### Epic 13: Agile Reports + Roadmap
Cung cấp bộ báo cáo Agile tiêu chuẩn (Burndown, Velocity, CFD, Sprint Report), Roadmap view cấp Epic, và Epic progress report (breakdown by status category). Story 13.6 added.
**FRs covered:** FR7, FR16, FR17 (mở rộng Agile reporting); updated with FR161-FR200

## Epic 1: Authentication + Portfolio/Project Setup + Gantt Interactive (Core Planning)

PM đăng nhập, tạo dự án/cấu trúc task, thao tác Gantt interactive để lập kế hoạch và cập nhật tiến độ cơ bản.

### Story 1.0: Starter template setup (Angular + .NET modular monolith) + repo skeleton

**FRs covered:** FR30, FR33, FR34, FR38, FR39

As a PM,
I want khởi tạo nền tảng dự án theo kiến trúc đã chốt (Angular 21 + .NET 10 modular monolith),
So that team có thể bắt đầu phát triển các story nghiệp vụ tiếp theo trên cấu trúc chuẩn ngay từ Sprint 1.

**Acceptance Criteria:**

**Given** Architecture đã chốt stack và cấu trúc solution/frontend
**When** khởi tạo codebase
**Then** tạo được skeleton theo cấu trúc: `.NET Host + Shared + Modules/*` (mỗi module tách layer) và `frontend/project-management-web` (Angular 21)
**And** tích hợp các dependency nền tảng đã chốt: Angular Material, NgRx, Vitest (FE) và xUnit (BE)

**Given** conventions đã chốt (naming, API prefix `/api/v1/`, JSON camelCase, ProblemDetails)
**When** chạy host API
**Then** API trả response theo chuẩn JSON camelCase và lỗi theo ProblemDetails
**And** có health endpoint tối thiểu để smoke test staging

**Given** dự án dùng optimistic locking và membership-only
**When** tạo “platform primitives” ban đầu
**Then** có baseline helpers/middleware để dùng lại cho stories sau (ETag/If-Match 412/409, membership-only trả 404, correlationId logging)

### Story 1.1: Local Authentication (login/logout/me) + JWT plumbing

**FRs covered:** FR35

As a PM,
I want đăng nhập/đăng xuất và duy trì session bằng JWT,
So that tôi có thể sử dụng hệ thống một cách an toàn và nhất quán trên staging/demo.

**Acceptance Criteria:**

**Given** user tồn tại và `isActive=true`
**When** user gọi `POST /api/auth/login` với email/password hợp lệ
**Then** trả `200` với `accessToken` (JWT) + `expiresIn` + thông tin user tối thiểu (`id`, `email`, `displayName`)
**And** response/error tuân thủ chuẩn `ProblemDetails` khi không thành công

**Given** email/password không hợp lệ **hoặc** user `isActive=false`
**When** gọi `POST /api/auth/login`
**Then** trả `401` (hoặc `403` cho inactive — chọn 1 và thống nhất) theo `ProblemDetails`
**And** không leak thông tin “email có tồn tại hay không”

**Given** client có JWT hợp lệ
**When** gọi `GET /api/auth/me`
**Then** trả `200` với user hiện tại (`id`, `email`, `displayName`)
**And** nếu thiếu/invalid/expired JWT thì trả `401 ProblemDetails`

**Given** user đang đăng nhập
**When** gọi `POST /api/auth/logout`
**Then** trả `204`
**And** (nếu backend có refresh token/cookie) session tương ứng bị vô hiệu hóa; còn không thì logout là client-side token disposal (document rõ)

**Given** bất kỳ lỗi validation/system nào xảy ra ở các endpoint auth
**When** trả lỗi 4xx/5xx
**Then** body luôn là `ProblemDetails` (không trả error shape tự chế)

**Given** Sprint 1 “SSO optional”
**When** deploy staging/demo
**Then** local login flow hoạt động end-to-end mà không phụ thuộc IdP

### Story 1.2: Project Membership-only authorization baseline

**FRs covered:** FR19

As a PM,
I want chỉ nhìn thấy/ truy cập được các project mà tôi là member,
So that dữ liệu không bị lộ giữa các dự án.

**Acceptance Criteria:**

**Given** user đã đăng nhập (JWT hợp lệ)
**When** gọi `GET /api/projects`
**Then** chỉ trả về danh sách project mà user là member (membership-only)

**Given** user đã đăng nhập và **là member** của project `{projectId}`
**When** gọi `GET /api/projects/{projectId}`
**Then** trả `200` với project detail

**Given** user đã đăng nhập nhưng **không phải member** của project `{projectId}`
**When** gọi `GET /api/projects/{projectId}`
**Then** trả `404 ProblemDetails` (không leak sự tồn tại của project)

**Given** user đã đăng nhập nhưng **không phải member** của project `{projectId}`
**When** gọi các endpoint con như `GET /api/projects/{projectId}/members`, `PUT /api/projects/{projectId}`, `DELETE /api/projects/{projectId}`
**Then** trả `404 ProblemDetails`

**Given** request không có/invalid/expired JWT
**When** gọi bất kỳ endpoint project nào
**Then** trả `401 ProblemDetails`

**Given** bất kỳ lỗi validation/system nào ở layer authorization/filtering
**When** trả lỗi 4xx/5xx
**Then** body luôn là `ProblemDetails` (chuẩn hoá)

### Story 1.3: Projects CRUD (create/list/detail/update/archive) with optimistic locking (ETag/If-Match + 409)

**FRs covered:** FR9

As a PM,
I want tạo và quản lý project (CRUD) với cơ chế chống ghi đè (optimistic locking),
So that nhiều PM có thể thao tác đồng thời mà không mất dữ liệu và có inline reconcile khi conflict.

**Acceptance Criteria:**

**Given** user đã đăng nhập (JWT hợp lệ)
**When** gọi `POST /api/projects` với payload hợp lệ (`code`, `name`, optional `description`)
**Then** trả `201` với project vừa tạo
**And** response header có `ETag` (version) để dùng cho update/delete
**And** project mặc định có `visibility=MembersOnly`

**Given** user đã đăng nhập
**When** gọi `GET /api/projects`
**Then** trả `200` danh sách projects (membership-only theo Story 1.2)
**And** mỗi item có `id`, `code`, `name`, `status`, `visibility` và `version`/`ETag` tương ứng

**Given** user đã đăng nhập và là member của `{projectId}`
**When** gọi `GET /api/projects/{projectId}`
**Then** trả `200` project detail
**And** header trả về `ETag`

**Given** user đã đăng nhập và là member của `{projectId}`
**When** gọi `PUT /api/projects/{projectId}` với header `If-Match: <etag>` hợp lệ và payload update hợp lệ
**Then** trả `200` project đã cập nhật
**And** trả `ETag` mới

**Given** user đã đăng nhập và là member của `{projectId}`
**When** gọi `PUT /api/projects/{projectId}` nhưng thiếu header `If-Match`
**Then** trả `412 ProblemDetails` (Precondition Failed)

**Given** user đã đăng nhập và là member của `{projectId}`
**When** gọi `PUT /api/projects/{projectId}` với `If-Match` không khớp phiên bản mới nhất
**Then** trả `409 ProblemDetails`
**And** payload có đủ thông tin để UI inline reconciliation, tối thiểu gồm `extensions.current` chứa “server state mới nhất” + `ETag` mới nhất

**Given** user đã đăng nhập và là member của `{projectId}`
**When** gọi `DELETE /api/projects/{projectId}` với `If-Match` khớp
**Then** trả `204`

**Given** user đã đăng nhập và là member của `{projectId}`
**When** gọi `DELETE /api/projects/{projectId}` nhưng thiếu `If-Match`
**Then** trả `412 ProblemDetails`

**Given** user đã đăng nhập và là member của `{projectId}`
**When** gọi `DELETE /api/projects/{projectId}` với `If-Match` không khớp
**Then** trả `409 ProblemDetails` (tương tự update)

**Given** user đã đăng nhập
**When** gọi `POST /api/projects` với `code` đã tồn tại
**Then** trả `409 ProblemDetails` (unique constraint)

**Given** request không có/invalid/expired JWT
**When** gọi bất kỳ endpoint projects nào
**Then** trả `401 ProblemDetails`

**Given** bất kỳ lỗi validation/system nào ở CRUD projects
**When** trả lỗi 4xx/5xx
**Then** body luôn là `ProblemDetails`

### Story 1.4: Project structure CRUD (Phase/Milestone/Task) + required task fields (MVP)

**FRs covered:** FR10, FR43

As a PM,
I want tạo và quản lý cấu trúc Dự Án → Phase → Milestone → Task và đầy đủ field của task (MVP),
So that tôi có thể biểu diễn kế hoạch như Excel/MS Project và chuẩn bị dữ liệu cho Gantt interactive.

**Acceptance Criteria:**

**Given** user là member của `{projectId}`
**When** user tạo/sửa/xóa Phase, Milestone, Task trong project
**Then** hệ thống lưu đúng quan hệ cha–con (Project → Phase → Milestone → Task)
**And** không cho tạo vòng lặp (một node không được là tổ tiên của chính nó)

**Given** user tạo hoặc cập nhật một Task
**When** submit dữ liệu
**Then** Task hỗ trợ tối thiểu các field: `vbs`, `name`, `type` (Phase/Milestone/Task), `priority`, `status`, `notes`, `plannedStartDate`, `plannedEndDate`, `actualStartDate`, `actualEndDate`, `plannedEffortHours`, `actualEffortHours`, `%complete`, `assigneeUserId` (1 người/task), `predecessors[]` với dependency types FS/SS/FF/SF

**Given** user thêm predecessor cho Task A
**When** chọn Task B làm predecessor (FS/SS/FF/SF)
**Then** không cho phép tạo dependency tạo thành cycle trong graph
**And** trả lỗi `400 ProblemDetails` với message rõ ràng khi cycle

**Given** Task có `plannedStartDate`/`plannedEndDate`
**When** user lưu Task
**Then** validate `plannedStartDate <= plannedEndDate`
**And** validate `actualStartDate <= actualEndDate` (nếu cả hai có giá trị)

**Given** user không phải member của `{projectId}`
**When** gọi bất kỳ endpoint CRUD hierarchy/task trong project đó
**Then** trả `404 ProblemDetails`

**Given** entity (Phase/Milestone/Task) có version/ETag
**When** update/delete thiếu `If-Match`
**Then** trả `412 ProblemDetails`
**And** mismatch version trả `409 ProblemDetails` kèm `extensions.current` để UI reconcile

### Story 1.5: Gantt adapter layer + Bryntum initial integration (read + render split panel)

**FRs covered:** FR2, FR11, FR37

As a PM,
I want xem project plan trên Gantt split-panel bằng Bryntum (qua adapter layer),
So that tôi có thể nhìn tổng quan timeline theo tuần và dependency links trực quan để chuẩn bị thao tác interactive.

**Acceptance Criteria:**

**Given** dự án dùng Bryntum
**When** app render Gantt
**Then** Bryntum được truy cập thông qua Gantt Adapter Layer (facade/service) để tránh lock-in UI logic trực tiếp vào vendor API
**And** adapter nhận input là model chuẩn hoá từ backend (projects/phases/milestones/tasks + dependencies)

**Given** project có hierarchy Phase/Milestone/Task
**When** mở màn Gantt của `{projectId}`
**Then** hiển thị split-panel: tree/grid bên trái + timeline bên phải
**And** tree thể hiện đúng hierarchy và thứ tự

**Given** mở Gantt lần đầu
**When** render timeline
**Then** granularity mặc định là Week
**And** có thể chuyển sang Day (nếu Bryntum hỗ trợ sẵn; không bắt buộc interactive trong story này)

**Given** task có plannedStart/End
**When** render Gantt
**Then** hiển thị bar planned theo màu/kiểu quy ước
**And** milestone hiển thị dạng marker riêng

**Given** task có `predecessors[]` (FS/SS/FF/SF)
**When** render Gantt
**Then** hiển thị dependency links/arrows tương ứng

**Given** user không phải member của `{projectId}`
**When** truy cập màn/endpoint Gantt data của project đó
**Then** trả `404 ProblemDetails`

**Given** project có ~100 tasks
**When** mở Gantt
**Then** thời gian render ban đầu đạt mục tiêu performance (theo NFR mục tiêu <2s cho scale vừa)
**And** nếu vượt ngưỡng thì ghi structured log/telemetry để đo (không `console.log`)

### Story 1.6: Gantt interactive edits (drag/resize/link) + save + 409 inline reconciliation

**FRs covered:** FR44

As a PM,
I want chỉnh kế hoạch trực tiếp trên Gantt (kéo/resize/link) và lưu an toàn,
So that tôi có thể lập kế hoạch nhanh nhưng không bị ghi đè dữ liệu của người khác.

**Acceptance Criteria:**

**Given** user là member của `{projectId}` và Gantt đã render (Story 1.5)
**When** user kéo task để đổi `plannedStartDate`/`plannedEndDate` hoặc resize bar
**Then** UI hiển thị trạng thái “chưa lưu” và cho phép lưu thay đổi

**Given** task có dependency
**When** user tạo/sửa link dependency (FS/SS/FF/SF)
**Then** UI update link và lưu theo cùng cơ chế optimistic locking

**Given** user bấm “Lưu”
**When** client gửi update với `If-Match` đúng version
**Then** server trả `200/204` và trả `ETag` mới
**And** UI refresh state theo bản mới nhất

**Given** client update nhưng thiếu `If-Match`
**When** server nhận request
**Then** trả `412 ProblemDetails`
**And** UI hiển thị thông báo “Dữ liệu đã thay đổi, cần tải lại” và reload snapshot mới nhất

**Given** client update với `If-Match` không khớp
**When** server phát hiện conflict
**Then** trả `409 ProblemDetails` kèm `extensions.current` (server state + ETag mới nhất)
**And** UI mở modal reconcile cho phép chọn “Dùng bản mới nhất” hoặc “Thử áp lại thay đổi của tôi”

**Given** user không phải member của `{projectId}`
**When** thao tác save từ Gantt
**Then** trả `404 ProblemDetails` và UI không leak thông tin dự án

### Story 1.7: Multi-project navigation (My Projects) + state isolation per project

**FRs covered:** FR6, FR18

As a PM,
I want chuyển đổi nhanh giữa các dự án và không bị lẫn dữ liệu,
So that tôi có thể quản lý nhiều dự án trong một giao diện thống nhất thay Excel.

**Acceptance Criteria:**

**Given** user đã đăng nhập
**When** mở trang “My Projects”
**Then** hiển thị danh sách projects (membership-only) và cho phép chọn 1 project để vào Gantt

**Given** user đang ở Gantt của Project A
**When** user chuyển sang Project B (qua selector hoặc route)
**Then** UI reset state/selection của Project A và tải dữ liệu Project B
**And** không có dữ liệu Project A xuất hiện trong UI của Project B

**Given** user truy cập trực tiếp URL `/projects/{projectId}/gantt`
**When** `projectId` không thuộc membership của user
**Then** trả `404` và UI hiển thị trang fallback phù hợp

### Story 1.8: Staging demo slice for Epic 1 (repeatable happy-path + 1 conflict path)

**FRs covered:** FR40, FR42

As a stakeholder,
I want có kịch bản demo staging lặp lại được cho Epic 1,
So that tôi có thể đánh giá nhanh “login → chọn dự án → Gantt → chỉnh → lưu → xử lý conflict”.

**Acceptance Criteria:**

**Given** môi trường staging có dữ liệu demo (seed hoặc data sẵn)
**When** chạy demo theo script: login → My Projects → mở Gantt → drag/resize 1 task → Save
**Then** thay đổi được persist và reload vẫn thấy đúng

**Given** mở 2 tab cho cùng 1 project
**When** tab A lưu thay đổi trước, tab B lưu sau với ETag cũ
**Then** tab B nhận `409/412` và UI hiển thị flow reconcile rõ ràng, không mất thao tác người dùng

---

## Epic 2: Workforce (People/Vendor) + Rate Model + Audit Foundation

PM quản lý vendor + nhân sự, cấu hình rate theo role/level và nền audit/immutability để làm cơ sở cost/time tracking.

### Story 2.1: Vendor master data CRUD + membership-only scope

**FRs covered:** FR13

As a PM,
I want quản lý danh sách vendor (active/inactive) và thông tin cơ bản,
So that tôi có thể quản lý workforce outsource và làm nền cho import/timesheet.

**Acceptance Criteria:**

**Given** user đã đăng nhập
**When** user tạo/sửa/inactive vendor
**Then** vendor được lưu với trạng thái rõ ràng (active/inactive) và không xoá dữ liệu lịch sử

**Given** user không có quyền trong scope vendor/workforce
**When** truy cập vendor endpoints
**Then** trả `404 ProblemDetails` theo quy ước membership-only

**Given** update/delete vendor yêu cầu version
**When** thiếu `If-Match`
**Then** trả `412 ProblemDetails`; mismatch trả `409 ProblemDetails` + `extensions.current`

### Story 2.2: Resource (inhouse/outsource) CRUD + vendor association

**FRs covered:** FR12

As a PM,
I want quản lý danh sách nhân sự (inhouse/outsource) và gắn vendor nếu outsource,
So that tôi có thể phân bổ người và tính cost đúng theo nguồn lực.

**Acceptance Criteria:**

**Given** resource có loại inhouse/outsource
**When** tạo resource outsource
**Then** bắt buộc chọn vendor và định danh resource rõ ràng (không trùng theo rule)

**Given** resource bị inactive
**When** truy vấn lịch sử/time entries cũ
**Then** dữ liệu vẫn truy vết được (retention)

### Story 2.3: Role/Level catalog + validation (no magic strings)

**FRs covered:** FR5, FR13

As a PM,
I want dùng danh mục Role/Level chuẩn hoá khi cấu hình rate và phân bổ,
So that dữ liệu nhất quán và tránh nhập tự do gây sai lệch.

**Acceptance Criteria:**

**Given** hệ thống có catalog Role/Level
**When** user tạo rate hoặc gán role/level
**Then** chỉ cho phép giá trị trong catalog (enum/const) và trả `400 ProblemDetails` nếu invalid

### Story 2.4: Monthly Rate model (vendor × role × level × month) + non-overlap rule

**FRs covered:** FR20

As a PM,
I want cấu hình monthly rate theo vendor × role × level với hiệu lực theo tháng,
So that hệ thống tính Hourly Rate và cost đúng, có thể reconstruct lịch sử.

**Acceptance Criteria:**

**Given** rate có hiệu lực theo tháng
**When** tạo rate mới cho cùng vendor/role/level/tháng đã tồn tại
**Then** bị chặn (non-overlap) và trả `409 ProblemDetails`

**Given** Hourly Rate = Monthly Rate ÷ 176
**When** hệ thống cần tính cost snapshot
**Then** công thức dùng chuẩn 176h cố định và ghi nhận rate snapshot theo rule

### Story 2.5: Audit events for master data changes (append-only)

**FRs covered:** FR8

As a PM,
I want mọi thay đổi vendor/resource/rate đều có audit trail append-only,
So that có thể đối soát “ai đổi gì, khi nào” như yêu cầu thay Excel.

**Acceptance Criteria:**

**Given** create/update/inactivate vendor/resource/rate
**When** mutation thành công
**Then** hệ thống ghi audit event (actor, timestamp, entity, before/after summary, correlationId)
**And** audit event không thể bị sửa/xoá qua API

---

## Epic 3: TimeEntry & Timesheet (2-tier: Grid + Vendor Import) + Status/Lock + Corrections

PM ghi nhận giờ (bulk grid) và import timesheet vendor (mapping + reconcile + lock), có status tracking và correction/void theo audit.

### Story 3.1: TimeEntry create (append-only) + status model (estimated/pm-adjusted/vendor-confirmed)

**FRs covered:** FR21, FR23

As a PM,
I want tạo TimeEntry theo ngày/tuần với status rõ ràng,
So that hệ thống có dữ liệu vận hành real-time và phục vụ báo cáo chính thức.

**Acceptance Criteria:**

**Given** user tạo TimeEntry từ UI/grid
**When** lưu entry
**Then** entry được INSERT mới (append-only) với `entryType` = `pm-adjusted` (hoặc `estimated` theo rule) và có `enteredBy` tách biệt `resourceId`

**Given** entry status dùng cho báo cáo
**When** truy vấn entries
**Then** response thể hiện rõ status và nguồn dữ liệu

### Story 3.2: TimeEntry list/filter (range, resource, project, status) + paging

**FRs covered:** FR24

As a PM,
I want xem danh sách TimeEntry theo kỳ và filter,
So that tôi kiểm tra nhanh dữ liệu đã confirmed vs chưa confirmed.

**Acceptance Criteria:**

**Given** query theo range ngày/tuần/tháng
**When** gọi list endpoint
**Then** hỗ trợ paging và filter theo resource/project/status, trả `200` nhanh và nhất quán

### Story 3.3: Void TimeEntry (soft void) + Correction entry (append-only chain)

**FRs covered:** FR8, FR21

As a PM,
I want sửa sai giờ công mà không overwrite lịch sử,
So that audit trail bất biến vẫn đúng và dữ liệu có thể đối soát.

**Acceptance Criteria:**

**Given** TimeEntry tồn tại
**When** user void entry với reason
**Then** entry chuyển sang trạng thái voided (soft) và vẫn giữ trong lịch sử
**And** bắt buộc lưu `voidReason` và `voidedBy` (audit), không được hard-delete

**Given** cần chỉnh số giờ
**When** user tạo correction
**Then** tạo bản ghi TimeEntry mới liên kết entry gốc (chain), không UPDATE record cũ
**And** correction phải có `reason` và giữ trace được “original vs corrected vs net”

### Story 3.4: Bulk Timesheet Grid (person × week) + validation (16h/day cap, >20% note)

**FRs covered:** FR22, FR25

As a PM,
I want nhập giờ theo dạng grid nhanh cho nhiều người/nhiều ngày,
So that tôi cập nhật vận hành giữa tháng mà không tốn công như Excel.

**Acceptance Criteria:**

**Given** PM nhập giờ trong grid
**When** nhập >16h/ngày cho 1 người
**Then** bị chặn với lỗi inline và `400 ProblemDetails` khi submit

**Given** giờ pm-adjusted lệch >20% so với estimate
**When** submit mà không có note/reason
**Then** hiển thị cảnh báo và yêu cầu note (policy) trước khi lưu
**And** grid có các state: loading/empty/error, và không mất dữ liệu người dùng nhập khi API lỗi (có retry)
**And** hỗ trợ keyboard cơ bản (Tab/Enter/Esc) cho thao tác cell (không bắt buộc dùng chuột)

### Story 3.5: Vendor CSV import pipeline (upload → mapping → validate → apply) + job status polling

**FRs covered:** FR22

As a PM,
I want import timesheet vendor theo CSV/Excel với mapping template,
So that cuối tháng có nguồn vendor-confirmed làm báo cáo chính thức.

**Acceptance Criteria:**

**Given** PM upload file vendor
**When** tạo import job
**Then** trả `jobId` và có endpoint status để polling progress

**Given** mapping template theo vendor
**When** map cột và validate
**Then** có preview lỗi theo dòng/cột; `dry-run` không commit domain data
**And** có phân loại lỗi blocking vs warning; warning yêu cầu confirm trước khi apply
**And** cho phép tải xuống error report (CSV) của các dòng lỗi

**Given** apply import
**When** job chạy
**Then** tạo/ghi nhận entries `vendor-confirmed` theo rule idempotency và lưu file gốc để đối soát
**And** re-run cùng file/correlationId không double-count (idempotent theo file hash + row fingerprint)

### Story 3.6: Reconcile + lock confirmed period (official cost uses confirmed)

**FRs covered:** FR22, FR24

As a PM,
I want reconcile dữ liệu import với dữ liệu estimate/pm-adjusted và lock kỳ,
So that báo cáo chính thức chỉ dùng dữ liệu đã confirmed và không bị thay đổi âm thầm.

**Acceptance Criteria:**

**Given** import tạo tập entries vendor-confirmed
**When** PM approve + lock kỳ
**Then** entries trong kỳ chuyển sang trạng thái locked (không edit trực tiếp)
**And** mọi thay đổi sau lock chỉ qua correction/adjustment (append-only)
**And** lock scope được định nghĩa rõ theo (vendor, period) và timezone hệ thống (ví dụ `Asia/Ho_Chi_Minh`)

---

## Epic 4: Overload Warning (Standard + Predictive) + Cross-project Aggregation

PM thấy và xử lý cảnh báo overload theo ngày/tuần, có predictive traffic-light trước khi xác nhận phân công; aggregation cross-project.

### Story 4.1: Overload rules engine (OL-01/OL-02) + explainable breakdown

**FRs covered:** FR3, FR14

As a PM,
I want hệ thống tính overload theo ngày/tuần và giải thích được,
So that tôi hiểu rõ vì sao bị cảnh báo và điều chỉnh kịp thời.

**Acceptance Criteria:**

**Given** time entries trong range
**When** compute overload
**Then** áp dụng OL-01 (>8h/day) và OL-02 (>40h/week) và trả breakdown theo ngày/tuần
**And** result deterministic với cùng input snapshot

### Story 4.2: Overload warning UI (warn-only) + polling-friendly UX

**FRs covered:** FR14

As a PM,
I want thấy cảnh báo overload rõ ràng nhưng không bị chặn thao tác,
So that tôi vẫn làm việc được và hiểu rủi ro.

**Acceptance Criteria:**

**Given** overload detected
**When** UI hiển thị cảnh báo
**Then** hiển thị severity/tooltip “vì sao”, không chặn thao tác lưu
**And** polling refresh không gây nhấp nháy UI
**And** UI có “Last updated” và state loading/empty/error rõ ràng (retry được)

### Story 4.3: Predictive traffic-light thresholds + override tracking

**FRs covered:** FR48

As a PM,
I want thấy dự báo overload theo traffic-light trước khi xác nhận,
So that tôi phòng ngừa trước khi quá tải thực xảy ra.

**Acceptance Criteria:**

**Given** capacity utilization %
**When** hiển thị predictive status
**Then** dùng ngưỡng Green<80, Yellow 80–95, Orange 95–105, Red>105
**And** nếu PM override cảnh báo thì ghi event/metric để tuning sau
**And** UI hiển thị nhãn “Dự báo” tách biệt với overload “đã xảy ra”, kèm giải thích ngắn (top 3 inputs)

### Story 4.4: Cross-project aggregation view (membership-only) + non-leak

**FRs covered:** FR28

As a PM,
I want xem tổng hợp overload cross-project trong phạm vi tôi có quyền,
So that tôi có cái nhìn tổng quan mà không lộ dữ liệu project khác.

**Acceptance Criteria:**

**Given** user là member của nhiều projects
**When** xem overload cross-project
**Then** chỉ aggregate trên projects user có membership
**And** non-member không suy luận được project tồn tại (404/no leak)

---

## Epic 5: Capacity Planning Suite (Heatmap + 4-week Forecast)

PM xem heatmap capacity-first và forecast 4 tuần để phát hiện bottleneck sớm, phục vụ vận hành.

### Story 5.1: Capacity heatmap (person × week) + legend + drill-down

**FRs covered:** FR49

As a PM,
I want xem heatmap capacity theo tuần với legend rõ,
So that tôi quyết định phân bổ dựa trên capacity trước.

**Acceptance Criteria:**

**Given** range 4–8 tuần
**When** render heatmap
**Then** có legend/ngưỡng overload rõ ràng và tooltip giải thích
**And** drill-down chỉ trong scope membership
**And** màu không là kênh duy nhất (có label/tooltip), hỗ trợ người mù màu (pattern/icon)

### Story 5.2: Forecast precompute job (4-week rolling) + versioned reads

**FRs covered:** FR50

As a PM,
I want forecast 4 tuần được precompute và đọc theo version ổn định,
So that UI tải nhanh và tránh tính toán nặng trên client.

**Acceptance Criteria:**

**Given** dữ liệu time entry thay đổi đáng kể
**When** trigger forecast precompute
**Then** tạo job và cập nhật forecast artifact theo “latest succeeded”
**And** API trả về `computedAt`/`version` để UI hiển thị “cập nhật lần cuối”

### Story 5.3: Forecast delta (“what changed”) + actionable hints

**FRs covered:** FR29

As a PM,
I want thấy thay đổi chính của forecast so với lần trước,
So that tôi phát hiện bottleneck proactively thay vì reactive.

**Acceptance Criteria:**

**Given** có 2 bản forecast gần nhất
**When** xem forecast
**Then** hiển thị delta chính (top changes) và gợi ý hành động tối thiểu

---

## Epic 6: Cost Tracking & Official Reporting (Confirmed vs Estimated) + Export

PM xem planned vs actual cost đa chiều, anomaly detection, xuất báo cáo PDF/Excel (server-side).

### Story 6.1: Cost computation (planned vs actual) using rate snapshots + confirmed policy

**FRs covered:** FR15, FR20, FR52

As a PM,
I want hệ thống tính cost planned vs actual theo rule confirmed/estimated,
So that báo cáo phản ánh đúng độ tin cậy dữ liệu.

**Acceptance Criteria:**

**Given** TimeEntry có status
**When** tính cost report chính thức
**Then** chỉ dùng vendor-confirmed (hoặc pm-adjusted cho inhouse) theo rule
**And** báo cáo hiển thị % confirmed vs estimated

### Story 6.2: Cost reporting APIs (vendor/project/resource/month) + filters

**FRs covered:** FR17

As a PM,
I want xem báo cáo chi phí theo nhiều chiều và filter,
So that tôi tổng hợp nhanh thay vì Excel.

**Acceptance Criteria:**

**Given** filter theo tháng/vendor/project/resource
**When** gọi reporting endpoint
**Then** trả dữ liệu theo chiều đã chọn với paging/limits và `ProblemDetails` chuẩn khi lỗi

### Story 6.3: Export jobs (CSV/XLSX/PDF) + progress + artifact download

**FRs covered:** FR17

As a PM,
I want export báo cáo ra file (async) với tiến trình rõ,
So that tôi chia sẻ với cấp trên và đối soát dễ dàng.

**Acceptance Criteria:**

**Given** user trigger export
**When** tạo export job
**Then** trả `jobId`, cho phép polling status, và khi xong cung cấp download (artifact/signed URL)
**And** export dùng snapshot/asOf để dữ liệu nhất quán
**And** tên file export chứa timestamp + phạm vi filter (dễ tái lập); export không leak dữ liệu ngoài scope membership

### Story 6.4: PDF generation via Puppeteer worker (async) + failure reporting

**FRs covered:** FR36

As a PM,
I want PDF export được generate server-side ổn định,
So that layout nhất quán và không phụ thuộc browser print.

**Acceptance Criteria:**

**Given** export format = PDF
**When** job chạy
**Then** worker Puppeteer generate PDF trong SLA mục tiêu và ghi lỗi chi tiết nếu fail

---

## Epic 7: Operations Layer (Notifications + In-product transparency metrics)

PM nhận weekly email digest và hệ thống ghi nhận các metrics (override predictive, accept/override suggestion, proactive vs reactive) để cải tiến vận hành/thuật toán.

### Story 7.1: Weekly digest notifications (overload + overdue) + unsubscribe controls

**FRs covered:** FR53

As a PM,
I want nhận weekly digest email/in-app cho overload và task sắp trễ,
So that tôi không bỏ sót rủi ro ngay cả khi không mở tool.

**Acceptance Criteria:**

**Given** lịch chạy hàng tuần
**When** gửi digest
**Then** nội dung có “vì sao” + link xử lý và không spam (coalesce)
**And** user có thể cấu hình tần suất/tắt theo loại thông báo

### Story 7.2: Metrics capture (override predictive, forecast proactive, suggestion accept/override)

**FRs covered:** FR29, FR51

As a PM,
I want hệ thống ghi nhận metrics hành vi quan trọng,
So that có dữ liệu để cải tiến ngưỡng cảnh báo và gợi ý phân công sau này.

**Acceptance Criteria:**

**Given** PM override cảnh báo predictive hoặc thực hiện hành vi liên quan forecast
**When** sự kiện xảy ra
**Then** hệ thống ghi metric event (correlationId, actor, timestamp, context) và có thể truy vấn tổng hợp cơ bản

### Story 7.3: Task deadline visual alerts — in-app UI (Project Detail + Gantt)

**FRs covered:** FR53 (in-product), FR11 (Gantt màu trạng thái)

As a PM,
I want thấy ngay trên màn Project Detail và Gantt những task nào quá hạn / đến hạn hôm nay / sắp đến hạn (7 ngày),
So that tôi không cần mở email digest hay scroll toàn bộ task tree mới phát hiện rủi ro deadline.

**Acceptance Criteria:**

**Given** có task chưa hoàn thành với `plannedEndDate` thuộc 3 nhóm (quá hạn / hôm nay / sắp hạn 7d)
**When** PM mở Project Detail hoặc Gantt view
**Then** alert banner phía trên hiển thị badge đếm per nhóm (màu đỏ/cam/vàng)
**And** task tree row được tô màu nền tương ứng (`.row-overdue` / `.row-due-today` / `.row-due-soon`)
**And** thanh Gantt có CSS class tương ứng (`.b-task-overdue` / `.b-task-due-today` / `.b-task-due-soon`)

**Given** PM click badge trên alert banner
**When** sự kiện click xảy ra
**Then** task tree cuộn đến task đầu tiên của nhóm đó và highlight nhẹ

**Given** không có task nào trong 3 nhóm
**When** render màn
**Then** alert banner KHÔNG hiển thị

**Technical notes:**
- 100% frontend-only — không cần API mới, tính toán từ tasks đã có trong NgRx store
- `DeadlineAlertService` (pure injectable, no HttpClient) — so sánh string ISO "YYYY-MM-DD"
- Collapsed state lưu `localStorage['deadline-banner-collapsed']`
- Áp dụng cả 2 màn: Project Detail (`/projects/{id}`) và Gantt (`/projects/{id}/gantt`)

### Story 7.4: Per-event notification triggers (assigned/commented/transitioned/@mentioned)

**FRs covered:** FR53

As a team member,
I want nhận thông báo ngay lập tức khi có sự kiện liên quan đến tôi (được assign, được @mention, issue chuyển trạng thái, có comment mới),
So that tôi phản hồi kịp thời mà không cần liên tục kiểm tra tool thủ công.

**Acceptance Criteria:**

**Given** một issue được assign cho user A
**When** PM hoặc user khác thực hiện assignment
**Then** user A nhận thông báo ngay (in-app notification + email) với link trực tiếp đến issue

**Given** có comment mới trên issue mà user đang watch hoặc từng được @mention
**When** comment được lưu
**Then** user nhận in-app notification và email với nội dung tóm tắt comment và link đến issue

**Given** issue chuyển trạng thái (status transition)
**When** transition được thực hiện
**Then** assignee, reporter và watchers của issue nhận in-app notification về sự thay đổi trạng thái

**Given** user được @mention trong comment hoặc description của issue
**When** nội dung được lưu
**Then** user được @mention nhận in-app notification và email riêng cho sự kiện @mention đó
**And** mỗi loại sự kiện (assigned/commented/status-changed/mentioned) có thể bật/tắt độc lập trong Notification Preferences của từng user

### Story 7.5: In-app Notification Center (bell icon + notification list)

**FRs covered:** FR53

As a team member,
I want xem tất cả thông báo của mình trong một Notification Center trong app với badge đếm chưa đọc,
So that tôi không bỏ sót thông báo quan trọng và có thể xử lý chúng trực tiếp từ một nơi.

**Acceptance Criteria:**

**Given** user có thông báo chưa đọc
**When** nhìn vào app header
**Then** bell icon hiển thị badge số lượng thông báo chưa đọc (unread count); badge biến mất khi không có thông báo chưa đọc

**Given** user click vào bell icon
**When** Notification Center mở (dropdown hoặc drawer)
**Then** hiển thị tối đa 50 thông báo gần nhất với: timestamp, actor (ai thực hiện hành động), tóm tắt sự kiện, và link đến issue liên quan
**And** thông báo chưa đọc được đánh dấu khác biệt với thông báo đã đọc

**Given** Notification Center đang mở
**When** user click vào một thông báo
**Then** app điều hướng đến issue/comment liên quan và thông báo đó được đánh dấu đã đọc

**Given** Notification Center có nhiều thông báo
**When** user click "Mark all as read"
**Then** tất cả thông báo được đánh dấu đã đọc và badge unread count về 0
**And** user có thể filter notifications theo loại: assigned / commented / mentioned / status-changed

---

## Epic 8: Issue Model Migration + Agile Foundation

Chuyển đổi bảng `tasks` hiện tại sang mô hình `issues` đa kiểu (Bug/Story/Epic/Task/Sub-task), duy trì backward compatibility qua view, và cung cấp nền tảng Agile để các Epic sau (Board, Workflow, Custom Fields) xây dựng lên.

**FRs covered:** FR9, FR10, FR43 (mở rộng nền tảng)

### Story 8.0: Issue table migration (expand-contract: tasks → issues + view backward compat)

**FRs covered:** FR9, FR10

As a developer,
I want migrate bảng `tasks` sang bảng `issues` theo pattern expand-contract mà không gián đoạn dữ liệu hiện có,
So that hệ thống hỗ trợ đa kiểu issue trong khi các module cũ vẫn hoạt động bình thường qua view.

**Acceptance Criteria:**

**Given** bảng `tasks` tồn tại với dữ liệu hiện có
**When** chạy migration expand-contract (thêm cột mới, backfill, tạo view `tasks`)
**Then** bảng `issues` có đầy đủ cột mới (`issue_type`, `parent_id`, `story_points`, `custom_fields` JSONB, `resolution`, `environment`)
**And** view `tasks` vẫn hoạt động và trả dữ liệu đúng cho mọi query cũ

**Given** view `tasks` được tạo như alias của `issues`
**When** module Gantt/Reporting đọc qua view
**Then** không có lỗi runtime và dữ liệu nhất quán với bảng `issues`

**Given** migration hoàn tất
**When** chạy bộ test integration hiện tại
**Then** tất cả test pass (không regression)

**And** có rollback script được document rõ để revert nếu cần

### Story 8.1: Issue Types catalog (Bug/Story/Epic/Task/Sub-task + custom)

**FRs covered:** FR9

As a system admin,
I want cấu hình danh mục Issue Type cho hệ thống (gồm các type mặc định và custom),
So that mỗi project có thể dùng đúng loại issue phù hợp workflow của họ.

**Acceptance Criteria:**

**Given** hệ thống khởi tạo
**When** seed data chạy
**Then** tồn tại 5 issue type mặc định: Bug, Story, Epic, Task, Sub-task với icon và màu tương ứng
**And** các type mặc định không thể xóa nhưng có thể ẩn ở cấp project

**Given** admin muốn tạo issue type tùy chỉnh
**When** gọi `POST /api/issue-types` với `name`, `icon`, `color`, `projectId` (nếu scoped)
**Then** trả `201` với type vừa tạo
**And** type mới có thể gán cho các project

**Given** issue type đang được dùng bởi ít nhất 1 issue
**When** admin xóa type đó
**Then** trả `409 ProblemDetails` với message rõ ràng

### Story 8.2: Issue Type CRUD UI (admin configures issue types per project)

**FRs covered:** FR9

As a project admin,
I want cấu hình issue types được phép trong project của mình,
So that team chỉ thấy những loại issue phù hợp, tránh nhầm lẫn.

**Acceptance Criteria:**

**Given** user có role Admin trong project
**When** vào trang Project Settings > Issue Types
**Then** thấy danh sách issue types đang active cho project, có thể bật/tắt từng type

**Given** admin bật/tắt issue type trong project
**When** lưu cấu hình
**Then** thay đổi có hiệu lực ngay; form tạo issue chỉ hiện types đang bật

### Story 8.3: Parent-child issue linking (Epic → Story → Sub-task)

**FRs covered:** FR9, FR10

As a PM,
I want liên kết issues theo quan hệ cha-con (Epic → Story → Sub-task),
So that tôi có thể tổ chức công việc theo hierarchy và theo dõi tiến độ từ Epic xuống Sub-task.

**Acceptance Criteria:**

**Given** user tạo hoặc chỉnh sửa một issue
**When** chọn parent issue
**Then** chỉ cho phép chọn parent hợp lệ theo rule: Sub-task cha phải là Story/Task, Story cha phải là Epic, Epic không có cha
**And** validate không tạo vòng lặp (cycle detection)

**Given** Epic có nhiều Story con
**When** xem detail của Epic
**Then** hiển thị danh sách Story con với progress (% hoàn thành, story points done/total)

**Given** user xóa parent của một issue (detach)
**When** lưu thay đổi
**Then** issue trở thành root-level issue, các child của nó vẫn giữ nguyên quan hệ

### Story 8.4: Project Settings Hub (central config navigation)

**FRs covered:** FR-66

As a project admin,
I want truy cập tất cả cấu hình của project qua một trang Settings trung tâm,
So that tôi không phải nhớ từng URL riêng lẻ để cấu hình issue types, workflow, fields, permissions hay notifications.

**Acceptance Criteria:**

**Given** user có role Admin trong project
**When** vào `/projects/{id}/settings`
**Then** hiển thị sidebar navigation với các mục: General, Members, Issue Types, Workflows, Custom Fields, Board, Priorities, Notifications, Permissions, Automation — mỗi mục link tới trang config tương ứng

**Given** user không có role Admin
**When** truy cập `/projects/{id}/settings`
**Then** trả `403` và UI hiển thị trang "Bạn không có quyền cấu hình project này"

**Given** user đang ở một sub-page của Settings (ví dụ Issue Types)
**When** nhìn sidebar
**Then** mục hiện tại được highlight; có breadcrumb `Project Name > Settings > Issue Types`

**And** mỗi settings page load độc lập (lazy route) — không load toàn bộ config khi vào Settings Hub

### Story 8.5: Project General Settings (name, key, lead, type, avatar, archive)

**FRs covered:** FR-66, FR-9

As a project admin,
I want chỉnh sửa thông tin chung của project và archive khi project kết thúc,
So that project metadata luôn chính xác và project cũ không làm rối danh sách.

**Acceptance Criteria:**

**Given** admin vào Project Settings > General
**When** xem trang
**Then** có thể chỉnh sửa: `name` (max 100 chars), `description`, `projectLead` (user picker), `projectType` (Scrum / Kanban / Business), `avatar` (upload ảnh hoặc chọn color + icon), `startDate`, `targetDate`

**Given** admin thay đổi `projectType` từ Scrum sang Kanban
**When** lưu
**Then** hệ thống cảnh báo "Chuyển type sẽ ẩn Sprint features. Active sprints sẽ bị đóng. Xác nhận?" trước khi apply

**Given** admin click "Archive Project"
**When** xác nhận
**Then** project chuyển sang `status = Archived`; không xuất hiện trong danh sách "My Projects" mặc định; vẫn truy cập được qua filter "Archived"; mọi data được giữ nguyên

**Given** admin click "Delete Project" (khác Archive)
**When** xác nhận bằng cách gõ project key
**Then** project và toàn bộ issues/data bị soft-delete (có thể restore trong 30 ngày); sau 30 ngày hard-delete tự động

**And** mọi thay đổi General Settings ghi audit log đầy đủ

### Story 8.6: Workflow Scheme (reusable mapping: issue type → workflow per project)

**FRs covered:** FR-96, FR-104

As a project admin,
I want cấu hình issue type nào dùng workflow nào trong project của mình,
So that Bug dùng workflow có Review step, còn Task dùng workflow đơn giản hơn trong cùng một project.

**Acceptance Criteria:**

**Given** project có nhiều issue types và nhiều workflow đã define (Epic 11)
**When** admin vào Project Settings > Workflows
**Then** hiển thị bảng: Issue Type (rows) × Workflow (assignable) — admin chọn workflow cho từng issue type

**Given** admin gán workflow X cho issue type "Bug"
**When** lưu
**Then** mọi Bug issue mới trong project áp dụng workflow X ngay lập tức
**And** Bug issues đang tồn tại: hiển thị warning "N issues đang dùng workflow cũ — migrate state?" với 2 lựa chọn: giữ state hiện tại (nếu state tồn tại trong workflow mới) hoặc reset về initial state

**Given** một workflow được dùng bởi ít nhất 1 issue type trong project
**When** admin xóa workflow đó (Epic 11)
**Then** trả `409` — không cho xóa; phải re-assign issue types sang workflow khác trước

### Story 8.7: Reporter field on Issue (auto-set on create, visible on detail)

**FRs covered:** FR-184, FR-185

As a PM,
I want biết ai đã tạo issue (reporter) tách biệt với assignee,
So that tôi có thể filter "issues tôi tạo" và hệ thống notify đúng người khi có thay đổi.

**Acceptance Criteria:**

**Given** user tạo issue mới
**When** issue được lưu
**Then** `reporter_user_id` tự động set bằng `currentUser.id` — không yêu cầu user chọn
**And** reporter không thể bị null (NOT NULL với default = creator)

**Given** issue detail hiển thị
**When** user mở issue
**Then** metadata panel hiển thị reporter avatar + name bên cạnh assignee
**And** reporter là read-only trừ project Admin (Admin có thể reassign reporter)

**Given** user dùng Advanced Filter Builder (Story 12.2)
**When** thêm condition `Reporter = [user]`
**Then** filter hoạt động đúng, trả issues có `reporter_user_id` khớp

**Given** reporter notification scheme bật (Story 15.6)
**When** issue của reporter bị thay đổi status hoặc có comment mới
**Then** reporter nhận notification ngoài assignee và watchers

**Technical notes:**
- `reporter_user_id` đã có trong V008_001 migration script (story-8-0-technical-spec.md)
- Backfill V008_002: set `reporter_user_id = created_by_user_id` cho issues hiện tại
- API response DTO thêm `reporter: { id, displayName, avatarUrl }` object
- Filter: extend `IssueFilterQuery` với `reporterId?: Guid`

### Story 8.8: Project Templates (Scrum / Kanban / Business pre-configuration)

**FRs covered:** FR-186, FR-187, FR-188

As a PM,
I want chọn template khi tạo project mới để hệ thống tự cấu hình sẵn,
So that project mới sẵn sàng dùng ngay mà không phải setup thủ công từng bước.

**Acceptance Criteria:**

**Given** user tạo project mới (Story 1.3)
**When** điền thông tin project
**Then** bước chọn template xuất hiện với 3 lựa chọn:
- **Scrum** — board type Scrum, issue types: Epic/Story/Task/Bug/Sub-task, workflow: Open→In Progress→In Review→Done, 2-week sprint default
- **Kanban** — board type Kanban, issue types: Task/Bug/Sub-task, workflow: Backlog→In Progress→Done, WIP limit 3 mặc định
- **Business (General)** — board type Kanban, issue types: Task/Milestone/Phase, workflow: To Do→In Progress→Done, không có sprint

**Given** user chọn template Scrum và confirm tạo project
**When** project được tạo
**Then** project có: 5 issue types enable (theo template), 1 workflow assign cho mỗi issue type, 1 board configure với columns đúng template, notification scheme = Default Scheme
**And** toàn bộ config này có thể override sau trong Project Settings

**Given** project đã tạo xong
**When** admin muốn đổi template
**Then** không có tính năng "đổi template" — template chỉ áp dụng lúc tạo; thay đổi thủ công qua Settings sau khi tạo

**Given** user muốn tạo project blank (không dùng template)
**When** chọn option "Blank Project"
**Then** project tạo với minimum config: 5 issue types mặc định (hệ thống), 1 workflow mặc định (Open→Done), board empty

**Technical notes:**
- `ProjectTemplateDefinition` là static config (không lưu DB), seed hardcode 3 templates + 1 blank
- `CreateProjectCommand` nhận `templateId?: string`; handler gọi `ProjectTemplateService.ApplyTemplate(project, template)` sau khi project tạo xong
- Apply template = batch gọi các commands: `EnableIssueType`, `AssignWorkflow`, `ConfigureBoard`, `SetNotificationScheme`
- Template application là transactional — nếu fail ở bước nào thì rollback toàn bộ project

### Story 8.9: Epic color coding (consistent color across Board, Roadmap, Backlog)

**FRs covered:** FR-199

As a PM,
I want mỗi Epic có màu riêng và màu đó hiển thị nhất quán trên Board/Roadmap/Backlog,
So that tôi nhận biết issues thuộc Epic nào chỉ bằng màu sắc mà không cần đọc tên.

**Acceptance Criteria:**

**Given** user tạo hoặc chỉnh sửa một Epic
**When** mở issue detail của Epic
**Then** có color picker (16 preset colors + custom hex) trong metadata panel; màu lưu vào `epic_color` field

**Given** Epic đã có màu và Board đang bật swimlane by Epic
**When** render board
**Then** swimlane header của Epic đó hiển thị đúng màu đã chọn; card thuộc Epic có colored dot/badge bên trái

**Given** Roadmap view (Story 13.5)
**When** render timeline
**Then** Epic bar dùng màu đã cấu hình; legend hiển thị tên + màu của từng Epic

**Given** Backlog view với grouping by Epic
**When** render
**Then** Epic group header có colored indicator nhất quán với Board và Roadmap

**Given** Epic chưa được assign màu
**When** hệ thống cần hiển thị màu
**Then** fallback về màu default theo thứ tự (#7C3AED, #2563EB, #059669, #DC2626...) — tương tự Jira auto-color

**Technical notes:**
- `epic_color VARCHAR(7) NULL` trong `issues` table — thêm trong V008_004 hoặc separate migration V008_006
- Chỉ meaningful cho `discriminator = 'Epic'`; backend validate
- Frontend: `EpicColorService` — singleton map `epicId → color`, shared bởi Board/Roadmap/Backlog components

---

## Epic 9: Agile Board (Scrum + Kanban)

Cung cấp Board view tương tác (Kanban và Scrum) với drag-drop, Sprint CRUD, Backlog management và Sprint Planning UI tích hợp capacity từ Epic 5. Đây là trung tâm vận hành hàng ngày của development team.

**FRs covered:** FR9, FR10, FR11, FR28 (capacity integration)

### Story 9.1: Board view (Kanban columns, card display, drag-drop status change)

**FRs covered:** FR9, FR10, FR11

As a team member,
I want xem và cập nhật trạng thái issue bằng cách kéo thả card trên Board view,
So that tôi và team có thể theo dõi tiến độ sprint một cách trực quan mà không cần mở từng issue.

**Acceptance Criteria:**

**Given** project có board cấu hình (columns mapping to workflow statuses)
**When** user mở Board view
**Then** hiển thị các cột tương ứng với workflow statuses của project, mỗi cột hiển thị số lượng và tổng story points

**Given** board đã render với issues
**When** user kéo một card từ cột A sang cột B
**Then** issue được cập nhật status theo transition hợp lệ (validate FSM từ Epic 11)
**And** nếu transition không hợp lệ thì card snap back và hiển thị toast lỗi
**And** optimistic update + rollback nếu API trả lỗi

**Given** board có nhiều issues
**When** user filter theo assignee, label, issue type
**Then** board chỉ hiển thị cards khớp filter, các cột giữ nguyên header

**Given** WIP limit được cấu hình cho một cột (Story 9.5)
**When** user kéo card vào cột đã đạt WIP limit
**Then** hiển thị cảnh báo rõ ràng; cho phép override với confirmation

### Story 9.2: Sprint CRUD (create/start/complete/cancel sprint)

**FRs covered:** FR9

As a PM,
I want tạo và quản lý vòng đời sprint (create/start/complete/cancel),
So that team có khung thời gian làm việc rõ ràng và tôi có thể tổng kết sprint đúng quy trình.

**Acceptance Criteria:**

**Given** project có board type = Scrum
**When** PM tạo sprint với `name`, `goal`, `startDate`, `endDate`
**Then** sprint được tạo ở trạng thái `planned`; chỉ 1 sprint ở trạng thái `active` tại một thời điểm

**Given** sprint ở trạng thái `planned`
**When** PM bấm "Start Sprint"
**Then** sprint chuyển sang `active`, `startDate` được set (hoặc xác nhận nếu đã set)
**And** nếu project đã có sprint `active` khác thì trả `409 ProblemDetails`

**Given** sprint ở trạng thái `active`
**When** PM bấm "Complete Sprint"
**Then** hiển thị modal tổng kết: số issue completed/incomplete
**And** cho phép chọn move incomplete issues về backlog hoặc sang sprint khác
**And** sprint chuyển sang `completed` sau khi confirm

**Given** sprint bị cancel
**When** PM bấm "Cancel Sprint"
**Then** tất cả issues trong sprint move về backlog, sprint chuyển sang `cancelled`

### Story 9.3: Backlog management (product backlog + sprint backlog, drag-drop priority)

**FRs covered:** FR9, FR10

As a PM,
I want quản lý backlog và kéo thả issue để sắp xếp thứ tự ưu tiên,
So that tôi luôn có product backlog được ưu tiên sẵn sàng cho sprint planning.

**Acceptance Criteria:**

**Given** project có danh sách issues chưa assigned vào sprint
**When** mở Backlog view
**Then** hiển thị product backlog (chưa vào sprint) và sprint backlog (theo sprint) trong cùng giao diện

**Given** user kéo thả issue trong backlog
**When** thả vào vị trí mới
**Then** thứ tự ưu tiên (`rank`) được cập nhật ngay; sử dụng LexoRank hoặc fractional indexing để tránh re-rank toàn bộ

### Story 9.4: Sprint planning UI (backlog left + sprint board right + capacity mini-chart)

**FRs covered:** FR9, FR28 (integration)

As a PM,
I want giao diện Sprint Planning với backlog bên trái, sprint board bên phải và mini-chart capacity tích hợp từ Epic 5,
So that tôi lên kế hoạch sprint có căn cứ capacity thực tế, không over-commit.

**Acceptance Criteria:**

**Given** PM mở Sprint Planning view
**When** trang render
**Then** hiển thị split layout: backlog (trái) + sprint backlog (phải) + capacity mini-chart (header/sidebar)

**Given** PM kéo issue từ backlog vào sprint
**When** thêm issue
**Then** story points tổng cộng cập nhật real-time và capacity mini-chart cập nhật tương ứng (tích hợp data từ Epic 5)

**Given** tổng story points vượt capacity dự kiến
**When** PM vẫn thêm issue vào sprint
**Then** hiển thị cảnh báo over-capacity (không chặn nhưng warn rõ)

### Story 9.5: WIP limits + column constraints

**FRs covered:** FR9

As a project admin,
I want đặt WIP limit cho từng cột trên board,
So that team duy trì flow ổn định và phát hiện bottleneck ngay khi cột bị tắc nghẽn.

**Acceptance Criteria:**

**Given** admin cấu hình WIP limit cho cột
**When** số card trong cột đạt hoặc vượt limit
**Then** cột được highlight màu cảnh báo và hiển thị "X/limit" badge

**Given** user kéo card vào cột đã đạt WIP limit
**When** thả card
**Then** hiển thị dialog xác nhận "Override WIP limit?" trước khi lưu

### Story 9.6: Scrum/Kanban configuration per project

**FRs covered:** FR9

As a project admin,
I want chọn board type (Scrum hoặc Kanban) và cấu hình columns cho project,
So that mỗi project có workflow board phù hợp với cách làm việc của team.

**Acceptance Criteria:**

**Given** project chưa có board cấu hình
**When** admin chọn board type = Scrum hoặc Kanban
**Then** tạo board với cấu hình mặc định tương ứng (Scrum: columns To Do/In Progress/Done; Kanban: To Do/In Progress/Review/Done)

**Given** admin chỉnh sửa columns (thêm/xóa/đổi tên/reorder)
**When** lưu cấu hình
**Then** board hiển thị đúng columns mới; issues mapping đến column cũ được giữ nguyên status

### Story 9.7: Board swimlanes (by Assignee / Epic / Label)

**FRs covered:** FR9, FR11

As a team member,
I want xem board với swimlanes ngang theo Assignee, Epic hoặc Label,
So that tôi nhóm được work theo context và thấy ngay ai đang làm gì hoặc epic nào đang tắc.

**Acceptance Criteria:**

**Given** user chọn swimlane mode (Assignee / Epic / Label) từ board toolbar
**When** board render
**Then** hiển thị các swimlane ngang, mỗi swimlane có header với tên nhóm, số card và tổng story points; mặc định không có swimlane (flat board)

**Given** swimlane đang mở (expanded)
**When** user click collapse toggle trên swimlane header
**Then** cards trong swimlane đó bị ẩn; click lại expand trở lại; trạng thái collapse/expand được giữ trong session

**Given** có issues không có assignee, không thuộc epic, hoặc không có label (tùy mode)
**When** swimlane render
**Then** nhóm "Unassigned" / "No Epic" / "No Label" xuất hiện ở cuối board và chứa những cards không thuộc nhóm nào

### Story 9.8: Board quick filters (Only My Issues / Recently Updated / Label filters)

**FRs covered:** FR9

As a team member,
I want lọc nhanh board bằng các nút "Only My Issues", "Recently Updated (24h)" và label filter chips,
So that tôi tập trung vào phần việc liên quan mà không cần mở filter panel phức tạp.

**Acceptance Criteria:**

**Given** board header hiển thị quick filter buttons
**When** user click "Only My Issues"
**Then** board ẩn tất cả cards không được assign cho current user ngay lập tức (client-side, không reload); nút được highlight để chỉ trạng thái active

**Given** một hoặc nhiều quick filter đang active
**When** user kích hoạt thêm filter khác
**Then** các filter áp dụng đồng thời (AND logic); nút "Clear all filters" xuất hiện trong header

**Given** quick filters đang active
**When** user click "Clear all filters"
**Then** tất cả filters bị xóa và board hiển thị toàn bộ cards

### Story 9.9: Sprint goal field + sprint meta

**FRs covered:** FR9

As a PM,
I want đặt sprint goal và xem thông tin meta của sprint (start/end date, days remaining) nổi bật trên Sprint Board,
So that cả team luôn biết mục tiêu sprint và tiến độ thời gian còn lại.

**Acceptance Criteria:**

**Given** PM tạo hoặc chỉnh sửa sprint
**When** điền vào field "Sprint Goal" (tối đa 500 ký tự)
**Then** sprint goal được lưu và hiển thị nổi bật ở đầu Sprint Board (dưới sprint name)

**Given** sprint đang active
**When** user xem Sprint Board
**Then** header hiển thị: sprint name, goal, start date, end date, và countdown "X days remaining" tính từ ngày hiện tại

**Given** PM click "Complete Sprint"
**When** sprint có issues chưa hoàn thành
**Then** hệ thống hiển thị modal prompt cho phép chọn: move incomplete issues sang sprint tiếp theo hoặc về backlog trước khi xác nhận complete

---

## Epic 10: Issue Collaboration (Comments, Attachments, Mentions, Watchers)

Cho phép team cộng tác trực tiếp trên issue: comment thread, đính kèm file, @mention, theo dõi thay đổi (watchers) và liên kết issues. Mọi hoạt động đều được ghi vào activity log trên issue.

**FRs covered:** FR8 (audit), FR53 (notification trigger)

### Story 10.1: Comment CRUD (threaded comments on issues)

**FRs covered:** FR8

As a team member,
I want thêm, sửa, xóa bình luận trên issue (hỗ trợ thread reply),
So that team có thể thảo luận ngay trên issue thay vì qua email/chat riêng lẻ.

**Acceptance Criteria:**

**Given** user là member của project chứa issue
**When** user gửi comment với nội dung Markdown
**Then** comment được lưu với `authorId`, `createdAt`, và nội dung render Markdown đúng cú pháp

**Given** user muốn reply một comment cụ thể
**When** click "Reply" và gửi nội dung
**Then** reply được gắn `parentCommentId`, hiển thị thụt lề dưới comment gốc (1 level thread)

**Given** user là tác giả của comment
**When** sửa comment
**Then** comment được cập nhật và hiển thị badge "(edited)" kèm timestamp sửa cuối
**And** audit log ghi nhận sự thay đổi nội dung

**Given** user là tác giả của comment hoặc project admin
**When** xóa comment
**Then** comment bị xóa mềm (soft delete); hiển thị placeholder "[Comment đã bị xóa]" thay vì xóa khỏi thread

### Story 10.2: @mentions (parse @username in comments, trigger notification)

**FRs covered:** FR53

As a team member,
I want gõ @username trong comment để tag người cụ thể,
So that người được tag nhận thông báo và phản hồi kịp thời.

**Acceptance Criteria:**

**Given** user đang gõ comment và nhập "@"
**When** nhập thêm ký tự
**Then** hiển thị dropdown autocomplete danh sách members của project khớp với text đang gõ

**Given** comment có @mention được lưu
**When** hệ thống xử lý
**Then** tạo notification cho user được mention (in-app + email digest nếu đã cấu hình)
**And** @mention trong comment render thành link profile (highlight màu)

### Story 10.3: File attachments (upload to S3, preview, download, delete)

**FRs covered:** FR9

As a team member,
I want đính kèm file (hình ảnh, document) vào issue,
So that tôi cung cấp context trực quan (screenshot bug, mockup) ngay trên issue.

**Acceptance Criteria:**

**Given** user upload file lên issue
**When** file được xử lý
**Then** file lưu vào S3-compatible storage (MinIO) với path cách ly theo project/issue
**And** response trả `attachmentId`, `fileName`, `fileSize`, `mimeType`, `url` (presigned)

**Given** file là ảnh (image/*)
**When** xem issue detail
**Then** hiển thị thumbnail preview inline, click để xem full size

**Given** user xóa attachment
**When** xác nhận xóa
**Then** file bị xóa khỏi storage và activity log ghi nhận "removed attachment: {fileName}"

**Given** file upload vượt quá giới hạn kích thước (cấu hình, mặc định 25MB)
**When** user chọn file
**Then** hiển thị lỗi validation ngay lập tức (không gọi API) với thông báo rõ ràng

### Story 10.4: Watchers (subscribe/unsubscribe, notify on change)

**FRs covered:** FR53

As a team member,
I want theo dõi (watch) issue để nhận thông báo khi có thay đổi,
So that tôi không bỏ lỡ cập nhật quan trọng mà không cần liên tục refresh trang.

**Acceptance Criteria:**

**Given** user click "Watch" trên issue
**When** lưu subscription
**Then** user được thêm vào danh sách watchers của issue
**And** khi issue có thay đổi (status, assignee, comment mới), watcher nhận notification

**Given** user muốn dừng theo dõi
**When** click "Unwatch"
**Then** user bị xóa khỏi watchers, không nhận notification tiếp theo

### Story 10.5: Issue links (blocks/blocked-by, relates-to, duplicates, clones)

**FRs covered:** FR9

As a PM,
I want liên kết issues với nhau theo loại quan hệ (blocks, relates-to, duplicates),
So that tôi theo dõi dependency và tránh làm việc trùng lặp.

**Acceptance Criteria:**

**Given** user tạo link từ Issue A sang Issue B với loại "blocks"
**When** lưu link
**Then** Issue A hiển thị "blocks Issue B" và Issue B hiển thị "is blocked by Issue A" (bidirectional)
**And** hỗ trợ loại: blocks/blocked-by, relates-to, duplicates/is-duplicated-by, clones/is-cloned-by

### Story 10.6: Activity log (all changes timeline on issue detail)

**FRs covered:** FR8

As a team member,
I want xem toàn bộ lịch sử thay đổi của issue theo timeline,
So that tôi hiểu issue đã trải qua những gì mà không cần hỏi lại team.

**Acceptance Criteria:**

**Given** issue có bất kỳ thay đổi nào (status, assignee, field, comment, attachment)
**When** xem tab Activity
**Then** hiển thị timeline theo thứ tự thời gian: actor, loại thay đổi, giá trị trước/sau, timestamp
**And** comment và activity log hiển thị xen kẽ trong cùng timeline stream

### Story 10.7: Multiple Assignees (optional team-managed mode per project)

**FRs covered:** FR-197

As a project admin,
I want bật chế độ multi-assignee cho project,
So that team có thể assign nhiều người cùng chịu trách nhiệm một issue thay vì chỉ 1 người.

**Acceptance Criteria:**

**Given** admin vào Project Settings > General
**When** bật toggle "Allow multiple assignees"
**Then** project chuyển sang multi-assignee mode; tất cả issues trong project hiển thị multi-user picker thay vì single picker

**Given** project ở multi-assignee mode và user assign issue
**When** chọn assignees
**Then** có thể chọn tối đa 10 assignees; UI hiển thị avatar stack (max 5 hiển thị + "+N more")
**And** overload detection tính cho TẤT CẢ assignees (mỗi người được cộng full effort của issue)

**Given** project ở multi-assignee mode
**When** notification scheme trigger event "Issue Assigned"
**Then** TẤT CẢ assignees nhận notification, không chỉ 1 người

**Given** project switch từ multi → single assignee mode
**When** admin xác nhận
**Then** hệ thống cảnh báo "N issues có nhiều assignees — primary assignee (người đầu tiên) sẽ được giữ lại"
**And** sau khi confirm, giữ lại `assignees[0]` làm `assignee_id` duy nhất

**Technical notes:**
- Schema: thêm `issue_assignees` junction table `(issue_id, user_id, assigned_at, assigned_by)` thay vì thay đổi `assignee_user_id`
- Backward compat: `assignee_user_id` giữ nguyên = `assignees[0]` (primary); junction table là extension
- `project_settings.allow_multiple_assignees BOOLEAN DEFAULT false`
- Overload engine: `SUM(effort_hours)` per resource kể cả khi là secondary assignee

### Story 10.8: Issue starring (per-user favourites with quick access list)

**FRs covered:** FR-198

As a PM,
I want đánh dấu sao các issues quan trọng để truy cập nhanh,
So that tôi không mất thời gian tìm lại issues đang theo dõi thường xuyên.

**Acceptance Criteria:**

**Given** user mở bất kỳ issue nào
**When** click icon ⭐ trên issue header (cạnh title)
**Then** issue được thêm vào starred list của user; icon chuyển sang filled star (màu vàng)
**And** action này instant (optimistic update) — không reload page

**Given** user đã star issue
**When** click star lần nữa
**Then** issue bị unstar; icon trở về empty star

**Given** user vào "My Work" section (sidebar)
**When** chọn tab "Starred"
**Then** hiển thị danh sách tất cả starred issues (sorted by starred_at DESC) với issue key, title, project, status, assignee

**Given** user đã star 100 issues (limit)
**When** cố gắng star issue thứ 101
**Then** toast warning "Đã đạt giới hạn 100 starred issues. Bỏ star một issue trước khi thêm mới."

**Technical notes:**
- `user_starred_issues (user_id, issue_id, starred_at)` — composite PK
- Index: `(user_id, starred_at DESC)` cho starred list query
- DELETE khi unstar (không soft-delete)
- API: `POST /api/v1/issues/{id}/star`, `DELETE /api/v1/issues/{id}/star`, `GET /api/v1/me/starred-issues`
- Frontend: `StarredIssuesStore` (NgRx slice) — preload starred list on app init, check `state.starredIds.has(issueId)` để render icon

---

## Epic 11: Configurable Workflow Engine

Cho phép admin cấu hình workflow riêng cho từng project: states, transitions, permissions và post-functions. Engine lưu FSM dạng JSON trong PostgreSQL và validate mọi transition theo rule đã định nghĩa.

**FRs covered:** FR9, FR19 (permission per transition)

### Story 11.1: Workflow definition CRUD (states + transitions + permissions per project)

**FRs covered:** FR9, FR19

As a project admin,
I want tạo và quản lý workflow riêng cho project (states + transitions + ai được phép transition),
So that process làm việc của team được enforce tự động thay vì dựa vào quy ước miệng.

**Acceptance Criteria:**

**Given** admin tạo workflow mới cho project
**When** định nghĩa states và transitions
**Then** mỗi state có `name`, `color`, `category` (todo/in-progress/done)
**And** mỗi transition có `fromState`, `toState`, `name` và `allowedRoles[]`

**Given** workflow đã được lưu
**When** admin thêm/xóa state hoặc transition
**Then** validate: phải có ít nhất 1 initial state (no incoming transitions) và 1 final state (no outgoing)
**And** thay đổi có hiệu lực với issues mới; issues đang ở state bị xóa cần migration rule

**Given** admin xóa một workflow đang được project sử dụng
**When** có issues đang active với workflow đó
**Then** trả `409 ProblemDetails`; phải migrate issues trước khi xóa workflow

**Given** workflow definition được lưu dưới dạng JSON trong PostgreSQL
**When** hệ thống load FSM
**Then** parse JSON và validate schema đúng (không có orphan transitions, không có duplicate state names)

### Story 11.2: Workflow transition validation (conditions, validators, post-functions)

**FRs covered:** FR9

As a system,
I want validate mọi transition request theo rules đã cấu hình (conditions + validators + post-functions),
So that issues chỉ thay đổi trạng thái khi đúng điều kiện và các side-effects được thực thi tự động.

**Acceptance Criteria:**

**Given** issue cần transition sang state mới
**When** hệ thống xử lý request
**Then** kiểm tra theo thứ tự: (1) transition tồn tại từ state hiện tại, (2) user có role được phép, (3) conditions đều thỏa (ví dụ: "sub-tasks phải xong trước khi close")
**And** nếu bất kỳ check nào fail thì trả `422 ProblemDetails` với message giải thích

**Given** transition thành công và có post-functions
**When** transition được commit
**Then** post-functions được thực thi đồng bộ trong cùng transaction (ví dụ: auto-assign, set field, send notification trigger)
**And** nếu post-function fail thì rollback transition và trả lỗi

### Story 11.3: Status transition UI (button per valid transition on issue detail)

**FRs covered:** FR9

As a team member,
I want thấy các nút transition hợp lệ ngay trên issue detail thay vì dropdown status tự do,
So that tôi chỉ thực hiện các bước chuyển trạng thái đúng quy trình, không nhầm.

**Acceptance Criteria:**

**Given** issue đang ở state X
**When** user mở issue detail
**Then** hiển thị các nút transition hợp lệ từ state X (theo workflow + role của user)
**And** các transitions không hợp lệ hoặc user không có quyền thì không hiện (hidden, không disabled)

**Given** user click nút transition có required fields (ví dụ: "Resolve" cần fill "Resolution")
**When** click
**Then** hiện dialog/form inline yêu cầu nhập trường bắt buộc trước khi confirm

### Story 11.4: Workflow templates (Default, Scrum, Bug Tracking)

**FRs covered:** FR9

As a project admin,
I want chọn workflow từ template có sẵn khi tạo project mới,
So that tôi không phải cấu hình workflow từ đầu mà vẫn có quy trình chuẩn.

**Acceptance Criteria:**

**Given** admin tạo project mới hoặc cấu hình workflow
**When** chọn "Dùng template"
**Then** hiển thị 3 template: Default (To Do/In Progress/Done), Scrum (Backlog/In Sprint/In Progress/Review/Done), Bug Tracking (Open/In Progress/Fixed/Verified/Closed)

**Given** admin chọn template
**When** apply
**Then** workflow được tạo với đầy đủ states, transitions và permissions mặc định; admin có thể chỉnh sửa tiếp

---

## Epic 12: Search, Filters & Bulk Operations

Cung cấp full-text search (PostgreSQL tsvector), advanced filter builder, saved filters và bulk operations. Cho phép PM tìm và xử lý nhóm issues hiệu quả thay vì thao tác từng cái một.

**FRs covered:** FR9, FR10

### Story 12.1: Basic search (full-text search on title/description using PostgreSQL tsvector)

**FRs covered:** FR9, FR10

As a team member,
I want tìm kiếm issues bằng từ khóa (full-text search),
So that tôi tìm nhanh issue liên quan mà không cần nhớ mã số.

**Acceptance Criteria:**

**Given** user nhập từ khóa vào search box
**When** gọi `GET /api/projects/{id}/issues?q={keyword}`
**Then** backend query PostgreSQL bằng `tsvector` GIN index trên `title` và `description`
**And** kết quả trả về sorted theo relevance (ts_rank), có paging

**Given** kết quả tìm kiếm
**When** render list
**Then** highlight từ khóa khớp trong title/description của mỗi result

**Given** search trong project scope
**When** user không phải member của project
**Then** trả `404` (không leak kết quả)

**Given** database có >1000 issues
**When** search với từ khóa phổ biến
**Then** response time < 500ms (nhờ GIN index)

### Story 12.2: Advanced filter builder (UI: field + operator + value combinator)

**FRs covered:** FR9, FR10

As a PM,
I want xây dựng filter phức tạp bằng giao diện kéo-thả (field + operator + value, kết hợp AND/OR),
So that tôi khoanh vùng chính xác tập issues cần xem xét mà không cần viết query.

**Acceptance Criteria:**

**Given** user mở Filter Builder
**When** thêm một điều kiện
**Then** chọn được field (assignee, status, issue type, priority, label, component, fix version, custom fields), operator (=, !=, in, not in, is empty, is not empty, >, <) và value phù hợp với field type

**Given** user kết hợp nhiều điều kiện
**When** chọn combinator AND/OR
**Then** backend nhận filter spec dạng JSON và translate sang SQL WHERE clause đúng logic

**Given** filter builder có điều kiện
**When** user apply filter
**Then** issue list được filter real-time (debounce 300ms), hiển thị số lượng results

### Story 12.3: Saved filters (save, share, reuse filter queries)

**FRs covered:** FR9

As a PM,
I want lưu filter thường dùng để tái sử dụng nhanh,
So that tôi không phải rebuild filter mỗi lần mở app.

**Acceptance Criteria:**

**Given** user đã build một filter
**When** click "Save Filter" với tên
**Then** filter spec được lưu gắn với user (private) hoặc project (shared)

**Given** user mở danh sách Saved Filters
**When** click vào một filter
**Then** issue list được apply ngay với filter đó

**Given** user muốn chia sẻ filter với team
**When** set visibility = "Project" và lưu
**Then** mọi member của project thấy filter trong danh sách shared filters

### Story 12.4: Bulk operations (bulk assign, bulk status change, bulk label)

**FRs covered:** FR9

As a PM,
I want chọn nhiều issues cùng lúc và thực hiện thao tác hàng loạt,
So that tôi cập nhật hàng chục issues trong vài giây thay vì mở từng cái.

**Acceptance Criteria:**

**Given** user tick checkbox để chọn nhiều issues trong list view
**When** chọn từ 2 issues trở lên
**Then** hiện thanh bulk actions với các tùy chọn: Assign to, Change Status, Add Label, Change Priority, Delete

**Given** user chọn "Change Status" bulk
**When** chọn status mới và confirm
**Then** hệ thống validate từng issue theo workflow rule; issues không hợp lệ được báo cáo riêng (partial success với summary)
**And** audit log ghi nhận bulk operation với `bulkOperationId` để truy vết

**Given** user chọn "Delete" bulk
**When** confirm với dialog cảnh báo
**Then** soft-delete tất cả issues đã chọn (user có quyền); issues không có quyền xóa hiển thị trong error summary

---

## Epic 13: Agile Reports + Roadmap

Cung cấp bộ báo cáo Agile tiêu chuẩn (Burndown, Velocity, CFD, Sprint Report) và Roadmap view ở cấp Epic. Tất cả charts đều dựa trên dữ liệu thực tế từ sprints và issues, không phải ước tính thủ công.

**FRs covered:** FR7, FR16, FR17 (mở rộng Agile reporting)

### Story 13.1: Burndown chart (sprint burndown — story points/hours remaining by day)

**FRs covered:** FR7, FR16

As a PM,
I want xem burndown chart của sprint theo story points còn lại mỗi ngày,
So that tôi biết sprint đang đi đúng track hay cần điều chỉnh trước khi trễ.

**Acceptance Criteria:**

**Given** sprint đang active hoặc đã completed
**When** mở Burndown Chart
**Then** hiển thị 2 đường: (1) ideal burndown (đường thẳng từ tổng SP ngày đầu về 0 ngày cuối) và (2) actual burndown (SP remaining thực tế mỗi ngày)

**Given** scope thay đổi trong sprint (thêm/bớt issues)
**When** render chart
**Then** scope change được hiển thị bằng điểm ghi chú trên chart (ví dụ: vertical marker "Added 5 SP" hoặc "Removed 3 SP")

**Given** sprint chưa kết thúc
**When** xem chart
**Then** actual line chỉ vẽ đến ngày hiện tại; phần còn lại để trống (không project/forecast trừ khi explicit)

**Given** user hover vào một điểm trên chart
**When** tooltip hiển thị
**Then** thể hiện: ngày, SP remaining, số issues đã done/total, scope changes nếu có

### Story 13.2: Velocity chart (completed story points per sprint, last 10 sprints)

**FRs covered:** FR7

As a PM,
I want xem velocity chart của team qua các sprint gần nhất,
So that tôi dự đoán capacity cho sprint planning tiếp theo dựa trên track record thực tế.

**Acceptance Criteria:**

**Given** project có ít nhất 2 sprint completed
**When** mở Velocity Chart
**Then** hiển thị bar chart, mỗi sprint 1 bar: SP committed (khi start sprint) và SP completed (khi end sprint)
**And** tính và hiển thị average velocity (đường ngang) trên chart

**Given** user hover vào bar của một sprint
**When** tooltip hiển thị
**Then** hiển thị sprint name, ngày start/end, SP committed, SP completed, số issues done/total

### Story 13.3: Cumulative Flow Diagram (issues by status over time)

**FRs covered:** FR7

As a PM,
I want xem CFD để phân tích flow của team,
So that tôi phát hiện bottleneck (area nở rộng bất thường) và cải thiện quy trình.

**Acceptance Criteria:**

**Given** project có issues với lịch sử status change
**When** mở CFD
**Then** hiển thị stacked area chart: trục X là thời gian, trục Y là số issues, mỗi band là một status
**And** legend hiển thị status names tương ứng với màu band

**Given** user chọn date range
**When** thay đổi range
**Then** chart cập nhật dữ liệu theo range mới (không reload page)

### Story 13.4: Sprint Report (completed/incomplete issues, scope change)

**FRs covered:** FR7, FR17

As a PM,
I want xem Sprint Report chi tiết sau khi hoàn thành sprint,
So that tôi có báo cáo chuẩn để share với stakeholders và làm retrospective.

**Acceptance Criteria:**

**Given** sprint đã completed
**When** mở Sprint Report
**Then** hiển thị: sprint goal, ngày start/end, SP committed, SP completed, SP added mid-sprint (scope changes), danh sách issues completed và incomplete

**Given** Sprint Report đã hiển thị
**When** user click "Export PDF"
**Then** trigger async PDF export job (tái dùng từ Epic 6) và cung cấp download link khi xong

### Story 13.5: Roadmap view (Epic-level timeline, date ranges, progress % inline)

**FRs covered:** FR2, FR7

As a PM,
I want xem Roadmap theo chiều ngang timeline ở cấp Epic,
So that tôi và stakeholders thấy big picture kế hoạch sản phẩm theo thời gian.

**Acceptance Criteria:**

**Given** project có ít nhất 1 Epic issue với startDate và dueDate
**When** mở Roadmap view
**Then** hiển thị horizontal timeline với mỗi Epic là một bar từ startDate đến dueDate
**And** trong bar hiển thị % progress (dựa trên story points done/total của stories con)

**Given** user kéo thả endpoint của Epic bar (resize)
**When** thả
**Then** cập nhật dueDate của Epic issue (optimistic update + API call với If-Match)

**Given** Roadmap có nhiều Epics chồng lấp thời gian
**When** render
**Then** auto-lane Epics để tránh overlap, scroll ngang cho phép xem timeline dài

### Story 13.6: Epic progress report (issues within epic: done/in-progress/to-do breakdown)

**FRs covered:** FR7, FR16

As a PM,
I want xem báo cáo tiến độ chi tiết của một Epic với breakdown số issue theo status category (To Do / In Progress / Done),
So that tôi và stakeholders thấy ngay Epic đang ở đâu trong vòng đời mà không cần đếm thủ công từng issue.

**Acceptance Criteria:**

**Given** user mở detail view của một Epic issue
**When** trang render
**Then** hiển thị progress breakdown gồm: số lượng issues theo status category (To Do / In Progress / Done) và visual progress bar thể hiện % issues ở trạng thái Done so với tổng số issues trong Epic

**Given** Epic có danh sách issues con
**When** xem Epic detail
**Then** hiển thị danh sách tất cả issues trong Epic với: tiêu đề, trạng thái hiện tại, assignee và story points của từng issue

**Given** Epic detail đang hiển thị
**When** user chọn filter theo sprint cụ thể
**Then** danh sách issues và breakdown chỉ hiển thị issues thuộc sprint đã chọn; "All Sprints" là lựa chọn mặc định

---

## Epic 14: Custom Fields + Labels/Components/Versions

Cho phép admin định nghĩa custom fields cho từng issue type, quản lý labels/tags tự do, components có owner và versions/releases. Story Points được xử lý như first-class field với tổng hợp trong sprint planning.

**FRs covered:** FR9, FR10

### Story 14.1: Custom field definitions (admin creates: text/number/date/select/multi-select per issue type)

**FRs covered:** FR9, FR10

As a project admin,
I want tạo custom fields cho từng issue type trong project,
So that team có thể track thông tin đặc thù nghiệp vụ (ví dụ: "Environment", "Customer Name") ngay trên issue.

**Acceptance Criteria:**

**Given** admin vào Project Settings > Custom Fields
**When** tạo custom field với `name`, `fieldType`, `issueTypes[]`, `required`, `defaultValue`
**Then** field được lưu và gắn với issue type tương ứng
**And** hỗ trợ fieldType: `text` (single-line), `textarea` (multi-line), `number`, `date`, `select` (single), `multi-select`, `url`, `checkbox`

**Given** custom field type = `select` hoặc `multi-select`
**When** admin định nghĩa options
**Then** phải có ít nhất 1 option; có thể thêm/xóa/reorder options sau khi tạo

**Given** custom field bị xóa bởi admin
**When** có issues đang lưu giá trị của field đó trong JSONB
**Then** soft-delete field (ẩn khỏi UI, giữ dữ liệu); không hard-delete để tránh mất data audit

### Story 14.2: Custom field rendering (show/edit custom fields on issue detail)

**FRs covered:** FR9

As a team member,
I want thấy và sửa custom fields trên issue detail theo đúng field type,
So that tôi nhập thông tin đặc thù mà không cần tool khác.

**Acceptance Criteria:**

**Given** issue thuộc issue type có custom fields
**When** mở issue detail
**Then** custom fields hiển thị trong section "Fields" với đúng UI component theo fieldType (input text, number spinner, date picker, single/multi select dropdown)

**Given** custom field có `required = true`
**When** user lưu issue mà không nhập field đó
**Then** validation error inline, không submit API

**Given** issue được lưu với giá trị custom fields
**When** query issues qua filter builder (Epic 12)
**Then** custom fields có thể dùng làm filter condition

### Story 14.3: Labels/Tags (free-form labels on issues, filter by label)

**FRs covered:** FR9

As a team member,
I want gắn labels tự do lên issues,
So that tôi phân loại issues theo tag linh hoạt mà không cần admin tạo trước.

**Acceptance Criteria:**

**Given** user nhập tên label mới trên issue
**When** lưu
**Then** label được tạo tự động (nếu chưa tồn tại trong project) và gắn vào issue
**And** label panel hiển thị màu tự động (hoặc user chọn màu)

**Given** có nhiều issues với cùng label
**When** click vào label tag
**Then** redirect đến issue list với filter `label = X` active

### Story 14.4: Components (project-level components, component owner)

**FRs covered:** FR9

As a project admin,
I want định nghĩa components của project (ví dụ: Frontend, Backend, Database) và gán owner,
So that tôi phân loại issues theo khu vực kỹ thuật và có người chịu trách nhiệm rõ ràng.

**Acceptance Criteria:**

**Given** admin tạo component với `name`, `description`, `defaultAssignee`
**When** issue được gán vào component đó
**Then** nếu issue chưa có assignee, tự động assign `defaultAssignee` của component (configurable: auto-assign on/off)

### Story 14.5: Versions/Releases (fix version, affects version, release notes)

**FRs covered:** FR9

As a PM,
I want tạo versions/releases và gắn issues vào "Fix Version" hoặc "Affects Version",
So that tôi track những gì sẽ/đã được release trong từng version.

**Acceptance Criteria:**

**Given** PM tạo version với `name`, `releaseDate`, `description`
**When** lưu
**Then** version có trạng thái: Unreleased / Released / Archived

**Given** PM đánh dấu version là Released
**When** confirm release
**Then** version chuyển sang Released; mọi incomplete issues trong version hiển thị warning

**Given** version đã Released
**When** xem Release Notes
**Then** tự động tổng hợp issues có `fixVersion = X` và status = Done theo loại (Bug Fixed, New Feature, Improvement)

### Story 14.6: Story Points (sp field on issues, sum in sprint planning)

**FRs covered:** FR9, FR28

As a PM,
I want nhập story points cho issues và xem tổng trong sprint planning,
So that tôi estimate và track velocity dựa trên đơn vị chuẩn.

**Acceptance Criteria:**

**Given** issue có trường `storyPoints` (integer, nullable)
**When** user nhập SP trên issue detail
**Then** giá trị được lưu và phản ánh ngay trong sprint backlog totals

**Given** sprint có nhiều issues với SP
**When** xem Sprint Planning (Story 9.4)
**Then** tổng SP committed hiển thị trong header và cập nhật real-time khi thêm/bớt issue

### Story 14.7: Priority configuration per project (custom priority levels + ordering)

**FRs covered:** FR-137

As a project admin,
I want cấu hình bộ mức priority riêng cho project và thứ tự hiển thị,
So that team dùng ngôn ngữ ưu tiên phù hợp với quy trình thực tế (không bị cứng với Highest/High/Medium/Low/Lowest mặc định).

**Acceptance Criteria:**

**Given** hệ thống seed 5 priority mặc định: `Highest`, `High`, `Medium`, `Low`, `Lowest` với màu và icon tương ứng
**When** admin vào Project Settings > Priorities
**Then** thấy danh sách priorities đang active cho project, có thể drag-drop reorder, bật/tắt từng priority

**Given** admin tạo priority tùy chỉnh (ví dụ "Critical" màu đen)
**When** lưu
**Then** priority mới xuất hiện trong dropdown tất cả issues của project và trong Board filter

**Given** priority đang được dùng bởi ít nhất 1 issue
**When** admin xóa priority đó
**Then** trả `409` với message "Reassign N issues trước khi xóa"; không xóa được cho tới khi issues được re-prioritize

**And** thứ tự priority ảnh hưởng tới sort order mặc định trong Backlog (priority cao nhất ở đầu)

### Story 14.8: Release/Deployment tracking on Versions

**FRs covered:** FR-200

As a PM,
I want ghi nhận khi nào và ở môi trường nào một Version được deploy,
So that tôi biết chính xác phiên bản nào đang chạy ở đâu và đối chiếu với issues đã fix.

**Acceptance Criteria:**

**Given** PM mở Version detail (Story 14.5)
**When** xem tab "Deployments"
**Then** thấy danh sách deployment records với: environment (Development/Staging/Production), deployed_at, deployed_by (user), notes (optional), deployment_status (Success/Failed/In Progress)

**Given** PM thêm deployment record mới
**When** điền: environment + deployed_at + status
**Then** record lưu và hiển thị trong timeline; nếu `deployment_status = Success` và environment = Production thì Version tự động được đánh dấu "Released" nếu chưa release

**Given** Version có ít nhất 1 deployment
**When** xem Versions list (Story 14.5 board)
**Then** mỗi Version hiển thị badge môi trường cuối cùng được deploy (Dev/Staging/Prod) với màu phân biệt

**Given** issue có `fixVersion` gán vào một Version
**When** Version đó được deploy lên Production
**Then** activity log của issue ghi nhận "Deployed to Production in Version X.Y.Z by [user]"

**Technical notes:**
- `version_deployments (id, version_id, environment ENUM, deployed_at, deployed_by_user_id, status ENUM, notes, created_at)` — append-only
- `environment`: `development | staging | production | other`
- `status`: `in_progress | success | failed | rolled_back`
- API: `POST /api/v1/projects/{pid}/versions/{vid}/deployments`, `GET /api/v1/projects/{pid}/versions/{vid}/deployments`
- Không có edit/delete deployment records — append-only để audit trail
- Frontend: deployment tab lazy-loaded trong Version detail drawer

---

## Epic 15: Automation, Webhooks & Permission Schemes

Cho phép tự động hóa quy trình qua Automation rules (If-Then builder), outbound webhooks và quản lý phân quyền chi tiết theo role/project. Giảm công việc thủ công và tích hợp với công cụ bên ngoài.

**FRs covered:** FR19, FR53 (auto-notifications)

### Story 15.1: Automation rules (If-Then: trigger → condition → action builder)

**FRs covered:** FR19, FR53

As a project admin,
I want tạo automation rules theo dạng "Khi X xảy ra, nếu Y thỏa thì thực hiện Z",
So that team tự động hóa các tác vụ lặp đi lặp lại mà không cần code.

**Acceptance Criteria:**

**Given** admin mở Automation Builder
**When** tạo rule mới
**Then** chọn được Trigger (issue created, issue status changed, comment added, sprint started/completed, scheduled), Conditions (field value equals, issue type is, assignee is, etc.) và Actions (assign issue, change status, add label, add comment, send notification, trigger webhook)

**Given** rule được tạo và enabled
**When** trigger event xảy ra
**Then** hệ thống evaluate conditions; nếu thỏa thì execute actions theo thứ tự
**And** ghi log mỗi rule execution (trigger, conditions evaluated, actions executed, outcome) với retention 30 ngày

**Given** rule execution có lỗi (action fail)
**When** log lỗi
**Then** rule không retry vô hạn (max 3 retry với exponential backoff); sau đó đánh dấu `failed` và ghi lý do

**Given** admin xem Automation History
**When** mở log
**Then** thấy danh sách executions: trigger time, rule name, status (success/failed/skipped), action taken

### Story 15.2: Built-in automation templates (auto-assign, auto-status, due date reminder)

**FRs covered:** FR19, FR53

As a project admin,
I want chọn automation template có sẵn thay vì build từ đầu,
So that tôi setup automation nhanh chóng cho các use case phổ biến.

**Acceptance Criteria:**

**Given** admin mở Automation Templates gallery
**When** xem danh sách
**Then** có ít nhất 3 template: (1) Auto-assign to component owner, (2) Auto-transition to "In Progress" when sub-task starts, (3) Send reminder 2 days before due date

**Given** admin chọn template
**When** click "Use Template"
**Then** rule được pre-fill với config của template; admin có thể customize trước khi save

### Story 15.3: Outbound webhooks (POST to URL on issue events)

**FRs covered:** FR19

As a project admin,
I want cấu hình webhook để gửi POST request đến URL bên ngoài khi có sự kiện trên issue,
So that tôi tích hợp project management tool với hệ thống nội bộ (CI/CD, Slack, monitoring).

**Acceptance Criteria:**

**Given** admin tạo webhook với `url`, `secret`, `events[]` (issue_created, issue_updated, status_changed, comment_added, sprint_started, sprint_completed)
**When** lưu webhook
**Then** system gửi test payload đến URL và hiển thị kết quả (200 OK hoặc lỗi)

**Given** event xảy ra và webhook đang active
**When** gửi POST request
**Then** payload gồm `eventType`, `issue` snapshot, `actor`, `timestamp`; header có `X-Webhook-Signature` (HMAC SHA256 với secret)

**Given** webhook endpoint trả lỗi hoặc timeout
**When** retry logic kích hoạt
**Then** retry tối đa 3 lần với exponential backoff (1s, 5s, 25s); sau đó đánh dấu delivery `failed`
**And** admin xem được delivery history: status, response code, response body (truncated), timestamp

### Story 15.4: Permission schemes (role definitions per project: Admin/Developer/Reporter/Viewer)

**FRs covered:** FR19

As a project admin,
I want gán roles cụ thể cho từng member trong project,
So that mỗi người có quyền truy cập đúng mức độ công việc của họ.

**Acceptance Criteria:**

**Given** project có 4 role mặc định: Admin, Developer, Reporter, Viewer
**When** admin thêm member vào project
**Then** phải chọn role cho member đó; không thể là member mà không có role

**Given** user có role Viewer trong project
**When** thao tác tạo issue hoặc chuyển status
**Then** trả `403 ProblemDetails` (không phải 404 vì user đã biết project tồn tại)

**Given** admin thay đổi role của một member
**When** lưu
**Then** permission thay đổi có hiệu lực ngay; token hiện tại của user vẫn dùng được nhưng permission check real-time trên mỗi request

### Story 15.5: Permission matrix (fine-grained: who can create/edit/delete/transition)

**FRs covered:** FR19

As a project admin,
I want cấu hình fine-grained permission matrix (action × role),
So that tôi kiểm soát chính xác ai được làm gì, vượt ra ngoài 4 role cứng.

**Acceptance Criteria:**

**Given** admin mở Permission Matrix
**When** xem giao diện
**Then** hiển thị bảng Actions (rows) × Roles (columns): Create Issue, Edit Issue, Delete Issue, Transition Issue, Manage Sprint, Manage Board, View Reports, Manage Members, Configure Workflows

**Given** admin tick/untick một ô trong matrix
**When** lưu
**Then** permission scheme được cập nhật ngay và áp dụng cho tất cả requests tiếp theo trong project

**Given** permission scheme thay đổi
**When** hệ thống enforce permission
**Then** check permission theo scheme hiện tại của project (không cache permission cũ quá 60 giây)
**And** mọi permission check failure ghi audit log (actor, action attempted, resource, timestamp) để admin xem xét

### Story 15.6: Notification Scheme per project (event → recipient mapping)

**FRs covered:** FR-189, FR-190, FR-191, FR-192, FR-193, FR-194, FR-195

As a project admin,
I want cấu hình ai nhận thông báo gì khi có event trong project,
So that team nhận đúng thông tin cần thiết mà không bị spam notification.

**Acceptance Criteria:**

**Given** admin vào Project Settings > Notifications
**When** xem Notification Scheme
**Then** thấy bảng: Event (rows) × Recipient Type (columns) với 9 event types: `Issue Created`, `Issue Updated`, `Issue Assigned`, `Issue Commented`, `Issue Status Changed`, `Issue Mentioned You`, `Sprint Started`, `Sprint Completed`, `Due Date Approaching`
**And** mỗi ô có thể bật/tắt cho từng recipient type: `Assignee`, `Reporter`, `Watchers`, `Project Lead`, `Role: Developer`, `Role: Admin`

**Given** admin enable `Issue Assigned` → `Assignee`
**When** issue được gán cho một user
**Then** user đó nhận notification (in-app + email nếu email preference bật)

**Given** user cá nhân vào profile > Notification Preferences
**When** tắt email cho event type cụ thể (ví dụ "Issue Commented")
**Then** override scheme của project — user đó không nhận email cho event đó dù scheme bật
**And** in-app notification vẫn hiển thị (chỉ tắt được in-app trong preference riêng)

**Given** project chưa có custom scheme
**When** mới tạo project
**Then** áp dụng Default Scheme: Assignee + Reporter nhận tất cả events; Watchers nhận Comment + Status Changed + Mentioned

**And** thay đổi scheme ghi audit log: ai thay đổi, thay đổi event nào, thời điểm

### Story 15.7: API Token management (personal + project-scoped)

**FRs covered:** FR-158, FR-159

As a developer or PM,
I want tạo API tokens để tích hợp với tools bên ngoài mà không dùng password,
So that automation scripts và integrations có thể gọi API an toàn mà không expose credentials.

**Acceptance Criteria:**

**Given** user vào Profile > API Tokens
**When** tạo token mới
**Then** nhập tên mô tả + expiry date (90/180/365 ngày hoặc no expiry) → system generate token value, hiển thị **một lần duy nhất** (không lấy lại được sau khi đóng dialog)

**Given** token đã tạo
**When** gọi API với header `Authorization: Bearer {token}`
**Then** API xác thực token và xử lý request với quyền của user sở hữu token

**Given** token hết hạn
**When** gọi API
**Then** trả `401 ProblemDetails` với message "Token expired" (không leak thông tin token khác)

**Given** admin vào Project Settings > API Access
**When** xem danh sách active tokens của project
**Then** thấy tokens (tên + owner + created + expiry + last used) nhưng không thấy token value; có thể revoke bất kỳ token nào
