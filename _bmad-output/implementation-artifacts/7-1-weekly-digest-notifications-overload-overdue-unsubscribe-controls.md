# Story 7.1: Weekly digest notifications (overload + overdue) + unsubscribe controls

Status: review

**Story ID:** 7.1
**Epic:** Epic 7 — Operations Layer (Notifications + In-product transparency metrics)
**Sprint:** Sprint 8
**Date Created:** 2026-04-26

---

## Story

As a PM,
I want nhận weekly digest email/in-app cho overload và task sắp trễ,
So that tôi không bỏ sót rủi ro ngay cả khi không mở tool.

## Acceptance Criteria

1. **Given** lịch chạy hàng tuần (mỗi thứ Hai 7:00 UTC)
   **When** DigestWorker kích hoạt
   **Then** mỗi PM nhận email chứa: danh sách resource bị overload + danh sách task sắp trễ (PlannedEndDate trong 7 ngày tới hoặc đã quá hạn) — chỉ trong phạm vi project của PM

2. **Given** user đã tắt notification type cụ thể
   **When** digest chạy
   **Then** user KHÔNG nhận email cho type đã tắt

3. **Given** digest đã gửi trong tuần hiện tại cho user (DigestLog)
   **When** worker chạy lại (restart)
   **Then** email KHÔNG được gửi lần 2 trong cùng tuần — coalesce bằng ISO week

4. **Given** user truy cập `/settings/notifications`
   **When** toggle preference
   **Then** `PATCH /api/v1/notification-preferences` cập nhật preference, phản ánh ngay trên UI

5. **Given** email không có người nhận hợp lệ (email null/empty)
   **When** worker chạy
   **Then** bỏ qua user đó, log warning, tiếp tục các user khác

---

## Tasks / Subtasks

- [x] **Task 1: Tạo Notifications module — Domain**
  - [x] 1.1 Tạo `Notifications.Domain/Entities/NotificationPreference.cs`
         (UserId, Type: string, IsEnabled, UpdatedAt)
  - [x] 1.2 Tạo `Notifications.Domain/Entities/DigestLog.cs`
         (Id, UserId, DigestType, IsoWeek, Year, SentAt)
  - [x] 1.3 Tạo `Notifications.Domain/Enums/NotificationType.cs`
         (`const string`: `"overload"`, `"overdue"`)
  - [x] 1.4 Tạo project file `ProjectManagement.Notifications.Domain.csproj`

- [x] **Task 2: Tạo Notifications module — Application**
  - [x] 2.1 Tạo `INotificationsDbContext` interface (`DbSet<NotificationPreference>`, `DbSet<DigestLog>`, `SaveChangesAsync`)
  - [x] 2.2 Tạo `IEmailService` interface (`SendAsync(to, subject, htmlBody)`)
  - [x] 2.3 Tạo `Commands/UpdateNotificationPreference/UpdateNotificationPreferenceCommand.cs` + handler
         (upsert preference cho CurrentUserId)
  - [x] 2.4 Tạo `Queries/GetNotificationPreferences/GetNotificationPreferencesQuery.cs` + handler
         (trả list preference của CurrentUserId)
  - [x] 2.5 Tạo project file `ProjectManagement.Notifications.Application.csproj`
         (refs: Domain, Projects.Application, Capacity.Application + MediatR, EF Core 10.0.7)

- [x] **Task 3: Tạo Notifications module — Infrastructure**
  - [x] 3.1 Tạo `NotificationsDbContext` (schema `"notifications"`)
  - [x] 3.2 Tạo `NotificationPreferenceConfiguration` + `DigestLogConfiguration`
  - [x] 3.3 Tạo migration `20260426150000_InitialNotifications` (2 bảng)
  - [x] 3.4 Tạo `ModelSnapshot`
  - [x] 3.5 Tạo `EmailService.cs` implements `IEmailService` — dùng MailKit
         (SMTP config từ `appsettings.json`: `Smtp:Host`, `Smtp:Port`, `Smtp:User`, `Smtp:Pass`, `Smtp:From`)
  - [x] 3.6 Tạo `DigestWorker.cs` implements `BackgroundService` với `PeriodicTimer`
  - [x] 3.7 Tạo project file `ProjectManagement.Notifications.Infrastructure.csproj`
         (MailKit 4.16.0, Npgsql.EFCore, EF.Design)

- [x] **Task 4: Tạo Notifications module — Api**
  - [x] 4.1 Tạo `NotificationPreferencesController` endpoint:
         `GET /api/v1/notification-preferences` + `PATCH /api/v1/notification-preferences/{type}`
  - [x] 4.2 Tạo `NotificationsModuleExtensions.cs`
  - [x] 4.3 Tạo project file `ProjectManagement.Notifications.Api.csproj`

- [x] **Task 5: Đăng ký Notifications module trong Host**
  - [x] 5.1 Thêm Notifications.Api reference vào `ProjectManagement.Host.csproj`
  - [x] 5.2 Thêm `AddNotificationsModule(...)` vào `Program.cs`
  - [x] 5.3 Thêm `NotificationsDbContext` migration vào auto-migrate block
  - [x] 5.4 Thêm SMTP config vào `appsettings.json`
  - [x] 5.5 Thêm Notifications projects vào `ProjectManagement.slnx`

- [x] **Task 6: Frontend — NotificationPreferences component**
  - [x] 6.1 Tạo `features/settings/notification-preferences/notification-preferences.ts` + html
         (2 toggles: Overload / Overdue; dùng `MatSlideToggleModule`, NO NgRx)
  - [x] 6.2 Tạo `features/settings/services/settings-api.service.ts`:
         `getPreferences()` + `updatePreference(type, enabled)`
  - [x] 6.3 Thêm route `{ path: 'settings/notifications', loadComponent: NotificationPreferencesComponent }` vào `app.routes.ts`

- [x] **Task 7: Build verification**
  - [x] 7.1 `dotnet build` → 0 errors
  - [x] 7.2 `ng build` → 0 errors

---

## Dev Notes

### Module structure mới — Notifications

Story này tạo Notifications module hoàn toàn mới (4 projects). Pattern giống Reporting module từ 6-1/6-3.

```
src/Modules/Notifications/
├── ProjectManagement.Notifications.Domain/
├── ProjectManagement.Notifications.Application/
├── ProjectManagement.Notifications.Infrastructure/
└── ProjectManagement.Notifications.Api/
```

---

### Task 1 — Domain Entities

**NotificationPreference:**
```csharp
// File: Notifications.Domain/Entities/NotificationPreference.cs
public class NotificationPreference
{
    public Guid UserId { get; private set; }
    public string Type { get; private set; } = string.Empty;  // "overload" | "overdue"
    public bool IsEnabled { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public static NotificationPreference Create(Guid userId, string type, bool isEnabled)
        => new() { UserId = userId, Type = type, IsEnabled = isEnabled, UpdatedAt = DateTime.UtcNow };

    public void SetEnabled(bool isEnabled) { IsEnabled = isEnabled; UpdatedAt = DateTime.UtcNow; }
}
```

**DigestLog:**
```csharp
// File: Notifications.Domain/Entities/DigestLog.cs
public class DigestLog
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string DigestType { get; private set; } = string.Empty;
    public int IsoWeek { get; private set; }
    public int Year { get; private set; }
    public DateTime SentAt { get; private set; }

    public static DigestLog Create(Guid userId, string digestType, int isoWeek, int year)
        => new()
        {
            Id = Guid.NewGuid(), UserId = userId, DigestType = digestType,
            IsoWeek = isoWeek, Year = year, SentAt = DateTime.UtcNow
        };
}
```

**NotificationType:**
```csharp
// File: Notifications.Domain/Enums/NotificationType.cs
public static class NotificationType
{
    public const string Overload = "overload";
    public const string Overdue  = "overdue";
}
```

---

### Task 2 — Application interfaces

**INotificationsDbContext:**
```csharp
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Notifications.Domain.Entities;

public interface INotificationsDbContext
{
    DbSet<NotificationPreference> NotificationPreferences { get; }
    DbSet<DigestLog> DigestLogs { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
```

**IEmailService:**
```csharp
public interface IEmailService
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
}
```

**UpdateNotificationPreferenceCommand handler:**
```csharp
// Upsert: Find existing preference → update; not found → create
var pref = await _db.NotificationPreferences
    .FirstOrDefaultAsync(p => p.UserId == cmd.UserId && p.Type == cmd.Type, ct);
if (pref is null)
{
    pref = NotificationPreference.Create(cmd.UserId, cmd.Type, cmd.IsEnabled);
    _db.NotificationPreferences.Add(pref);
}
else
{
    pref.SetEnabled(cmd.IsEnabled);
}
await _db.SaveChangesAsync(ct);
```

**GetNotificationPreferencesQuery** — trả list với defaults (nếu chưa có record → default IsEnabled = true):
```csharp
var stored = await _db.NotificationPreferences
    .AsNoTracking()
    .Where(p => p.UserId == query.UserId)
    .ToListAsync(ct);

var allTypes = new[] { NotificationType.Overload, NotificationType.Overdue };
return allTypes.Select(type =>
{
    var pref = stored.FirstOrDefault(p => p.Type == type);
    return new NotificationPreferenceDto(type, pref?.IsEnabled ?? true);
}).ToList();
```

---

### Task 2.5 — Application.csproj dependencies

```xml
<ItemGroup>
  <PackageReference Include="MediatR" Version="12.4.1" />
  <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.7" />
</ItemGroup>
<ItemGroup>
  <ProjectReference Include="...\Notifications.Domain\..." />
  <!-- Cross-module: read overload data -->
  <ProjectReference Include="...\Capacity\ProjectManagement.Capacity.Application\..." />
  <!-- Cross-module: read task/project data -->
  <ProjectReference Include="...\Projects\ProjectManagement.Projects.Application\..." />
</ItemGroup>
```

---

### Task 3 — Infrastructure

**NotificationsDbContext:**
- Schema: `"notifications"`
- Tables: `notification_preferences` (composite PK: user_id + type), `digest_logs`

**NotificationPreferenceConfiguration:**
```csharp
b.ToTable("notification_preferences");
b.HasKey(x => new { x.UserId, x.Type });  // composite primary key
b.Property(x => x.UserId).HasColumnName("user_id");
b.Property(x => x.Type).HasColumnName("type").HasMaxLength(30).IsRequired();
b.Property(x => x.IsEnabled).HasColumnName("is_enabled");
b.Property(x => x.UpdatedAt).HasColumnName("updated_at");
```

**DigestLogConfiguration:**
```csharp
b.ToTable("digest_logs");
b.HasKey(x => x.Id);
b.Property(x => x.Id).HasColumnName("id");
b.Property(x => x.UserId).HasColumnName("user_id");
b.Property(x => x.DigestType).HasColumnName("digest_type").HasMaxLength(30).IsRequired();
b.Property(x => x.IsoWeek).HasColumnName("iso_week");
b.Property(x => x.Year).HasColumnName("year");
b.Property(x => x.SentAt).HasColumnName("sent_at");

b.HasIndex(new[] { "UserId", "DigestType", "IsoWeek", "Year" })
    .HasDatabaseName("ix_digest_logs_user_type_week")
    .IsUnique();
```

**EmailService.cs — MailKit:**
```csharp
// NuGet: MailKit (latest stable, current: 4.x)
using MailKit.Net.Smtp;
using MimeKit;

public class EmailService : IEmailService
{
    private readonly SmtpSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<SmtpSettings> settings, ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_settings.Host))
        {
            _logger.LogInformation("[EmailService] SMTP not configured — logging email: To={To}, Subject={Subject}", to, subject);
            return;  // dev mode: just log
        }

        var msg = new MimeMessage();
        msg.From.Add(MailboxAddress.Parse(_settings.From));
        msg.To.Add(MailboxAddress.Parse(to));
        msg.Subject = subject;
        msg.Body = new TextPart("html") { Text = htmlBody };

        using var client = new SmtpClient();
        await client.ConnectAsync(_settings.Host, _settings.Port, _settings.UseSsl, ct);
        if (!string.IsNullOrEmpty(_settings.User))
            await client.AuthenticateAsync(_settings.User, _settings.Pass, ct);
        await client.SendAsync(msg, ct);
        await client.DisconnectAsync(true, ct);
    }
}

public class SmtpSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; } = false;
    public string User { get; set; } = string.Empty;
    public string Pass { get; set; } = string.Empty;
    public string From { get; set; } = "noreply@project-management.local";
}
```

---

### Task 3.6 — DigestWorker

```csharp
public class DigestWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DigestWorker> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Check every hour — send on Mondays at 7:00 UTC
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var now = DateTime.UtcNow;
            if (now.DayOfWeek == DayOfWeek.Monday && now.Hour == 7)
            {
                await SendDigestsAsync(stoppingToken);
            }
        }
    }

    private async Task SendDigestsAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var mediator   = scope.ServiceProvider.GetRequiredService<IMediator>();
        var db         = scope.ServiceProvider.GetRequiredService<INotificationsDbContext>();
        var emailSvc   = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var userMgr    = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var projectsDb = scope.ServiceProvider.GetRequiredService<IProjectsDbContext>();

        var today    = DateOnly.FromDateTime(DateTime.UtcNow);
        var isoWeek  = ISOWeek.GetWeekOfYear(DateTime.UtcNow);
        var year     = DateTime.UtcNow.Year;

        // Get all active project member user IDs
        var allUserIds = await projectsDb.ProjectMemberships
            .Select(m => m.UserId).Distinct().ToListAsync(ct);

        foreach (var userId in allUserIds)
        {
            try
            {
                await SendDigestForUserAsync(
                    userId, today, isoWeek, year, mediator, db, emailSvc, userMgr, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DigestWorker: error sending digest for user {UserId}", userId);
            }
        }
    }

    private async Task SendDigestForUserAsync(
        Guid userId, DateOnly today, int isoWeek, int year,
        IMediator mediator, INotificationsDbContext db,
        IEmailService emailSvc, UserManager<ApplicationUser> userMgr, CancellationToken ct)
    {
        var user = await userMgr.FindByIdAsync(userId.ToString());
        if (user is null || string.IsNullOrEmpty(user.Email)) return;

        // Load preferences (default: all enabled)
        var prefs = await db.NotificationPreferences
            .Where(p => p.UserId == userId)
            .ToDictionaryAsync(p => p.Type, p => p.IsEnabled, ct);

        bool overloadEnabled = prefs.GetValueOrDefault(NotificationType.Overload, true);
        bool overdueEnabled  = prefs.GetValueOrDefault(NotificationType.Overdue, true);

        if (!overloadEnabled && !overdueEnabled) return;

        // Coalesce: skip if already sent this week
        var alreadySent = await db.DigestLogs.AnyAsync(
            l => l.UserId == userId && l.DigestType == "weekly"
              && l.IsoWeek == isoWeek && l.Year == year, ct);
        if (alreadySent) return;

        // Build content
        var sections = new List<string>();

        if (overloadEnabled)
        {
            var weekStart = today.AddDays(-(int)today.DayOfWeek + 1); // Monday
            var weekEnd   = weekStart.AddDays(6);
            var overload  = await mediator.Send(
                new GetCrossProjectOverloadQuery(userId, weekStart, weekEnd), ct);
            var overloaded = overload.Resources.Where(r => r.HasOverload).ToList();
            if (overloaded.Count > 0)
            {
                var rows = string.Join("", overloaded.Select(r =>
                    $"<tr><td>{r.ResourceId}</td><td style='color:red'>{r.TotalHours:F1}h</td><td>{r.OverloadedDays} ngày</td></tr>"));
                sections.Add($"""
                    <h3>⚠️ Nhân sự quá tải ({overloaded.Count})</h3>
                    <table border='1' cellpadding='4'>
                    <tr><th>ResourceId</th><th>Tổng giờ</th><th>Ngày quá tải</th></tr>
                    {rows}
                    </table>
                    """);
            }
        }

        if (overdueEnabled)
        {
            var lookahead = today.AddDays(7);
            var memberProjectIds = await projectsDb.ProjectMemberships
                .Where(m => m.UserId == userId)
                .Select(m => m.ProjectId).Distinct().ToListAsync(ct);

            var overdueTasks = await projectsDb.ProjectTasks
                .Where(t => memberProjectIds.Contains(t.ProjectId)
                         && !t.IsDeleted
                         && t.PlannedEndDate.HasValue
                         && t.PlannedEndDate.Value <= lookahead
                         && t.Status != ProjectTaskStatus.Completed
                         && t.Status != ProjectTaskStatus.Cancelled)
                .Select(t => new { t.Name, t.PlannedEndDate, t.Status, t.ProjectId })
                .OrderBy(t => t.PlannedEndDate)
                .Take(20)
                .ToListAsync(ct);

            if (overdueTasks.Count > 0)
            {
                var rows = string.Join("", overdueTasks.Select(t =>
                {
                    var isLate = t.PlannedEndDate < today;
                    var color  = isLate ? "color:red" : "color:orange";
                    return $"<tr><td style='{color}'>{t.Name}</td><td>{t.PlannedEndDate:yyyy-MM-dd}</td><td>{t.Status}</td></tr>";
                }));
                sections.Add($"""
                    <h3>📋 Task sắp trễ / đã trễ ({overdueTasks.Count})</h3>
                    <table border='1' cellpadding='4'>
                    <tr><th>Task</th><th>Deadline</th><th>Trạng thái</th></tr>
                    {rows}
                    </table>
                    """);
            }
        }

        if (sections.Count == 0) return;  // nothing to report

        var html = $"""
            <html><body>
            <h2>Weekly Digest — {DateTime.UtcNow:yyyy-MM-dd}</h2>
            {string.Join("<hr/>", sections)}
            <hr/><p style='font-size:11px;color:#999'>
            Để tắt thông báo: <a href='/settings/notifications'>Cài đặt thông báo</a></p>
            </body></html>
            """;

        await emailSvc.SendAsync(user.Email, $"[PM Tool] Weekly Digest {DateTime.UtcNow:yyyy-MM-dd}", html, ct);

        db.DigestLogs.Add(DigestLog.Create(userId, "weekly", isoWeek, year));
        await db.SaveChangesAsync(ct);
        _logger.LogInformation("DigestWorker: sent digest to {Email} (week {Week}/{Year})", user.Email, isoWeek, year);
    }
}
```

**Lưu ý:** `DigestWorker` cần inject `UserManager<ApplicationUser>` via scope — `.NET Identity` UserManager được đăng ký scoped, không thể inject vào singleton. Luôn dùng `_scopeFactory.CreateAsyncScope()`.

---

### Task 3.7 — Infrastructure.csproj

```xml
<PackageReference Include="MailKit" Version="4.11.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.7" ... />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.1" />
```

Project references: Notifications.Application, Shared.Infrastructure, Auth.Domain (for ApplicationUser type).

**Quan trọng:** Auth.Domain không phải Auth.Infrastructure — chỉ cần type `ApplicationUser`, không cần full Auth stack.

---

### Task 4 — API Controller

```csharp
[ApiController]
[Route("api/v1/notification-preferences")]
[Authorize]
public class NotificationPreferencesController : ControllerBase
{
    // GET /api/v1/notification-preferences
    [HttpGet]
    public async Task<IActionResult> GetPreferences(CancellationToken ct)
    {
        var result = await _mediator.Send(
            new GetNotificationPreferencesQuery(_currentUser.UserId), ct);
        return Ok(result);
    }

    // PATCH /api/v1/notification-preferences/{type}
    [HttpPatch("{type}")]
    public async Task<IActionResult> UpdatePreference(string type, [FromBody] UpdatePreferenceRequest req, CancellationToken ct)
    {
        await _mediator.Send(
            new UpdateNotificationPreferenceCommand(_currentUser.UserId, type, req.IsEnabled), ct);
        return NoContent();
    }
}

public sealed record UpdatePreferenceRequest(bool IsEnabled);
```

---

### Task 4.2 — NotificationsModuleExtensions

```csharp
public static IServiceCollection AddNotificationsModule(
    this IServiceCollection services, IConfiguration configuration, IMvcBuilder mvc)
{
    var connectionString = configuration.GetConnectionString("Default")!;

    services.AddDbContext<NotificationsDbContext>(opts => opts.UseNpgsql(connectionString));
    services.AddScoped<INotificationsDbContext>(sp => sp.GetRequiredService<NotificationsDbContext>());

    services.Configure<SmtpSettings>(configuration.GetSection("Smtp"));
    services.AddTransient<IEmailService, EmailService>();

    services.AddHostedService<DigestWorker>();

    services.AddMediatR(cfg =>
        cfg.RegisterServicesFromAssembly(typeof(GetNotificationPreferencesHandler).Assembly));

    mvc.AddApplicationPart(typeof(NotificationPreferencesController).Assembly);
    return services;
}
```

---

### Task 5 — appsettings.json SMTP config

```json
"Smtp": {
  "Host": "",
  "Port": 587,
  "UseSsl": false,
  "User": "",
  "Pass": "",
  "From": "noreply@project-management.local"
}
```

Khi `Host` rỗng, `EmailService` chỉ log mà không gửi thật — safe cho dev environment.

---

### Task 6 — Frontend

**NotificationPreferencesComponent** — NO NgRx, dùng service trực tiếp:

```typescript
@Component({
  selector: 'app-notification-preferences',
  standalone: true,
  imports: [AsyncPipe, NgFor, NgIf, MatCardModule, MatSlideToggleModule, MatProgressSpinnerModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotificationPreferencesComponent implements OnInit {
  private readonly api = inject(SettingsApiService);
  private readonly cdr = inject(ChangeDetectorRef);

  preferences: { type: string; label: string; isEnabled: boolean }[] = [];
  loading = true;

  ngOnInit(): void {
    this.api.getNotificationPreferences().subscribe(prefs => {
      this.preferences = prefs.map(p => ({
        type: p.type,
        label: p.type === 'overload' ? 'Cảnh báo Overload' : 'Task sắp trễ',
        isEnabled: p.isEnabled,
      }));
      this.loading = false;
      this.cdr.markForCheck();
    });
  }

  toggle(type: string, isEnabled: boolean): void {
    this.api.updateNotificationPreference(type, isEnabled).subscribe();
    const pref = this.preferences.find(p => p.type === type);
    if (pref) { pref.isEnabled = isEnabled; this.cdr.markForCheck(); }
  }
}
```

**SettingsApiService:**
```typescript
@Injectable({ providedIn: 'root' })
export class SettingsApiService {
  private readonly http = inject(HttpClient);

  getNotificationPreferences(): Observable<{ type: string; isEnabled: boolean }[]> {
    return this.http.get<{ type: string; isEnabled: boolean }[]>('/api/v1/notification-preferences');
  }

  updateNotificationPreference(type: string, isEnabled: boolean): Observable<void> {
    return this.http.patch<void>(`/api/v1/notification-preferences/${type}`, { isEnabled });
  }
}
```

**Route** — thêm vào `app.routes.ts`:
```typescript
{
  path: 'settings/notifications',
  loadComponent: () =>
    import('./features/settings/notification-preferences/notification-preferences')
      .then(m => m.NotificationPreferencesComponent),
}
```

---

### ISOWeek — .NET built-in

`System.Globalization.ISOWeek.GetWeekOfYear(DateTime)` — có sẵn trong .NET, không cần NuGet.

---

### Lưu ý về IsDeleted trên ProjectTask

`ProjectTask` kế thừa `AuditableEntity` — có `IsDeleted` field. Filter `!t.IsDeleted` trong query là bắt buộc.

---

### Patterns từ Stories trước

1. **New module = 4 csproj**: Domain → Application → Infrastructure → Api (giống Reporting từ 6-1)
2. **Manual migration**: Tạo thủ công, không dùng `dotnet ef` (giống Reporting, TimeTracking)
3. **EF Core 10.0.7 trong Application**: Align version để tránh CS1705 (lesson from 6-3)
4. **BackgroundService scope**: KHÔNG inject scoped services trực tiếp — luôn `IServiceScopeFactory.CreateAsyncScope()` (giống ExportWorker)
5. **PeriodicTimer**: Dùng thay `Timer` — không allocate callback, built-in .NET 6+
6. **Email dev mode**: Khi `Smtp:Host` rỗng → log only, không throw. Production cấu hình Host mới gửi thật.

---

### Anti-patterns cần tránh

- **KHÔNG** inject `UserManager<ApplicationUser>` trực tiếp vào `DigestWorker` — là scoped service.
- **KHÔNG** inject `IProjectsDbContext` trực tiếp vào `DigestWorker` — tạo scope mới mỗi iteration.
- **KHÔNG** gửi email trong transaction — gửi email SAU khi `SaveChangesAsync` thành công.
- **KHÔNG** bỏ qua coalesce check — `DigestLog` với unique index `(user_id, digest_type, iso_week, year)` ngăn duplicate.
- **KHÔNG** tạo `Auth.Infrastructure` reference trong Notifications.Infrastructure — chỉ cần `Auth.Domain` cho `ApplicationUser`.
- **KHÔNG** query tất cả tasks không có giới hạn — luôn `.Take(20)` cho digest.

---

## Completion Notes

- Tạo Notifications module mới hoàn chỉnh (4 projects) theo pattern Reporting module
- `DigestWorker` dùng `PeriodicTimer(1h)`, chỉ gửi thứ Hai lúc 7:00 UTC — coalesce bằng `DigestLog` với unique index `(user_id, digest_type, iso_week, year)`
- `EmailService` dev-mode: khi `Smtp:Host` rỗng chỉ log, không gửi thật
- MailKit nâng lên 4.16.0 để giải quyết CVE (từ 4.11.0 trong story spec)
- Cross-module: `GetCrossProjectOverloadQuery` (Capacity), `IProjectsDbContext` (Projects), `UserManager<ApplicationUser>` (Auth.Domain) — tất cả dùng qua `IServiceScopeFactory.CreateAsyncScope()`
- Frontend: `NotificationPreferencesComponent` OnPush standalone với `MatSlideToggleModule`, lazy-loaded tại `/settings/notifications`
- `dotnet build`: 0 errors; `ng build`: 0 errors; chunk `notification-preferences` xuất hiện trong output

## Files Created/Modified

**Backend — Domain:**
- `src/Modules/Notifications/ProjectManagement.Notifications.Domain/Entities/NotificationPreference.cs` — mới
- `src/Modules/Notifications/ProjectManagement.Notifications.Domain/Entities/DigestLog.cs` — mới
- `src/Modules/Notifications/ProjectManagement.Notifications.Domain/Enums/NotificationType.cs` — mới
- `src/Modules/Notifications/ProjectManagement.Notifications.Domain/ProjectManagement.Notifications.Domain.csproj` — mới

**Backend — Application:**
- `src/Modules/Notifications/ProjectManagement.Notifications.Application/Common/Interfaces/INotificationsDbContext.cs` — mới
- `src/Modules/Notifications/ProjectManagement.Notifications.Application/Common/Interfaces/IEmailService.cs` — mới
- `src/Modules/Notifications/ProjectManagement.Notifications.Application/Commands/UpdateNotificationPreference/UpdateNotificationPreferenceCommand.cs` — mới
- `src/Modules/Notifications/ProjectManagement.Notifications.Application/Queries/GetNotificationPreferences/GetNotificationPreferencesQuery.cs` — mới
- `src/Modules/Notifications/ProjectManagement.Notifications.Application/ProjectManagement.Notifications.Application.csproj` — mới

**Backend — Infrastructure:**
- `src/Modules/Notifications/ProjectManagement.Notifications.Infrastructure/Persistence/NotificationsDbContext.cs` — mới
- `src/Modules/Notifications/ProjectManagement.Notifications.Infrastructure/Persistence/Configurations/NotificationPreferenceConfiguration.cs` — mới
- `src/Modules/Notifications/ProjectManagement.Notifications.Infrastructure/Persistence/Configurations/DigestLogConfiguration.cs` — mới
- `src/Modules/Notifications/ProjectManagement.Notifications.Infrastructure/Migrations/20260426150000_InitialNotifications.cs` — mới
- `src/Modules/Notifications/ProjectManagement.Notifications.Infrastructure/Migrations/NotificationsDbContextModelSnapshot.cs` — mới
- `src/Modules/Notifications/ProjectManagement.Notifications.Infrastructure/Services/EmailService.cs` — mới (SmtpSettings + EmailService)
- `src/Modules/Notifications/ProjectManagement.Notifications.Infrastructure/Workers/DigestWorker.cs` — mới
- `src/Modules/Notifications/ProjectManagement.Notifications.Infrastructure/ProjectManagement.Notifications.Infrastructure.csproj` — mới (MailKit 4.16.0)

**Backend — Api:**
- `src/Modules/Notifications/ProjectManagement.Notifications.Api/Controllers/NotificationPreferencesController.cs` — mới
- `src/Modules/Notifications/ProjectManagement.Notifications.Api/Extensions/NotificationsModuleExtensions.cs` — mới
- `src/Modules/Notifications/ProjectManagement.Notifications.Api/ProjectManagement.Notifications.Api.csproj` — mới

**Backend — Host:**
- `src/Host/ProjectManagement.Host/ProjectManagement.Host.csproj` — thêm Notifications.Api ref
- `src/Host/ProjectManagement.Host/Program.cs` — thêm AddNotificationsModule + NotificationsDbContext migration
- `src/Host/ProjectManagement.Host/appsettings.json` — thêm Smtp config block

**Solution:**
- `ProjectManagement.slnx` — thêm Notifications folder với 4 projects

**Frontend:**
- `frontend/project-management-web/src/app/features/settings/services/settings-api.service.ts` — mới
- `frontend/project-management-web/src/app/features/settings/notification-preferences/notification-preferences.ts` — mới
- `frontend/project-management-web/src/app/features/settings/notification-preferences/notification-preferences.html` — mới
- `frontend/project-management-web/src/app/app.routes.ts` — thêm route settings/notifications
