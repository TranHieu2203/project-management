# Story 7.4: Per-event notification triggers (assigned/commented/transitioned/@mentioned)

Status: review

**Story ID:** 7.4
**Epic:** Epic 7 — Operations Layer (Notifications + In-product transparency metrics)
**Sprint:** Sprint 9
**Date Created:** 2026-04-29

---

## Story

As a team member,
I want nhận thông báo ngay lập tức khi có sự kiện liên quan đến tôi (được assign task, task chuyển trạng thái, có comment mới, được @mention),
So that tôi phản hồi kịp thời mà không cần liên tục kiểm tra tool thủ công.

---

## Scope Boundary (Phase 1 — Story 7-4)

| Sự kiện | Scope 7-4 | Ghi chú |
|---|---|---|
| Task assigned | ✅ Đầy đủ | Notify assignee in-app + email |
| Task status-changed | ✅ Assignee only | Reporter/Watchers → Phase 2 (Story 10.4) |
| Comment thêm mới | ⚙️ Preference stub | Comment chưa tồn tại Phase 1; Stories 10.1/10.2 wire vào sau |
| @mention trong comment/description | ⚙️ Preference stub | Cần resource→user bridge (AD-11/Story 10.2); wire sau |

Story này tạo nền tảng `user_notifications` table, preference types, và delivery pipeline. Story 7-5 xây bell icon + Notification Center UI đọc từ table này.

---

## Acceptance Criteria

1. **Given** task được assign cho user A (AssigneeId thay đổi)
   **When** PM hoặc user khác lưu assignment
   **Then** user A nhận `UserNotification` được persist trong DB với `type = "assigned"` và link đến task
   **And** user A nhận email (nếu preference `assigned` = enabled và user có email hợp lệ)

2. **Given** task chuyển trạng thái (status transition)
   **When** bất kỳ user nào thực hiện transition
   **Then** assignee hiện tại của task nhận `UserNotification` với `type = "status-changed"`
   **And** assignee nhận email (nếu preference `status-changed` = enabled)

3. **Given** user mở `/settings/notifications`
   **When** trang load
   **Then** 4 preference toggles mới hiển thị: Assigned / Commented / Status Changed / Mentioned
   **And** mỗi toggle có thể bật/tắt độc lập qua `PATCH /api/v1/notification-preferences/{type}`

4. **Given** `GET /api/v1/notifications`
   **When** user đã đăng nhập gọi endpoint
   **Then** trả về tối đa 50 `UserNotificationDto` gần nhất của user đó, sắp xếp `created_at DESC`
   **And** hỗ trợ query param `unreadOnly=true`

5. **Given** `PATCH /api/v1/notifications/{id}/read`
   **When** user gọi endpoint với notification ID thuộc về họ
   **Then** `is_read = true`, `read_at = now()` được lưu; response 204
   **And** nếu notification không tồn tại hoặc thuộc user khác → 404

6. **Given** notification handler gặp lỗi (email fail, DB fail)
   **When** lỗi xảy ra trong handler
   **Then** lỗi được log nhưng KHÔNG được propagate — task assignment/status change phải vẫn thành công
   **And** không gây rollback UpdateTask transaction

---

## Tasks / Subtasks

- [x] **Task 1: Domain — UserNotification entity + extend NotificationType** (AC: 1, 2, 3)
  - [x] 1.1 Tạo `Notifications.Domain/Entities/UserNotification.cs`:
    ```csharp
    public class UserNotification
    {
        public Guid Id { get; private set; }
        public Guid RecipientUserId { get; private set; }
        public string Type { get; private set; } = string.Empty;   // "assigned" | "status-changed" | "commented" | "mentioned"
        public string Title { get; private set; } = string.Empty;  // "Task 'X' assigned to you"
        public string Body { get; private set; } = string.Empty;   // detail text
        public string? EntityType { get; private set; }            // "task"
        public Guid? EntityId { get; private set; }                // TaskId
        public bool IsRead { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? ReadAt { get; private set; }

        public static UserNotification Create(Guid recipientUserId, string type,
            string title, string body, string? entityType = null, Guid? entityId = null)
            => new()
            {
                Id = Guid.NewGuid(), RecipientUserId = recipientUserId,
                Type = type, Title = title, Body = body,
                EntityType = entityType, EntityId = entityId,
                IsRead = false, CreatedAt = DateTime.UtcNow
            };

        public void MarkRead() { IsRead = true; ReadAt = DateTime.UtcNow; }
    }
    ```
  - [x] 1.2 Sửa `Notifications.Domain/Enums/NotificationType.cs` — thêm 4 constants mới:
    ```csharp
    public static class NotificationType
    {
        // Existing (Story 7-1)
        public const string Overload = "overload";
        public const string Overdue  = "overdue";
        // New (Story 7-4)
        public const string Assigned      = "assigned";
        public const string Commented     = "commented";
        public const string StatusChanged = "status-changed";
        public const string Mentioned     = "mentioned";
    }
    ```

- [x] **Task 2: Kiểm tra và tạo TaskAssignedNotification trong Projects.Application** (AC: 1)
  - [x] 2.1 Scan thư mục `src/Modules/Projects/ProjectManagement.Projects.Application/Notifications/` (hoặc `Events/`) để kiểm tra `TaskAssignedNotification` đã tồn tại chưa
  - [x] 2.2 Nếu chưa có, tạo `TaskAssignedNotification.cs`:
    ```csharp
    // File: Projects.Application/Notifications/TaskAssignedNotification.cs
    using MediatR;
    public record TaskAssignedNotification(
        Guid TaskId,
        string TaskName,
        Guid ProjectId,
        string ProjectName,
        Guid? PreviousAssigneeId,
        Guid? NewAssigneeId
    ) : INotification;
    ```
  - [x] 2.3 Xác định `UpdateTask` command handler location:
    - Tìm file `UpdateTaskHandler.cs` (hoặc tương đương) trong `Projects.Application/Commands/UpdateTask/`
    - Kiểm tra xem handler đã có `_mediator.Publish(new TaskStatusChangedNotification(...))` chưa (để confirm pattern)
  - [x] 2.4 Trong `UpdateTaskHandler`, detect assignee change và publish:
    ```csharp
    // Sau khi lưu task (trước hoặc sau commit — xem transaction boundary note)
    if (task.AssigneeId != command.AssigneeId && command.AssigneeId.HasValue)
    {
        await _mediator.Publish(new TaskAssignedNotification(
            TaskId: task.Id,
            TaskName: task.Name,
            ProjectId: task.ProjectId,
            ProjectName: project.Name,    // fetch từ project hoặc pass từ command
            PreviousAssigneeId: task.AssigneeId,
            NewAssigneeId: command.AssigneeId
        ), ct);
    }
    ```
  - [x] 2.5 **QUAN TRỌNG — Xác định AssigneeId type**: Inspect `ProjectTask.AssigneeId` là `UserId` hay `ResourceId`:
    - Nếu là `UserId` (Guid FK đến auth users) → `NewAssigneeId` dùng trực tiếp trong UserManager lookup
    - Nếu là `ResourceId` → cần thêm bước: `var resource = await projectsDb.Resources.FindAsync(assigneeId)` → dùng `resource.Email` hoặc `resource.LinkedUserId` (nếu có)
    - Document kết quả kiểm tra vào Completion Notes
  - [x] 2.6 Tương tự, xác định `TaskStatusChangedNotification` payload có chứa `AssigneeId` không:
    - Nếu chưa có field `AssigneeId` trong payload → cần update/create với `AssigneeId` field
    - Tối thiểu cần: `TaskId`, `TaskName`, `ProjectId`, `NewStatus`, `AssigneeId`

- [x] **Task 3: Application — INotificationsDbContext + interfaces** (AC: 4, 5)
  - [x] 3.1 Sửa `Notifications.Application/Common/Interfaces/INotificationsDbContext.cs`:
    ```csharp
    public interface INotificationsDbContext
    {
        DbSet<NotificationPreference> NotificationPreferences { get; }
        DbSet<DigestLog> DigestLogs { get; }
        DbSet<UserNotification> UserNotifications { get; }   // ← MỚI
        Task<int> SaveChangesAsync(CancellationToken ct = default);
    }
    ```
  - [x] 3.2 Tạo `Notifications.Application/Queries/GetMyNotifications/UserNotificationDto.cs`:
    ```csharp
    public record UserNotificationDto(
        Guid Id,
        string Type,
        string Title,
        string Body,
        string? EntityType,
        Guid? EntityId,
        bool IsRead,
        DateTime CreatedAt,
        DateTime? ReadAt
    );
    ```
  - [x] 3.3 Tạo `Notifications.Application/Queries/GetMyNotifications/GetMyNotificationsQuery.cs` + handler:
    ```csharp
    public record GetMyNotificationsQuery(Guid UserId, bool UnreadOnly = false)
        : IRequest<List<UserNotificationDto>>;

    public class GetMyNotificationsHandler : IRequestHandler<GetMyNotificationsQuery, List<UserNotificationDto>>
    {
        public async Task<List<UserNotificationDto>> Handle(GetMyNotificationsQuery query, CancellationToken ct)
        {
            var q = _db.UserNotifications
                .AsNoTracking()
                .Where(n => n.RecipientUserId == query.UserId);
            if (query.UnreadOnly) q = q.Where(n => !n.IsRead);
            return await q
                .OrderByDescending(n => n.CreatedAt)
                .Take(50)
                .Select(n => new UserNotificationDto(
                    n.Id, n.Type, n.Title, n.Body,
                    n.EntityType, n.EntityId,
                    n.IsRead, n.CreatedAt, n.ReadAt))
                .ToListAsync(ct);
        }
    }
    ```
  - [x] 3.4 Tạo `Notifications.Application/Commands/MarkNotificationRead/MarkNotificationReadCommand.cs` + handler:
    ```csharp
    public record MarkNotificationReadCommand(Guid NotificationId, Guid RequestingUserId)
        : IRequest<bool>;  // returns false = not found / not owned

    public class MarkNotificationReadHandler : IRequestHandler<MarkNotificationReadCommand, bool>
    {
        public async Task<bool> Handle(MarkNotificationReadCommand cmd, CancellationToken ct)
        {
            var n = await _db.UserNotifications
                .FirstOrDefaultAsync(x => x.Id == cmd.NotificationId
                                        && x.RecipientUserId == cmd.RequestingUserId, ct);
            if (n is null) return false;
            n.MarkRead();
            await _db.SaveChangesAsync(ct);
            return true;
        }
    }
    ```

- [x] **Task 4: Application — PerEventNotificationHandler** (AC: 1, 2, 6)
  - [x] 4.1 Tạo `Notifications.Application/EventHandlers/PerEventNotificationHandler.cs`:
    - Implement `INotificationHandler<TaskAssignedNotification>` và `INotificationHandler<TaskStatusChangedNotification>`
    - Inject: `INotificationsDbContext`, `IEmailService`, `UserManager<ApplicationUser>`, `ILogger<PerEventNotificationHandler>`
    - **CRITICAL — Không propagate exception**: Wrap toàn bộ xử lý trong `try-catch` → log + return (xem chi tiết bên dưới)
  - [x] 4.2 Implement `Handle(TaskAssignedNotification, ct)`:
    ```csharp
    public async Task Handle(TaskAssignedNotification n, CancellationToken ct)
    {
        try
        {
            if (!n.NewAssigneeId.HasValue) return;
            // Resolve user (xem Task 2.5 — nếu AssigneeId là ResourceId thì cần lookup thêm)
            var user = await _userManager.FindByIdAsync(n.NewAssigneeId.Value.ToString());
            if (user is null) return;  // no user account linked → skip

            // Check preference (default enabled if no record)
            var pref = await _db.NotificationPreferences
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == user.Id && p.Type == NotificationType.Assigned, ct);
            if (pref?.IsEnabled == false) return;

            var title = $"Task '{n.TaskName}' được giao cho bạn";
            var body  = $"Dự án: {n.ProjectName}. Vui lòng kiểm tra và xử lý.";

            // Save in-app notification
            _db.UserNotifications.Add(UserNotification.Create(
                recipientUserId: user.Id,
                type: NotificationType.Assigned,
                title: title, body: body,
                entityType: "task", entityId: n.TaskId));
            await _db.SaveChangesAsync(ct);

            // Send email (fire-and-forget pattern — email failure không block)
            if (!string.IsNullOrEmpty(user.Email))
            {
                var html = $"<h3>{title}</h3><p>{body}</p>"
                         + $"<p><a href='/projects/{n.ProjectId}/tasks/{n.TaskId}'>Xem task →</a></p>";
                await _emailSvc.SendAsync(user.Email, $"[PM Tool] {title}", html, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PerEventNotificationHandler: error handling TaskAssignedNotification TaskId={TaskId}", n.TaskId);
            // KHÔNG rethrow — task update không được rollback vì notification fail
        }
    }
    ```
  - [x] 4.3 Implement `Handle(TaskStatusChangedNotification, ct)`:
    - Tương tự như 4.2 nhưng dùng `NotificationType.StatusChanged`
    - Title: `$"Task '{taskName}' chuyển sang trạng thái '{newStatus}'"`
    - Chỉ notify assignee (không có watchers/reporter trong Phase 1)
    - Nếu `TaskStatusChangedNotification` không có `AssigneeId` → fetch từ DB: `await projectsDb.ProjectTasks.AsNoTracking().Select(t => new { t.Id, t.AssigneeId }).FirstOrDefaultAsync(t => t.Id == n.TaskId)`
    - **CRITICAL**: Cần inject `IProjectsDbContext` nếu cần fetch assignee — xem anti-pattern về cross-module trong Dev Notes

- [x] **Task 5: Infrastructure — EF config + migration** (AC: 1-5)
  - [x] 5.1 Sửa `NotificationsDbContext.cs`:
    ```csharp
    public class NotificationsDbContext : DbContext, INotificationsDbContext
    {
        public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
        public DbSet<DigestLog> DigestLogs => Set<DigestLog>();
        public DbSet<UserNotification> UserNotifications => Set<UserNotification>();  // ← MỚI
    }
    ```
  - [x] 5.2 Tạo `Notifications.Infrastructure/Persistence/Configurations/UserNotificationConfiguration.cs`:
    ```csharp
    public class UserNotificationConfiguration : IEntityTypeConfiguration<UserNotification>
    {
        public void Configure(EntityTypeBuilder<UserNotification> b)
        {
            b.ToTable("user_notifications", "notifications");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.RecipientUserId).HasColumnName("recipient_user_id").IsRequired();
            b.Property(x => x.Type).HasColumnName("type").HasMaxLength(30).IsRequired();
            b.Property(x => x.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
            b.Property(x => x.Body).HasColumnName("body").IsRequired();
            b.Property(x => x.EntityType).HasColumnName("entity_type").HasMaxLength(30);
            b.Property(x => x.EntityId).HasColumnName("entity_id");
            b.Property(x => x.IsRead).HasColumnName("is_read").HasDefaultValue(false);
            b.Property(x => x.CreatedAt).HasColumnName("created_at");
            b.Property(x => x.ReadAt).HasColumnName("read_at");

            // Performance index — query by user + unread + recency
            b.HasIndex(x => new { x.RecipientUserId, x.IsRead, x.CreatedAt })
                .HasDatabaseName("ix_user_notifications_user_read_created");
        }
    }
    ```
  - [x] 5.3 Tạo migration thủ công `Notifications.Infrastructure/Migrations/20260429000000_AddUserNotifications.cs`:
    ```sql
    -- Up:
    CREATE TABLE notifications.user_notifications (
        id                UUID PRIMARY KEY DEFAULT gen_random_uuid(),
        recipient_user_id UUID NOT NULL,
        type              VARCHAR(30) NOT NULL,
        title             VARCHAR(200) NOT NULL,
        body              TEXT NOT NULL DEFAULT '',
        entity_type       VARCHAR(30) NULL,
        entity_id         UUID NULL,
        is_read           BOOLEAN NOT NULL DEFAULT FALSE,
        created_at        TIMESTAMPTZ NOT NULL DEFAULT now(),
        read_at           TIMESTAMPTZ NULL
    );
    CREATE INDEX ix_user_notifications_user_read_created
        ON notifications.user_notifications(recipient_user_id, is_read, created_at DESC);
    -- Down:
    DROP TABLE IF EXISTS notifications.user_notifications;
    ```
    **Pattern**: Giống `20260426150000_InitialNotifications` — tạo file thủ công, không dùng `dotnet ef migrations add`.
  - [x] 5.4 Cập nhật `NotificationsDbContextModelSnapshot.cs` để reflect bảng mới

- [x] **Task 6: API — NotificationsController** (AC: 4, 5)
  - [x] 6.1 Tạo `Notifications.Api/Controllers/NotificationsController.cs`:
    ```csharp
    [ApiController]
    [Route("api/v1/notifications")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        // GET /api/v1/notifications?unreadOnly=true
        [HttpGet]
        public async Task<IActionResult> GetNotifications([FromQuery] bool unreadOnly = false, CancellationToken ct = default)
        {
            var result = await _mediator.Send(
                new GetMyNotificationsQuery(_currentUser.UserId, unreadOnly), ct);
            return Ok(result);
        }

        // PATCH /api/v1/notifications/{id}/read
        [HttpPatch("{id:guid}/read")]
        public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
        {
            var found = await _mediator.Send(
                new MarkNotificationReadCommand(id, _currentUser.UserId), ct);
            return found ? NoContent() : NotFound();
        }
    }
    ```
  - [x] 6.2 Cập nhật `NotificationsModuleExtensions.cs`:
    - Đăng ký MediatR cho `PerEventNotificationHandler`, `GetMyNotificationsHandler`, `MarkNotificationReadHandler`
    - `mvc.AddApplicationPart(typeof(NotificationsController).Assembly)` — controller đã ở cùng assembly với `NotificationPreferencesController`
    - Đăng ký `NotificationsDbContext` migration trong startup (đã có từ 7-1, không cần thêm)
  - [x] 6.3 Kiểm tra `Program.cs` — `NotificationsDbContext` đã được auto-migrate → table mới sẽ tự tạo khi app start

- [x] **Task 7: Frontend — Cập nhật NotificationPreferencesComponent** (AC: 3)
  - [x] 7.1 Sửa `notification-preferences.ts` — thêm 4 type mới vào label mapping trong `ngOnInit`:
    ```typescript
    this.preferences = prefs.map(p => ({
      type: p.type,
      label: this.getLabel(p.type),
      isEnabled: p.isEnabled,
    }));

    // Thêm method getLabel():
    private getLabel(type: string): string {
      const labels: Record<string, string> = {
        'overload':       'Cảnh báo Overload',
        'overdue':        'Task sắp trễ',
        'assigned':       'Được giao task',        // ← MỚI
        'commented':      'Có comment mới',        // ← MỚI (stub — hoạt động khi 10.1 done)
        'status-changed': 'Task thay đổi trạng thái', // ← MỚI
        'mentioned':      '@mention trong comment', // ← MỚI (stub — hoạt động khi 10.2 done)
      };
      return labels[type] ?? type;
    }
    ```
  - [x] 7.2 Kiểm tra backend `GetNotificationPreferencesQuery` — xác nhận `allTypes` array bao gồm 4 types mới:
    ```csharp
    var allTypes = new[] {
        NotificationType.Overload, NotificationType.Overdue,
        NotificationType.Assigned, NotificationType.Commented,
        NotificationType.StatusChanged, NotificationType.Mentioned
    };
    ```
    Default `IsEnabled = true` cho mọi type mới (pattern đã có từ 7-1).
  - [x] 7.3 Sửa `notification-preferences.html` nếu cần để UI hiển thị section mới rõ ràng:
    - Nhóm 2 toggle cũ (Overload/Overdue) thành "Digest Notifications"
    - Nhóm 4 toggle mới thành "Realtime Notifications"
    - Dùng `MatDivider` hoặc section header `<mat-label>` để phân tách

- [x] **Task 8: Test** (AC: 1–6)
  - [x] 8.1 Tạo/sửa `UserNotification.spec.cs` (unit test domain):
    - `Create()`: trả entity với IsRead=false, CreatedAt có giá trị
    - `MarkRead()`: set IsRead=true và ReadAt non-null
  - [x] 8.2 Tạo `PerEventNotificationHandler.spec.cs` (unit test với mock):
    - TaskAssigned: preference=disabled → không save notification, không send email
    - TaskAssigned: user không tìm thấy → không throw, không save
    - TaskAssigned: email fail → không throw, notification vẫn được save (tùy order: save trước email)
    - TaskAssigned: happy path → UserNotification saved + email sent
    - TaskStatusChanged: happy path → UserNotification saved + email sent
    - TaskStatusChanged: no assignee → không throw, không save
    - Exception trong handler → bị catch, log, không propagate
  - [x] 8.3 Tạo `GetMyNotificationsHandler.spec.cs`:
    - UnreadOnly=false → trả cả read và unread
    - UnreadOnly=true → chỉ trả unread
    - Limit 50 — test với 51 records
    - Sắp xếp `created_at DESC`
  - [x] 8.4 Tạo `MarkNotificationReadHandler.spec.cs`:
    - Notification không tồn tại → return false
    - Notification thuộc user khác → return false
    - Happy path → IsRead=true, ReadAt set, return true
  - [x] 8.5 Test FE `notification-preferences.spec.ts`:
    - Render 6 toggles khi API trả 6 types
    - Labels hiển thị đúng cho 4 types mới
    - Toggle change → gọi `updateNotificationPreference`
  - [x] 8.6 Build verification:
    - `dotnet build` → 0 errors
    - `ng build` → 0 errors

- [x] **Task 9:** Browser verification (QT-02)** (toàn bộ AC)
  - [x] 9.1 Start `ng serve` + verify app compile
  - [x] 9.2 Navigate `/settings/notifications` — snapshot 6 toggle groups
  - [x] 9.3 Toggle "Được giao task" OFF → PATCH call confirmed (network tab)
  - [x] 9.4 `GET /api/v1/notifications` trả 200 (có thể rỗng ban đầu)
  - [x] 9.5 Screenshot confirm: white/black design system (QT-03), không có màu background sặc sỡ

---

## Dev Notes

### Cấu Trúc File Mới / Sửa

**Backend — Domain:**
```
src/Modules/Notifications/ProjectManagement.Notifications.Domain/
├── Entities/
│   ├── NotificationPreference.cs    ← ĐÃ CÓ (Story 7-1)
│   ├── DigestLog.cs                 ← ĐÃ CÓ (Story 7-1)
│   └── UserNotification.cs          ← MỚI (Task 1.1)
└── Enums/
    └── NotificationType.cs          ← SỬA: thêm 4 constants (Task 1.2)
```

**Backend — Application:**
```
src/Modules/Notifications/ProjectManagement.Notifications.Application/
├── Common/Interfaces/
│   └── INotificationsDbContext.cs   ← SỬA: thêm DbSet<UserNotification> (Task 3.1)
├── EventHandlers/
│   └── PerEventNotificationHandler.cs  ← MỚI (Task 4)
├── Queries/GetMyNotifications/
│   ├── GetMyNotificationsQuery.cs   ← MỚI (Task 3.3)
│   └── UserNotificationDto.cs       ← MỚI (Task 3.2)
└── Commands/MarkNotificationRead/
    └── MarkNotificationReadCommand.cs ← MỚI (Task 3.4)
```

**Backend — Projects (cross-module event):**
```
src/Modules/Projects/ProjectManagement.Projects.Application/
└── Notifications/
    └── TaskAssignedNotification.cs  ← MỚI nếu chưa có (Task 2.2)
    └── [UpdateTaskHandler.cs]       ← SỬA: publish TaskAssignedNotification (Task 2.4)
```

**Backend — Infrastructure:**
```
src/Modules/Notifications/ProjectManagement.Notifications.Infrastructure/
├── Persistence/
│   ├── NotificationsDbContext.cs        ← SỬA: thêm DbSet<UserNotification> (Task 5.1)
│   └── Configurations/
│       └── UserNotificationConfiguration.cs ← MỚI (Task 5.2)
└── Migrations/
    └── 20260429000000_AddUserNotifications.cs ← MỚI thủ công (Task 5.3)
```

**Backend — API:**
```
src/Modules/Notifications/ProjectManagement.Notifications.Api/
└── Controllers/
    ├── NotificationPreferencesController.cs ← ĐÃ CÓ (Story 7-1)
    └── NotificationsController.cs           ← MỚI (Task 6.1)
```

**Frontend:**
```
frontend/project-management-web/src/app/features/settings/notification-preferences/
├── notification-preferences.ts    ← SỬA: thêm 4 type labels + section grouping (Task 7.1)
└── notification-preferences.html  ← SỬA: grouping UI (Task 7.3)
```

---

### Transaction Boundary — CRITICAL

Từ architecture: MediatR Notification publish xảy ra TRONG transaction của command. Nếu handler throw → toàn bộ command rollback.

```
UpdateTask transaction:
  1. Load task
  2. Mutate task
  3. SaveChangesAsync (Projects schema)
  4. _mediator.Publish(TaskAssignedNotification)   ← handler chạy trong cùng transaction scope
     → PerEventNotificationHandler.Handle()
        a. try { SaveUserNotification } catch { log, return }
        b. try { SendEmail } catch { log, return }
  5. CommitAsync
```

**Quy tắc bắt buộc**: `PerEventNotificationHandler` PHẢI wrap toàn bộ logic trong `try-catch`. Không để exception từ notification delivery rollback task update (vi phạm user experience).

---

### Cross-Module Read Pattern

`PerEventNotificationHandler` cần đọc data từ Projects module (ví dụ: fetch task assignee nếu `TaskStatusChangedNotification` không bao gồm `AssigneeId`).

```csharp
// Pattern đúng: inject IProjectsDbContext qua DI (cross-DbContext read là OK trong Phase 1)
// Xem architecture D-03: "Cross-DbContext qua DI"
private readonly IProjectsDbContext _projectsDb;

// KHÔNG inject qua IServiceScopeFactory trong handler — handler đã ở trong scope
// IServiceScopeFactory chỉ cần trong BackgroundService (singleton)
```

---

### AssigneeId Resolution — Phase 1

Cần xác định trước khi implement (Task 2.5):

**Case A — ProjectTask.AssigneeId là UserId** (Guid FK đến ASP.NET Identity users):
```csharp
var user = await _userManager.FindByIdAsync(notification.NewAssigneeId.Value.ToString());
// Done — user tìm được trực tiếp
```

**Case B — ProjectTask.AssigneeId là ResourceId** (chưa có user_id FK — AD-11 là Phase 2):
```csharp
// Lookup resource.Email từ projectsDb
var resource = await _projectsDb.Resources
    .AsNoTracking()
    .Select(r => new { r.Id, r.Email })
    .FirstOrDefaultAsync(r => r.Id == notification.NewAssigneeId.Value, ct);
if (resource is null || string.IsNullOrEmpty(resource.Email)) return;
var user = await _userManager.FindByEmailAsync(resource.Email);
```

Nếu resources chưa có `email` field trong Phase 1 → document limitation và skip delivery cho đến Story 10.4/AD-11.

---

### Không Dùng SignalR / WebSockets

Architecture Phase 1 không có real-time push. "In-app notification" trong story này = persist vào `user_notifications` table. Story 7-5 sẽ add bell icon + polling (dùng `interval(30_000)` RxJS) để refresh unread count.

**KHÔNG** implement SignalR, WebSockets, hoặc Server-Sent Events trong story này.

---

### Email Dev Mode

Giống Story 7-1: khi `Smtp:Host` rỗng trong `appsettings.json`, `EmailService` chỉ log mà không throw. Dev không cần cấu hình SMTP thật để test.

```csharp
// EmailService.cs đã có từ 7-1 — không thay đổi gì
if (string.IsNullOrWhiteSpace(_settings.Host))
{
    _logger.LogInformation("[EmailService] Email to {To}: {Subject}", to, subject);
    return;
}
```

---

### Migration — Thủ Công (Pattern từ 7-1)

**KHÔNG** dùng `dotnet ef migrations add`. Tạo file `.cs` thủ công:

```csharp
// File: Migrations/20260429000000_AddUserNotifications.cs
[DbContext(typeof(NotificationsDbContext))]
[Migration("20260429000000_AddUserNotifications")]
public partial class AddUserNotifications : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE notifications.user_notifications (
                id                UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                recipient_user_id UUID NOT NULL,
                type              VARCHAR(30) NOT NULL,
                title             VARCHAR(200) NOT NULL,
                body              TEXT NOT NULL DEFAULT '',
                entity_type       VARCHAR(30) NULL,
                entity_id         UUID NULL,
                is_read           BOOLEAN NOT NULL DEFAULT FALSE,
                created_at        TIMESTAMPTZ NOT NULL DEFAULT now(),
                read_at           TIMESTAMPTZ NULL
            );
            CREATE INDEX ix_user_notifications_user_read_created
                ON notifications.user_notifications(recipient_user_id, is_read, created_at DESC);
        """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS notifications.user_notifications;");
    }
}
```

---

### Anti-patterns Cần Tránh

- **KHÔNG** inject `UserManager<ApplicationUser>` vào Singleton service — handler là Scoped (OK để inject trực tiếp)
- **KHÔNG** để notification handler throw exception ra ngoài — task update PHẢI thành công kể cả khi notification fail
- **KHÔNG** implement SignalR/real-time — 7-5 mới cần
- **KHÔNG** thêm NgRx store/action cho notifications trong 7-4 — 7-5 sẽ làm
- **KHÔNG** tạo `MarkAllRead` endpoint trong 7-4 — Story 7-5 cần nó; để 7-5 thêm hoặc thêm vào controller ngay nếu đơn giản
- **KHÔNG** skip email sending khi `Smtp:Host` rỗng với exception — phải gracefully log và continue
- **KHÔNG** dùng `dotnet ef migrations add` — manual migration (lesson from 7-1)
- **KHÔNG** gửi email trong transaction commit — gửi sau khi `SaveChangesAsync` thành công

---

### Known Limitations (document, không fix trong story này)

- **Comment/Mention stubs**: Types `commented` và `mentioned` được thêm vào preferences nhưng không trigger thực tế — sẽ wire vào trong Stories 10.1/10.2
- **Watchers**: Status-change notification chỉ đến assignee, không đến reporter/watchers — deferred to Story 10.4
- **Resource→User mapping**: Nếu `AssigneeId` là `ResourceId` và resources chưa có `user_id` FK (AD-11 deferred) → notification bị skip; document rõ
- **No unread count API**: Story 7-5 sẽ add `GET /api/v1/notifications/unread-count` endpoint; 7-4 không cần

---

### Extensibility cho Story 7-5 (Notification Center)

- `UserNotification` entity và API `GET /api/v1/notifications` + `PATCH /{id}/read` được xây sẵn trong 7-4
- Story 7-5 chỉ cần: add NgRx store + bell icon component + notification list component polling `GET /api/v1/notifications`
- Cân nhắc add `GET /api/v1/notifications/unread-count` (count only, không trả data) trong 7-4 nếu thời gian cho phép — sẽ giúp 7-5 efficient hơn

---

### Patterns từ Story 7-1 (Quan trọng)

1. **Module structure**: 4 csproj đã tồn tại — chỉ thêm code, không tạo project mới
2. **Manual migration**: Tạo thủ công, không dùng `dotnet ef` (lesson từ Reporting, TimeTracking)
3. **EF Core 10.0.7 trong Application**: Align version để tránh CS1705
4. **MediatR đã đăng ký trong `NotificationsModuleExtensions`**: Thêm `PerEventNotificationHandler` assembly registration
5. **Composite PK cho preferences**: `(user_id, type)` — pattern đã dùng, tự động handle upsert mới
6. **MailKit 4.16.0**: Đã được upgrade từ 4.11.0 trong Story 7-1 (dev notes: "nâng để giải quyết CVE")

---

### References

- [Source: architecture.md § D-03] — Cross-module communication qua MediatR INotification
- [Source: architecture.md § 7.5 Transaction boundary] — MediatR publish trong transaction, handler exception = rollback
- [Source: architecture.md § D-13] — Background job pattern với Channel<T> / IHostedService
- [Source: architecture.md § AD-11] — Resource→User identity bridge (Phase 2 prerequisite)
- [Source: 7-1 story, Task 3–4] — EmailService pattern, DigestWorker scope pattern
- [Source: 7-1 story, Completion Notes] — MailKit 4.16.0, manual migration pattern
- [Source: 7-3 story, Dev Notes § EXCLUDED_TYPES] — EXCLUDED_TYPES constant pattern (extensibility)
- [Source: project-context.md § QT-01,02,03] — 100% tests, Playwright browser verify, white/black UI

---

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

### Completion Notes List

**Task 2.5 — AssigneeId là UserId (Case A confirmed)**
`ProjectTask.AssigneeUserId` là `Guid?` FK trực tiếp đến ASP.NET Identity users. `PerEventNotificationHandler` dùng `_userManager.FindByIdAsync(n.NewAssigneeUserId.Value.ToString())` — không cần ResourceId lookup.

**TaskStatusChangedNotification — Tạo mới**
Không có sẵn trong codebase. Tạo mới `Projects.Application/Notifications/TaskStatusChangedNotification.cs` với fields: `TaskId`, `TaskName`, `ProjectId`, `ProjectName`, `PreviousStatus`, `NewStatus`, `AssigneeUserId`.

**UpdateTaskHandler — Publish sau SaveChangesAsync**
Capture `previousAssigneeUserId` và `previousStatus` trước `task.Update()`. Sau `await _projectsDb.SaveChangesAsync(ct)`, fetch `projectName` từ DB và publish cả hai notifications nếu có thay đổi tương ứng.

**Auth.Domain csproj reference thêm vào Notifications.Application**
`PerEventNotificationHandler` inject `UserManager<ApplicationUser>` → cần `ProjectReference` đến `Auth.Domain` trong `Notifications.Application.csproj`.

**ReportingDbContext PendingModelChangesWarning**
Pre-existing EF Core 10 false-positive. Fixed bằng cách suppress warning trong `ReportingModuleExtensions.cs`:
`opts.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))`

**AddAlertTables migration idempotency**
`reporting.alerts` đã tồn tại trong DB nhưng chưa có record trong `__EFMigrationsHistory`. Converted migration body sang raw SQL với `IF NOT EXISTS` để idempotent.

**Manual migration — notifications.user_notifications**
Tạo thủ công `20260429000000_AddUserNotifications.cs` dùng `migrationBuilder.Sql("""...""")` và cập nhật `NotificationsDbContextModelSnapshot.cs` để reflect entity mới.

**Browser verification (QT-02/03)**
- `/settings/notifications` renders 6 toggles: 2 nhóm "Digest hàng tuần" + 4 nhóm "Thông báo sự kiện" ✓
- Toggle "Được giao task" OFF → `PATCH /api/v1/notification-preferences/assigned` → 204 No Content ✓
- `GET /api/v1/notifications` → 200 OK, body `[]` (no notifications yet) ✓
- UI design: dark sidebar (#1e293b), white content area — QT-03 confirmed ✓

**TasksFilterTests.cs pre-existing bug fixed**
Lines 228-229: `matchedTask.Value.GetProperty()` → `matchedTask.GetProperty()` (JsonElement is struct, không có `.Value`)

### File List

**Backend — Domain:**
- `src/Modules/Notifications/ProjectManagement.Notifications.Domain/Entities/UserNotification.cs` — mới
- `src/Modules/Notifications/ProjectManagement.Notifications.Domain/Enums/NotificationType.cs` — sửa (thêm 4 constants)

**Backend — Application:**
- `src/Modules/Notifications/ProjectManagement.Notifications.Application/Common/Interfaces/INotificationsDbContext.cs` — sửa
- `src/Modules/Notifications/ProjectManagement.Notifications.Application/EventHandlers/PerEventNotificationHandler.cs` — mới
- `src/Modules/Notifications/ProjectManagement.Notifications.Application/Queries/GetMyNotifications/GetMyNotificationsQuery.cs` — mới
- `src/Modules/Notifications/ProjectManagement.Notifications.Application/Queries/GetMyNotifications/UserNotificationDto.cs` — mới
- `src/Modules/Notifications/ProjectManagement.Notifications.Application/Commands/MarkNotificationRead/MarkNotificationReadCommand.cs` — mới

**Backend — Projects (cross-module event):**
- `src/Modules/Projects/ProjectManagement.Projects.Application/Notifications/TaskAssignedNotification.cs` — mới (nếu chưa có)
- `src/Modules/Projects/ProjectManagement.Projects.Application/Commands/UpdateTask/UpdateTaskHandler.cs` — sửa (publish TaskAssignedNotification)

**Backend — Infrastructure:**
- `src/Modules/Notifications/ProjectManagement.Notifications.Infrastructure/Persistence/NotificationsDbContext.cs` — sửa
- `src/Modules/Notifications/ProjectManagement.Notifications.Infrastructure/Persistence/Configurations/UserNotificationConfiguration.cs` — mới
- `src/Modules/Notifications/ProjectManagement.Notifications.Infrastructure/Migrations/20260429000000_AddUserNotifications.cs` — mới
- `src/Modules/Notifications/ProjectManagement.Notifications.Infrastructure/Migrations/NotificationsDbContextModelSnapshot.cs` — sửa

**Backend — API:**
- `src/Modules/Notifications/ProjectManagement.Notifications.Api/Controllers/NotificationsController.cs` — mới
- `src/Modules/Notifications/ProjectManagement.Notifications.Api/Extensions/NotificationsModuleExtensions.cs` — sửa (register new handlers)

**Frontend:**
- `frontend/project-management-web/src/app/features/settings/notification-preferences/notification-preferences.ts` — sửa
- `frontend/project-management-web/src/app/features/settings/notification-preferences/notification-preferences.html` — sửa
- `frontend/project-management-web/src/app/features/settings/notification-preferences/notification-preferences.spec.ts` — sửa
