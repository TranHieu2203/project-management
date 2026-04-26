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
PM nhận weekly email digest và hệ thống ghi nhận các metrics (override predictive, accept/override suggestion, proactive vs reactive) để cải tiến vận hành/thuật toán.
**FRs covered:** FR29, FR53

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
