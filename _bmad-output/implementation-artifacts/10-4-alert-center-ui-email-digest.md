# Story 10.4: Alert Center UI & Email Digest (Growth)

Status: review

## Story

As a PM,
I want to see prioritized alerts on my dashboard header and receive weekly email digests,
So that I never miss critical issues even when I'm not actively monitoring the dashboard.

## Acceptance Criteria

**AC-1: Alert badge hiển thị unread count**
- **Given** PM có alerts chưa đọc trong DB
- **When** mở bất kỳ trang nào trong app (dashboard, projects, v.v.)
- **Then** icon chuông trong `AppShellComponent` toolbar hiển thị badge số lượng unread alerts
- **And** badge update realtime khi mark alert là read

**AC-2: Alert panel — danh sách alerts**
- **Given** PM click vào icon chuông trong toolbar
- **When** click xảy ra
- **Then** slide-over panel hiển thị danh sách alerts sorted by `created_at DESC`
- **And** mỗi alert hiển thị: title, type badge (deadline/overload/budget), created_at, trạng thái đã đọc/chưa đọc
- **And** alerts chưa đọc có visual distinction (bold/background)

**AC-3: Mark alert as read khi click**
- **Given** PM xem alert panel và click vào một alert
- **When** click xảy ra
- **Then** gọi `PATCH /api/v1/alerts/{id}/read` → 204
- **And** unread badge count giảm đi 1
- **And** nếu alert có `entity_id` + `entity_type == "Task"` → navigate tới `/projects/{projectId}`
- **And** nếu entity_type == "Project" → navigate tới `/projects/{projectId}`

**AC-4: Background job tạo deadline alerts**
- **Given** `AlertRulesWorker` chạy mỗi giờ
- **When** có task với `PlannedEndDate <= DateTime.UtcNow.AddHours(48)` AND `Status != Completed/Cancelled` AND `!IsDeleted`
- **Then** tạo `Alert` record cho mỗi project member với `type = "deadline"`, `title = "{task name} — deadline trong {N}h"`, `entity_type = "Task"`, `entity_id = task.Id`
- **And** dedup: không tạo duplicate alert nếu đã có alert cho cùng (userId, entityId, type) trong ngày hôm nay

**AC-5: Email digest hàng tuần**
- **Given** `AlertDigestWorker` chạy mỗi thứ Hai 7:00 UTC
- **When** user có `AlertPreference` với `alert_type = "overload" AND enabled = true`
- **Then** user nhận email với danh sách overload alerts từ tuần trước
- **And** nếu không có alerts nào → KHÔNG gửi email (skip)
- **And** dedup: dùng digest log để không gửi 2 lần trong cùng tuần

**AC-6: Per-user isolation**
- **Given** User A và User B có alerts khác nhau
- **When** User A mở alert panel
- **Then** chỉ thấy alerts của User A — không thấy alerts của User B (xác nhận qua JWT + `userId == currentUserId`)

---

## Dev Notes

### ⚠️ Brownfield Context — Đọc trước khi code

Story 10-4 là **Growth feature** với cả backend và frontend. Đây là story BUILD UPON story 10-2 (Alert schema đã có). KHÔNG tạo lại entities, migrations, hay AlertsController.

**Infrastructure đã có từ Story 10-2:**
- `Alert.cs` + `AlertPreference.cs` entities trong `Reporting.Domain/Entities/`
- `AlertConfiguration.cs` + `AlertPreferenceConfiguration.cs` EF configs
- `ReportingDbContext` đã có `DbSet<Alert> Alerts` + `DbSet<AlertPreference> AlertPreferences`
- `AlertsController` tại `/api/v1/alerts` với 3 endpoints: GET list, PATCH mark-read, PUT preferences
- `GetMyAlertsQuery` + `MarkAlertReadCommand` + `UpsertAlertPreferenceCommand` — TẤT CẢ đã tồn tại
- KHÔNG có `UpdatedAt` trên `Alert` — append-only, chỉ `is_read/read_at` được update

**Infrastructure tái dụng cho workers:**
- `DigestWorker` pattern trong `Notifications.Infrastructure/Workers/DigestWorker.cs` — COPY EXACT pattern: `BackgroundService`, `IServiceScopeFactory`, `PeriodicTimer`, `_scopeFactory.CreateAsyncScope()`
- `ExportWorker` trong `Reporting.Infrastructure/Workers/ExportWorker.cs` — pattern tham khảo cho scoped DB access
- `IEmailService` interface: `Notifications.Application.Common.Interfaces.IEmailService` — đã registered trong DI
- `EmailService` (MailKit-based): `Notifications.Infrastructure.Services.EmailService` — đã configured via `SmtpSettings`
- `SmtpSettings` class đã configured trong `NotificationsModuleExtensions.Configure<SmtpSettings>()` — available in DI

**Frontend infrastructure đã có:**
- `app.state.ts` — tất cả feature reducers registered ở đây (pattern: add `alerts: AlertsState`)
- `app.config.ts` — tất cả Effects registered trong `provideEffects([...])` (add `AlertsEffects`)
- `AppShellComponent` tại `src/app/core/shell/app-shell.ts` — KHÔNG inject Store hiện tại, cần thêm Store injection + alert badge
- `app-shell.html` — sidenav + toolbar pattern, cần thêm notification icon button với badge
- `DashboardEffects` pattern tại `features/dashboard/store/dashboard.effects.ts` — COPY pattern cho AlertsEffects

### Architecture Compliance

| Rule | Requirement |
|---|---|
| NFR-14 | Per-user isolation — `AlertRulesWorker` tạo alert PER USER, `GetMyAlertsQuery` đã filter by userId |
| NFR-15 | KHÔNG cache `/api/v1/alerts` — sensitive data, no `Cache-Control` |
| AR-9 | Route đã đúng: `/api/v1/alerts` (không phải `/api/v1/reports/alerts`) |
| AR-6 | Workers trong `Reporting.Infrastructure/Workers/` — KHÔNG tạo trong Host project |
| D-13 | `IHostedService` pattern — dùng `BackgroundService` + `PeriodicTimer` (KHÔNG dùng Hangfire) |

### Backend — File Locations

```
src/Modules/Reporting/
├── ProjectManagement.Reporting.Infrastructure/
│   ├── Workers/
│   │   ├── ExportWorker.cs          (đã có — KHÔNG sửa)
│   │   ├── AlertRulesWorker.cs      ← MỚI
│   │   └── AlertDigestWorker.cs     ← MỚI
│   └── ProjectManagement.Reporting.Infrastructure.csproj  ← SỬA: thêm 2 project references
```

### Backend — Reporting.Infrastructure.csproj Update

Cần thêm 2 references vào `Reporting.Infrastructure.csproj`:

```xml
<!-- Thêm vào ItemGroup sau PackageReferences -->
<ProjectReference Include="..\..\Notifications\ProjectManagement.Notifications.Application\ProjectManagement.Notifications.Application.csproj" />
<!-- Auth.Domain — cho UserManager<ApplicationUser> -->
<ProjectReference Include="..\..\Auth\ProjectManagement.Auth.Domain\ProjectManagement.Auth.Domain.csproj" />
```

**KIỂM TRA TRƯỚC KHI THÊM:** `grep -r "Notifications.Application" src/Modules/Reporting/ --include="*.csproj"` — nếu đã có thì bỏ qua.

### Backend — AlertRulesWorker

```csharp
// src/Modules/Reporting/ProjectManagement.Reporting.Infrastructure/Workers/AlertRulesWorker.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectManagement.Projects.Application.Common.Interfaces;
using ProjectManagement.Reporting.Application.Common.Interfaces;
using ProjectManagement.Reporting.Domain.Entities;

namespace ProjectManagement.Reporting.Infrastructure.Workers;

public class AlertRulesWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AlertRulesWorker> _logger;

    public AlertRulesWorker(IServiceScopeFactory scopeFactory, ILogger<AlertRulesWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await EvaluateRulesAsync(stoppingToken);
        }
    }

    private async Task EvaluateRulesAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var projectsDb  = scope.ServiceProvider.GetRequiredService<IProjectsDbContext>();
            var reportingDb = scope.ServiceProvider.GetRequiredService<IReportingDbContext>();

            await CreateDeadlineAlertsAsync(projectsDb, reportingDb, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AlertRulesWorker: error during evaluation");
        }
    }

    private async Task CreateDeadlineAlertsAsync(
        IProjectsDbContext projectsDb,
        IReportingDbContext reportingDb,
        CancellationToken ct)
    {
        var threshold = DateTime.UtcNow.AddHours(48);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Tasks approaching deadline (< 48h)
        var tasks = await projectsDb.ProjectTasks
            .AsNoTracking()
            .Where(t => !t.IsDeleted
                     && t.PlannedEndDate.HasValue
                     && t.PlannedEndDate.Value <= today.AddDays(2)
                     && t.Status != ProjectManagement.Projects.Domain.Enums.ProjectTaskStatus.Completed
                     && t.Status != ProjectManagement.Projects.Domain.Enums.ProjectTaskStatus.Cancelled)
            .Select(t => new { t.Id, t.Name, t.ProjectId, t.PlannedEndDate })
            .ToListAsync(ct);

        if (tasks.Count == 0) return;

        var projectIds = tasks.Select(t => t.ProjectId).Distinct().ToList();

        // Get project members for affected projects
        var memberships = await projectsDb.ProjectMemberships
            .AsNoTracking()
            .Where(m => projectIds.Contains(m.ProjectId))
            .Select(m => new { m.ProjectId, m.UserId })
            .ToListAsync(ct);

        var today_dt = DateTime.UtcNow.Date;

        // Existing deadline alerts today (for dedup)
        var taskIds = tasks.Select(t => t.Id).ToList();
        var existingToday = await reportingDb.Alerts
            .AsNoTracking()
            .Where(a => a.Type == "deadline"
                     && a.EntityType == "Task"
                     && taskIds.Contains(a.EntityId!.Value)
                     && a.CreatedAt >= today_dt)
            .Select(a => new { a.UserId, a.EntityId })
            .ToListAsync(ct);

        var existingSet = existingToday
            .Select(x => (x.UserId, x.EntityId!.Value))
            .ToHashSet();

        var membersByProject = memberships.ToLookup(m => m.ProjectId, m => m.UserId);

        int created = 0;
        foreach (var task in tasks)
        {
            var hoursLeft = task.PlannedEndDate.HasValue
                ? (task.PlannedEndDate.Value.ToDateTime(TimeOnly.MinValue) - DateTime.UtcNow).TotalHours
                : 48;
            var nHours = Math.Max(0, (int)Math.Ceiling(hoursLeft));
            var title = $"{task.Name} — deadline trong {nHours}h";

            foreach (var userId in membersByProject[task.ProjectId])
            {
                if (existingSet.Contains((userId, task.Id))) continue;

                var alert = Alert.Create(
                    userId, "deadline", title,
                    projectId: task.ProjectId,
                    entityType: "Task",
                    entityId: task.Id);

                reportingDb.Alerts.Add(alert);
                created++;
            }
        }

        if (created > 0)
        {
            await reportingDb.SaveChangesAsync(ct);
            _logger.LogInformation("AlertRulesWorker: created {Count} deadline alerts", created);
        }
    }
}
```

**⚠️ NOTE về DateOnly vs DateTime:** `task.PlannedEndDate` trong `ProjectsDbContext` là `DateOnly?`. So sánh với `today.AddDays(2)` (DateOnly) để filter trong EF. Sau đó tính `hoursLeft` bằng in-memory conversion.

### Backend — AlertDigestWorker

```csharp
// src/Modules/Reporting/ProjectManagement.Reporting.Infrastructure/Workers/AlertDigestWorker.cs
using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectManagement.Auth.Domain.Users;
using ProjectManagement.Notifications.Application.Common.Interfaces;
using ProjectManagement.Reporting.Application.Common.Interfaces;

namespace ProjectManagement.Reporting.Infrastructure.Workers;

public class AlertDigestWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AlertDigestWorker> _logger;

    public AlertDigestWorker(IServiceScopeFactory scopeFactory, ILogger<AlertDigestWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
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
        var db       = scope.ServiceProvider.GetRequiredService<IReportingDbContext>();
        var emailSvc = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var userMgr  = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var isoWeek = ISOWeek.GetWeekOfYear(DateTime.UtcNow);
        var year    = DateTime.UtcNow.Year;

        // Users who want overload email digest
        var userPrefs = await db.AlertPreferences
            .AsNoTracking()
            .Where(p => p.AlertType == "overload" && p.Enabled)
            .Select(p => p.UserId)
            .Distinct()
            .ToListAsync(ct);

        var weekStart = DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek + 1).AddDays(-7);
        var weekEnd   = weekStart.AddDays(7);

        foreach (var userId in userPrefs)
        {
            try
            {
                var alerts = await db.Alerts
                    .AsNoTracking()
                    .Where(a => a.UserId == userId
                             && a.Type == "overload"
                             && a.CreatedAt >= weekStart
                             && a.CreatedAt < weekEnd)
                    .OrderByDescending(a => a.CreatedAt)
                    .Take(50)
                    .Select(a => new { a.Title, a.CreatedAt })
                    .ToListAsync(ct);

                if (alerts.Count == 0) continue;

                var user = await userMgr.FindByIdAsync(userId.ToString());
                if (user is null || string.IsNullOrEmpty(user.Email)) continue;

                var rows = string.Join("", alerts.Select(a =>
                    $"<tr><td>{a.Title}</td><td>{a.CreatedAt:yyyy-MM-dd HH:mm}</td></tr>"));

                var html = $"""
                    <html><body>
                    <h2>Alert Digest — Tuần {isoWeek}/{year}</h2>
                    <h3>⚠️ Overload Alerts ({alerts.Count})</h3>
                    <table border='1' cellpadding='4'>
                    <tr><th>Alert</th><th>Thời gian</th></tr>
                    {rows}
                    </table>
                    <hr/><p style='font-size:11px;color:#999'>
                    Để tắt: <a href='/settings/notifications'>Cài đặt thông báo</a></p>
                    </body></html>
                    """;

                await emailSvc.SendAsync(user.Email,
                    $"[PM Tool] Alert Digest — Tuần {isoWeek}/{year}", html, ct);

                _logger.LogInformation(
                    "AlertDigestWorker: sent digest to {Email} (week {Week}/{Year})",
                    user.Email, isoWeek, year);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AlertDigestWorker: error for user {UserId}", userId);
            }
        }
    }
}
```

### Backend — ReportingModuleExtensions Update

Thêm 2 dòng `AddHostedService` vào `ReportingModuleExtensions.cs`:

```csharp
// Sau dòng services.AddHostedService<ExportWorker>();
services.AddHostedService<AlertRulesWorker>();
services.AddHostedService<AlertDigestWorker>();
```

**Sau khi thêm, verify build:**
```bash
dotnet build src/Modules/Reporting/ProjectManagement.Reporting.Infrastructure
```

### Backend — IProjectsDbContext.ProjectTasks Check

`AlertRulesWorker` sử dụng `projectsDb.ProjectTasks`. Verify rằng `IProjectsDbContext` có `ProjectTasks` DbSet:
```bash
grep -n "ProjectTasks" src/Modules/Projects/ProjectManagement.Projects.Application/Common/Interfaces/IProjectsDbContext.cs
```
Nếu không có (unlikely nhưng verify) → dùng `projectsDb.Tasks` hoặc tên DbSet đúng.

### Frontend — File Structure

```
src/app/
├── core/
│   ├── shell/
│   │   ├── app-shell.ts            ← SỬA: inject Store, add badge + panel logic
│   │   └── app-shell.html          ← SỬA: thêm notification button + alert panel
│   └── store/
│       └── app.state.ts            ← SỬA: add AlertsState
├── app.config.ts                   ← SỬA: add AlertsEffects
└── features/
    └── alerts/                     ← MỚI folder
        ├── models/
        │   └── alert.model.ts      ← MỚI
        ├── services/
        │   └── alerts-api.service.ts  ← MỚI
        ├── store/
        │   ├── alert.actions.ts    ← MỚI
        │   ├── alert.reducer.ts    ← MỚI (createFeature)
        │   └── alert.effects.ts    ← MỚI
        └── components/
            └── alert-panel/
                ├── alert-panel.ts  ← MỚI
                └── alert-panel.html ← MỚI
```

### Frontend — alert.model.ts

```typescript
// src/app/features/alerts/models/alert.model.ts
export interface AlertDto {
  id: string;
  projectId: string | null;
  type: string;         // "deadline" | "overload" | "budget"
  entityType: string | null;   // "Task" | "Project" | null
  entityId: string | null;
  title: string;
  description: string | null;
  isRead: boolean;
  createdAt: string;    // ISO datetime string
  readAt: string | null;
}
```

### Frontend — alerts-api.service.ts

```typescript
// src/app/features/alerts/services/alerts-api.service.ts
import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { AlertDto } from '../models/alert.model';

@Injectable({ providedIn: 'root' })
export class AlertsApiService {
  private readonly http = inject(HttpClient);

  getAlerts(unreadOnly?: boolean): Observable<AlertDto[]> {
    let params = new HttpParams();
    if (unreadOnly) params = params.set('unreadOnly', 'true');
    return this.http.get<AlertDto[]>('/api/v1/alerts', { params });
  }

  markRead(id: string): Observable<void> {
    return this.http.patch<void>(`/api/v1/alerts/${id}/read`, {});
  }
}
```

**QUAN TRỌNG:** Backend `GET /api/v1/alerts` trả về `AlertDto[]` (từ `GetMyAlertsQuery` → `GetMyAlertsResult`). 
Verify response shape: `GetMyAlertsQuery` handler trả về `List<AlertDto>` trực tiếp (không wrap trong `{ items, totalCount }`). Kiểm tra lại `AlertsController.GetMyAlerts()` — nếu return `Ok(result)` với result là `GetMyAlertsResult` (có `Items` + `TotalCount`), thì frontend cần map `data.items` hoặc backend trả về list trực tiếp.
→ Kiểm tra: `grep -A5 "GetMyAlertsResult" src/Modules/Reporting/ProjectManagement.Reporting.Application/Alerts/GetMyAlerts/GetMyAlertsQuery.cs`

### Frontend — NgRx Alerts Store

**alert.actions.ts:**
```typescript
import { createActionGroup, emptyProps, props } from '@ngrx/store';
import { AlertDto } from '../models/alert.model';

export const AlertActions = createActionGroup({
  source: 'Alerts',
  events: {
    'Load Alerts': emptyProps(),
    'Load Alerts Success': props<{ alerts: AlertDto[] }>(),
    'Load Alerts Failure': props<{ error: string }>(),
    'Mark Alert Read': props<{ id: string; projectId: string | null; entityType: string | null; entityId: string | null }>(),
    'Mark Alert Read Success': props<{ id: string }>(),
    'Mark Alert Read Failure': props<{ error: string }>(),
    'Toggle Panel': emptyProps(),
    'Close Panel': emptyProps(),
  },
});
```

**alert.reducer.ts:**
```typescript
import { createFeature, createReducer, on } from '@ngrx/store';
import { AlertDto } from '../models/alert.model';
import { AlertActions } from './alert.actions';

export interface AlertsState {
  alerts: AlertDto[];
  loading: boolean;
  panelOpen: boolean;
  unreadCount: number;
}

const initialState: AlertsState = {
  alerts: [],
  loading: false,
  panelOpen: false,
  unreadCount: 0,
};

export const alertsFeature = createFeature({
  name: 'alerts',
  reducer: createReducer(
    initialState,
    on(AlertActions.loadAlerts, state => ({ ...state, loading: true })),
    on(AlertActions.loadAlertsSuccess, (state, { alerts }) => ({
      ...state,
      loading: false,
      alerts,
      unreadCount: alerts.filter(a => !a.isRead).length,
    })),
    on(AlertActions.loadAlertsFailure, state => ({ ...state, loading: false })),
    on(AlertActions.markAlertReadSuccess, (state, { id }) => {
      const alerts = state.alerts.map(a =>
        a.id === id ? { ...a, isRead: true, readAt: new Date().toISOString() } : a
      );
      return { ...state, alerts, unreadCount: alerts.filter(a => !a.isRead).length };
    }),
    on(AlertActions.togglePanel, state => ({ ...state, panelOpen: !state.panelOpen })),
    on(AlertActions.closePanel, state => ({ ...state, panelOpen: false })),
  ),
});

export const {
  selectAlerts,
  selectLoading,
  selectPanelOpen,
  selectUnreadCount,
} = alertsFeature;
```

**alert.effects.ts:**
```typescript
import { Injectable, inject } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { Router } from '@angular/router';
import { catchError, map, of, switchMap, tap } from 'rxjs';
import { AlertsApiService } from '../services/alerts-api.service';
import { AlertActions } from './alert.actions';

@Injectable()
export class AlertsEffects {
  private readonly actions$ = inject(Actions);
  private readonly api = inject(AlertsApiService);
  private readonly router = inject(Router);

  loadAlerts$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AlertActions.loadAlerts),
      switchMap(() =>
        this.api.getAlerts().pipe(
          map(alerts => AlertActions.loadAlertsSuccess({ alerts })),
          catchError(err => of(AlertActions.loadAlertsFailure({ error: err?.message ?? 'Lỗi tải alerts.' })))
        )
      )
    )
  );

  markAlertRead$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AlertActions.markAlertRead),
      switchMap(({ id, projectId, entityType, entityId }) =>
        this.api.markRead(id).pipe(
          map(() => AlertActions.markAlertReadSuccess({ id })),
          catchError(err => of(AlertActions.markAlertReadFailure({ error: err?.message ?? 'Lỗi mark read.' })))
        )
      )
    )
  );

  navigateOnMarkRead$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(AlertActions.markAlertRead),
        tap(({ entityType, projectId, entityId }) => {
          if (entityType === 'Task' && projectId) {
            this.router.navigate(['/projects', projectId]);
          } else if (entityType === 'Project' && (projectId || entityId)) {
            this.router.navigate(['/projects', projectId ?? entityId]);
          }
        })
      ),
    { dispatch: false }
  );
}
```

### Frontend — AlertPanelComponent

**alert-panel.ts:**
```typescript
import { ChangeDetectionStrategy, Component, inject, Output, EventEmitter } from '@angular/core';
import { AsyncPipe, DatePipe, NgClass, NgFor, NgIf } from '@angular/common';
import { Store } from '@ngrx/store';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDividerModule } from '@angular/material/divider';
import { AlertActions } from '../../store/alert.actions';
import { selectAlerts, selectLoading } from '../../store/alert.reducer';
import { AlertDto } from '../../models/alert.model';

@Component({
  selector: 'app-alert-panel',
  standalone: true,
  imports: [AsyncPipe, DatePipe, NgFor, NgIf, NgClass, MatButtonModule, MatIconModule, MatDividerModule],
  templateUrl: './alert-panel.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AlertPanelComponent {
  private readonly store = inject(Store);

  readonly alerts$ = this.store.select(selectAlerts);
  readonly loading$ = this.store.select(selectLoading);

  @Output() closed = new EventEmitter<void>();

  trackAlert(_: number, alert: AlertDto): string { return alert.id; }

  onAlertClick(alert: AlertDto): void {
    this.store.dispatch(AlertActions.markAlertRead({
      id: alert.id,
      projectId: alert.projectId,
      entityType: alert.entityType,
      entityId: alert.entityId,
    }));
    this.store.dispatch(AlertActions.closePanel());
    this.closed.emit();
  }

  typeLabel(type: string): string {
    return { deadline: 'Deadline', overload: 'Quá tải', budget: 'Budget' }[type] ?? type;
  }
}
```

**alert-panel.html:**
```html
<div class="alert-panel">
  <div class="alert-panel__header">
    <span class="alert-panel__title">Thông báo</span>
    <button mat-icon-button (click)="closed.emit()">
      <mat-icon>close</mat-icon>
    </button>
  </div>
  <mat-divider />

  <div *ngIf="loading$ | async" class="alert-panel__loading">Đang tải...</div>

  <ng-container *ngIf="alerts$ | async as alerts">
    <div *ngIf="alerts.length === 0" class="alert-panel__empty">
      Không có thông báo nào.
    </div>
    <div class="alert-panel__list">
      <div *ngFor="let alert of alerts; trackBy: trackAlert"
           class="alert-item"
           [ngClass]="{ 'alert-item--unread': !alert.isRead }"
           (click)="onAlertClick(alert)"
           role="button">
        <span class="alert-item__badge alert-item__badge--{{ alert.type }}">
          {{ typeLabel(alert.type) }}
        </span>
        <span class="alert-item__title">{{ alert.title }}</span>
        <span class="alert-item__time">{{ alert.createdAt | date:'dd/MM HH:mm' }}</span>
      </div>
    </div>
  </ng-container>
</div>
```

### Frontend — AppShellComponent Update

**app-shell.ts** — thêm Store injection và alert badge:

```typescript
// Thêm imports:
import { Store } from '@ngrx/store';
import { MatBadgeModule } from '@angular/material/badge';
import { AlertActions } from '../../features/alerts/store/alert.actions';
import { selectUnreadCount, selectPanelOpen } from '../../features/alerts/store/alert.reducer';
import { AlertPanelComponent } from '../../features/alerts/components/alert-panel/alert-panel';

// Thêm vào @Component.imports[]:
// MatBadgeModule, AlertPanelComponent

// Thêm vào class AppShellComponent:
private readonly store = inject(Store);
readonly unreadCount$ = this.store.select(selectUnreadCount);
readonly panelOpen$ = this.store.select(selectPanelOpen);

ngOnInit(): void {
  this.store.dispatch(AlertActions.loadAlerts());
}

toggleAlertPanel(): void {
  this.store.dispatch(AlertActions.togglePanel());
}
```

**app-shell.html** — thêm notification button trong toolbar và alert panel overlay:

```html
<!-- Thêm vào sidenav-footer hoặc tạo toolbar ở đầu shell-content -->
<!-- Trong sidenav-footer hoặc header, thêm: -->
<button mat-icon-button
        (click)="toggleAlertPanel()"
        class="alert-btn"
        matTooltip="Thông báo"
        matTooltipPosition="right"
        [matBadge]="(unreadCount$ | async) || null"
        matBadgeColor="warn"
        matBadgeSize="small">
  <mat-icon>notifications</mat-icon>
</button>

<!-- Alert panel overlay — đặt trong shell-content, sau <router-outlet />: -->
<div class="alert-overlay" *ngIf="panelOpen$ | async" (click)="toggleAlertPanel()"></div>
<div class="alert-panel-container" [class.alert-panel-container--open]="panelOpen$ | async">
  <app-alert-panel (closed)="store.dispatch(alertCloseAction())"></app-alert-panel>
</div>
```

**⚠️ DESIGN CHOICE:** Do `AppShellComponent` dùng `ChangeDetectionStrategy.OnPush`, phải dùng `async` pipe cho `unreadCount$` và `panelOpen$` — KHÔNG dùng `.subscribe()`. `MatBadge` với `[matBadge]="null"` khi count = 0 sẽ ẩn badge tự động.

**SIMPLER ALTERNATIVE:** Nếu `mat-badge` phức tạp, có thể dùng `*ngIf`:
```html
<span *ngIf="(unreadCount$ | async) as count" class="notification-badge">{{ count }}</span>
```

### Frontend — app.state.ts Update

```typescript
// Thêm vào app.state.ts:
import { alertsFeature, AlertsState } from '../../features/alerts/store/alert.reducer';

export interface AppState {
  // ... existing ...
  alerts: AlertsState;
}

export const reducers: ActionReducerMap<AppState> = {
  // ... existing ...
  alerts: alertsFeature.reducer,
};
```

### Frontend — app.config.ts Update

```typescript
// Thêm AlertsEffects vào imports:
import { AlertsEffects } from './features/alerts/store/alert.effects';

// Thêm vào provideEffects([...]):
// AlertsEffects
```

### Frontend — Unit Tests Pattern

Tests nên là pure function tests (templateUrl components không resolve trong Vitest jsdom — vấn đề đã biết từ story 10-3).

**alert-panel.spec.ts pattern:**
```typescript
import { describe, it, expect } from 'vitest';
import { AlertDto } from '../../models/alert.model';

function typeLabel(type: string): string {
  return { deadline: 'Deadline', overload: 'Quá tải', budget: 'Budget' }[type] ?? type;
}

describe('AlertPanelComponent — pure logic', () => {
  describe('typeLabel', () => {
    it('returns Deadline for deadline type', () => expect(typeLabel('deadline')).toBe('Deadline'));
    it('returns Quá tải for overload type', () => expect(typeLabel('overload')).toBe('Quá tải'));
    it('returns Budget for budget type', () => expect(typeLabel('budget')).toBe('Budget'));
    it('returns type string for unknown type', () => expect(typeLabel('custom')).toBe('custom'));
  });
  // ... reducer tests ...
});
```

**alert.reducer.spec.ts:**
```typescript
import { describe, it, expect } from 'vitest';
import { AlertDto } from '../../models/alert.model';

// Test reducer logic inline (không import reducer — tránh Angular deps)
describe('alertsReducer — pure logic', () => {
  it('unreadCount = count of alerts where isRead = false', () => {
    const alerts: Pick<AlertDto, 'isRead'>[] = [
      { isRead: false }, { isRead: true }, { isRead: false },
    ];
    const count = alerts.filter(a => !a.isRead).length;
    expect(count).toBe(2);
  });

  it('markReadSuccess updates isRead to true for matching id', () => {
    const alerts: AlertDto[] = [
      { id: '1', isRead: false } as AlertDto,
      { id: '2', isRead: false } as AlertDto,
    ];
    const updated = alerts.map(a => a.id === '1' ? { ...a, isRead: true } : a);
    expect(updated[0].isRead).toBe(true);
    expect(updated[1].isRead).toBe(false);
  });
});
```

### Git Intelligence

- Commit gần nhất: "comit" (52b732a)
- Story 10-3 thêm: `resource-report.ts`, `milestone-report.ts`, `reporting.effects.ts` (extended), `reporting.reducer.ts` (extended), 2 backend query handlers + ReportingController extended
- Pattern đã confirmed: pure logic tests cho templateUrl components (Vitest jsdom env issue)
- `AppShellComponent` hiện tại CHƯA inject Store — cần thêm `Store` injection + `ngOnInit()`/`ngOnDestroy()` lifecycle

### Previous Story Intelligence (Story 10-2 & 10-3)

**Story 10-2 gotchas đã biết:**
- `MarkAlertReadHandler` catch `UnauthorizedAccessException` → Forbid() và `KeyNotFoundException` → NotFound() — đã implement trong `AlertsController`
- `Alert.MarkAsRead()` là idempotent — KHÔNG gọi `SaveChanges` nếu đã read (cần check trong handler)
- Migration `AddAlertCenterSchema` — đã apply, bảng `alerts` và `alert_preferences` tồn tại
- `GetMyAlertsQuery` returns `GetMyAlertsResult` (có `Items` + `TotalCount`) — frontend cần map `result.items` (KHÔNG phải `result` trực tiếp)

**CRITICAL — Verify response shape:**
```bash
grep -A 5 "class GetMyAlertsResult\|record GetMyAlertsResult" src/Modules/Reporting/ProjectManagement.Reporting.Application/Alerts/GetMyAlerts/GetMyAlertsQuery.cs
```
Nếu controller return `Ok(result)` với `result.Items` → frontend `alerts-api.service.ts` cần:
```typescript
// map response to items array:
return this.http.get<{ items: AlertDto[]; totalCount: number }>('/api/v1/alerts', { params })
  .pipe(map(r => r.items));
```

**Story 10-3 gotchas đã biết:**
- Edit outside class brace: luôn verify class closing brace sau khi thêm methods vào controller
- `projectNames[id]` trong EF LINQ-to-SQL fails: load dict before, map in-memory
- Vitest jsdom: templateUrl không resolve → dùng pure function tests
- `ITimeTrackingDbContext` cần verify csproj reference trước khi dùng

---

## Tasks / Subtasks

### Backend Tasks

- [x] **Task BE-1: Cập nhật Reporting.Infrastructure.csproj**
  - [x] BE-1.1: Kiểm tra `grep -r "Notifications.Application" src/Modules/Reporting/ProjectManagement.Reporting.Infrastructure/ProjectManagement.Reporting.Infrastructure.csproj`
  - [x] BE-1.2: Nếu chưa có → thêm `ProjectReference` đến `Notifications.Application` (cho `IEmailService`)
  - [x] BE-1.3: Kiểm tra `grep -r "Auth.Domain" src/Modules/Reporting/ProjectManagement.Reporting.Infrastructure/ProjectManagement.Reporting.Infrastructure.csproj`
  - [x] BE-1.4: Nếu chưa có → thêm `ProjectReference` đến `Auth.Domain` (cho `UserManager<ApplicationUser>`)
  - [x] BE-1.5: `dotnet build src/Modules/Reporting/ProjectManagement.Reporting.Infrastructure` → 0 errors

- [x] **Task BE-2: AlertRulesWorker**
  - [x] BE-2.1: Tạo `Reporting.Infrastructure/Workers/AlertRulesWorker.cs`
  - [x] BE-2.2: Implement `ExecuteAsync` với `PeriodicTimer(TimeSpan.FromHours(1))`
  - [x] BE-2.3: Implement `CreateDeadlineAlertsAsync` — query tasks, get members, create alerts, dedup
  - [x] BE-2.4: Verify `IProjectsDbContext.ProjectTasks` DbSet name (check interface file)
  - [x] BE-2.5: Build → 0 errors

- [x] **Task BE-3: AlertDigestWorker**
  - [x] BE-3.1: Tạo `Reporting.Infrastructure/Workers/AlertDigestWorker.cs`
  - [x] BE-3.2: Implement `ExecuteAsync` với `PeriodicTimer(TimeSpan.FromHours(1))`, check `Monday && hour == 7`
  - [x] BE-3.3: Implement `SendDigestsAsync` — query `AlertPreferences` với overload type, send email per user
  - [x] BE-3.4: Build → 0 errors

- [x] **Task BE-4: ReportingModuleExtensions Update**
  - [x] BE-4.1: Thêm `services.AddHostedService<AlertRulesWorker>()` sau `AddHostedService<ExportWorker>()`
  - [x] BE-4.2: Thêm `services.AddHostedService<AlertDigestWorker>()` sau `AddHostedService<AlertRulesWorker>()`
  - [x] BE-4.3: `dotnet build src/Modules/Reporting/ProjectManagement.Reporting.Api` → 0 errors (0 errors, 1 pre-existing MSB3277 warning)

### Frontend Tasks

- [x] **Task FE-1: Alert Model & API Service**
  - [x] FE-1.1: Tạo `features/alerts/models/alert.model.ts` với `AlertDto` interface
  - [x] FE-1.2: Tạo `features/alerts/services/alerts-api.service.ts` với `getAlerts()` và `markRead()`
  - [x] FE-1.3: **VERIFY** response shape từ backend — `GetMyAlertsResult` có `.items` → service map `r.items`

- [x] **Task FE-2: NgRx Alerts Store**
  - [x] FE-2.1: Tạo `features/alerts/store/alert.actions.ts` — createActionGroup với 8 events
  - [x] FE-2.2: Tạo `features/alerts/store/alert.reducer.ts` — createFeature với `AlertsState`, 6 on() handlers
  - [x] FE-2.3: Tạo `features/alerts/store/alert.effects.ts` — `loadAlerts$`, `markAlertRead$`, `navigateOnMarkRead$`
  - [x] FE-2.4: Update `core/store/app.state.ts` — thêm `alerts: AlertsState` + import

- [x] **Task FE-3: AlertPanelComponent**
  - [x] FE-3.1: Tạo `features/alerts/components/alert-panel/alert-panel.ts` (standalone, OnPush)
  - [x] FE-3.2: Tạo `features/alerts/components/alert-panel/alert-panel.html`
  - [x] FE-3.3: Implement `onAlertClick()` — dispatch markAlertRead + closePanel + navigate

- [x] **Task FE-4: AppShellComponent Update**
  - [x] FE-4.1: Thêm `Store` injection vào `AppShellComponent`
  - [x] FE-4.2: Thêm `ngOnInit()` dispatch `AlertActions.loadAlerts()`
  - [x] FE-4.3: Thêm `unreadCount$` + `panelOpen$` selectors
  - [x] FE-4.4: Thêm `toggleAlertPanel()` + `closeAlertPanel()` methods
  - [x] FE-4.5: Update `app-shell.html` — notification button với badge + AlertPanelComponent overlay
  - [x] FE-4.6: Thêm `MatBadgeModule` + `AlertPanelComponent` + `AsyncPipe` + `NgIf` vào `@Component.imports`

- [x] **Task FE-5: app.config.ts Update**
  - [x] FE-5.1: Thêm `AlertsEffects` vào `provideEffects([...])`

- [x] **Task FE-6: Unit Tests**
  - [x] FE-6.1: Tạo `features/alerts/components/alert-panel/alert-panel.spec.ts` — pure function tests: `typeLabel` (4), `unreadCount` (3), `markReadInList` (4) = 11 tests total
  - [x] FE-6.2: Chạy vitest → 11/11 new tests pass

---

## References

- Epics: `_bmad-output/planning-artifacts/epics-dashboard.md` — Story 10-4
- Architecture: `_bmad-output/planning-artifacts/architecture.md` — Section 8, D-13 (IHostedService), AR-6, NFR-14, NFR-15
- Story 10-2 (Alert schema + API): `_bmad-output/implementation-artifacts/10-2-alert-center-data-model-schema-migration.md`
- `DigestWorker` pattern: `src/Modules/Notifications/ProjectManagement.Notifications.Infrastructure/Workers/DigestWorker.cs`
- `ExportWorker` pattern: `src/Modules/Reporting/ProjectManagement.Reporting.Infrastructure/Workers/ExportWorker.cs`
- `IEmailService`: `src/Modules/Notifications/ProjectManagement.Notifications.Application/Common/Interfaces/IEmailService.cs`
- `ReportingModuleExtensions`: `src/Modules/Reporting/ProjectManagement.Reporting.Api/Extensions/ReportingModuleExtensions.cs`
- `AppShellComponent`: `src/app/core/shell/app-shell.ts` + `app-shell.html`
- `app.state.ts`: `src/app/core/store/app.state.ts`
- `app.config.ts`: `src/app/app.config.ts`
- `DashboardEffects` pattern: `src/app/features/dashboard/store/dashboard.effects.ts`
- `ReportingEffects` pattern: `src/app/features/reporting/store/reporting.effects.ts`

---

## Dev Agent Record

### Agent Model Used
claude-sonnet-4-6

### Debug Log References
- `System.Web.HttpUtility.HtmlEncode` not available in .NET 10 without extra package. Fixed: used `System.Net.WebUtility.HtmlEncode` (built-in, no extra dep).
- `Reporting.Infrastructure.csproj` had no reference to `Notifications.Application` or `Auth.Domain`. Both added, build: 0 errors.
- Backend `GET /api/v1/alerts` returns `{ items: AlertDto[], totalCount: number }` (not `AlertDto[]` directly). Frontend `alerts-api.service.ts` correctly maps `r.items`.
- `AppShellComponent` initially had unused `AsyncPipe`/`NgIf`/`AlertPanelComponent` imports (IDE warning) because template hadn't been updated yet. Fixed by updating `app-shell.html` to use all three.

### Completion Notes List
- BE: `AlertRulesWorker` creates deadline alerts hourly (PeriodicTimer) with dedup; `AlertDigestWorker` sends overload email digest on Monday 7:00 UTC based on `AlertPreference` table; both registered in `ReportingModuleExtensions`. Build: 0 errors.
- FE: `alerts` NgRx feature created (model, API service, actions, reducer, effects); `AlertPanelComponent` standalone OnPush with click-to-mark-read + navigation; `AppShellComponent` extended with Store injection, badge, panel toggle; `app.state.ts` + `app.config.ts` updated.
- Tests: 11/11 pure function tests pass (typeLabel ×4, unreadCount ×3, markReadInList ×4). Pre-existing TestBed failures (stat-cards etc.) are unrelated.
- `getAlerts()` service maps backend `{ items, totalCount }` to `AlertDto[]` via RxJS `map()`.
- Alert panel placed as CSS overlay outside `mat-sidenav-container` so it renders on top of all content.

### File List
- `src/Modules/Reporting/ProjectManagement.Reporting.Infrastructure/ProjectManagement.Reporting.Infrastructure.csproj` — MODIFIED (added Notifications.Application + Auth.Domain refs)
- `src/Modules/Reporting/ProjectManagement.Reporting.Infrastructure/Workers/AlertRulesWorker.cs` — NEW
- `src/Modules/Reporting/ProjectManagement.Reporting.Infrastructure/Workers/AlertDigestWorker.cs` — NEW
- `src/Modules/Reporting/ProjectManagement.Reporting.Api/Extensions/ReportingModuleExtensions.cs` — MODIFIED (AddHostedService x2)
- `frontend/project-management-web/src/app/features/alerts/models/alert.model.ts` — NEW
- `frontend/project-management-web/src/app/features/alerts/services/alerts-api.service.ts` — NEW
- `frontend/project-management-web/src/app/features/alerts/store/alert.actions.ts` — NEW
- `frontend/project-management-web/src/app/features/alerts/store/alert.reducer.ts` — NEW
- `frontend/project-management-web/src/app/features/alerts/store/alert.effects.ts` — NEW
- `frontend/project-management-web/src/app/features/alerts/components/alert-panel/alert-panel.ts` — NEW
- `frontend/project-management-web/src/app/features/alerts/components/alert-panel/alert-panel.html` — NEW
- `frontend/project-management-web/src/app/features/alerts/components/alert-panel/alert-panel.spec.ts` — NEW
- `frontend/project-management-web/src/app/core/store/app.state.ts` — MODIFIED (alerts slice added)
- `frontend/project-management-web/src/app/core/shell/app-shell.ts` — MODIFIED (Store + alerts badge + panel)
- `frontend/project-management-web/src/app/core/shell/app-shell.html` — MODIFIED (notification button + alert panel)
- `frontend/project-management-web/src/app/app.config.ts` — MODIFIED (AlertsEffects registered)
