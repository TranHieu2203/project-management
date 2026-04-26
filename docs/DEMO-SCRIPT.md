# Demo Script — Epic 1 Staging Review

**Thời gian:** ~15–20 phút  
**Người thực hiện:** PM demo (pm1@local.test)  
**Mục tiêu:** Chứng minh "login → chọn dự án → Gantt → chỉnh → lưu → xử lý conflict"

---

## Tài khoản Demo

| Email | Mật khẩu | Vai trò |
|---|---|---|
| pm1@local.test | P@ssw0rd!123 | PM One — tài khoản demo chính |
| pm2@local.test | P@ssw0rd!123 | PM Two — dùng cho conflict demo |

---

## Phần 1: Happy Path (10 phút)

### Bước 1: Đăng nhập
1. Mở trình duyệt → `http://localhost:4200`
2. Nhập email: `pm1@local.test`, mật khẩu: `P@ssw0rd!123`
3. Click **Đăng nhập**
4. ✅ Hệ thống điều hướng về trang **My Projects**

### Bước 2: My Projects
1. Trang "My Projects" hiển thị danh sách dự án của bạn (membership-only)
2. Thấy card **"Dự Án Demo Sprint 1"** (DEMO-01)
3. Click nút **Gantt** trên card đó

### Bước 3: Xem Gantt
1. Gantt mở ra với split-panel: cây tasks bên trái, timeline SVG bên phải
2. Quan sát:
   - **Phase 1 "Khởi động"**: bar xanh lam, tháng 5/2026
   - **Task 1.1 "Thu thập yêu cầu"**: xanh lá (Completed 100%)
   - **Task 1.2 "Phân tích nghiệp vụ"**: vàng (InProgress 60%)
   - **Milestone "Kick-off Approved"**: diamond cam
   - **Phase 2 "Phát triển"**: bar xanh lam, tháng 6/2026
   - Các **dependency arrows** (FS và SS) giữa tasks
   - **Đường đỏ đứt** = ngày hôm nay
3. Toggle **Tuần / Ngày** để xem granularity khác nhau

### Bước 4: Drag task để đổi ngày (Demo thao tác)
1. Hover lên bar của task **"Xây dựng API Backend"** (Phase 2, Task 2.2)
2. **Drag** bar sang trái hoặc phải ~3–5 ngày
3. ✅ Ghost bar (opacity 50%) hiện trong khi drag
4. Thả chuột → Task chuyển màu có chỉ thị **dirty** (sọc cam bên trái)
5. Toolbar hiện nút **"Lưu (1)"** và **"Hoàn tác"**

### Bước 5: Resize bar (Demo thao tác)
1. Hover lên **cạnh phải** của bar task "Xây dựng UI Frontend" (Task 2.3)
2. Cursor đổi sang `ew-resize`
3. Drag phải để kéo dài thêm 5 ngày
4. ✅ Toolbar hiện **"Lưu (2)"** (2 tasks dirty)

### Bước 6: Lưu thay đổi
1. Click nút **"Lưu (2)"** trong toolbar
2. ✅ Spinner hiện trong khi lưu
3. ✅ Sau khi lưu: dirty indicators biến mất, toolbar về trạng thái ban đầu
4. **Reload trang** (F5) → mở lại Gantt → ngày đã cập nhật đúng ✅

---

## Phần 2: Conflict Path — 409 Demo (5 phút)

Mục đích: Chứng minh optimistic locking ngăn mất dữ liệu khi 2 người cùng chỉnh task.

### Setup
1. Mở **Tab A**: Chrome bình thường
2. Mở **Tab B**: Chrome Incognito (hoặc tab mới trong cùng session)
3. Cả 2 tab: login `pm1@local.test` → My Projects → DEMO-01 → Gantt

### Thực hiện conflict
1. **Tab A**: Drag task **"Thiết kế database"** (Task 2.1) sang phải 3 ngày → click **"Lưu (1)"**
2. Tab A lưu thành công → task version tăng N → N+1
3. **Tab B**: (Không reload — vẫn dùng version N cũ) → Drag cùng task đó sang trái 5 ngày → click **"Lưu (1)"**
4. ✅ Tab B nhận **409 Conflict** từ server
5. ✅ **ConflictDialogComponent** mở ra với 2 lựa chọn:
   - **"Dùng bản mới nhất"** → discard thay đổi của Tab B, reload từ server
   - **"Thử áp lại của tôi"** → lấy version mới từ server, retry save với thay đổi của Tab B

### Demo lựa chọn "Dùng bản mới nhất"
1. Click **"Dùng bản mới nhất"**
2. ✅ Dialog đóng, Gantt reload, hiển thị dữ liệu từ Tab A

### Demo lựa chọn "Thử áp lại của tôi" (optional)
1. Lặp lại setup conflict
2. Lần này click **"Thử áp lại của tôi"**
3. ✅ Hệ thống retry PUT với server version mới → nếu không có conflict thứ 2, lưu thành công

---

## Phần 3: Dependency Link Demo (Optional, 3 phút)

1. Trong Gantt toolbar, click icon **link** (Connect Mode)
2. Cursor đổi sang `crosshair`
3. Hover lên task bar — 2 circle endpoints hiện ở đầu/cuối bar
4. **Drag** từ right endpoint của Task 2.3 → left endpoint của Milestone 2.M
5. ✅ Temporary dashed line hiện trong khi drag
6. Thả → dependency mới được tạo (FS mặc định) → dirty badge tăng
7. Click **"Lưu"** để persist

---

## Reset Demo Data

Để reset về trạng thái ban đầu cho demo lần sau:

### Option A: Truncate database (nhanh nhất)
```sql
-- Chạy trong psql hoặc pgAdmin:
TRUNCATE TABLE task_dependencies, project_tasks, project_memberships, projects CASCADE;
-- Sau đó restart app với AutoMigrate=true sẽ seed lại
```

### Option B: Drop và recreate database
```bash
# Stop app
dropdb project_management && createdb project_management
# Start app với ASPNETCORE_ENVIRONMENT=Development và Host__AutoMigrate=true
```

### Option C: Chạy lại seeder via API (nếu có reset endpoint)
```bash
# Nếu có endpoint /api/v1/admin/reset-demo (staging only):
curl -X POST http://localhost:5000/api/v1/admin/reset-demo
```

---

## Lỗi thường gặp khi Demo

| Lỗi | Nguyên nhân | Giải pháp |
|---|---|---|
| Gantt trống (không có tasks) | Seed chưa chạy hoặc project tạo nhưng không có tasks | Kiểm tra `Host:AutoMigrate=true` trong config; restart app |
| Lỗi "Không tìm thấy dự án" | User không phải member | Verify pm1/pm2 đều có membership với DEMO-01 |
| 409 không xảy ra | 2 tab cùng version N vì tab B đã reload | Tab B KHÔNG được reload sau khi Tab A save |
| Conflict dialog không mở | Tab B bị 412 thay vì 409 | 412 xảy ra nếu thiếu If-Match header (hiếm) |
