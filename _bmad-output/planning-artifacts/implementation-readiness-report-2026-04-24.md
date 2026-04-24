---
stepsCompleted:
  - step-01-document-discovery
  - step-02-prd-analysis
  - step-03-epic-coverage-validation
  - step-04-ux-alignment
  - step-05-epic-quality-review
  - step-06-final-assessment
workflowType: 'implementation-readiness'
assessmentDate: '2026-04-24'
includedDocuments:
  prd:
    - '_bmad-output/planning-artifacts/prd.md'
  architecture: []
  ux: []
  epics_and_stories: []
---

# Implementation Readiness Assessment Report

**Date:** 2026-04-24  
**Project:** project-management

## Step 1: Document Discovery (Inventory)

Beginning **Document Discovery** to inventory all project files.

### PRD Files Found

**Whole Documents:**
- `prd.md` (32210 bytes, modified 2026-04-24 16:05:29)

**Sharded Documents:**
- None found

### Architecture Files Found

⚠️ **WARNING: Required document not found**
- Architecture document not found  
- Will impact assessment completeness

### Epics & Stories Files Found

⚠️ **WARNING: Required document not found**
- Epics & Stories document not found  
- Will impact assessment completeness

### UX Design Files Found

⚠️ **WARNING: Required document not found**
- UX design document not found  
- Will impact assessment completeness

### Duplicates

- No duplicate whole/sharded document formats found.

## PRD Analysis

### Functional Requirements Extracted

FR1: Hệ thống là web application nội bộ được thiết kế để thay thế Excel trong việc quản lý lực lượng lao động hỗn hợp — nhân sự inhouse và outsource từ nhiều vendor — trên nhiều dự án đồng thời; phục vụ ~20 người dùng nội bộ (Project Manager và các cấp quản lý) với cái nhìn tập trung, thời gian thực về phân công nhân sự, chi phí theo vendor, tiến độ dự án và năng lực đội ngũ.

FR2: Hệ thống phải cung cấp **Microsoft Project-style Gantt view** dạng split-panel — task tree phân cấp (Dự Án → Phase → Milestone → Task) bên trái, Gantt calendar bên phải — với các cột: VBS, tên task, loại, effort KH/TT (giờ), ngày bắt đầu/kết thúc KH & TT, % hoàn thành, predecessor, người phụ trách (1 người/task), ưu tiên, trạng thái, ghi chú; Gantt phân màu: KH, thực tế, đang làm, trễ hạn, milestone, hôm nay.

FR3: Hệ thống phải hỗ trợ **effort tracking theo giờ** cho từng task (gán số giờ ước tính) và tính tổng giờ của từng nhân sự theo ngày/tuần, có cảnh báo khi vượt ngưỡng (lịch Thứ 2–Thứ 6, 8 giờ/ngày).

FR4: Hệ thống phải hỗ trợ **multi-role allocation**: một nhân sự có thể giữ nhiều vai trò trên nhiều dự án đồng thời; overload được phát hiện và cảnh báo tự động.

FR5: Hệ thống phải hỗ trợ **per-vendor cost tracking**: mỗi vendor có đơn giá riêng theo role/level; tổng hợp chi phí thực tế theo vendor và đối chiếu ngân sách.

FR6: Hệ thống phải cung cấp **unified multi-project view**: tất cả dự án hiển thị trong một giao diện, thay thế các Excel sheet rời rạc.

FR7: Hệ thống phải cung cấp **1-click reporting**: dashboard tiến độ và chi phí sẵn sàng chia sẻ với cấp trên, không cần tổng hợp thủ công.

FR8: Hệ thống phải có **audit trail**: lịch sử đầy đủ phân công, thay đổi role và rate theo thời gian; mọi thay đổi (task, effort, phân công, rate) được ghi log đầy đủ: ai sửa, sửa gì, thời điểm; không retroactive adjustment — nếu cần đính chính, tạo amendment entry mới (không overwrite lịch sử cũ).

FR9: (MVP) Hệ thống phải cho phép **quản lý dự án**: tạo/sửa/xóa dự án với phân cấp Dự Án → Phase → Milestone → Task.

FR10: (MVP) Task phải hỗ trợ các trường: VBS, tên, loại, effort KH/TT (giờ), ngày bắt đầu/kết thúc KH & TT, % hoàn thành, predecessor, người phụ trách (1 người/task), ưu tiên, trạng thái, ghi chú.

FR11: (MVP) Hệ thống phải cung cấp **Gantt Chart view** với split-panel layout (task tree trái, Gantt calendar phải) và phân màu theo trạng thái (KH, thực tế, đang làm, trễ hạn, milestone, hôm nay).

FR12: (MVP) Hệ thống phải hỗ trợ **quản lý nhân sự**: danh sách inhouse + outsource; mỗi người có thể giữ nhiều vai trò trên nhiều dự án với tỷ lệ phân bổ khác nhau.

FR13: (MVP) Hệ thống phải hỗ trợ **quản lý vendor**: danh sách vendor; mỗi vendor có nhiều nhân sự; đơn giá theo role/level.

FR14: (MVP) Hệ thống phải hỗ trợ **cảnh báo overload** tự động khi vượt 8h/ngày hoặc 40h/tuần.

FR15: (MVP) Hệ thống phải hỗ trợ **cost tracking**: chi phí planned vs actual; tổng hợp theo vendor, theo dự án, theo nhân sự.

FR16: (MVP) Hệ thống phải cung cấp **dashboard tổng quan**: trạng thái tất cả dự án, overload alert, tiến độ, chi phí.

FR17: (MVP) Hệ thống phải cung cấp **báo cáo chi phí** theo vendor/dự án/nhân sự; xuất được (PDF hoặc Excel).

FR18: (MVP) Hệ thống phải cung cấp **multi-project view**: tất cả dự án trong một giao diện thống nhất.

FR19: (MVP) **Dữ liệu & phân quyền**: tất cả PM truy cập toàn bộ dữ liệu (vendor rates, chi phí nhân sự, giờ công) — không phân quyền chi tiết; data model được thiết kế sẵn sàng hỗ trợ phân quyền ở giai đoạn sau (không cần refactor).

FR20: Hệ thống phải áp dụng **công thức tính chi phí**:
- Hourly Rate = Monthly Rate ÷ 176h (chuẩn cố định: 22 ngày × 8h/ngày)
- Chi phí = Giờ thực tế (confirmed) × Hourly Rate
- Rate được thiết lập theo vendor × level (ví dụ: Vendor A, Senior Dev = 20 triệu/tháng)
- Rate thay đổi: chỉ ở ranh giới tháng, không thay đổi giữa tháng
- Lịch sử rate lưu dạng immutable (vendor, level, rate, tháng áp dụng) — có thể reconstruct chi phí tại bất kỳ thời điểm nào
- Nhân sự vắng mặt: giờ không log = chi phí = 0
- Partial month (người join giữa tháng): tự nhiên xử lý — log ít giờ hơn → chi phí thấp hơn theo tỷ lệ thực tế

FR21: Hệ thống phải implement **cơ chế ghi nhận giờ thực tế (Actual Hours)** theo nguyên tắc: `actual_hours` trên task là computed field (tổng hợp từ các bản ghi TimeEntry), không phải field nhập trực tiếp; mọi thay đổi giờ thực tế đều tạo TimeEntry mới (immutable log), đảm bảo audit trail đầy đủ.

FR22: Hệ thống phải hỗ trợ **hai tầng ghi nhận Actual Hours**:
- Tầng 1 — Cuối tháng: Vendor CSV Import (nguồn chính): PM import file (CSV/Excel); hệ thống hiển thị mapping UI để map cột từ file vendor → fields trong hệ thống (mỗi vendor lưu một mapping template riêng); PM review diff giữa dữ liệu import và estimate hiện có → approve → lock; sau khi lock dữ liệu trở thành vendor-confirmed dùng làm nguồn chính cho cost report chính thức; file gốc được lưu trữ để đối chiếu khi có tranh chấp.
- Tầng 2 — Giữa tháng: Bulk Timesheet Grid (bổ sung liên tục): PM nhập/điều chỉnh giờ thực tế qua grid người × ngày (hoặc tuần) × task; dùng cho overload detection và 4-week forecast vận hành liên tục giữa các lần import vendor; áp dụng cho cả nhân sự inhouse (không có vendor timesheet); PM nhập theo tuần, không bắt buộc nhập theo ngày.

FR23: Hệ thống phải hỗ trợ **data status tracking** cho TimeEntry với trạng thái:
- `estimated` — chưa có actual, dùng estimate làm proxy tạm thời
- `pm-adjusted` — PM đã nhập thủ công qua bulk grid
- `vendor-confirmed` — đã reconcile với timesheet vendor và được lock

FR24: Hệ thống phải áp dụng **quy tắc hiển thị báo cáo** liên quan TimeEntry:
- Báo cáo cost chính thức: chỉ dùng `vendor-confirmed` (hoặc `pm-adjusted` cho inhouse)
- Dashboard overload và forecast: dùng tất cả trạng thái (bao gồm `estimated`) để có dữ liệu real-time
- Mọi report hiển thị % dữ liệu đã confirmed vs estimated để PM biết độ tin cậy

FR25: Hệ thống phải áp dụng **validation rules** cho TimeEntry:
- Cảnh báo nếu giờ thực tế (pm-adjusted) chênh lệch > 20% so với estimate mà không có ghi chú lý do
- Không cho phép nhập > 16h/ngày cho một người (hard cap, tránh nhập nhầm)
- Audit log ghi rõ `entered_by` (PM user) và `resource_id` (người thực tế làm việc) — hai field tách biệt

FR26: Hệ thống phải hỗ trợ **lịch ngày lễ (configurable)**:
- Admin có thể thêm/sửa/xóa ngày lễ trong hệ thống (không hardcode cố định)
- Ngày lễ được loại khỏi tính toán overload và capacity planning
- Ngày lễ hiển thị trên Gantt calendar với màu phân biệt (visual indicator)
- Deadline task tự động được đẩy forward khi task kết thúc trùng vào hoặc span qua ngày lễ (tương tự Microsoft Project behavior)

FR27: Hệ thống phải hỗ trợ **lưu trữ dữ liệu dài hạn**: dữ liệu lưu trữ dài hạn, không tự xóa; dự án cũ, nhân sự đã nghỉ việc, vendor ngừng hợp tác: dữ liệu được giữ nguyên, đánh dấu trạng thái inactive.

FR28: Hệ thống phải hỗ trợ các “innovation areas” được mô tả:
- Predictive Overload Warning: cảnh báo tác động overload trước khi xác nhận assign, với traffic light spectrum (xanh/vàng/cam/đỏ theo % capacity còn lại); PM thấy mức độ tác động ngay trên dialog xác nhận; hệ thống có thể chặn mềm nếu PM không chủ đích override.
- Capacity-First Assignment View: heatmap (person × tuần) kết hợp thông tin chi phí vendor, cho phép PM quyết định dựa trên cả capacity lẫn cost trong cùng 1 view.
- Smart Assignment Suggestion: đề xuất người assign tối ưu dựa trên 3 tiêu chí: role match + capacity còn lại + cost; PM approve hoặc override; là “suggestion” không phải “auto-assign”.
- 4-Week Capacity Forecast: rolling forecast 4 tuần cross-project cho từng nhân sự.

FR29: Hệ thống phải hỗ trợ các chỉ số/validation approach cho các innovation areas:
- Predictive overload: đo số lần PM phải undo/reassign trước và sau khi có feature
- Smart suggestion: track tỷ lệ PM accept vs override gợi ý của hệ thống — nếu override rate > 50% thì algorithm cần điều chỉnh
- Capacity forecast: đo số lần PM phát hiện bottleneck proactively (từ forecast) vs reactive (sau khi đã overload)

FR30: Hệ thống là **Single Page Application (SPA)** xây dựng bằng **Angular**, chạy desktop browser; dữ liệu được load qua REST API; các tính toán overload được thực hiện client-side để đảm bảo tốc độ phản hồi tức thì.

FR31: Hệ thống phải hỗ trợ data refresh dạng **polling-based** (interval configurable, khuyến nghị 30–60 giây); không dùng WebSocket/real-time push ở giai đoạn này.

FR32: Hệ thống phải hỗ trợ **overload check client-side** trên dữ liệu đã load — phản hồi tức thì (<200ms) khi PM thay đổi assignment.

FR33: Hệ thống phải cung cấp **RESTful API (JSON)**.

FR34: Hệ thống phải hỗ trợ API theo hướng:
- Token-based authentication (JWT)
- Pagination cho danh sách lớn (nhân sự, task log, audit trail)

FR35: **Authentication Model (MVP)**:
- Local username/password (email + mật khẩu hashed)
- JWT token, session timeout hợp lý (ví dụ: 8 giờ — hết giờ làm việc tự logout)
- Admin tạo/vô hiệu hóa tài khoản
- Data model và auth middleware được tách biệt rõ ràng để có thể thêm SSO (Azure AD / Google Workspace) sau mà không cần refactor toàn bộ; Phase 2: thêm OAuth2/OIDC provider mà không thay đổi authorization logic

FR36: **PDF Export** phải là server-side generation (không dùng browser print) để đảm bảo layout nhất quán và có thể schedule/batch trong tương lai; thư viện khuyến nghị: Puppeteer (headless Chrome) hoặc tương đương.

FR37: **Gantt Rendering**:
- Custom Angular component dựa trên Angular CDK, không phụ thuộc vào thư viện Gantt thương mại (tránh license lock-in)
- Canvas rendering nếu > 500 task để đảm bảo performance

FR38: **State Management**:
- RxJS BehaviorSubject/Store pattern (hoặc NgRx nếu team quen thuộc)
- Cache dữ liệu capacity/assignment client-side để overload check instant

FR39: **Deployment**: web server nội bộ (on-premise hoặc private cloud) — không deploy public; không có yêu cầu CDN hay multi-region.

FR40: **MVP Strategy & Philosophy**: Full Product Delivery với staged rollout nội bộ — xây dựng sản phẩm hoàn chỉnh ngay từ đầu, không cắt tính năng để ra sớm; release theo từng sprint để lấy feedback liên tục; với ~20 users nhu cầu xác định rõ, dữ liệu vận hành yêu cầu hệ thống đủ chính xác từ ngày đầu; partial system không thể thay thế Excel; staged rollout trong Phase 1 để lấy feedback liên tục.

FR41: **Resource Requirements**: team 3–4 developers (2 frontend Angular chuyên sâu cho Gantt, 1–2 backend), 1 PM, 1 QA.

FR42: **Staged Rollout trong Phase 1**:
- Sprint 1–2: Core Gantt + Assignment Engine → PMs dùng thật
- Sprint 3–4: Overload Warning + Capacity Heatmap → layer lên
- Sprint 5–6: 4-Week Forecast + Cost Reports + Export → hoàn thiện
- Sprint 7+: Smart Assignment Suggestion → enable sau khi có 4–6 tuần actual data

FR43: **Phase 1 must-have — Quản lý Dự án & Task**:
- Cấu trúc phân cấp: Dự Án → Phase → Milestone → Task
- Task fields đầy đủ: VBS, tên, loại, Estimate (KH), Actual (TT — computed từ TimeEntry), ngày KH & TT, % hoàn thành (manual), predecessor, assignee (1 người), ưu tiên, trạng thái, ghi chú
- Carry-over balance khi onboard: PM nhập "hours spent to date" và "remaining estimate" cho dự án đang chạy khi migrate từ Excel — không yêu cầu re-enter toàn bộ lịch sử

FR44: **Phase 1 must-have — Gantt Chart View**:
- Split-panel: task tree trái + Gantt calendar phải
- Dual bar: KH (planned) và TT (actual) song song theo màu phân biệt
- Drag-drop task (thay đổi ngày, chuyển assignee), dependency arrows, holiday overlay

FR45: **Phase 1 must-have — Quản lý Nhân sự & Vendor**:
- Inhouse + outsource với multi-role cross-project
- Vendor management: rate theo vendor × level, immutable rate history

FR46: **Phase 1 must-have — Actual Hours Logging — Hai tầng**:
- Monthly: Vendor CSV import → mapping template → reconcile → lock (vendor-confirmed)
- Mid-month: Bulk timesheet grid (người × tuần) → pm-adjusted
- Data status: `estimated | pm-adjusted | vendor-confirmed`

FR47: **Phase 1 must-have — Holiday Calendar**: admin-configurable; auto-shift deadline; Gantt overlay.

FR48: **Phase 1 must-have — Overload Warning + Predictive Overload**:
- Standard: >8h/ngày, >40h/tuần, client-side <200ms
- Predictive: traffic light (xanh/vàng/cam/đỏ) trước khi confirm assign

FR49: **Phase 1 must-have — Capacity-First Assignment View**: heatmap person × tuần kết hợp chi phí vendor trong cùng 1 view.

FR50: **Phase 1 must-have — 4-Week Capacity Forecast**: rolling cross-project, server-side precompute.

FR51: **Smart Assignment Suggestion enable Sprint 7+**:
- Rule-based: role match → capacity → cost
- Transparency bắt buộc: top 3 candidates + reasoning rõ ràng
- Track acceptance rate; nếu <40% → algorithm cần tune

FR52: **Cost Tracking & Reporting**:
- Planned vs Actual per vendor/dự án/nhân sự/tháng
- Phát hiện bất thường tự động
- Export: PDF + Excel
- Report label rõ % data confirmed vs estimated

FR53: **Notifications cơ bản (Phase 1)**:
- Email digest hàng tuần: overload alerts + task sắp trễ (cron job đơn giản)
- Đảm bảo PM nhận được cảnh báo kể cả khi không mở tool

FR54: **Authentication & Audit (Phase 1)**:
- Local username/password, JWT 8h session, ~20 tài khoản PM
- Audit trail: `entered_by` (PM) và `resource_id` (người làm việc) tách biệt

Total FRs: 54

### Non-Functional Requirements Extracted

NFR1: Performance: Dashboard chính tải trong < 3 giây với 20 người dùng đồng thời.

NFR2: Độ chính xác: Tính toán giờ, chi phí và overload chính xác 100% theo quy tắc ngày làm việc Thứ 2–Thứ 6, 8 giờ/ngày.

NFR3: Availability: Hệ thống hoạt động ổn định trong giờ làm việc; không mất dữ liệu.

NFR4: Trình duyệt: Hoạt động đúng trên Chrome, Edge (các phiên bản hiện đại).

NFR5: Browser matrix / hỗ trợ trình duyệt:
- Google Chrome 100+ (Primary, bắt buộc hoạt động hoàn hảo)
- Microsoft Edge 100+ (Primary, bắt buộc hoạt động hoàn hảo)
- Firefox, Safari, khác: không hỗ trợ chính thức

NFR6: Overload check khi gán nhân sự: < 200ms (client-side, tức thì).

NFR7: Performance targets:
- Dashboard tổng quan (cold load): < 3 giây
- Gantt render (dự án ~100 task): < 2 giây
- Báo cáo chi phí tháng: < 3 giây
- Overload check khi gán nhân sự: < 200ms (client-side, tức thì)
- Xuất báo cáo PDF: < 10 giây (server-side generation)

NFR8: Đo lường performance theo giả định: 20 người dùng đồng thời; dữ liệu ~10 dự án, ~100 task/dự án, ~50 nhân sự.

NFR9: Responsive design & viewport:
- Desktop-only: tối thiểu viewport 1280×768px
- Không thiết kế cho tablet hay mobile
- Gantt view yêu cầu màn hình rộng (khuyến nghị 1440px+); có thể scroll ngang

NFR10: SEO strategy: không áp dụng; ứng dụng nội bộ; không cần SSR.

NFR11: Accessibility level: không có yêu cầu WCAG đặc biệt ở MVP; đủ để người dùng thông thường dùng bình thường (tab navigation cơ bản, không cần screen reader support).

NFR12: Constraint về real-time: sử dụng polling-based data refresh, khuyến nghị 30–60 giây; không dùng WebSocket/real-time push ở giai đoạn này (tránh độ phức tạp infrastructure).

NFR13: Gantt rendering performance constraint: canvas rendering nếu > 500 task để đảm bảo performance.

Total NFRs: 13

### Additional Requirements / Constraints / Assumptions Extracted

AR1: Tích hợp (Giai đoạn 1): không có.

AR2: Tích hợp tương lai: Jira, Confluence (chưa xác định timeline).

AR3: Lịch làm việc chuẩn: Thứ 2–Thứ 6, 8 giờ/ngày; chuẩn 22 ngày × 8h/ngày dùng cho quy đổi Monthly Rate → Hourly Rate (176h).

AR4: Hard cap nhập giờ: không cho phép nhập > 16h/ngày cho một người.

AR5: Dữ liệu lưu trữ dài hạn, không tự xóa; các đối tượng có thể đánh dấu inactive.

AR6: Deployment nội bộ (on-premise hoặc private cloud), không public.

AR7: Không có yêu cầu mobile, SEO, hay accessibility nâng cao trong MVP.

AR8: Gantt custom component tránh license lock-in; ưu tiên Angular CDK, RxJS; component library Angular Material hoặc tương đương.

AR9: API kiểu REST JSON; JWT auth; pagination cho danh sách lớn.

AR10: PDF export server-side với Puppeteer hoặc tương đương; hướng tới schedule/batch trong tương lai.

AR11: “Full Product Delivery” philosophy: không cắt tính năng để ra sớm; staged rollout trong Phase 1 theo sprint.

AR12: Giai đoạn 1 auth local username/password; thiết kế sẵn sàng cho SSO (Azure AD/Google Workspace) ở Phase 2.

### PRD Completeness Assessment (Initial)

- PRD có mô tả rõ product scope, journeys, quy tắc domain (cost, timesheet), và một số mục tiêu hiệu năng.
- Tuy nhiên, PRD hiện chưa có tài liệu **Architecture**, **UX**, và đặc biệt **Epics & Stories** để thực hiện traceability/coverage validation ở các bước tiếp theo của workflow.

## Epic Coverage Validation

Beginning **Epic Coverage Validation**.

### Epics & Stories Document Load Status

⚠️ **WARNING: Required document not found**
- Epics & Stories document không tồn tại trong `{planning_artifacts}` tại thời điểm assessment.
- Vì không có epics/stories để đối chiếu, **không thể thực hiện traceability** từ FR → Epic/Story.

### Coverage Matrix

| FR Number | PRD Requirement | Epic Coverage | Status |
| --------- | --------------- | ------------ | ------ |
| FR1 | Hệ thống là web application nội bộ... | **NOT FOUND** | ❌ MISSING |
| FR2 | Microsoft Project-style Gantt view split-panel... | **NOT FOUND** | ❌ MISSING |
| FR3 | Effort tracking theo giờ + cảnh báo overload... | **NOT FOUND** | ❌ MISSING |
| FR4 | Multi-role allocation... | **NOT FOUND** | ❌ MISSING |
| FR5 | Per-vendor cost tracking... | **NOT FOUND** | ❌ MISSING |
| FR6 | Unified multi-project view... | **NOT FOUND** | ❌ MISSING |
| FR7 | 1-click reporting / dashboard... | **NOT FOUND** | ❌ MISSING |
| FR8 | Audit trail... | **NOT FOUND** | ❌ MISSING |
| FR9 | (MVP) Quản lý dự án CRUD + hierarchy... | **NOT FOUND** | ❌ MISSING |
| FR10 | (MVP) Task fields... | **NOT FOUND** | ❌ MISSING |
| FR11 | (MVP) Gantt view layout + màu... | **NOT FOUND** | ❌ MISSING |
| FR12 | (MVP) Quản lý nhân sự... | **NOT FOUND** | ❌ MISSING |
| FR13 | (MVP) Quản lý vendor... | **NOT FOUND** | ❌ MISSING |
| FR14 | (MVP) Overload alert 8h/day 40h/week... | **NOT FOUND** | ❌ MISSING |
| FR15 | (MVP) Cost tracking planned vs actual... | **NOT FOUND** | ❌ MISSING |
| FR16 | (MVP) Dashboard tổng quan... | **NOT FOUND** | ❌ MISSING |
| FR17 | (MVP) Báo cáo chi phí + export PDF/Excel... | **NOT FOUND** | ❌ MISSING |
| FR18 | (MVP) Multi-project view... | **NOT FOUND** | ❌ MISSING |
| FR19 | (MVP) Dữ liệu & phân quyền (PM full access)... | **NOT FOUND** | ❌ MISSING |
| FR20 | Công thức tính chi phí (rate, history, partial month)... | **NOT FOUND** | ❌ MISSING |
| FR21 | Actual hours computed từ TimeEntry (immutable log)... | **NOT FOUND** | ❌ MISSING |
| FR22 | 2 tầng actual hours (vendor import + bulk grid)... | **NOT FOUND** | ❌ MISSING |
| FR23 | TimeEntry status tracking (estimated/pm-adjusted/vendor-confirmed)... | **NOT FOUND** | ❌ MISSING |
| FR24 | Quy tắc báo cáo theo status + % confirmed... | **NOT FOUND** | ❌ MISSING |
| FR25 | Validation rules (20% deviation note, 16h cap, entered_by/resource_id)... | **NOT FOUND** | ❌ MISSING |
| FR26 | Holiday calendar configurable + auto shift deadline + overlay... | **NOT FOUND** | ❌ MISSING |
| FR27 | Retention dài hạn + inactive marking... | **NOT FOUND** | ❌ MISSING |
| FR28 | Innovation areas (predictive overload, heatmap, suggestion, forecast)... | **NOT FOUND** | ❌ MISSING |
| FR29 | Validation approach metrics cho innovation areas... | **NOT FOUND** | ❌ MISSING |
| FR30 | SPA Angular + REST API; overload calc client-side... | **NOT FOUND** | ❌ MISSING |
| FR31 | Polling-based refresh 30–60s; no websocket... | **NOT FOUND** | ❌ MISSING |
| FR32 | Overload check <200ms client-side... | **NOT FOUND** | ❌ MISSING |
| FR33 | RESTful API JSON... | **NOT FOUND** | ❌ MISSING |
| FR34 | JWT auth + pagination... | **NOT FOUND** | ❌ MISSING |
| FR35 | Auth model local username/password + JWT 8h + admin manage + SSO-ready... | **NOT FOUND** | ❌ MISSING |
| FR36 | PDF export server-side (Puppeteer hoặc tương đương)... | **NOT FOUND** | ❌ MISSING |
| FR37 | Gantt custom Angular CDK + canvas >500 tasks... | **NOT FOUND** | ❌ MISSING |
| FR38 | State management RxJS store/NgRx + client cache... | **NOT FOUND** | ❌ MISSING |
| FR39 | Deployment nội bộ; no CDN/multi-region... | **NOT FOUND** | ❌ MISSING |
| FR40 | MVP strategy: full delivery + staged rollout... | **NOT FOUND** | ❌ MISSING |
| FR41 | Resource requirements (team composition)... | **NOT FOUND** | ❌ MISSING |
| FR42 | Staged rollout plan Sprint 1–7+... | **NOT FOUND** | ❌ MISSING |
| FR43 | Phase 1: project/task mgmt + carry-over balance... | **NOT FOUND** | ❌ MISSING |
| FR44 | Phase 1: Gantt dual bar + drag-drop + dependencies + holiday overlay... | **NOT FOUND** | ❌ MISSING |
| FR45 | Phase 1: people/vendor mgmt + immutable rate history... | **NOT FOUND** | ❌ MISSING |
| FR46 | Phase 1: actual hours logging 2-tier + status... | **NOT FOUND** | ❌ MISSING |
| FR47 | Phase 1: holiday calendar... | **NOT FOUND** | ❌ MISSING |
| FR48 | Phase 1: overload warning + predictive traffic light... | **NOT FOUND** | ❌ MISSING |
| FR49 | Phase 1: capacity-first heatmap... | **NOT FOUND** | ❌ MISSING |
| FR50 | Phase 1: 4-week capacity forecast precompute... | **NOT FOUND** | ❌ MISSING |
| FR51 | Smart suggestion enable Sprint 7+ + transparency + acceptance rate tracking... | **NOT FOUND** | ❌ MISSING |
| FR52 | Cost tracking/reporting + anomaly detection + export + confirmed label... | **NOT FOUND** | ❌ MISSING |
| FR53 | Notifications cơ bản: weekly email digest... | **NOT FOUND** | ❌ MISSING |
| FR54 | Phase 1: auth local + audit entered_by/resource_id... | **NOT FOUND** | ❌ MISSING |

### Missing Requirements

#### Critical Missing FRs

Do **không có Epics & Stories document**, mọi FR đều **chưa có đường triển khai** (FR → Epic/Story) để xác nhận coverage. Điều này là **blocker** cho implementation readiness.

### Coverage Statistics

- Total PRD FRs: 54
- FRs covered in epics: 0 (không có epics document để đối chiếu)
- Coverage percentage: 0%

## UX Alignment Assessment

Beginning **UX Alignment** validation.

### UX Document Status

⚠️ **Not Found**
- Không tìm thấy UX document trong `{planning_artifacts}` theo pattern `*ux*.md` hoặc `*ux*/index.md` tại thời điểm assessment.

### Alignment Issues

- Không thể kiểm tra UX ↔ PRD alignment vì thiếu UX document.
- Không thể kiểm tra UX ↔ Architecture alignment (tài liệu kiến trúc riêng) vì thiếu Architecture document; PRD có nêu một số ràng buộc kỹ thuật, nhưng chưa đủ để đối chiếu UX (IA, màn hình, state, permission, empty/error/loading, Gantt interactions).

### Warnings

⚠️ **UX is implied but missing**
- PRD mô tả rõ đây là **web application (SPA Angular)** với nhiều màn hình/interaction nặng (Gantt split-panel, drag-drop reschedule, dashboard, reports/export, heatmap/capacity view, timesheet grid, audit trail). Do đó UX spec là cần thiết để tránh lệch kỳ vọng UI/flow và giảm rework.

## Epic Quality Review

Beginning **Epic Quality Review** against create-epics-and-stories standards.

### Epics & Stories Document Status

⚠️ **Not Found**
- Không có Epics & Stories document để thực hiện kiểm tra chất lượng epic/story (user value, independence, dependencies, AC, sizing).

### Findings

🔴 **Critical Violation / Blocker**
- Không thể đánh giá readiness cho implementation vì thiếu artifact bắt buộc để chuyển FR → Epic → Story → Acceptance Criteria.

### Recommendation

- Cần tạo Epics & Stories document (ưu tiên chạy `bmad-create-epics-and-stories`) trước khi có thể tiếp tục bước kiểm tra chất lượng epic/story.

## Summary and Recommendations

### Overall Readiness Status

**NOT READY**

### Critical Issues Requiring Immediate Action

1. **Thiếu Epics & Stories document** → không thể traceability FR → Epic/Story/AC; Epic Coverage = 0% (54/54 FR chưa có coverage path).
2. **Thiếu UX document** trong khi PRD mô tả UI/interaction phức tạp (Gantt split-panel, drag-drop, heatmap, timesheet grid, reports/export, audit) → rủi ro lệch flow/state/permission và rework cao.
3. **Thiếu Architecture document** (độc lập) để khóa các quyết định ảnh hưởng lớn (RBAC, audit immutability/retention, concurrency, background jobs export/import, aggregation/reporting) → rủi ro thay đổi thiết kế giữa chừng.

### Recommended Next Steps

1. Chạy **`bmad-create-epics-and-stories`** để tạo backlog triển khai có epic/story + acceptance criteria, đồng thời map coverage cho các FR trọng yếu (Gantt, Timesheet/TimeEntry, Cost model, Overload, Audit, Export).
2. Chạy **`bmad-create-ux-design`** để khóa IA, screen inventory, user flows, state machines (task/timeentry/import lock), permission UX, và các trạng thái empty/error/loading cho MVP.
3. Chạy **`bmad-create-architecture`** để chốt kiến trúc mục tiêu và các quyết định bắt buộc (auth/JWT, audit log strategy, data model sơ bộ, concurrency strategy, reporting/export jobs, performance targets & measurement).
4. (Tuỳ chọn nhưng khuyến nghị) Chạy **`bmad-edit-prd`** để làm rõ non-goals/MVP cutline và các phần yêu cầu còn mơ hồ (timesheet rounding/timezone/holiday rules, baseline vs actual vs forecast, export constraints).

### Final Note

This assessment identified **3 critical missing artifacts** (Architecture, UX, Epics & Stories) and **1 traceability blocker** (Epic coverage cannot be validated; currently 0%). Address the critical issues before proceeding to implementation. These findings can be used to improve the artifacts or you may choose to proceed as-is (high risk).

