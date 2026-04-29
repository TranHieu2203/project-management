# Functional Requirements FR55–FR160: Full Jira Feature Set

**Tài liệu:** Phần mở rộng yêu cầu chức năng cho hệ thống Project Management  
**Phiên bản:** 1.0  
**Ngày:** 2026-04-29  
**Tác giả:** Mary (Business Analyst)  
**Ghi chú:** Kế thừa FR1–FR54 từ `epics.md`. Giải quyết conflicts đã biết C1–C5.

---

## FR55–FR65: Issue Model & Types

**FR-55:** Hệ thống cho phép Admin cấu hình danh sách Issue Type theo từng project (tối thiểu: Epic, Story, Task, Sub-task, Bug); tên, icon và màu sắc của mỗi loại có thể tùy chỉnh. *(Giải quyết C1: thay thế `TaskType` enum cố định.)*

**FR-56:** Hệ thống thực thi cây phân cấp Issue: Epic → Story/Task → Sub-task; một Sub-task không thể là cha của Story; một Issue không thể đồng thời là con của hai Issue khác; độ sâu tối đa là 3 cấp.

**FR-57:** Mỗi Issue Type có field schema riêng: Admin định nghĩa tập field hiển thị, field bắt buộc, field mặc định cho từng loại; thay đổi schema chỉ ảnh hưởng issue tạo mới (không retroactive).

**FR-58:** Hệ thống cung cấp Issue Template per Issue Type: PM tạo template với giá trị field mặc định sẵn; khi tạo issue mới từ template, tất cả giá trị mặc định được điền sẵn và có thể sửa trước khi lưu.

**FR-59:** Hệ thống hỗ trợ Clone Issue: người dùng sao chép một issue hiện có thành issue mới trong cùng project hoặc project khác (nếu có quyền); clone bao gồm field values, attachments (tùy chọn), sub-tasks (tùy chọn); comment và activity log không được clone.

**FR-60:** Hệ thống hỗ trợ Move Issue: chuyển issue từ project này sang project khác với xác nhận; workflow state được map lại sang trạng thái tương đương của project đích (hoặc trạng thái mặc định nếu không map được).

**FR-61:** Hệ thống hỗ trợ Archive Issue: issue archived bị ẩn khỏi board và backlog mặc định nhưng vẫn tìm kiếm được qua filter `archived=true`; archived issue không thể transition workflow.

**FR-62:** Hệ thống cung cấp Trash cho Issue: issue bị xóa vào trash, tự động purge sau 30 ngày (configurable per project); Admin có thể restore từ trash trong thời hạn hoặc purge ngay lập tức.

**FR-63:** Mỗi Issue có trường `issueKey` dạng `{PROJECT_CODE}-{sequence_number}` được hệ thống tự tạo, không thể sửa, dùng làm permalink định danh bất biến.

**FR-64:** Hệ thống hỗ trợ chuyển đổi Issue Type trong cùng một project: khi chuyển, field không tương thích được cảnh báo và có thể giữ lại như custom field tạm thời; workflow state phải được map lại tường minh.

**FR-65:** Hệ thống cho phép import hàng loạt Issue từ file CSV/Excel theo template chuẩn; import job xử lý bất đồng bộ, hiển thị tiến trình, báo cáo lỗi từng dòng và hỗ trợ idempotency theo `externalId`.

---

## FR66–FR80: Agile Board & Sprint

**FR-66:** Hệ thống hỗ trợ hai chế độ Board per project: **Scrum Board** (sprint-based) và **Kanban Board** (flow-based); Admin chọn chế độ khi tạo project, có thể đổi sau với xác nhận dữ liệu.

**FR-67:** Scrum Board hiển thị issues của sprint đang active theo cột workflow; mỗi card hiển thị: issueKey, tóm tắt, assignee avatar, priority icon, story points, loại issue; kéo thả card giữa các cột để thực hiện transition.

**FR-68:** Kanban Board hiển thị tất cả issues không thuộc sprint (hoặc không có sprint); hỗ trợ WIP Limit per column: khi vượt giới hạn, cột hiển thị cảnh báo màu đỏ và không cho phép thêm card (có thể override với lý do).

**FR-69:** Sprint Lifecycle cho Scrum: PM tạo sprint (tên, ngày bắt đầu, ngày kết thúc, sprint goal); start sprint khi đã có ít nhất 1 issue; complete sprint khi sprint kết thúc — issues chưa done được chuyển sang sprint tiếp theo hoặc backlog (PM chọn từng issue).

**FR-70:** Hệ thống quản lý Backlog như danh sách issues chưa được phân vào sprint, sắp xếp theo độ ưu tiên (drag-drop để reorder); PM kéo issue từ backlog vào sprint planning hoặc ngược lại.

**FR-71:** Sprint Planning view: hiển thị backlog bên trái và sprint đang plan bên phải; PM kéo thả issues vào sprint; hiển thị tổng story points đã commit so với velocity trung bình (warning nếu vượt quá).

**FR-72:** Story Points là trường số nguyên (Fibonacci: 1, 2, 3, 5, 8, 13, 21) trên Story/Task; tách biệt hoàn toàn với `plannedEffortHours`; tổng story points per sprint hiển thị trên Sprint header.

**FR-73:** Velocity Tracking: hệ thống tự động tính velocity (story points done) cho mỗi sprint đã complete; hiển thị velocity history dạng bar chart 6–10 sprint gần nhất; velocity trung bình dùng làm baseline cho Sprint Planning.

**FR-74:** WIP Limits: Admin cấu hình giới hạn số issue đồng thời trên mỗi cột của Kanban Board; khi vượt giới hạn, hệ thống cảnh báo và log sự kiện; không block cứng mà cho phép override với lý do bắt buộc.

**FR-75:** Hệ thống tính và hiển thị Cumulative Flow Diagram (CFD) cho board: trục X là thời gian (theo ngày), trục Y là số issue, mỗi band màu là một cột/trạng thái; dữ liệu CFD được precompute hằng ngày.

**FR-76:** Sprint Report tự động sinh sau khi complete sprint: tóm tắt sprint goal, danh sách issues completed, danh sách issues incomplete với lý do, velocity thực tế vs planned, biểu đồ burndown retrospective.

**FR-77:** Burndown Chart của sprint đang active: trục X là ngày trong sprint, trục Y là story points còn lại; đường lý tưởng (ideal line) và đường thực tế (actual line); cập nhật mỗi lần issue được cập nhật story points hoặc chuyển trạng thái.

**FR-78:** Hệ thống hỗ trợ Board Quick Filters: lọc nhanh theo assignee, issue type, priority, label; filters áp dụng ngay lập tức không cần reload; Board nhớ filter cuối cùng của người dùng trong session.

**FR-79:** Board hỗ trợ swimlanes: nhóm cards theo Epic, Assignee hoặc không có swimlane; PM chọn chế độ swimlane trên board view; swimlane có thể collapse/expand.

**FR-80:** Sprint goal hiển thị trên Board header; khi sprint complete, sprint goal và kết quả được lưu vĩnh viễn trong Sprint Report và không thể sửa.

---

## FR81–FR95: Issue Collaboration

**FR-81:** Hệ thống hỗ trợ Comments trên Issue: người dùng có quyền thêm, sửa, xóa comment của chính mình; Admin/Project Lead có thể xóa comment của người khác; comment hỗ trợ rich text (Markdown).

**FR-82:** Comments hỗ trợ Threading: reply trực tiếp vào một comment tạo sub-thread; mặc định hiển thị thread phẳng nhưng có thể chuyển sang view nested; xóa parent comment chuyển thành `[deleted]` nếu còn replies.

**FR-83:** Hệ thống hỗ trợ @mention trong Comment và Description: gõ `@` gợi ý danh sách user trong project; người được mention nhận thông báo in-app và email (nếu cấu hình); @mention lưu theo userId, không bị sai khi người dùng đổi tên.

**FR-84:** Watchers/Subscribers: người dùng có thể watch/unwatch một issue; watchers nhận thông báo cho mọi thay đổi trên issue đó (comment mới, status change, assignee change, field update); assignee mặc định trở thành watcher khi được gán. *(Giải quyết C4: hỗ trợ multi-perspective thay vì single assignee.)*

**FR-85:** File Attachments trên Issue: người dùng upload file (image, PDF, document) tối đa 25MB/file; hình ảnh hiển thị thumbnail inline; tất cả files liệt kê trong tab Attachments với tên, size, uploader, timestamp; xóa attachment tạo audit log.

**FR-86:** Issue Links: người dùng tạo liên kết giữa issues với loại: `blocks`/`is blocked by`, `relates to`, `duplicates`/`is duplicated by`, `clones`/`is cloned by`; một issue có thể có nhiều links; links hiển thị trong panel Linked Issues với resolve indicator.

**FR-87:** Activity Log trên Issue: mọi thay đổi field, transition, comment, attachment, link đều ghi vào activity log với actor, timestamp, giá trị trước/sau; Activity Log là append-only, không thể sửa/xóa; hiển thị trên tab Activity theo thứ tự thời gian.

**FR-88:** Hệ thống hỗ trợ Work Log (giải quyết C3 — khác TimeEntry): người dùng log thời gian làm việc trực tiếp trên issue (time spent + time remaining + date + description); Work Log là mutable (có thể sửa/xóa bởi người tạo hoặc Admin); Work Log tổng hợp thành `Time Tracking` widget trên issue.

**FR-89:** Time Tracking widget trên Issue: hiển thị thanh tiến trình original estimate vs time spent vs time remaining; khi time spent vượt original estimate, thanh chuyển màu cảnh báo; dữ liệu từ Work Log, không phải TimeEntry (hai hệ thống song song).

**FR-90:** Reactions/Emoji trên Comment: người dùng thêm emoji reaction vào comment (tối thiểu: 👍 ✅ 🚧 ❓); số lượng reaction mỗi loại hiển thị; người dùng có thể thêm/bỏ reaction của mình; không required, chỉ optional social feature.

**FR-91:** Issue Description hỗ trợ Markdown với preview real-time: Bold, Italic, Code block, Code inline, Ordered/Unordered list, Table, Heading H1–H3, Quote, Horizontal rule, Link; hình ảnh nhúng từ attachment.

**FR-92:** Hệ thống gửi thông báo in-app cho các sự kiện: issue được assign cho tôi, tôi được mention, issue tôi watch thay đổi, sprint tôi có issue bắt đầu/kết thúc; thông báo in-app có bell icon với badge đếm chưa đọc.

**FR-93:** User Bridge: Resource entity trong hệ thống hiện tại (Epic 1–7) phải liên kết với User entity (Auth) qua trường `userId`; bridge này cho phép @mention, Watcher, và Assignment trên Jira features dùng cùng auth context. *(Giải quyết C5.)*

**FR-94:** Hệ thống hỗ trợ Issue Voting: người dùng vote cho issue để thể hiện mức độ quan tâm; số vote hiển thị trên issue; PM dùng số vote như tiêu chí ưu tiên; vote không ảnh hưởng tự động đến priority field.

**FR-95:** Comment và Activity Log hỗ trợ xuất ra PDF/CSV khi PM export Issue Detail Report; export bao gồm toàn bộ history theo thứ tự thời gian.

---

## FR96–FR105: Workflow Engine

**FR-96:** Hệ thống cung cấp Workflow Engine per project: Admin định nghĩa workflow riêng cho từng Issue Type trong project, gồm danh sách States và Transitions; workflow mặc định (To Do → In Progress → Done) được tạo tự động khi tạo project. *(Giải quyết C2: thay thế `ProjectTaskStatus` enum cố định.)*

**FR-97:** Admin cấu hình State: tên, loại (`initial`/`in-progress`/`final`), màu, category; mỗi workflow có đúng một `initial` state và ít nhất một `final` state; issue tạo mới tự động ở `initial` state của workflow tương ứng.

**FR-98:** Admin cấu hình Transition: từ state nào → đến state nào, tên transition (ví dụ: "Start Progress", "Resolve"), có hiển thị trên board hay không; một state có thể có nhiều outgoing transition.

**FR-99:** Transition Validators: Admin gắn validator cho transition, tối thiểu hỗ trợ: `required_field` (field X phải có giá trị), `assignee_required` (phải có assignee), `no_open_subtasks` (tất cả sub-task phải ở final state); transition bị chặn nếu validator không pass, hiển thị lý do rõ ràng.

**FR-100:** Permission-gated Transitions: Admin giới hạn transition chỉ được thực hiện bởi role nhất định (ví dụ: chỉ Developer mới được transition sang "In Review", chỉ PM mới được "Close"); người dùng không đủ quyền không thấy transition button.

**FR-101:** Post-transition Actions: Admin cấu hình action tự động sau khi transition thành công, tối thiểu hỗ trợ: `set_field` (gán giá trị field), `assign_to_reporter` (gán lại cho reporter), `send_notification` (gửi thông báo tới watcher/assignee/role), `add_comment` (thêm comment tự động với template).

**FR-102:** Workflow History: mọi transition đều được ghi vào Activity Log với: actor, from_state, to_state, transition_name, timestamp, thời gian ở state trước (time in status); dữ liệu này dùng cho Cycle Time và Lead Time metrics.

**FR-103:** Workflow Migration: khi Admin thay đổi workflow (thêm/xóa state), hệ thống hiển thị danh sách issues đang ở state bị xóa và yêu cầu Admin map chúng sang state còn tồn tại trước khi lưu thay đổi.

**FR-104:** Hệ thống cung cấp Workflow Scheme: Admin tạo workflow scheme (tập hợp workflow per issue type) và apply scheme cho nhiều project cùng lúc; thay đổi scheme ảnh hưởng tất cả project dùng scheme đó (với xác nhận).

**FR-105:** Hệ thống cung cấp Board Configuration: Admin map cột board với một hoặc nhiều workflow states; issue ở state không có cột tương ứng không hiển thị trên board (chỉ thấy trong backlog/list view).

---

## FR106–FR115: Search & Filters

**FR-106:** Hệ thống cung cấp Full-text Search: tìm kiếm đồng thời trên issueKey, summary, description, comment; kết quả trả về trong < 500ms cho ~10.000 issues; highlight từ khóa trong kết quả; hỗ trợ tìm exact phrase với dấu ngoặc kép.

**FR-107:** Hệ thống cung cấp Advanced Filter Builder (query language đơn giản, không JQL): người dùng xây filter theo điều kiện AND/OR trên các field: project, issueType, status, assignee, reporter, priority, label, component, version, sprint, createdDate, updatedDate, dueDate, storyPoints; mỗi điều kiện có operator phù hợp với kiểu dữ liệu.

**FR-108:** Saved Filters: người dùng lưu filter với tên, visibility (private/shared with project/shared globally); filter được share chỉ được edit/delete bởi owner hoặc Admin; saved filter có thể subscribe để nhận notification khi có issue mới thỏa điều kiện.

**FR-109:** Recent Searches: hệ thống lưu 10 search queries gần nhất của người dùng (per browser, localStorage); gợi ý khi người dùng bắt đầu gõ trong search box.

**FR-110:** Bulk Operations: người dùng chọn nhiều issues từ list/backlog view và thực hiện hành động hàng loạt: re-assign, change priority, add label, move to sprint, transition workflow, archive, delete; bulk operation tạo audit log riêng ghi số lượng và loại thay đổi.

**FR-111:** Issue List View: ngoài Board view, hệ thống cung cấp List View dạng bảng có sort by bất kỳ column, ẩn/hiện columns, inline edit cho các field đơn giản (priority, assignee, status); hỗ trợ export list ra CSV.

**FR-112:** Search kết quả hỗ trợ phân trang (page size 20/50/100); kết quả nhóm theo project nếu tìm kiếm cross-project; người dùng chỉ thấy issues thuộc project mình có quyền truy cập (membership-only).

**FR-113:** Hệ thống cung cấp Issue Navigator: URL searchable (filter params encode vào URL query string), cho phép bookmark và share link tìm kiếm với đồng nghiệp; recipient phải có quyền membership để thấy kết quả.

**FR-114:** Hệ thống hỗ trợ Filter Subscriptions: người dùng đăng ký nhận email daily/weekly tóm tắt số issue mới/cập nhật thỏa filter đã lưu; unsubscribe qua link trong email.

**FR-115:** Hệ thống hỗ trợ Quick Search (keyboard shortcut `/`): focus vào search box, gõ issueKey (ví dụ `PM-123`) để điều hướng thẳng đến issue; gõ text thường để full-text search.

---

## FR116–FR125: Reporting

**FR-116:** Burndown Chart Report: hiển thị burndown theo story points hoặc issue count cho sprint hiện tại và sprint đã hoàn thành; đường ideal vs actual; có thể xem lại burndown của sprint cũ từ Sprint Report.

**FR-117:** Velocity Chart Report: bar chart hiển thị story points committed vs completed cho 6–10 sprint gần nhất; average velocity line; export sang PNG/PDF.

**FR-118:** Cumulative Flow Diagram (CFD) Report: chọn date range tùy ý (không chỉ sprint); chọn filter issues; bands màu theo workflow state; trục X là ngày, trục Y là số issue; precompute snapshot hằng ngày.

**FR-119:** Sprint Report: tự động sinh sau complete sprint; nội dung: sprint goal + kết quả đạt/không đạt, issues completed (với story points), issues not completed (với lý do), issues added during sprint (scope creep indicator), velocity, burndown retrospective; lưu vĩnh viễn, không sửa được.

**FR-120:** Roadmap View: timeline view theo Epics (trục X là tháng/quý); thanh Epic kéo dài từ ngày bắt đầu đến ngày kết thúc target; màu theo status; drill-down để xem Stories trong Epic; có thể drag Epic để thay đổi target dates.

**FR-121:** Epic Progress Tracking: mỗi Epic hiển thị % hoàn thành tính theo story points done / total story points của tất cả stories trong Epic; breakdown trạng thái (To Do / In Progress / Done) theo count và %; cập nhật realtime khi story thay đổi.

**FR-122:** Time in Status Report: cho mỗi issue, hiển thị thời gian đã dành ở mỗi workflow state; tổng hợp average time in status cho tập issues theo filter; giúp phát hiện bottleneck trong workflow.

**FR-123:** Cycle Time và Lead Time Report: Cycle Time = khoảng thời gian từ "In Progress" đến "Done"; Lead Time = từ "Created" đến "Done"; hiển thị scatter plot và percentile (50th, 85th, 95th); filter theo issue type và date range.

**FR-124:** Created vs Resolved Chart: line chart hiển thị số issue created và resolved theo ngày/tuần trong date range; cho thấy xu hướng tích lũy (backlog growing/shrinking).

**FR-125:** Tất cả Report Charts hỗ trợ export: PNG (screenshot) và CSV (raw data); export job async với download link; dữ liệu export snapshot tại thời điểm trigger (không thay đổi dù data cập nhật sau).

---

## FR126–FR140: Custom Fields, Labels, Versions

**FR-126:** Hệ thống hỗ trợ Custom Fields với các loại: `text` (single-line), `textarea` (multi-line), `number` (integer/decimal), `date`, `datetime`, `url`, `select` (single-choice dropdown), `multi-select`, `user-picker` (single), `multi-user-picker`, `checkbox`, `radio-group`.

**FR-127:** Admin tạo Custom Field per project, gắn vào Issue Type cụ thể (không global); mỗi field có tên, loại, description, placeholder, giá trị mặc định, bắt buộc/không; thứ tự hiển thị trên form có thể kéo thả.

**FR-128:** Custom Field `select` và `multi-select` hỗ trợ tập giá trị admin-defined (options); Admin thêm/sửa/xóa option; xóa option không xóa giá trị đã có trên issues cũ mà hiển thị `[removed option]`.

**FR-129:** Labels/Tags: người dùng gán nhãn tự do (free-text label) cho issue; label tự động autocomplete từ labels đã dùng trong project; label không cần admin pre-define; tìm kiếm và filter theo label; Label Cloud widget trên Project Overview.

**FR-130:** Components: Admin tạo Component per project (tên, description, lead/owner); issue có thể gán một hoặc nhiều components; Component owner tự động thêm vào watchlist khi issue thuộc component được tạo/cập nhật.

**FR-131:** Versions/Releases: Admin tạo Version per project (tên, release date, description, status: Unreleased/Released/Archived); issue có thể gán vào Fix Version(s) và Affects Version(s); Released version không thể thêm issue mới vào Fix Version.

**FR-132:** Release Management: Admin "release" một Version: hệ thống kiểm tra issues chưa done trong Fix Version đó và cảnh báo; sau khi release, tạo Release Note tự động (danh sách issues resolved theo type); release date được stamp vĩnh viễn.

**FR-133:** Story Points field: integer field trên Story/Task/Bug; giá trị gợi ý theo Fibonacci nhưng cho phép nhập tùy ý; tách biệt với `plannedEffortHours`; không có conversion cố định giữa story points và giờ.

**FR-134:** Custom Field giá trị được index cho search: người dùng có thể filter theo custom field trong Advanced Filter Builder; performance đảm bảo với index phù hợp trong PostgreSQL.

**FR-135:** Field Configuration Scheme: Admin tạo scheme gom cấu hình field cho nhiều project dùng chung; thay đổi scheme propagate ngay; override per-project được phép và không bị ghi đè khi scheme update.

**FR-136:** Hệ thống hỗ trợ Field Validation per field: Admin cấu hình: min/max (number), min/max length (text), regex pattern (text), date range (date); validation chạy khi submit issue form và khi transition workflow.

**FR-137:** Priority field: Admin cấu hình danh sách priority levels per project (tối thiểu: Highest, High, Medium, Low, Lowest); mỗi level có icon/màu; default priority khi tạo issue = Medium.

**FR-138:** Due Date field trên Issue: tách biệt với `plannedEndDate` trong Gantt (hai hệ thống có thể đồng bộ thủ công); Due Date hiển thị countdown badge trên board card khi còn ≤ 3 ngày hoặc đã quá hạn.

**FR-139:** Environment field cho Bug Issues: dropdown (Production/Staging/Development/Test); mặc định không hiển thị trên Story/Task; Admin có thể enable cho Issue Type khác.

**FR-140:** Hệ thống hỗ trợ import/export Custom Field schema per project dạng JSON để backup và migrate giữa các project/environment.

---

## FR141–FR150: Automation & Webhooks

**FR-141:** Hệ thống cung cấp Automation Rule CRUD per project: Admin tạo rule với tên, mô tả, enabled/disabled; rule gồm 1 trigger + 0..N conditions + 1..N actions; rules thực thi bất đồng bộ, không ảnh hưởng response time API.

**FR-142:** Trigger Types hỗ trợ tối thiểu: `issue_created`, `issue_updated` (any field), `issue_field_changed` (field cụ thể), `issue_transitioned` (from/to state cụ thể), `issue_assigned`, `issue_commented`, `sprint_started`, `sprint_completed`, `scheduled` (cron expression).

**FR-143:** Condition Types hỗ trợ tối thiểu: `field_equals`, `field_contains`, `field_is_empty`, `field_changed_from`, `field_changed_to`, `assignee_is`, `reporter_is`, `project_is`, `issue_type_is`, `priority_is`, `label_includes`.

**FR-144:** Action Types hỗ trợ tối thiểu: `assign_issue` (đến user/role cụ thể), `update_field` (set giá trị), `add_label`, `remove_label`, `add_comment` (nội dung template với biến `{{issue.key}}`, `{{user.name}}`), `send_notification` (to: watchers/assignee/role/specific user), `transition_issue`, `create_sub_task`, `post_webhook`.

**FR-145:** Outbound Webhooks: Admin cấu hình webhook endpoint (URL, secret, events đăng ký); khi event xảy ra, hệ thống POST JSON payload đến URL với HMAC-SHA256 signature header; retry 3 lần với exponential backoff; log delivery status (success/failed/retrying).

**FR-146:** Automation Rule Execution Log: mọi lần rule trigger đều log: rule_id, trigger_event, conditions_evaluated (pass/fail), actions_executed, duration_ms, error (nếu có); log giữ 30 ngày; Admin có thể xem log và re-run failed execution thủ công.

**FR-147:** Rate Limiting cho Automation: tối đa 100 rule executions/project/giờ; khi vượt giới hạn, rule bị throttle và log cảnh báo; Admin nhận notification khi project liên tục hit limit.

**FR-148:** Automation Rule Testing: Admin có thể "dry-run" rule trên một issue cụ thể để xem conditions nào pass/fail và actions nào sẽ được thực thi; dry-run không commit thay đổi.

**FR-149:** Infinite Loop Prevention: hệ thống phát hiện automation loop (rule A trigger rule B trigger rule A); giới hạn tối đa 5 cấp trigger chain; loop bị dừng và log lỗi với trace đầy đủ.

**FR-150:** Webhook Secret Rotation: Admin có thể rotate webhook secret bất kỳ lúc nào; trong 24h sau rotation, hệ thống accept cả secret cũ và mới (grace period) để tránh gián đoạn; sau grace period, secret cũ bị hủy hoàn toàn.

---

## FR151–FR160: Permission Schemes

**FR-151:** Hệ thống định nghĩa Project Roles mặc định: `Project Admin`, `Developer`, `Reporter`, `Viewer`; Admin có thể tạo thêm custom role per project với tên tùy chọn; role là tập hợp permissions, không phải hierarchy cố định.

**FR-152:** Permission Matrix per Role: Admin cấu hình permission per role, tối thiểu các permission sau: `BROWSE_PROJECT`, `CREATE_ISSUE`, `EDIT_ISSUE`, `DELETE_ISSUE`, `ASSIGN_ISSUE`, `CLOSE_ISSUE`, `MANAGE_SPRINTS`, `ADMINISTER_PROJECT`, `ADD_COMMENT`, `DELETE_ALL_COMMENTS`, `MANAGE_WORKFLOW`, `MANAGE_AUTOMATION`.

**FR-153:** Project Role Assignment: Admin gán user vào role trong project; một user có thể có nhiều role trong cùng project; role assignment có hiệu lực ngay lập tức; history assignment được log trong audit trail.

**FR-154:** Global Permission vs Project Permission: Global Admin có thể access tất cả project bất kể role assignment; Global Permission gồm: `ADMINISTER_SYSTEM`, `MANAGE_USERS`, `CREATE_PROJECT`; Project Permission chỉ có hiệu lực trong scope project cụ thể.

**FR-155:** Permission Inheritance và Default: khi user không có role trong project, mặc định không có quyền gì (deny-by-default); Admin project có thể mở quyền `BROWSE_PROJECT` cho tất cả authenticated users (public project mode).

**FR-156:** Permission Check trả về lỗi nhất quán: thiếu quyền trả `403 Forbidden` với ProblemDetails nêu rõ permission bị thiếu; resource không tồn tại hoặc không có quyền browse trả `404` (no information leak).

**FR-157:** Permission Scheme Sharing: Admin tạo Permission Scheme (tập role-permission definitions) có thể apply cho nhiều project; thay đổi scheme apply cho tất cả project dùng scheme đó; project có thể override một số permission mà không ảnh hưởng scheme gốc.

**FR-158:** Service Account / API Token: người dùng có thể tạo API Token thay thế password cho integration; API Token có tên, scope (read-only/read-write), expiry date; Admin có thể revoke token của bất kỳ user nào; token không lưu plain text (hashed).

**FR-159:** Audit Log for Permissions: mọi thay đổi role assignment, permission matrix, permission scheme đều ghi audit log; Admin có thể export audit log theo date range; log giữ vĩnh viễn (không xóa).

**FR-160:** IP Allowlist (optional security feature): Admin có thể cấu hình danh sách IP/CIDR được phép truy cập project; request từ IP ngoài danh sách nhận `403` ngay tại API gateway layer; tính năng này là per-project và optional (không required cho MVP Jira features).

---

## Non-Functional Requirements Mới: NFR14–NFR20

**NFR-14:** **Board Performance** — Kanban/Scrum Board render hoàn tất (first meaningful paint) trong < 1.5 giây với tối đa 200 cards visible; sử dụng virtual scrolling cho board có >200 cards để đảm bảo performance không degraded khi data tăng.

**NFR-15:** **Search Latency** — Full-text search query trả về kết quả đầu tiên (first page, 20 items) trong < 500ms cho dataset ~10.000 issues trên PostgreSQL với full-text index (`tsvector`); p95 latency < 1s.

**NFR-16:** **Attachment Storage** — Kích thước tối đa mỗi file attachment là 25MB; tổng dung lượng attachment per project là 5GB (configurable by Admin); khi vượt quota, upload bị từ chối với lỗi rõ ràng và gợi ý Admin tăng quota; storage backend có thể là local filesystem hoặc S3-compatible object storage.

**NFR-17:** **Automation Execution** — Automation rules thực thi bất đồng bộ qua background job queue; API response trả về ngay lập tức (không chờ rule execution); execution latency mục tiêu < 5 giây sau trigger event trong điều kiện bình thường; queue có dead-letter mechanism cho failed jobs.

**NFR-18:** **Workflow Transition Validation** — Transition validator phải hoàn thành trong < 100ms (server-side); validator logic phải là stateless và không gọi external service; kết quả validation được trả về đồng bộ trong cùng API request transition.

**NFR-19:** **Activity Log Retention** — Activity Log và Audit Trail không bao giờ bị tự động xóa hoặc purge; dữ liệu lưu vĩnh viễn (no TTL); không có API endpoint nào cho phép xóa activity log; truy vấn log lớn phải hỗ trợ cursor-based pagination để tránh timeout.

**NFR-20:** **Real-time Board Sync** — Board view sử dụng polling interval 10 giây (nhanh hơn dashboard polling 30–60 giây theo NFR12); polling chỉ fetch delta (Last-Modified/ETag header) để giảm payload; khi không có thay đổi, server trả `304 Not Modified` và không tốn bandwidth.

---

## Requirements Coverage Map: FR55–FR160 → Epic 8–15

| FR Range | Category | Epic |
|---|---|---|
| FR55–FR65 | Issue Model & Types | **Epic 8**: Issue Model (Types, Hierarchy, Schema, Clone, Archive) |
| FR66–FR80 | Agile Board & Sprint | **Epic 9**: Agile Board & Sprint (Scrum/Kanban, Backlog, Velocity, WIP) |
| FR81–FR95 | Issue Collaboration | **Epic 10**: Collaboration (Comments, Mentions, Watchers, Attachments, Links) |
| FR96–FR105 | Workflow Engine | **Epic 11**: Workflow Engine (States, Transitions, Validators, Post-actions) |
| FR106–FR115 | Search & Filters | **Epic 12**: Search & Filters (Full-text, Advanced Filter, Bulk Ops) |
| FR116–FR125 | Reporting | **Epic 13**: Reporting (Burndown, Velocity, CFD, Sprint Report, Roadmap) |
| FR126–FR140 | Custom Fields & Versioning | **Epic 14**: Custom Fields, Labels, Components, Versions |
| FR141–FR160 | Automation, Webhooks & Permissions | **Epic 15**: Automation, Webhooks & Permission Schemes |

### Chi tiết mapping

#### Epic 8: Issue Model (Types, Hierarchy, Schema, Clone, Archive)
**FRs covered:** FR55, FR56, FR57, FR58, FR59, FR60, FR61, FR62, FR63, FR64, FR65  
**Conflict giải quyết:** C1 (Issue Type configurable thay TaskType enum), C2 (cơ sở cho Workflow Engine)  
**Dependencies:** Epic 1 (Project/Task model), Epic 11 (Workflow Engine)

#### Epic 9: Agile Board & Sprint (Scrum/Kanban, Backlog, Velocity, WIP)
**FRs covered:** FR66, FR67, FR68, FR69, FR70, FR71, FR72, FR73, FR74, FR75, FR76, FR77, FR78, FR79, FR80  
**Dependencies:** Epic 8 (Issue Model), Epic 11 (Workflow States → Board Columns)

#### Epic 10: Collaboration (Comments, Mentions, Watchers, Attachments, Links)
**FRs covered:** FR81, FR82, FR83, FR84, FR85, FR86, FR87, FR88, FR89, FR90, FR91, FR92, FR93, FR94, FR95  
**Conflict giải quyết:** C3 (WorkLog mutable vs TimeEntry immutable), C4 (Watchers multi-perspective), C5 (Resource-User bridge)  
**Dependencies:** Epic 1 (Auth/User), Epic 2 (Resource entity), Epic 8 (Issue)

#### Epic 11: Workflow Engine (States, Transitions, Validators, Post-actions)
**FRs covered:** FR96, FR97, FR98, FR99, FR100, FR101, FR102, FR103, FR104, FR105  
**Conflict giải quyết:** C2 (ProjectTaskStatus enum → configurable Workflow)  
**Dependencies:** Epic 8 (Issue Types), Epic 15 (Permission — permission-gated transitions)

#### Epic 12: Search & Filters (Full-text, Advanced Filter, Bulk Ops)
**FRs covered:** FR106, FR107, FR108, FR109, FR110, FR111, FR112, FR113, FR114, FR115  
**Dependencies:** Epic 8 (Issue data), Epic 14 (Custom Fields searchable), NFR15

#### Epic 13: Reporting (Burndown, Velocity, CFD, Sprint Report, Roadmap)
**FRs covered:** FR116, FR117, FR118, FR119, FR120, FR121, FR122, FR123, FR124, FR125  
**Dependencies:** Epic 9 (Sprint data), Epic 11 (Workflow History), Epic 6 (Cost data), NFR14, NFR15

#### Epic 14: Custom Fields, Labels, Components, Versions
**FRs covered:** FR126, FR127, FR128, FR129, FR130, FR131, FR132, FR133, FR134, FR135, FR136, FR137, FR138, FR139, FR140  
**Dependencies:** Epic 8 (Issue Schema), Epic 11 (Field Validation in Transition)

#### Epic 15: Automation, Webhooks & Permission Schemes
**FRs covered:** FR141, FR142, FR143, FR144, FR145, FR146, FR147, FR148, FR149, FR150, FR151, FR152, FR153, FR154, FR155, FR156, FR157, FR158, FR159, FR160  
**Dependencies:** Epic 1 (Auth/JWT), Epic 8 (Issue events), Epic 11 (Transition trigger), NFR17

---

*Tài liệu này là phần mở rộng của `epics.md`. Khi triển khai Epic 8–15, team cần review conflicts C1–C5 và thực hiện database migration phù hợp để không breaking change Epic 1–7.*

---

## Phần Bổ Sung — FR161-FR200: Jira Completeness Gaps (Audit 2026-04-29)

_Các requirements bổ sung sau khi audit toàn diện so với Jira. Phát hiện bởi Mary (Business Analyst)._

_**Audit methodology:** Cross-reference 20 known gap areas against FR55–FR160 and Epic 1–15. Gaps already fully covered by existing FRs are noted but not duplicated. Only genuinely missing or partially covered items receive new FRs._

_**Pre-audit findings — already covered (no new FR needed):**_
- _Gap 11 (Clone Issue): fully covered by FR-59._
- _Gap 12 (@mention in Description): explicitly covered by FR-83 ("Comment và Description")._
- _Gap 9 (Work Log developer-side): fully covered by FR-88 and FR-89._
- _Gap 16 (Sprint goal field): FR-69 (sprint creation) + FR-80 (board header display) together cover this._
- _Gap 20 (Issue history / change log): FR-87 Activity Log covers before/after values, actor, timestamp for all field changes._
- _Gap 19 (Linked Issues panel on detail): FR-86 covers links panel with resolve indicator._

---

### FR161-FR170: In-app Notification Center

**FR-161:** Hệ thống hiển thị biểu tượng chuông (bell icon) cố định trên thanh navigation toàn cục; badge số nguyên không âm hiển thị tổng số thông báo chưa đọc (unread count); khi unread count = 0, badge ẩn đi; unread count cập nhật qua polling 30 giây hoặc real-time push nếu WebSocket/SSE khả dụng.

**FR-162:** Nhấp vào bell icon mở Notification Center dạng dropdown panel (không điều hướng trang); panel hiển thị danh sách tối đa 50 thông báo gần nhất theo thứ tự thời gian giảm dần; cuộn xuống cuối danh sách tải thêm 50 thông báo tiếp theo (infinite scroll / load more).

**FR-163:** Mỗi notification item trong Notification Center hiển thị: avatar của actor, mô tả sự kiện ngắn gọn (ví dụ: "Alice commented on PM-42"), issue key dạng link có thể nhấp, thời gian tương đối (ví dụ: "5 phút trước") và trạng thái đã đọc/chưa đọc (chưa đọc = nền xanh nhạt hoặc chấm tròn).

**FR-164:** Người dùng có thể đánh dấu đã đọc từng thông báo bằng cách nhấp vào notification item; nhấp item điều hướng đến issue/sprint/comment liên quan và tự động mark as read; action mark-as-read ghi lại `readAt` timestamp và không thể undo.

**FR-165:** Notification Center cung cấp nút "Mark all as read": đánh dấu toàn bộ thông báo hiện có là đã đọc trong một thao tác; sau khi thực hiện, badge count = 0 và hiển thị xác nhận toast ngắn gọn.

**FR-166:** Notification Center hỗ trợ filter theo loại sự kiện: người dùng chọn bộ lọc từ dropdown "All / Assigned to me / Mentioned / Watched issues / Sprint events"; filter áp dụng ngay lập tức, chỉ hiển thị thông báo thuộc loại đã chọn.

**FR-167:** Người dùng có thể mở trang Notification Settings từ link "Manage notification preferences" ở footer của Notification Center panel; trang này cho phép bật/tắt từng loại sự kiện cho cả in-app và email riêng biệt (xem FR-171–FR-178).

**FR-168:** Hệ thống lưu trữ thông báo in-app tối đa 90 ngày tính từ ngày tạo; thông báo cũ hơn 90 ngày bị tự động purge; người dùng được thông báo qua tooltip rằng lịch sử chỉ giữ 90 ngày.

**FR-169:** Thông báo in-app không bị tạo cho hành động do chính người dùng thực hiện (ví dụ: tự assign cho bản thân, tự comment) — hệ thống bỏ qua self-notification; rule này áp dụng đồng nhất cho tất cả loại sự kiện.

**FR-170:** Khi người dùng đang ở trang issue và có thông báo mới đến liên quan đến issue đó, hệ thống hiển thị toast nhỏ ở góc màn hình ("New activity on this issue") với nút "Refresh" để reload activity log mà không mất nội dung form đang nhập.

---

### FR171-FR178: Per-event Notification Configuration

**FR-171:** Hệ thống hỗ trợ Per-event Email Notifications (tách biệt hoàn toàn với weekly digest FR-29 của Epic 7): người dùng nhận email riêng lẻ ngay khi sự kiện xảy ra, không gom batch; per-event email và weekly digest là hai hệ thống song song có thể bật/tắt độc lập.

**FR-172:** Hệ thống gửi per-event email cho sự kiện "Issue assigned to me": email chứa issue key, summary, assignee mới (chính người nhận), reporter, priority, link trực tiếp đến issue; gửi trong vòng 60 giây sau khi assignment xảy ra.

**FR-173:** Hệ thống gửi per-event email cho sự kiện "Comment added on issue I watch/am assigned to": email chứa tên người comment, nội dung comment (truncate tại 500 ký tự với link "view full"), issue key và link đến comment cụ thể (anchor link).

**FR-174:** Hệ thống gửi per-event email cho sự kiện "Issue status changed": email chứa from_state → to_state, actor, issue key, summary, link đến issue; chỉ gửi cho assignee và watchers hiện tại của issue đó.

**FR-175:** Hệ thống gửi per-event email cho sự kiện "@mentioned": người được mention trong comment hoặc description nhận email ngay lập tức với context (đoạn văn chứa mention, link đến comment/issue); @mention email có độ ưu tiên cao hơn các email khác (không bị queue delay).

**FR-176:** Hệ thống gửi per-event email cho sự kiện "Due date approaching": email nhắc nhở khi issue của tôi còn đúng 3 ngày và 1 ngày trước due date; người dùng cấu hình được threshold (1/2/3/5/7 ngày) hoặc tắt hoàn toàn; email chứa issue key, summary, due date, current status.

**FR-177:** Trang Notification Preferences cho phép người dùng bật/tắt từng loại per-event notification cho cả hai kênh (in-app và email) qua matrix dạng bảng: hàng = loại sự kiện, cột = kênh; mặc định khi tạo tài khoản mới: in-app bật tất cả, email chỉ bật "Assigned to me" và "@mentioned".

**FR-178:** Hệ thống cung cấp email unsubscribe one-click cho mọi per-event notification email: link unsubscribe ở footer email, nhấp một lần tắt loại notification đó mà không cần đăng nhập; sau khi unsubscribe hiển thị trang xác nhận với link để resubscribe.

---

### FR179-FR183: Board Swimlanes & Quick Filters

_Note: FR-79 đã cover swimlane theo Epic và Assignee. FR-78 đã cover quick filter cơ bản. Các FRs dưới đây bổ sung chi tiết còn thiếu._

**FR-179:** Board Swimlanes hỗ trợ thêm chế độ nhóm theo **Label**: mỗi label duy nhất của issues trong sprint/backlog tạo một swim row; issues có nhiều labels xuất hiện ở row đầu tiên trong danh sách labels; một row đặc biệt "No Label" chứa issues không có label.

**FR-180:** Board Swimlanes hỗ trợ thêm chế độ nhóm theo **Story (Epic child grouping)**: issues được nhóm theo Story cha của chúng (áp dụng khi board hiển thị Sub-tasks); issues không có Story cha vào row "No Story".

**FR-181:** Mỗi swimlane row hiển thị: tên nhóm (Epic title / assignee name / label name), số issue trong row, tổng story points trong row, nút collapse/expand; trạng thái collapsed/expanded của từng row được lưu per-user per-board trong localStorage.

**FR-182:** Board Quick Filters bổ sung hai preset buttons cố định trên Board header (ngoài filter builder FR-78): **"Only My Issues"** (chỉ hiển thị issues assigned to current user) và **"Recently Updated"** (chỉ hiển thị issues updated trong 24h qua); hai buttons này là toggle, có thể active đồng thời với nhau và với các filter khác.

**FR-183:** Board Backlog Configuration: Admin cấu hình mapping giữa workflow states và board display mode; states được đánh dấu `showOnBoard=false` không xuất hiện trên Board view nhưng vẫn xuất hiện trong Backlog/List view; mặc định khi tạo project: initial state và final state ở Backlog, các state in-progress ở Board.

---

### FR184-FR188: Project Templates & Reporter Field

**FR-184:** Trường **Reporter** là mandatory field trên mọi issue: hệ thống tự động gán Reporter = current user khi tạo issue; Reporter có thể thay đổi bởi người tạo hoặc Admin (không phải assignee); Reporter được lưu riêng biệt với Assignee và không thể là null sau khi issue đã tạo.

**FR-185:** Reporter hiển thị trên Issue Detail view với avatar và display name; Reporter là một trong các recipients mặc định của per-event notifications (assigned, status changed, commented); Reporter được include trong Advanced Filter Builder (FR-107) và Saved Filter conditions.

**FR-186:** Hệ thống cung cấp Project Templates: khi tạo project mới, Admin chọn template từ danh sách: **Scrum Software Development**, **Kanban Software Development**, **Business Project** (task-based, không sprint); mỗi template pre-configure: board type, workflow states, default issue types, default columns.

**FR-187:** Template **Scrum Software Development** pre-configure: board = Scrum, workflow = To Do / In Progress / In Review / Done, issue types = Epic + Story + Task + Sub-task + Bug, backlog enabled, sprint planning enabled; Admin có thể customize sau khi tạo.

**FR-188:** Template **Kanban Software Development** pre-configure: board = Kanban, workflow = Backlog / Selected for Development / In Progress / Done, issue types = Story + Task + Bug (không có Sprint), WIP limit mặc định = 5 cho cột "In Progress"; Template **Business Project** pre-configure: board = Kanban đơn giản, workflow = To Do / In Progress / Done, issue types = Task + Sub-task (không Epic/Story/Sprint).

---

### FR189-FR195: Notification Schemes (Admin)

**FR-189:** Hệ thống cung cấp Notification Scheme: Admin hệ thống (Global Admin) tạo và quản lý Notification Schemes; mỗi scheme định nghĩa: với mỗi event type → tập recipients; một scheme có thể apply cho nhiều projects; project không apply scheme sẽ dùng Default Notification Scheme.

**FR-190:** Notification Scheme hỗ trợ các Event Types: `Issue Created`, `Issue Updated (any field)`, `Issue Assigned`, `Issue Commented`, `Issue Transitioned`, `Issue Deleted`, `Sprint Started`, `Sprint Completed`, `Version Released`; Admin có thể thêm event types mới khi hệ thống mở rộng.

**FR-191:** Notification Scheme hỗ trợ các Recipient Types per event: `Current Assignee`, `Reporter`, `All Watchers`, `Project Lead`, `Project Role: [role_name]`, `Specific User: [userId]`, `All Project Members`; Admin chọn một hoặc nhiều recipient types cho mỗi event; recipient types là additive (union, không intersection).

**FR-192:** Admin apply Notification Scheme lên project qua trang Project Settings > Notifications > Scheme; khi scheme thay đổi, tất cả projects đang dùng scheme đó bị ảnh hưởng ngay lập tức (không retroactive với notifications đã gửi); Admin xem danh sách projects đang dùng scheme trước khi chỉnh sửa.

**FR-193:** Người dùng individual có thể override Notification Scheme ở tầng cá nhân thông qua trang Notification Preferences (FR-177): nếu scheme bật "Issue Commented" → email người dùng, nhưng người dùng tắt email cho loại đó trong preferences, email không được gửi; personal preference ghi đè scheme (opt-out model).

**FR-194:** Default Notification Scheme (system-provided, không xóa được) cấu hình: `Issue Assigned` → Current Assignee; `Issue Commented` → Current Assignee + All Watchers; `Issue Transitioned` → Reporter + Current Assignee; `Sprint Started` → All Project Members; `Version Released` → All Project Members; Admin có thể clone Default Scheme để tạo custom scheme.

**FR-195:** Notification Scheme audit log: mọi thay đổi scheme (tạo, sửa, xóa, apply to project, remove from project) đều được ghi vào Audit Log (FR-159) với actor, timestamp, before/after snapshot; Global Admin có thể xem lịch sử thay đổi scheme trong trang Scheme Management.

---

### FR196-FR200: Additional Coverage Gaps

**FR-196:** **Epic Burndown Chart** (bổ sung cho Sprint Burndown FR-116): hệ thống cung cấp Epic-level burndown chart: trục X là ngày từ ngày bắt đầu đến ngày kết thúc target của Epic, trục Y là story points còn lại của tất cả Stories/Tasks thuộc Epic; ideal line + actual line; cập nhật mỗi khi issue trong Epic thay đổi story points hoặc status; truy cập từ Epic detail view tab "Reports".

**FR-197:** **Multiple Assignees** (team-managed mode): Project Admin có thể bật chế độ "Multiple Assignees" per project; khi bật, issue có thể có tối đa 5 assignees đồng thời; tất cả assignees hiển thị avatar stack trên board card; notification "assigned to me" gửi tới tất cả assignees; report và filter hỗ trợ "assignee contains [user]" thay vì exact match.

**FR-198:** **Issue Starring / Pinning**: người dùng có thể star (đánh dấu sao) bất kỳ issue nào bằng nút star icon trên Issue Detail và Board card; starred issues tổng hợp trong trang "My Starred Issues" (truy cập từ profile dropdown); star là per-user (không visible với người khác); tối đa 100 starred issues per user (FIFO khi vượt giới hạn).

**FR-199:** **Epic Color Coding**: mỗi Epic có một `color` field (hex color, mặc định ngẫu nhiên từ palette 12 màu khi tạo); Admin/PM có thể thay đổi màu Epic bất kỳ lúc nào; màu Epic được áp dụng nhất quán trên: Board (badge màu trên card), Roadmap (thanh Epic), Sprint Planning view (label màu), và Backlog (dòng Epic header).

**FR-200:** **Release / Deployment Tracking trên Version**: Admin có thể gắn Deployment Records lên một Version đã Released: mỗi Deployment Record gồm environment (Production/Staging), deployment date, deployed_by (user), release notes URL (optional); Version detail page hiển thị danh sách deployment records theo thứ tự thời gian; deployment record là append-only, không xóa được.

---

### Coverage Gap Summary

| # | Gap | Audit Result | FR Coverage | Epic Mapping |
|---|---|---|---|---|
| 1 | In-app Notification Center (bell, list, mark-as-read, filter) | ❌ Missing — FR-92 chỉ liệt kê events, không specify Notification Center UI | **FR-161–FR-170** | Epic 7 extension |
| 2 | Per-event email notifications (assigned, commented, status, @mention, due date) | ❌ Missing — FR-29 chỉ là weekly digest; FR-83 mention email nhưng không specify cơ chế | **FR-171–FR-178** | Epic 7 extension |
| 3 | Board Swimlanes by Label/Custom | ⚠️ Partial — FR-79 covers Epic + Assignee only | **FR-179–FR-181** | Epic 9 extension |
| 4 | Reporter field (separate from Assignee) | ❌ Missing — không có FR nào define Reporter entity | **FR-184–FR-185** | Epic 8 extension |
| 5 | Project Templates (Scrum/Kanban/Business pre-configure) | ❌ Missing — không có FR nào | **FR-186–FR-188** | Epic 8/9 extension |
| 6 | Notification Schemes (Admin-level event→recipient mapping) | ❌ Missing — FR-141/FR-144 có `send_notification` action nhưng không có Scheme concept | **FR-189–FR-195** | Epic 15 extension |
| 7 | Issue Screens / Field Layouts (Screen Schemes) | 🚫 Out of scope — cực kỳ phức tạp, FR-57 + FR-127 cover field schema per type đủ cho MVP | — | Deferred post-MVP |
| 8 | Epic Burndown Chart (burndown tại epic level) | ❌ Missing — FR-116 chỉ có Sprint Burndown; FR-121 là progress % không phải burndown chart | **FR-196** | Epic 13 extension |
| 9 | Work Log (developer time logging) | ✅ Fully covered — FR-88, FR-89 | — | Epic 10 |
| 10 | Multiple Assignees | ❌ Missing — FR-84 Watchers, không phải multi-assignee | **FR-197** | Epic 8 extension |
| 11 | Clone Issue | ✅ Fully covered — FR-59 | — | Epic 8 |
| 12 | @mention in Description | ✅ Fully covered — FR-83 explicitly mentions both Comment and Description | — | Epic 10 |
| 13 | Issue Starring / Pinning | ❌ Missing — FR-94 là Voting (separate concept) | **FR-198** | Epic 10 extension |
| 14 | Quick Filters "Only My Issues" + "Recently Updated" preset | ⚠️ Partial — FR-78 covers filter builder but not preset button shortcuts | **FR-182** | Epic 9 extension |
| 15 | Board Backlog configuration (status → Board vs Backlog mapping) | ⚠️ Partial — FR-105 covers board column mapping but not explicit Backlog/Board split config | **FR-183** | Epic 9/11 extension |
| 16 | Sprint Goal field | ✅ Fully covered — FR-69 (creation) + FR-80 (board display) | — | Epic 9 |
| 17 | Release / Deployment tracking on Versions | ❌ Missing — FR-131/FR-132 cover version release but not deployment records | **FR-200** | Epic 14 extension |
| 18 | Epic Color Coding | ❌ Missing — không có FR nào | **FR-199** | Epic 8/9/13 extension |
| 19 | Linked Issues panel on Issue Detail | ✅ Fully covered — FR-86 covers links panel with resolve indicator | — | Epic 10 |
| 20 | Issue History / Change Log completeness | ✅ Fully covered — FR-87 Activity Log covers before/after values, all events | — | Epic 10 |

### FR161–FR200 → Epic Mapping

| FR Range | Category | Epic Extension |
|---|---|---|
| FR161–FR170 | In-app Notification Center | **Epic 7** (Operations Layer) |
| FR171–FR178 | Per-event Notification Configuration | **Epic 7** (Operations Layer) |
| FR179–FR183 | Board Swimlanes & Quick Filters | **Epic 9** (Agile Board & Sprint) |
| FR184–FR185 | Reporter Field | **Epic 8** (Issue Model) |
| FR186–FR188 | Project Templates | **Epic 8 + 9** (Issue Model + Board) |
| FR189–FR195 | Notification Schemes (Admin) | **Epic 15** (Automation & Permissions) |
| FR196 | Epic Burndown Chart | **Epic 13** (Reporting) |
| FR197 | Multiple Assignees | **Epic 8** (Issue Model) |
| FR198 | Issue Starring / Pinning | **Epic 10** (Collaboration) |
| FR199 | Epic Color Coding | **Epic 8 + 9 + 13** (Issue/Board/Reporting) |
| FR200 | Release / Deployment Tracking | **Epic 14** (Custom Fields & Versions) |

---

_Audit completed: 2026-04-29. Auditor: Mary (Business Analyst). Total new FRs added: 40 (FR161–FR200). Items confirmed already covered: 7. Items deferred as out-of-scope: 1 (Issue Screen Schemes)._
