---
stepsCompleted: ['step-01-init', 'step-02-discovery', 'step-02b-vision', 'step-02c-executive-summary', 'step-03-success', 'step-04-journeys', 'step-05-domain', 'step-06-innovation', 'step-07-project-type', 'step-08-scoping', 'step-01b-continue', 'step-09-functional', 'step-10-nonfunctional', 'step-11-polish', 'step-12-complete']
inputDocuments: []
workflowType: 'prd'
briefCount: 0
researchCount: 0
brainstormingCount: 0
projectDocsCount: 0
classification:
  projectType: web_app
  domain: enterprise_project_management
  complexity: medium
  projectContext: greenfield
  userCount: 20
  integrations_phase1: []
  integrations_future: ['jira', 'confluence']
  workingDays: 'Monday to Friday, 8 hours/day'
  effortUnit: hours
  keyFeatures:
    - gantt_task_view
    - effort_overload_warning
    - multi_vendor_cost_tracking
    - multi_role_allocation
    - progress_reporting_dashboard
    - unified_multi_project_view
---

# Product Requirements Document - project-management

**Author:** HieuTV-Team-Project-Management
**Date:** 2026-04-24

## Executive Summary

**[Tên dự án]** là web application nội bộ được thiết kế để thay thế Excel trong việc quản lý lực lượng lao động hỗn hợp — nhân sự inhouse và outsource từ nhiều vendor — trên nhiều dự án đồng thời. Hệ thống phục vụ ~20 người dùng nội bộ (Project Manager và các cấp quản lý) cần cái nhìn tập trung, thời gian thực về phân công nhân sự, chi phí theo vendor, tiến độ dự án và năng lực đội ngũ.

**Vấn đề cốt lõi:** Nhiều file Excel riêng lẻ (mỗi dự án một sheet) không thể cho cái nhìn tổng thể về lực lượng lao động hỗn hợp: một nhân sự tham gia nhiều dự án với nhiều vai trò, tỷ lệ phân bổ và số giờ làm việc khác nhau; nhiều vendor cung cấp nhân sự outsource với đơn giá riêng; ngân sách dự án cần đối chiếu liên tục với chi phí thực tế. Hệ quả: mất kiểm soát về capacity, overload nhân sự không phát hiện kịp, báo cáo thủ công dễ sai sót.

**Trạng thái mục tiêu:** Một trung tâm điều hành dự án duy nhất thay thế toàn bộ file Excel phân tán — nơi mọi thông tin về người → dự án → vai trò → giờ làm → chi phí → tiến độ được quản lý tập trung và cập nhật thời gian thực.

**Vision mở rộng (Phase 2+):** Từ công cụ thay thế Excel, sản phẩm tiến hóa thành **nền tảng quản lý dự án toàn diện** — cạnh tranh trực tiếp với Jira ở phân khúc team kỹ thuật nội bộ, nhưng giữ vững lợi thế khác biệt không thể sao chép: Vendor Cost Intelligence (đơn giá theo vendor × level, reconciliation với timesheet thực tế), Gantt tương tác Microsoft Project-style (Jira không có), Overload Detection + Predictive Warnings (Jira không có), và Capacity Heatmap + 4-week Forecast (Jira không có). Phase 2 bổ sung Agile Board, Sprint Management, và Collaboration layer để đội ngũ kỹ thuật có thể làm việc hoàn toàn trong một tool — thay vì dùng song song cả Jira lẫn Excel lẫn công cụ này.

### What Makes This Special

Thiết kế riêng cho mô hình **multi-vendor outsource**, lấy cảm hứng từ workflow Excel thực tế của người dùng:

- **Microsoft Project-style Gantt view:** Split-panel — task tree phân cấp (Dự Án → Phase → Milestone → Task) bên trái, Gantt calendar bên phải. Các cột: VBS, tên task, loại, effort KH/TT (giờ), ngày bắt đầu/kết thúc KH & TT, % hoàn thành, predecessor, người phụ trách (1 người/task), ưu tiên, trạng thái, ghi chú. Gantt phân màu: KH, thực tế, đang làm, trễ hạn, milestone, hôm nay
- **Effort tracking theo giờ + cảnh báo overload:** Mỗi task được gán số giờ ước tính; hệ thống tính tổng giờ của từng nhân sự theo ngày/tuần và cảnh báo khi vượt ngưỡng (lịch Thứ 2–Thứ 6, 8 giờ/ngày)
- **Multi-role allocation engine:** Một nhân sự có thể giữ nhiều vai trò trên nhiều dự án đồng thời; overload được phát hiện và cảnh báo tự động
- **Per-vendor cost tracking:** Mỗi vendor có đơn giá riêng theo role/level; tổng hợp chi phí thực tế theo vendor và đối chiếu ngân sách
- **Unified multi-project view:** Tất cả dự án hiển thị trong một giao diện, thay thế các Excel sheet rời rạc
- **1-click reporting:** Dashboard tiến độ và chi phí sẵn sàng chia sẻ với cấp trên, không cần tổng hợp thủ công
- **Audit trail:** Lịch sử đầy đủ phân công, thay đổi role và rate theo thời gian

## Project Classification

| | |
|---|---|
| **Loại sản phẩm** | Web Application – Internal Tool |
| **Domain** | Enterprise Project Management |
| **Độ phức tạp** | Medium – multi-role, multi-vendor, Gantt view, hour-level effort tracking |
| **Ngữ cảnh** | Greenfield |
| **Người dùng** | ~20 (Project Manager, quản lý cấp cao) |
| **Tích hợp (Giai đoạn 1)** | Không có |
| **Tích hợp (Tương lai)** | Jira, Confluence *(chưa xác định timeline)* |
| **Lịch làm việc** | Thứ 2 – Thứ 6, 8 giờ/ngày |

## Success Criteria

### User Success

- **Visibility tức thời:** PM có thể kiểm tra bất kỳ lúc nào — ai đang làm task gì, task nào quá hạn, nhân sự nào đang overload — không cần mở Excel hay hỏi thủ công
- **Cảnh báo overload chủ động:** Hệ thống tự động cảnh báo khi bất kỳ nhân sự nào vượt **8 giờ/ngày** hoặc **40 giờ/tuần** (theo lịch Thứ 2–Thứ 6)
- **Báo cáo chi phí cuối tháng:** Tổng hợp chi phí theo 3 chiều — theo vendor, theo dự án, theo nhân sự — trong vài giây, không tổng hợp thủ công
- **Lịch sử thay đổi (Audit Trail):** Mọi thay đổi về task, phân công, effort, chi phí đều được ghi lại với thông tin ai sửa, sửa gì, lúc nào

### Business Success

- **Kiểm soát dự án:** 100% dự án đang chạy có trạng thái tiến độ cập nhật, không có dự án "mất tích" không theo dõi được
- **Kiểm soát con người:** Mọi nhân sự (inhouse + outsource) được phân bổ rõ ràng; overload được phát hiện trước khi ảnh hưởng đến tiến độ
- **Kiểm soát chi phí:** Chi phí thực tế (actual) vs kế hoạch (planned) được đối chiếu theo thời gian thực cho từng dự án và từng vendor
- **Thay thế hoàn toàn Excel:** Sau khi triển khai, không cần dùng file Excel để quản lý dự án, nhân sự, hay tổng hợp báo cáo

### Technical Success

- **Performance:** Dashboard chính tải trong < 3 giây với 20 người dùng đồng thời
- **Độ chính xác:** Tính toán giờ, chi phí và overload chính xác 100% theo quy tắc ngày làm việc Thứ 2–Thứ 6, 8 giờ/ngày
- **Availability:** Hệ thống hoạt động ổn định trong giờ làm việc; không mất dữ liệu
- **Trình duyệt:** Hoạt động đúng trên Chrome, Edge (các phiên bản hiện đại)

### Measurable Outcomes

| Chỉ số | Mục tiêu |
|---|---|
| Thời gian tổng hợp báo cáo chi phí cuối tháng | Từ vài giờ (Excel) → < 1 phút |
| Phát hiện overload nhân sự | Thời gian thực (real-time) |
| Task quá hạn không được phát hiện | 0 |
| Nhân sự không được theo dõi trong hệ thống | 0 |

## Product Scope

### MVP — Giai đoạn 1 (Cần từ ngày đầu)

- **Quản lý dự án:** Tạo/sửa/xóa dự án với phân cấp Dự Án → Phase → Milestone → Task; các trường VBS, tên, loại, effort KH/TT (giờ), ngày bắt đầu/kết thúc KH & TT, % hoàn thành, predecessor, người phụ trách (1 người/task), ưu tiên, trạng thái, ghi chú
- **Gantt Chart view:** Split-panel layout — task tree bên trái, Gantt calendar bên phải; phân màu theo trạng thái (KH, thực tế, đang làm, trễ hạn, milestone, hôm nay)
- **Quản lý nhân sự:** Danh sách inhouse + outsource; mỗi người có thể giữ nhiều vai trò trên nhiều dự án với tỷ lệ phân bổ khác nhau
- **Quản lý vendor:** Danh sách vendor, mỗi vendor có nhiều nhân sự; đơn giá theo role/level
- **Cảnh báo overload:** Tự động khi vượt 8h/ngày hoặc 40h/tuần
- **Cost tracking:** Chi phí planned vs actual; tổng hợp theo vendor, theo dự án, theo nhân sự
- **Dashboard tổng quan:** Trạng thái tất cả dự án, overload alert, tiến độ, chi phí
- **Báo cáo chi phí:** Theo vendor / dự án / nhân sự; xuất được (PDF hoặc Excel)
- **Audit trail:** Lịch sử mọi thay đổi (ai, sửa gì, lúc nào)
- **Multi-project view:** Tất cả dự án trong một giao diện thống nhất

### Growth Features — Giai đoạn 2 (Post-MVP)

- Approval workflow khi gán nhân sự vào task
- Thông báo (email/in-app) khi task sắp quá hạn hoặc overload
- Import dữ liệu từ Excel (migration)
- Phân quyền chi tiết theo vai trò người dùng (PM, Viewer, Admin)

### Vision — Tương lai

- Tích hợp Jira / Confluence
- Mobile app (xem dashboard, cập nhật tiến độ)
- Dự báo chi phí và tiến độ dựa trên dữ liệu lịch sử
- AI gợi ý phân công nhân sự tối ưu

## User Journeys

### Journey 1 — PM: Kiểm tra buổi sáng (Happy Path)

**Nhân vật:** Minh — Project Manager quản lý 3 dự án đồng thời, team 12 người gồm 5 inhouse và 7 outsource từ 2 vendor.

**Hoàn cảnh:** 8:15 sáng thứ Hai. Minh vừa đến văn phòng, cà phê trên tay, mở laptop. Trước đây mỗi sáng anh mất 30–45 phút mở 3 file Excel khác nhau, kiểm tra từng sheet, hỏi thủ công xem ai đang làm gì. Hôm nay lần đầu dùng hệ thống mới.

**Hành trình:**

- **Màn hình chào đón:** Dashboard tổng quan hiện ra — 3 dự án, 2 task đang cảnh báo trễ hạn, 1 nhân sự (Tuấn) được đánh dấu overload 47h/tuần. Minh nhìn thấy toàn bộ tình trạng trong 30 giây.
- **Khám phá vấn đề:** Anh click vào Tuấn — thấy ngay Tuấn đang được phân công 3 task song song ở 2 dự án khác nhau, tổng 47 giờ trong tuần này.
- **Xử lý:** Minh mở Gantt view của Dự án B, kéo 1 task từ Tuấn sang Hùng — hệ thống tự kiểm tra Hùng còn 6h trống trong tuần, không overload. Task được chuyển, timeline tự cập nhật.
- **Kết thúc:** 8:35 sáng — Minh đóng laptop, ra họp. Mọi vấn đề đã được xử lý trong 20 phút thay vì 45 phút.

**Giá trị cốt lõi:** Thay thế 3 file Excel bằng 1 màn hình duy nhất; phát hiện và xử lý overload proactively trước khi ảnh hưởng tiến độ.

---

### Journey 2 — PM: Xử lý cascade khi task trễ (Edge Case)

**Nhân vật:** Minh — cùng bối cảnh trên, nhưng lần này xử lý tình huống phức tạp hơn.

**Hoàn cảnh:** Thứ Tư 2:00 chiều. Hệ thống gửi cảnh báo: Task "Thiết kế API gateway" của Dự án A đang trễ 3 ngày, và task này là predecessor của 4 task khác đang bị block.

**Hành trình:**

- **Nhận cảnh báo:** Thông báo hiện trực tiếp trên dashboard — 1 task trễ đang kéo chậm 4 task phụ thuộc.
- **Phân tích cascade:** Minh click xem dependency chain — Gantt hiển thị màu đỏ toàn bộ chuỗi bị ảnh hưởng: task gốc trễ → 4 task con bị đẩy lùi → deadline dự án bị ảnh hưởng 5 ngày.
- **Tìm giải pháp:** Minh xem capacity của từng người trong team — hệ thống hiển thị ai còn bandwidth tuần này. Phát hiện Lan (outsource từ Vendor A) có 16h trống.
- **Phân công lại:** Minh gán Lan vào task đang trễ để hỗ trợ, điều chỉnh effort estimate từ 24h xuống 16h (vì có thêm người). Hệ thống tự tính lại timeline.
- **Báo cáo:** Minh xuất báo cáo tiến độ tức thì — gửi lên cấp trên kèm kế hoạch giải quyết rõ ràng.

**Giá trị cốt lõi:** Phát hiện tác động dây chuyền trước khi quá muộn; tìm nguồn lực khả dụng dựa trên dữ liệu thực thay vì phỏng đoán.

---

### Journey 3 — PM: Tổng hợp báo cáo chi phí cuối tháng

**Nhân vật:** Minh — cuối tháng, cần báo cáo chi phí cho ban lãnh đạo.

**Hoàn cảnh:** Ngày 30 hàng tháng. Trước đây Minh mất 3–4 giờ tổng hợp chi phí từ nhiều file Excel, đối chiếu invoice từng vendor, tính toán thủ công. Năm ngoái từng sai số 15 triệu do nhầm sheet.

**Hành trình:**

- **Mở báo cáo chi phí:** Minh chọn tháng → hệ thống tổng hợp chi phí theo 3 chiều: theo vendor, theo dự án, theo nhân sự — trong 3 giây.
- **Phát hiện bất thường:** Vendor B báo 24 ngày công trong tháng — hệ thống tự đánh dấu bất thường (tháng này chỉ 22 ngày làm việc theo lịch Thứ 2–Thứ 6). Minh có căn cứ để hỏi lại vendor.
- **Kiểm tra audit trail:** Minh xem lịch sử thay đổi effort của các task trong tháng — phát hiện 1 task bị sửa effort từ 16h lên 24h không có lý do rõ ràng. Có đủ bằng chứng để trao đổi với vendor.
- **Xuất báo cáo:** Click "Xuất PDF" → báo cáo chi phí đầy đủ, sẵn sàng gửi ban lãnh đạo trong 15 phút.

**Giá trị cốt lõi:** Từ 3–4 giờ tổng hợp thủ công xuống 15 phút; audit trail cho phép phát hiện và đối chiếu bất thường chi phí với bằng chứng cụ thể.

---

### Journey 4 — PM/Admin: Onboarding vendor và dự án mới

**Nhân vật:** Minh — tiếp nhận vendor mới (Vendor C) và khởi động dự án mới.

**Hoàn cảnh:** Công ty vừa ký hợp đồng với Vendor C cung cấp 3 nhân sự outsource. Đồng thời cần tạo dự án mới với team kết hợp inhouse + outsource.

**Hành trình:**

- **Thêm vendor mới:** Minh tạo Vendor C, nhập thông tin liên hệ và đơn giá theo role/level (BA: 150k/h, Dev: 200k/h, QA: 120k/h).
- **Thêm nhân sự vendor:** Thêm 3 nhân sự của Vendor C, gán role và rate tương ứng. Hệ thống lưu lịch sử rate để đối chiếu về sau.
- **Tạo dự án mới:** Minh tạo "Dự án ERP Phase 2" với cấu trúc Phase → Milestone → Task. Nhập effort ước tính (giờ) và ngày bắt đầu/kết thúc kế hoạch cho từng task.
- **Phân công nhân sự:** Phân công nhân sự vào từng task. Khi gán người thứ 3 vào task cuối cùng, hệ thống cảnh báo: "Nguyễn Văn A sẽ đạt 44h tuần này — vượt ngưỡng 40h/tuần." Minh điều chỉnh phân bổ trước khi xác nhận.
- **Kết quả:** Dự án mới được thiết lập hoàn chỉnh, toàn bộ nhân sự phân công hợp lệ, không overload ngay từ đầu.

**Giá trị cốt lõi:** Onboarding vendor và dự án mới nhanh, có kiểm tra overload real-time ngay khi phân công, không để vấn đề phát sinh sau khi đã chạy.

---

### Journey 5 — Developer: Daily Board Flow

**Nhân vật:** Tuấn — developer trong team, tham gia Sprint 3 của Dự án ERP Phase 2. Không phải PM, không quản lý Gantt hay cost — chỉ cần biết hôm nay làm gì, hỏi nhanh khi bị block, và log giờ trước khi tắt máy.

**Hoàn cảnh:** Thứ Ba 9:00 sáng. Tuấn mở tool thay vì hỏi PM qua chat "hôm nay mình làm gì tiếp".

**Hành trình:**

- **Mở Board view:** Tuấn thấy Sprint 3 Board — 4 cột: To Do / In Progress / In Review / Done. Card của mình đang nằm ở "In Progress": "Implement Auth API". Còn 2 card ở "To Do" được assign cho Tuấn.
- **Nhận task tiếp theo:** Tuấn kéo card "Viết unit test cho Auth API" từ To Do sang In Progress. Status tự động cập nhật; PM nhìn thấy trên Board ngay lập tức mà không cần Tuấn báo cáo thủ công.
- **Bị block, cần hỏi:** Tuấn không rõ spec cho edge case — token expiry behavior. Thay vì nhắn chat, Tuấn vào card "Implement Auth API", để lại comment: "@Linh token expiry cần throw 401 hay redirect login page? Spec chưa rõ ở đây." Linh nhận notification, trả lời trong 10 phút ngay trên card. Thread comment được lưu lại — ai vào sau cũng thấy quyết định này.
- **Log giờ cuối ngày:** 5:30 chiều, Tuấn vào card, nhập "6h" vào time log. Hệ thống cộng dồn vào actual hours của task; PM không cần hỏi "hôm nay làm được bao nhiêu giờ".
- **Kết thúc:** Tuấn done, kéo card sang "In Review". PM thấy progress update tức thì trên Board mà không cần standup thủ công.

**Giá trị cốt lõi:** Developer tự quản lý workday qua Board; mọi giao tiếp bám vào issue cụ thể (không scatter qua chat); giờ công log tự nhiên trong flow làm việc thay vì báo cáo thêm cuối ngày.

---

### Journey 6 — Scrum Master: Sprint Planning & Review

**Nhân vật:** Linh — Scrum Master của team 6 người, quản lý Sprint 2 tuần. Trước đây dùng Excel để track sprint, copy-paste từ Jira sang báo cáo Word — mỗi sprint review tốn 2 tiếng chuẩn bị.

**Hoàn cảnh:** Thứ Sáu cuối Sprint 3. Sáng: Sprint Review + Retrospective. Chiều: Sprint Planning cho Sprint 4.

**Hành trình — Sprint Review (9:00–10:30):**

- **Mở Sprint Report:** Linh vào Reports → Sprint Report (Sprint 3). Thấy ngay: 14/18 story points done, 4 SP carry-over (2 story, 1 bug chưa xong). Thời gian chuẩn bị: 0 phút — số liệu đã sẵn sàng.
- **Review velocity:** Linh xem Velocity Chart — Sprint 1: 12 SP, Sprint 2: 15 SP, Sprint 3: 14 SP. Velocity trung bình 3 sprint: **13.7 SP**. Đây là baseline để commit Sprint 4.
- **Đóng Sprint:** Linh click "Close Sprint 3". Hệ thống hỏi: 4 issue chưa done — move to Backlog hay Sprint 4? Linh chọn từng cái: 2 story → Backlog, 1 bug → Sprint 4 (critical), 1 story → Backlog.

**Hành trình — Sprint Planning (14:00–15:30):**

- **Tạo Sprint 4:** Linh click "Create Sprint" → đặt tên "Sprint 4 - Auth & Dashboard", ngày bắt đầu/kết thúc, sprint goal: "Complete authentication flow và basic dashboard".
- **Kéo items từ Backlog:** Linh mở Backlog view — tất cả issue chưa assign sprint nằm đây, sắp xếp theo priority. Team estimate story points live (Linh nhập SP trực tiếp trên card). Kéo drag-drop 12 issue vào Sprint 4. Hệ thống hiện tổng: **16 SP** — nhắc nhở velocity baseline là 13.7 SP.
- **Cảnh báo overload:** Khi kéo thêm 1 story nữa (2 SP), hệ thống highlight: "Tuấn đã có 40h task trong Sprint 4 — thêm story này sẽ đẩy lên 48h (overload)." Linh reassign story đó cho Hùng còn bandwidth.
- **Commit Sprint:** Linh click "Start Sprint 4". Board tự reset với 16 SP chia đều cho team. Mọi người thấy ngay task của mình sáng thứ Hai.

**Giá trị cốt lõi:** Sprint Planning từ 2 tiếng chuẩn bị thủ công xuống 90 phút làm thật; velocity data có sẵn không cần tính Excel; overload được phát hiện ngay trong planning thay vì phát sinh giữa sprint.

---

### Journey Requirements Summary

| Khả năng cần có | Journey liên quan |
|---|---|
| Dashboard tổng quan với overload alert và task trễ | J1, J2 |
| Gantt view với phân màu trạng thái task | J1, J2 |
| Tính toán capacity theo giờ (Thứ 2–Thứ 6, 8h/ngày) | J1, J2, J4, J6 |
| Cảnh báo overload real-time khi phân công | J1, J4, J6 |
| Dependency tracking và cascade impact visualization | J2 |
| Báo cáo chi phí đa chiều (vendor/dự án/nhân sự) | J3 |
| Phát hiện bất thường chi phí vs lịch làm việc | J3 |
| Audit trail đầy đủ (ai sửa, sửa gì, lúc nào) | J3 |
| Xuất báo cáo PDF/Excel | J3 |
| Quản lý vendor và đơn giá theo role/level | J4 |
| Tạo dự án với cấu trúc phân cấp (Phase/Milestone/Task) | J4 |
| Phân công nhân sự nhiều vai trò trên nhiều dự án | J1, J2, J4 |
| Agile Board (Scrum/Kanban) với drag-drop card | J5, J6 |
| Comment @mention trên issue + notification | J5 |
| Time log per issue (developer self-service) | J5 |
| Sprint Management: tạo/đóng sprint, move issues | J6 |
| Velocity Chart và Sprint Report | J6 |
| Backlog grooming với drag-drop vào sprint | J6 |
| Story Points estimation và tổng SP khi planning | J6 |

## Domain-Specific Requirements

### Dữ liệu & Phân quyền

- MVP: Tất cả PM truy cập toàn bộ dữ liệu (vendor rates, chi phí nhân sự, giờ công) — không phân quyền chi tiết
- Data model được thiết kế sẵn sàng hỗ trợ phân quyền ở giai đoạn sau (không cần refactor)

### Công thức tính chi phí

- **Hourly Rate = Monthly Rate ÷ 176h** (chuẩn cố định: 22 ngày × 8h/ngày)
- **Chi phí = Giờ thực tế (confirmed) × Hourly Rate**
- Rate được thiết lập theo **vendor × level** (ví dụ: Vendor A, Senior Dev = 20 triệu/tháng)
- Rate thay đổi: chỉ ở ranh giới tháng, không thay đổi giữa tháng
- Lịch sử rate lưu dạng immutable (vendor, level, rate, tháng áp dụng) — có thể reconstruct chi phí tại bất kỳ thời điểm nào
- Nhân sự vắng mặt: giờ không log = chi phí = 0
- Partial month (người join giữa tháng): tự nhiên xử lý — log ít giờ hơn → chi phí thấp hơn theo tỷ lệ thực tế

### Cơ chế ghi nhận giờ thực tế (Actual Hours)

**Nguyên tắc kiến trúc:** `actual_hours` trên task là **computed field** — tổng hợp từ các bản ghi TimeEntry — không phải field nhập trực tiếp. Mọi thay đổi giờ thực tế đều tạo TimeEntry mới (immutable log), đảm bảo audit trail đầy đủ.

**Hai tầng ghi nhận:**

**Tầng 1 — Cuối tháng: Vendor CSV Import (nguồn chính)**
- Khi nhận được timesheet từ vendor, PM import file (CSV/Excel)
- Hệ thống hiển thị mapping UI: PM map cột từ file vendor → fields trong hệ thống (mỗi vendor lưu một mapping template riêng)
- PM review diff giữa dữ liệu import và estimate hiện có → approve → lock
- Sau khi lock: dữ liệu trở thành **vendor-confirmed**, dùng làm nguồn chính cho cost report chính thức
- File gốc được lưu trữ để đối chiếu khi có tranh chấp

**Tầng 2 — Giữa tháng: Bulk Timesheet Grid (bổ sung liên tục)**
- PM có thể nhập/điều chỉnh giờ thực tế qua grid: người × ngày (hoặc tuần) × task
- Dùng để overload detection và 4-week forecast vận hành liên tục giữa các lần import vendor
- Áp dụng cho cả nhân sự inhouse (không có vendor timesheet)
- PM nhập theo tuần, không bắt buộc nhập theo ngày

**Data status tracking:**

Mỗi TimeEntry có trạng thái rõ ràng:
- `estimated` — chưa có actual, dùng estimate làm proxy tạm thời
- `pm-adjusted` — PM đã nhập thủ công qua bulk grid
- `vendor-confirmed` — đã reconcile với timesheet vendor và được lock

**Quy tắc hiển thị báo cáo:**
- Báo cáo cost chính thức: chỉ dùng `vendor-confirmed` (hoặc `pm-adjusted` cho inhouse)
- Dashboard overload và forecast: dùng tất cả trạng thái (bao gồm `estimated`) để có dữ liệu real-time
- Mọi report hiển thị % dữ liệu đã confirmed vs estimated để PM biết độ tin cậy

**Validation rules:**
- Cảnh báo nếu giờ thực tế (pm-adjusted) chênh lệch > 20% so với estimate mà không có ghi chú lý do
- Không cho phép nhập > 16h/ngày cho một người (hard cap, tránh nhập nhầm)
- Audit log ghi rõ `entered_by` (PM user) và `resource_id` (người thực tế làm việc) — hai field tách biệt

### Lịch ngày lễ (Configurable)

- Admin có thể thêm / sửa / xóa ngày lễ trong hệ thống (không hardcode cố định)
- Ngày lễ được loại khỏi tính toán overload và capacity planning
- Ngày lễ hiển thị trên Gantt calendar với màu phân biệt (visual indicator)
- **Deadline task tự động được đẩy forward** khi task kết thúc trùng vào hoặc span qua ngày lễ (tương tự Microsoft Project behavior)

### Lưu trữ dữ liệu

- Dữ liệu lưu trữ dài hạn, không tự xóa
- Dự án cũ, nhân sự đã nghỉ việc, vendor ngừng hợp tác: dữ liệu được giữ nguyên, đánh dấu trạng thái inactive

### Audit Trail

- Mọi thay đổi (task, effort, phân công, rate) được ghi log đầy đủ: ai sửa, sửa gì, thời điểm
- Không retroactive adjustment — nếu cần đính chính, tạo amendment entry mới (không overwrite lịch sử cũ)

## Innovation & Novel Patterns

### Detected Innovation Areas

1. **Predictive Overload Warning** — Cảnh báo tác động overload TRƯỚC khi xác nhận assign, với traffic light spectrum (xanh/vàng/cam/đỏ theo % capacity còn lại). PM thấy mức độ tác động ngay trên dialog xác nhận; hệ thống có thể chặn mềm nếu PM không chủ đích override.

2. **Capacity-First Assignment View** — View đảo ngược: heatmap (person × tuần) kết hợp thông tin chi phí vendor, cho phép PM quyết định dựa trên cả capacity lẫn cost trong cùng 1 view. Thay vì "chọn task → tìm người", PM có thể "xem ai rảnh → assign vào slot phù hợp."

3. **Smart Assignment Suggestion** — Khi tạo/sửa task, hệ thống tự đề xuất người assign tối ưu dựa trên 3 tiêu chí: role match + capacity còn lại + cost. PM approve hoặc override — không cần nhớ ai đang rảnh hay vendor nào rẻ hơn.

4. **4-Week Capacity Forecast** — Rolling forecast 4 tuần cross-project cho từng nhân sự. PM thấy bottleneck trước khi xảy ra, không chỉ phản ứng với overload hiện tại.

### Market Context & Competitive Landscape

MS Project và các enterprise PM tools xử lý overload detection sau khi đã assign. Smart suggestion kết hợp predictive capacity ở web-based internal tool cùng phân khúc hiếm gặp. Đây là execution innovation, không phải technology innovation — nhưng có giá trị thực tế cao với PM quản lý mixed workforce đa vendor.

### Validation Approach

- **Predictive overload:** Đo số lần PM phải undo/reassign trước và sau khi có feature
- **Smart suggestion:** Track tỷ lệ PM accept vs override gợi ý của hệ thống — nếu override rate > 50% thì algorithm cần điều chỉnh
- **Capacity forecast:** Đo số lần PM phát hiện bottleneck proactively (từ forecast) vs reactive (sau khi đã overload)

### Risk Mitigation

- Smart suggestion có thể gợi ý sai trong giai đoạn đầu khi dữ liệu giờ công chưa đầy đủ → cần onboarding period, feature nên là "suggestion" không phải "auto-assign"
- 4-week forecast phụ thuộc vào effort estimate chính xác → chất lượng forecast tỷ lệ thuận với chất lượng data đầu vào; cần hướng dẫn PM về tầm quan trọng của estimate đúng

## Web Application – Yêu Cầu Kỹ Thuật

### Project-Type Overview

Hệ thống là **Single Page Application (SPA)** xây dựng bằng **Angular**, phục vụ ~20 người dùng nội bộ (PM và cấp quản lý). Ứng dụng chạy hoàn toàn trên desktop browser, không có yêu cầu mobile, SEO, hay accessibility nâng cao. Dữ liệu được load qua REST API; các tính toán overload được thực hiện client-side để đảm bảo tốc độ phản hồi tức thì.

### Browser Matrix

Chrome 100+ và Edge 100+ (Chromium) là primary — bắt buộc hoạt động hoàn hảo. Firefox, Safari không hỗ trợ chính thức. *(Chi tiết: NFR-C1 đến NFR-C3)*

### Technical Architecture Considerations

**Frontend Framework:** Angular (latest stable)
- Angular CDK drag-drop cho Gantt interaction (kéo thả task, điều chỉnh timeline)
- RxJS cho state management, xử lý async data streams
- Component library Angular Material hoặc tương đương cho UI components nhất quán

**Data Layer:**
- Polling-based data refresh (interval configurable, khuyến nghị 30–60 giây)
- Không dùng WebSocket/real-time push (tránh độ phức tạp infrastructure không cần thiết ở giai đoạn này)
- Overload check: tính toán client-side trên dữ liệu đã load — phản hồi tức thì (<200ms) khi PM thay đổi assignment

**API Design:**
- RESTful API (JSON)
- Token-based authentication (JWT)
- Pagination cho danh sách lớn (nhân sự, task log, audit trail)

### Authentication Model

**Giai đoạn 1 (MVP): Local username/password**
- Hệ thống tự quản lý tài khoản người dùng (email + mật khẩu hashed)
- JWT token, session timeout hợp lý (ví dụ: 8 giờ — hết giờ làm việc tự logout)
- Admin tạo/vô hiệu hóa tài khoản

**Thiết kế hướng tới tương lai:**
- Data model và auth middleware được tách biệt rõ ràng để có thể thêm SSO (Azure AD / Google Workspace) sau mà không cần refactor toàn bộ
- Phase 2: Nếu công ty triển khai SSO, chỉ cần thêm OAuth2/OIDC provider mà không thay đổi authorization logic

### Performance Targets & Constraints

Mục tiêu performance, viewport, và browser compatibility được định nghĩa trong NFR-P1 đến NFR-P6 và NFR-C1 đến NFR-C3. SEO, SSR, và WCAG không áp dụng — ứng dụng nội bộ desktop-only.

### Implementation Considerations

**PDF Export:**
- Server-side generation (không dùng browser print) để đảm bảo layout nhất quán và có thể schedule/batch trong tương lai
- Thư viện khuyến nghị: Puppeteer (headless Chrome) hoặc tương đương

**Gantt Rendering:**
- Custom Angular component dựa trên Angular CDK, không phụ thuộc vào thư viện Gantt thương mại (tránh license lock-in)
- Canvas rendering nếu > 500 task để đảm bảo performance

**State Management:**
- RxJS BehaviorSubject/Store pattern (hoặc NgRx nếu team quen thuộc)
- Cache dữ liệu capacity/assignment client-side để overload check instant

**Deployment:**
- Web server nội bộ (on-premise hoặc private cloud) — không deploy public
- Không có yêu cầu CDN hay multi-region

## Project Scoping & Phased Development

### MVP Strategy & Philosophy

**Triết lý:** Full Product Delivery với staged rollout nội bộ — xây dựng sản phẩm hoàn chỉnh ngay từ đầu, không cắt tính năng để ra sớm. Tuy nhiên release theo từng sprint để lấy feedback liên tục.

**Lý do:** Sản phẩm nội bộ với ~20 người dùng đã xác định rõ nhu cầu; dữ liệu vận hành thực tế (vendor rate, giờ công, chi phí) yêu cầu hệ thống phải đủ chính xác ngay từ ngày đầu. Partial system không thể thay thế Excel — người dùng sẽ không migrate. Với 20 users ngồi cùng tòa nhà, staged rollout trong Phase 1 cho phép lấy feedback liên tục thay vì build mù quáng.

**Resource Requirements:** Team 3–4 developers (2 frontend Angular chuyên sâu cho Gantt, 1–2 backend), 1 PM, 1 QA.

**Staged Rollout trong Phase 1:**
- Sprint 1–2: Core Gantt + Assignment Engine → PMs dùng thật
- Sprint 3–4: Overload Warning + Capacity Heatmap → layer lên
- Sprint 5–6: 4-Week Forecast + Cost Reports + Export → hoàn thiện
- Sprint 7+: Smart Assignment Suggestion → enable sau khi có 4–6 tuần actual data

### Phase 1 — Initial Full Release

**Core User Journeys Covered:** Tất cả 4 journeys đã định nghĩa.

**Must-Have Capabilities:**

**Quản lý Dự án & Task**
- Cấu trúc phân cấp: Dự Án → Phase → Milestone → Task
- Task fields đầy đủ: VBS, tên, loại, Estimate (KH), Actual (TT — computed từ TimeEntry), ngày KH & TT, % hoàn thành (manual), predecessor, assignee (1 người), ưu tiên, trạng thái, ghi chú
- **Carry-over balance khi onboard:** PM nhập "hours spent to date" và "remaining estimate" cho dự án đang chạy khi migrate từ Excel — không yêu cầu re-enter toàn bộ lịch sử

**Gantt Chart View**
- Split-panel: task tree trái + Gantt calendar phải
- Dual bar: KH (planned) và TT (actual) song song theo màu phân biệt
- Drag-drop task (thay đổi ngày, chuyển assignee), dependency arrows, holiday overlay

**Quản lý Nhân sự & Vendor**
- Inhouse + outsource với multi-role cross-project
- Vendor management: rate theo vendor × level, immutable rate history

**Actual Hours Logging — Hai tầng**
- Monthly: Vendor CSV import → mapping template → reconcile → lock (vendor-confirmed)
- Mid-month: Bulk timesheet grid (người × tuần) → pm-adjusted
- Data status: `estimated | pm-adjusted | vendor-confirmed`

**Holiday Calendar**
- Admin-configurable; auto-shift deadline; Gantt overlay

**Overload Warning + Predictive Overload**
- Standard: >8h/ngày, >40h/tuần, client-side <200ms
- Predictive: traffic light (xanh/vàng/cam/đỏ) trước khi confirm assign

**Capacity-First Assignment View**
- Heatmap person × tuần kết hợp chi phí vendor trong cùng 1 view

**4-Week Capacity Forecast**
- Rolling cross-project, server-side precompute

**Smart Assignment Suggestion** *(enable Sprint 7+ sau khi có data)*
- Rule-based: role match → capacity → cost
- Transparency bắt buộc: top 3 candidates + reasoning rõ ràng
- Track acceptance rate; nếu <40% → algorithm cần tune

**Cost Tracking & Reporting**
- Planned vs Actual per vendor/dự án/nhân sự/tháng
- Phát hiện bất thường tự động
- Export: PDF + Excel
- Report label rõ % data confirmed vs estimated

**Notifications cơ bản (Phase 1)**
- Email digest hàng tuần: overload alerts + task sắp trễ (cron job đơn giản)
- Đảm bảo PM nhận được cảnh báo kể cả khi không mở tool

**Authentication & Audit**
- Local username/password, JWT 8h session, ~20 tài khoản PM
- Audit trail: `entered_by` (PM) và `resource_id` (người làm việc) tách biệt

### Phase 2 — Agile & Collaboration Core

**Mục tiêu:** Biến sản phẩm từ PM-only tool thành toàn bộ team tool — developer, QA, và Scrum Master đều có workspace riêng. Đạt feature parity với Jira ở lớp Agile cơ bản.

**Agile Board:**
- Scrum Board (theo Sprint) và Kanban Board (continuous flow)
- Drag-drop card giữa các cột status; swimlane theo assignee hoặc epic
- WIP limit cấu hình được trên Kanban board

**Sprint Management & Backlog:**
- Tạo Sprint với tên, goal, ngày bắt đầu/kết thúc
- Backlog grooming: kéo issue từ backlog vào sprint (drag-drop hoặc bulk select)
- Story Points nhập trực tiếp trên card; tổng SP hiển thị khi planning
- Sprint velocity tự động tính từ các sprint đã đóng

**Issue Collaboration:**
- Comment threaded trên mỗi issue (@mention gửi notification)
- File attachment (ảnh, tài liệu, log file) đính kèm issue
- Watcher: subscribe nhận notification khi issue thay đổi
- Issue linking: "blocks / is blocked by / relates to / duplicates"

**Issue Types cấu hình được:**
- Types mặc định: Epic, Story, Task, Bug, Sub-task
- Admin có thể thêm custom type với icon và workflow riêng

**Basic Workflow Engine:**
- Mỗi issue type có workflow riêng (chuỗi status + transitions có tên)
- Transition có thể có điều kiện đơn giản (ví dụ: không được close nếu còn sub-task chưa done)
- Tối đa 10 status per workflow ở giai đoạn này

**Labels, Components, Versions:**
- Labels: tag tự do, nhiều label per issue, filter được
- Components: nhóm issue theo phần hệ thống (backend, frontend, infra...)
- Versions/Releases: gán issue vào release; xem progress per release

**Basic Search & Saved Filters:**
- Full-text search trong tên + description issue
- Filter theo: assignee, status, type, priority, sprint, label, component, version
- Lưu bộ filter thường dùng với tên; chia sẻ filter với team

**Hoạt động song song với Phase 2 (Operations):**
- Approval workflow khi gán nhân sự vào task
- Full notification system (in-app, email per-event)
- Excel/CSV import nâng cao (full migration tool cho historical data)
- Granular permissions: PM / Viewer / Admin roles
- SSO integration (Azure AD / Google Workspace)
- Smart Suggestion nâng cao sau khi có acceptance rate data để tune

### Phase 3 — Advanced Platform

**Mục tiêu:** Đạt full Jira parity và vượt Jira ở 3 điểm khác biệt: cost intelligence, Gantt tương tác, và predictive capacity. Mở nền tảng API cho tích hợp bên ngoài.

**Configurable Workflow Engine (full):**
- Drag-drop workflow builder: tạo status, transition, và điều kiện tùy ý
- Transition validators: required fields, approval step, script condition
- Post-functions: tự động update field, gán lại assignee, trigger notification khi transition

**Advanced Search (JQL-like):**
- Query language dạng text: `project = "ERP" AND status != Done AND assignee = currentUser()`
- Auto-complete gợi ý field/operator khi gõ
- Lưu query thành filter; dùng filter trong dashboard widget
- Export kết quả search ra CSV/Excel

**Custom Fields:**
- Admin tạo custom field: text, number, date, dropdown, multi-select, user picker
- Gán custom field cho issue type cụ thể hoặc toàn hệ thống
- Custom field hiển thị trên card, detail view, và báo cáo

**Agile Reports:**
- Burndown Chart: per sprint, so sánh ideal line vs actual
- Velocity Chart: SP hoàn thành theo từng sprint (12 sprint gần nhất)
- Cumulative Flow Diagram (CFD): thấy bottleneck theo status qua thời gian
- Sprint Report: issue completed, not completed, và added mid-sprint

**Roadmap View (Epic-level timeline):**
- Timeline view chỉ show Epic bars (không phải task-level như Gantt)
- Kéo-thả Epic để điều chỉnh kế hoạch high-level
- Màu phân biệt theo team/component
- Link trực tiếp từ Epic bar xuống sprint/issue detail

**Automation Rules:**
- Trigger: issue created/updated/transitioned, sprint started/closed, comment added
- Condition: field value, JQL match, user membership
- Action: update field, assign user, move issue, send notification, create sub-task
- Audit log cho mỗi rule execution

**Permission Schemes (fine-grained):**
- Permission per project: Browse, Create, Edit, Delete, Transition, Comment, Manage Sprints
- Role-based: Project Admin, Developer, Reporter, Viewer
- Override per user nếu cần

**Webhooks + API Platform:**
- Outgoing webhooks: gửi event JSON đến URL bên ngoài (issue created/updated, sprint events)
- REST API public với API key authentication
- API docs tự sinh (OpenAPI/Swagger)
- Tích hợp Jira / Confluence (sync task status 2 chiều)

**Bulk Operations:**
- Bulk edit: chọn nhiều issue → đổi status, assignee, priority, label cùng lúc
- Bulk delete, bulk move (giữa project/sprint)
- Bulk export ra CSV/Excel với custom column chọn

**Platform nâng cao song hành Phase 3:**
- Mobile app (dashboard, cập nhật % hoàn thành, overload alerts)
- AI-powered assignment optimization (ML thay vì rule-based)
- Predictive analytics (dự báo cost và deadline từ velocity lịch sử)

### Risk Mitigation Strategy

| Rủi ro | Mức độ | Giảm thiểu |
|---|---|---|
| Custom Gantt complexity — ước tính 5–8 sprints | Cao | Spike/prototype riêng Sprint 1; dependency arrows có thể defer nếu cần |
| Smart Suggestion accuracy khi data chưa đủ | Cao | Feature flag: enable sau 4–6 tuần có actual data; không auto-enable |
| PM không cập nhật actual hours đều đặn | Cao | Bulk grid nhanh; vendor CSV import giảm manual entry; dashboard nhắc nhở |
| Migration barrier — không có lịch sử Excel | Cao | Carry-over balance input khi onboard active projects |
| Vendor timesheet format không chuẩn | Trung bình | Mapping templates per vendor; lưu file gốc; reconciliation workflow |
| Estimate-as-actual dùng cho forecast trước khi có confirmed data | Trung bình | Report label rõ % confirmed vs estimated; cảnh báo >20% deviation |

## Functional Requirements

### Authentication & User Management

- FR1: PM có thể đăng nhập hệ thống bằng email và mật khẩu
- FR2: Hệ thống tự động logout người dùng sau 8 giờ không hoạt động
- FR3: Admin có thể tạo, vô hiệu hóa và khôi phục tài khoản người dùng

### Navigation & Application Shell

- FR4: Người dùng có thể ẩn/hiện sidebar navigation (toggle menu) để tối đa hóa không gian làm việc
- FR5: Hệ thống ghi nhớ trạng thái sidebar (collapsed/expanded) giữa các phiên làm việc

### Dashboard & Tổng quan

- FR6: PM có thể xem tổng quan tất cả dự án đang chạy trong một màn hình thống nhất
- FR7: PM có thể xem danh sách task đang trễ hạn trực tiếp trên dashboard
- FR8: PM có thể xem cảnh báo overload nhân sự trực tiếp trên dashboard

### Quản lý Dự án & Task

- FR9: PM có thể tạo, chỉnh sửa và xóa dự án với cấu trúc phân cấp Dự Án → Phase → Milestone → Task
- FR10: PM có thể nhập đầy đủ thông tin task: VBS, tên, loại, effort ước tính, ngày KH/TT, % hoàn thành, predecessor, assignee, ưu tiên, trạng thái, ghi chú
- FR11: PM có thể thiết lập mối quan hệ predecessor giữa các task trong cùng dự án
- FR12: PM có thể cập nhật % hoàn thành của task thủ công
- FR13: PM có thể nhập carry-over balance (giờ đã dùng + remaining estimate) khi onboard dự án đang chạy từ Excel

### Gantt Chart View

- FR14: PM có thể xem dự án theo Gantt view dạng split-panel (task tree bên trái, Gantt calendar bên phải)
- FR15: PM có thể xem dual bar KH/TT song song với màu phân biệt trên Gantt
- FR16: PM có thể kéo-thả task trên Gantt để thay đổi ngày bắt đầu/kết thúc
- FR17: PM có thể xem dependency arrows giữa các task liên quan trên Gantt
- FR18: PM có thể xem cascade impact khi task trễ — toàn bộ dependency chain bị ảnh hưởng được highlight rõ ràng
- FR19: Hệ thống hiển thị ngày lễ trên Gantt calendar với màu phân biệt

### Quản lý Nhân sự & Vendor

- FR20: Admin có thể thêm, sửa và vô hiệu hóa nhân sự (inhouse và outsource)
- FR21: PM có thể gán nhân sự vào nhiều role trên nhiều dự án đồng thời với tỷ lệ phân bổ khác nhau
- FR22: Admin có thể thêm, sửa và vô hiệu hóa vendor với thông tin liên hệ
- FR23: Admin có thể cấu hình đơn giá theo vendor × level với lịch sử rate immutable
- FR24: Hệ thống giữ nguyên toàn bộ dữ liệu nhân sự/vendor đã nghỉ/ngừng hợp tác (inactive thay vì xóa)

### Ghi nhận Giờ công (Time Tracking)

- FR25: PM có thể import timesheet vendor từ file CSV/Excel với mapping template riêng theo từng vendor
- FR26: PM có thể review diff giữa dữ liệu import và estimate hiện có, sau đó approve và lock
- FR27: PM có thể nhập/điều chỉnh giờ thực tế qua bulk timesheet grid (người × tuần × task)
- FR28: Hệ thống tracking trạng thái từng time entry: `estimated`, `pm-adjusted`, `vendor-confirmed`
- FR29: Mọi báo cáo hiển thị rõ tỷ lệ % dữ liệu confirmed vs estimated

### Overload Detection & Capacity Planning

- FR30: Hệ thống cảnh báo real-time khi nhân sự vượt 8h/ngày hoặc 40h/tuần ngay khi PM thực hiện assignment
- FR31: Hệ thống hiển thị predictive overload với traffic light (xanh/vàng/cam/đỏ) TRƯỚC khi PM confirm assign
- FR32: PM có thể xem capacity heatmap (người × tuần) kết hợp thông tin chi phí vendor trong cùng 1 view
- FR33: PM có thể xem 4-week rolling capacity forecast cross-project cho từng nhân sự

### Smart Assignment Suggestion

- FR34: Hệ thống gợi ý top 3 candidates khi tạo/sửa task dựa trên role match, capacity còn lại, và cost
- FR35: PM có thể accept hoặc override gợi ý với lý do rõ ràng; hệ thống track tỷ lệ acceptance

### Cost Tracking & Reporting

- FR36: PM có thể xem chi phí planned vs actual theo vendor, dự án, và nhân sự
- FR37: Hệ thống tự động phát hiện và đánh dấu bất thường chi phí so với lịch làm việc thực tế
- FR38: PM có thể xuất báo cáo chi phí tháng dưới dạng PDF và Excel
- FR39: PM có thể xem và xuất báo cáo tiến độ dự án tổng hợp

### Lịch ngày lễ

- FR40: Admin có thể thêm, sửa và xóa ngày lễ trong hệ thống
- FR41: Hệ thống tự động đẩy deadline task về phía trước khi task kết thúc trùng vào ngày lễ

### Audit Trail & History

- FR42: Hệ thống ghi log đầy đủ mọi thay đổi (ai sửa, sửa gì, thời điểm) cho task, effort, phân công, và rate
- FR43: PM có thể xem lịch sử thay đổi của bất kỳ task, time entry, hoặc assignment nào
- FR44: Hệ thống không cho phép overwrite lịch sử — mọi đính chính tạo amendment entry mới

### Notifications

- FR45: Hệ thống gửi email digest hàng tuần cho PM với danh sách overload alerts và task sắp trễ

## Non-Functional Requirements

### Performance

- NFR-P1: Dashboard tổng quan tải trong < 3 giây với 20 người dùng đồng thời
- NFR-P2: Gantt chart render dự án ~100 task trong < 2 giây
- NFR-P3: Overload check khi gán nhân sự phản hồi trong < 200ms (client-side calculation)
- NFR-P4: Báo cáo chi phí tháng tạo xong trong < 3 giây
- NFR-P5: Xuất báo cáo PDF hoàn thành trong < 10 giây (server-side generation)
- NFR-P6: Baseline đo lường: 20 người dùng đồng thời, ~10 dự án, ~100 task/dự án, ~50 nhân sự

### Security

- NFR-S1: Mật khẩu lưu trữ dạng hash (bcrypt hoặc tương đương) — không bao giờ lưu plaintext
- NFR-S2: Mọi communication client ↔ server sử dụng HTTPS
- NFR-S3: JWT token có thời hạn 8 giờ; hết hạn tự động yêu cầu đăng nhập lại
- NFR-S4: Tất cả API endpoints yêu cầu valid authentication token — không có endpoint nào trả dữ liệu nhạy cảm mà không xác thực
- NFR-S5: Dữ liệu tài chính (vendor rate, chi phí, giờ công) chỉ truy cập được bởi người dùng đã xác thực

### Reliability

- NFR-R1: Hệ thống hoạt động ổn định trong giờ làm việc (Thứ 2–Thứ 6, 8:00–17:00)
- NFR-R2: Không mất dữ liệu đã được lưu — mọi write operation được confirm ở server trước khi phản hồi thành công về UI
- NFR-R3: Audit trail là immutable — không có cơ chế xóa hoặc chỉnh sửa log history ở tầng nào

### Data Integrity

- NFR-D1: Tính toán overload chính xác 100% theo lịch Thứ 2–Thứ 6, 8h/ngày, loại trừ ngày lễ đã cấu hình
- NFR-D2: Tính toán chi phí chính xác theo công thức cố định: Hourly Rate = Monthly Rate ÷ 176h; Chi phí = Actual Hours × Hourly Rate
- NFR-D3: Hệ thống từ chối nhập giờ công > 16h/ngày cho một nhân sự (hard validation, tránh nhập nhầm)

### Browser Compatibility

- NFR-C1: Hoạt động đúng trên Google Chrome 100+ và Microsoft Edge 100+ (Chromium-based)
- NFR-C2: Viewport tối thiểu 1280×768px; Gantt view được tối ưu tại 1440px+; scroll ngang được phép
- NFR-C3: Không hỗ trợ chính thức Firefox, Safari, hoặc mobile browser

---

## Jira Feature Parity — Phase 2 & Beyond

### Tại sao mở rộng

Phase 1 giải quyết đúng pain point cốt lõi: thay thế Excel cho PM quản lý multi-vendor workforce. Nhưng khi PM đã dùng tool này để quản lý dự án, câu hỏi tự nhiên xuất hiện từ team kỹ thuật: *"Tại sao chúng tôi vẫn phải dùng Jira riêng để track task hàng ngày, rồi lại báo cáo sang tool kia cho PM?"*

Sự phân mảnh này tạo ra overhead không cần thiết: double-entry (nhập task ở Jira, nhập giờ ở tool PM), context-switching liên tục, và mất traceability (comment, quyết định về task nằm ở Jira — cost và capacity nằm chỗ khác). Với team nội bộ ~20 người và tham vọng mở rộng thêm user trong tương lai, việc converge về một nền tảng duy nhất có ROI rõ ràng.

**Quyết định:** Mở rộng sản phẩm thành full project management platform. Mọi tính năng Jira sẽ được build — không phải để cạnh tranh Jira trên thị trường đại chúng, mà để loại bỏ sự phụ thuộc vào Jira trong team nội bộ và tận dụng dữ liệu tập trung để phát huy competitive advantage riêng.

### Feature Categories cần đạt được

| Category | Jira tương đương | Trạng thái |
|---|---|---|
| Issue Tracking (create/edit/assign/comment) | Core Jira | Phase 2 |
| Agile Board (Scrum + Kanban) | Jira Software Board | Phase 2 |
| Sprint Management + Backlog | Jira Sprints | Phase 2 |
| Issue Types + Basic Workflow | Jira Issue Types | Phase 2 |
| Labels / Components / Versions | Jira Labels & Components | Phase 2 |
| Story Points + Agile Estimation | Jira Story Points | Phase 2 |
| Basic Search + Saved Filters | Jira Filters | Phase 2 |
| Configurable Workflow Engine (full) | Jira Workflow Designer | Phase 3 |
| Advanced Search (JQL-like) | JQL | Phase 3 |
| Custom Fields | Jira Custom Fields | Phase 3 |
| Agile Reports (Burndown, Velocity, CFD) | Jira Reports | Phase 3 |
| Roadmap View (Epic timeline) | Jira Roadmap | Phase 3 |
| Automation Rules | Jira Automation | Phase 3 |
| Permission Schemes | Jira Permission Schemes | Phase 3 |
| Webhooks + REST API | Jira API | Phase 3 |
| Bulk Operations | Jira Bulk Edit | Phase 3 |

### Differentiation Strategy: "Jira + Cost Intelligence + Gantt"

Jira giỏi ở issue tracking và agile workflow. Jira kém ở ba điểm mà sản phẩm này làm tốt hơn bản chất:

**1. Vendor Cost Intelligence (Jira không có)**
Jira không biết vendor nào cung cấp người nào với giá bao nhiêu. Sản phẩm này track đơn giá theo vendor × level, reconcile với timesheet thực tế của vendor, và tự động flag bất thường. Khi issue được close, actual cost đã có sẵn — không cần bảng Excel riêng.

**2. Gantt tương tác Microsoft Project-style (Jira chỉ có basic Roadmap)**
Jira Roadmap chỉ show Epic-level, không có dual bar KH/TT, không có dependency arrows chi tiết, không có drag-drop trên task level. Sản phẩm này có full Gantt — PM thấy tiến độ thực tế vs kế hoạch tại từng task, kéo thả để reschedule, thấy cascade impact ngay lập tức.

**3. Overload Detection + Capacity Intelligence (Jira không có)**
Jira không tính giờ làm việc của người theo lịch thực tế. Sản phẩm này phát hiện overload real-time, dự báo capacity 4 tuần, và gợi ý người assign tối ưu dựa trên capacity còn lại + cost. PM không cần hỏi "ai còn trống tuần này" — hệ thống đã biết.

**Tóm lại:** Với team quản lý multi-vendor outsource workforce, "Jira + Excel" là combo phổ biến nhưng kém hiệu quả. Sản phẩm này là combo đó trong một tool duy nhất, với intelligence layer mà cả Jira lẫn Excel riêng lẻ không thể cung cấp.

---

## Updated Success Criteria — Phase 2 Metrics

*(Bổ sung vào Success Criteria ban đầu — đo sau 30 ngày kể từ khi Phase 2 go-live)*

### Adoption Metrics

| Chỉ số | Mục tiêu 30 ngày | Lý do đo |
|---|---|---|
| % PM dùng Board view ít nhất 3 lần/tuần | ≥ 60% | Nếu < 60%, Board chưa đủ hữu ích để thay Gantt trong daily workflow |
| % developer log work qua issue (không qua PM) | ≥ 50% | Self-service time log — giảm overhead PM hỏi-đáp |
| % sprint planning hoàn tất trong tool (không Excel) | ≥ 80% | Nếu Scrum Master vẫn dùng Excel để plan sprint, tool chưa thay thế được |

### Collaboration Metrics

| Chỉ số | Mục tiêu 30 ngày | Lý do đo |
|---|---|---|
| Số comment trung bình per issue sau tháng đầu | ≥ 2 comment/issue | < 2 nghĩa là team vẫn communicate qua chat/email thay vì bám vào issue |
| % issue có ít nhất 1 @mention | ≥ 40% | @mention = collaboration xảy ra trong tool, không ngoài tool |
| Số attachment đính kèm issue (file, ảnh, log) | Track as baseline | Chưa có target — đo để hiểu usage pattern tháng đầu |

### Sprint Cadence Metrics

| Chỉ số | Mục tiêu | Lý do đo |
|---|---|---|
| % team hoàn tất sprint planning trong tool | ≥ 80% sau Sprint 2 | Sprint 1 có thể hybrid; từ Sprint 2 phải fully in-tool |
| Velocity tracking: số sprint có data đủ để tính velocity | 100% sprint từ Sprint 2 | Nếu sprint không được đóng đúng cách, velocity chart không có giá trị |
| Carry-over rate (% SP không done chuyển sang sprint sau) | Đo baseline, target < 20% sau Q1 | High carry-over = planning kém hoặc scope creep — cần visibility để cải thiện |

### Retention Signal

- **Tháng 2:** Số PM request tắt/giảm thông báo (quá nhiều = spam → tune notification logic)
- **Tháng 3:** Số PM yêu cầu custom filter mới (tín hiệu họ đang dùng search thật sự)
- **Tháng 3:** Override rate của Smart Assignment Suggestion (nếu > 50% → algorithm cần tune trước Phase 3)
