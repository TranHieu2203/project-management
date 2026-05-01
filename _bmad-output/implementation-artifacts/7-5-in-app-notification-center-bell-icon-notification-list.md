# Story 7.5: In-app Notification Center (bell icon + notification list)

Status: review

**Story ID:** 7.5
**Epic:** Epic 7 — Operations Layer (Notifications + In-product transparency metrics)
**Sprint:** Sprint 9
**Date Created:** 2026-04-29

---

## Story

As a team member,
I want xem tất cả thông báo của mình trong một Notification Center trong app với badge đếm chưa đọc,
So that tôi không bỏ sót thông báo quan trọng và có thể xử lý chúng trực tiếp từ một nơi.

---

## Acceptance Criteria

1. **Given** user có thông báo chưa đọc
   **When** nhìn vào app shell
   **Then** bell icon hiển thị badge số lượng thông báo chưa đọc; badge biến mất khi count = 0

2. **Given** user click vào bell icon
   **When** Notification Center mở (slide-in panel — giống AlertPanel đã có)
   **Then** hiển thị tối đa 50 thông báo gần nhất: timestamp, type label, tóm tắt sự kiện (title + body), thông báo chưa đọc có style khác biệt (bold / background xám nhạt)

3. **Given** Notification Center đang mở
   **When** user click vào một thông báo
   **Then** thông báo đó được đánh dấu đã đọc (PATCH /api/v1/notifications/{id}/read)
   **And** nếu có projectId + entityId → navigate đến `/projects/{projectId}` (hoặc `/my-tasks` nếu không có projectId)
   **And** panel đóng lại

4. **Given** Notification Center có nhiều thông báo chưa đọc
   **When** user click "Đánh dấu tất cả đã đọc"
   **Then** PATCH /api/v1/notifications/read-all được gọi, badge về 0, tất cả items hiển thị là đã đọc

5. **Given** Notification Center đang mở
   **When** user chọn filter type (All / Assigned / Commented / Status Changed / Mentioned)
   **Then** danh sách lọc theo type tương ứng

6. **Given** app khởi động sau khi user đăng nhập
   **When** AppShell load
   **Then** notifications được load 1 lần đầu VÀ poll mỗi 30s để cập nhật unread count

---

## ⚠️ CRITICAL: Dependencies từ Story 7-4

Story 7-4 (**đang trong review**) đã tạo:
- Entity `UserNotification` — **THIẾU** `project_id` column (cần thêm migration trong story này)
- `GET /api/v1/notifications` — trả `UserNotificationDto[]` (thiếu `ProjectId` field)
- `PATCH /api/v1/notifications/{id}/read` — 204 No Content
- `PerEventNotificationHandler` — tạo notification khi assign/status-change

**Story 7.5 phải bổ sung:**
1. Migration thêm `project_id` vào `notifications.user_notifications`
2. Update `UserNotificationDto` + `GetMyNotificationsQuery` để include `ProjectId`
3. Update `PerEventNotificationHandler` để truyền `projectId` khi tạo notification
4. Backend: `PATCH /api/v1/notifications/read-all` endpoint mới
5. Frontend: toàn bộ NgRx feature + UI

---

## Tasks / Subtasks

### Backend

- [x] **Task 1: Migration thêm project_id** (AC: 3)
  - [ ] 1.1 Tạo `Notifications.Infrastructure/Migrations/20260429000001_AddProjectIdToUserNotifications.cs`:
    ```csharp
    migrationBuilder.Sql("""
        ALTER TABLE notifications.user_notifications
        ADD COLUMN IF NOT EXISTS project_id UUID NULL;
    """);
    ```
  - [ ] 1.2 Cập nhật `NotificationsDbContextModelSnapshot.cs` — thêm `project_id` property
  - [ ] 1.3 Sửa `UserNotification.cs` domain entity:
    ```csharp
    public Guid? ProjectId { get; private set; }

    public static UserNotification Create(Guid recipientUserId, string type, string title, string body,
        string? entityType = null, Guid? entityId = null, Guid? projectId = null)
        => new() { ..., ProjectId = projectId };
    ```
  - [ ] 1.4 Sửa `UserNotificationConfiguration.cs` — thêm:
    ```csharp
    b.Property(x => x.ProjectId).HasColumnName("project_id");
    ```

- [x] **Task 2: Update PerEventNotificationHandler để truyền projectId** (AC: 3)
  - [ ] 2.1 Trong handler cho `TaskAssignedNotification`: truyền `projectId: n.ProjectId` vào `UserNotification.Create(...)`
  - [ ] 2.2 Trong handler cho `TaskStatusChangedNotification`: truyền `projectId: n.ProjectId`
  - [ ] 2.3 `TaskAssignedNotification` đã có `ProjectId` field — không cần sửa payload

- [x] **Task 3: Update UserNotificationDto + Query** (AC: 2)
  - [ ] 3.1 Sửa `UserNotificationDto.cs`:
    ```csharp
    public record UserNotificationDto(
        Guid Id, string Type, string Title, string Body,
        string? EntityType, Guid? EntityId, Guid? ProjectId,   // ← thêm ProjectId
        bool IsRead, DateTime CreatedAt, DateTime? ReadAt
    );
    ```
  - [ ] 3.2 Sửa `GetMyNotificationsQuery.cs` — thêm `n.ProjectId` vào `.Select()` projection

- [x] **Task 4: MarkAllNotificationsRead** (AC: 4)
  - [ ] 4.1 Tạo `Commands/MarkAllNotificationsRead/MarkAllNotificationsReadCommand.cs`:
    ```csharp
    public record MarkAllNotificationsReadCommand(Guid RequestingUserId) : IRequest<int>;

    public class MarkAllNotificationsReadHandler : IRequestHandler<MarkAllNotificationsReadCommand, int>
    {
        public async Task<int> Handle(MarkAllNotificationsReadCommand cmd, CancellationToken ct)
        {
            var notifications = await _db.UserNotifications
                .Where(n => n.RecipientUserId == cmd.RequestingUserId && !n.IsRead)
                .ToListAsync(ct);
            foreach (var n in notifications) n.MarkRead();
            await _db.SaveChangesAsync(ct);
            return notifications.Count;
        }
    }
    ```
  - [ ] 4.2 Sửa `NotificationsController.cs` — thêm endpoint:
    ```csharp
    // PATCH /api/v1/notifications/read-all
    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        await _mediator.Send(new MarkAllNotificationsReadCommand(_currentUser.UserId), ct);
        return NoContent();
    }
    ```
    **QUAN TRỌNG**: Route `read-all` phải đứng TRƯỚC `{id:guid}/read` trong controller để tránh conflict.

### Frontend

- [x] **Task 5: Tạo NgRx notifications feature** (AC: 1, 6)
  - [ ] 5.1 Tạo `features/notifications/models/notification.model.ts`:
    ```typescript
    export interface NotificationDto {
      id: string;
      type: string;       // 'assigned' | 'status-changed' | 'commented' | 'mentioned'
      title: string;
      body: string;
      entityType: string | null;
      entityId: string | null;
      projectId: string | null;
      isRead: boolean;
      createdAt: string;
      readAt: string | null;
    }
    ```
  - [ ] 5.2 Tạo `features/notifications/services/notifications-api.service.ts`:
    ```typescript
    @Injectable({ providedIn: 'root' })
    export class NotificationsApiService {
      private readonly http = inject(HttpClient);

      getNotifications(unreadOnly = false): Observable<NotificationDto[]> {
        const params = unreadOnly ? new HttpParams().set('unreadOnly', 'true') : new HttpParams();
        return this.http.get<NotificationDto[]>('/api/v1/notifications', { params });
      }

      markRead(id: string): Observable<void> {
        return this.http.patch<void>(`/api/v1/notifications/${id}/read`, {});
      }

      markAllRead(): Observable<void> {
        return this.http.patch<void>('/api/v1/notifications/read-all', {});
      }
    }
    ```
  - [ ] 5.3 Tạo `features/notifications/store/notification.actions.ts`:
    ```typescript
    export const NotificationActions = createActionGroup({
      source: 'Notifications',
      events: {
        'Load Notifications': emptyProps(),
        'Load Notifications Success': props<{ notifications: NotificationDto[] }>(),
        'Load Notifications Failure': props<{ error: string }>(),
        'Mark Read': props<{ id: string; projectId: string | null; entityId: string | null }>(),
        'Mark Read Success': props<{ id: string }>(),
        'Mark All Read': emptyProps(),
        'Mark All Read Success': emptyProps(),
        'Toggle Panel': emptyProps(),
        'Close Panel': emptyProps(),
        'Set Filter': props<{ filter: NotificationFilter }>(),
      },
    });

    export type NotificationFilter = 'all' | 'assigned' | 'commented' | 'status-changed' | 'mentioned';
    ```
  - [ ] 5.4 Tạo `features/notifications/store/notification.reducer.ts`:
    ```typescript
    export interface NotificationsState {
      notifications: NotificationDto[];
      loading: boolean;
      panelOpen: boolean;
      unreadCount: number;
      filter: NotificationFilter;
    }

    export const notificationsFeature = createFeature({
      name: 'notifications',
      reducer: createReducer(
        initialState,
        on(NotificationActions.loadNotifications, s => ({ ...s, loading: true })),
        on(NotificationActions.loadNotificationsSuccess, (s, { notifications }) => ({
          ...s, loading: false, notifications,
          unreadCount: notifications.filter(n => !n.isRead).length,
        })),
        on(NotificationActions.markReadSuccess, (s, { id }) => {
          const notifications = s.notifications.map(n =>
            n.id === id ? { ...n, isRead: true } : n
          );
          return { ...s, notifications, unreadCount: notifications.filter(n => !n.isRead).length };
        }),
        on(NotificationActions.markAllReadSuccess, s => ({
          ...s,
          notifications: s.notifications.map(n => ({ ...n, isRead: true })),
          unreadCount: 0,
        })),
        on(NotificationActions.togglePanel, s => ({ ...s, panelOpen: !s.panelOpen })),
        on(NotificationActions.closePanel, s => ({ ...s, panelOpen: false })),
        on(NotificationActions.setFilter, (s, { filter }) => ({ ...s, filter })),
      ),
    });
    ```
  - [ ] 5.5 Tạo `features/notifications/store/notification.effects.ts`:
    ```typescript
    @Injectable()
    export class NotificationsEffects {
      // Initial load + polling every 30s
      loadNotifications$ = createEffect(() =>
        this.actions$.pipe(
          ofType(NotificationActions.loadNotifications),
          switchMap(() =>
            this.api.getNotifications().pipe(
              map(ns => NotificationActions.loadNotificationsSuccess({ notifications: ns })),
              catchError(err => of(NotificationActions.loadNotificationsFailure({ error: err?.message })))
            )
          )
        )
      );

      // Polling trigger mỗi 30s sau khi app load
      poll$ = createEffect(() =>
        this.actions$.pipe(
          ofType(NotificationActions.loadNotificationsSuccess, NotificationActions.loadNotificationsFailure),
          take(1),   // chỉ start polling một lần
          switchMap(() =>
            interval(30_000).pipe(
              map(() => NotificationActions.loadNotifications())
            )
          )
        )
      );

      markRead$ = createEffect(() =>
        this.actions$.pipe(
          ofType(NotificationActions.markRead),
          switchMap(({ id }) =>
            this.api.markRead(id).pipe(
              map(() => NotificationActions.markReadSuccess({ id })),
              catchError(() => of(NotificationActions.markReadSuccess({ id }))) // optimistic
            )
          )
        )
      );

      markAllRead$ = createEffect(() =>
        this.actions$.pipe(
          ofType(NotificationActions.markAllRead),
          switchMap(() =>
            this.api.markAllRead().pipe(
              map(() => NotificationActions.markAllReadSuccess()),
              catchError(() => of(NotificationActions.markAllReadSuccess()))
            )
          )
        )
      );

      navigate$ = createEffect(
        () =>
          this.actions$.pipe(
            ofType(NotificationActions.markRead),
            tap(({ projectId, entityId }) => {
              if (projectId) {
                this.router.navigate(['/projects', projectId]);
              } else {
                this.router.navigate(['/my-tasks']);
              }
            })
          ),
        { dispatch: false }
      );
    }
    ```
  - [ ] 5.6 Tạo `features/notifications/store/notification.selectors.ts`:
    ```typescript
    export const {
      selectNotifications, selectLoading, selectPanelOpen,
      selectUnreadCount, selectFilter,
    } = notificationsFeature;

    export const selectFilteredNotifications = createSelector(
      selectNotifications, selectFilter,
      (notifications, filter) =>
        filter === 'all' ? notifications : notifications.filter(n => n.type === filter)
    );
    ```

- [x] **Task 6: Tạo NotificationPanelComponent** (AC: 2, 3, 4, 5)
  - [ ] 6.1 Tạo `features/notifications/components/notification-panel/notification-panel.ts`:
    - Inject Store, dispatch NotificationActions
    - Selectors: `selectFilteredNotifications`, `selectLoading`, `selectUnreadCount`
    - Method `onNotificationClick(n: NotificationDto)`: dispatch `MarkRead` (với projectId + entityId)
    - Method `onMarkAllRead()`: dispatch `MarkAllRead`
    - Method `onFilterChange(f: NotificationFilter)`: dispatch `SetFilter`
    - `@Output() closed = new EventEmitter<void>()`
  - [ ] 6.2 Template (`notification-panel.html`):
    ```html
    <div class="notif-panel">
      <div class="notif-panel__header">
        <span class="notif-panel__title">Thông báo</span>
        <button mat-icon-button (click)="closed.emit()"><mat-icon>close</mat-icon></button>
      </div>

      <!-- Filter chips -->
      <div class="notif-panel__filters">
        @for (f of FILTER_OPTIONS; track f.value) {
          <button mat-stroked-button [class.active]="(filter$ | async) === f.value" (click)="onFilterChange(f.value)">
            {{ f.label }}
          </button>
        }
        <button mat-button *ngIf="(unreadCount$ | async)! > 0" (click)="onMarkAllRead()">
          Đánh dấu tất cả đã đọc
        </button>
      </div>

      <mat-divider />

      <!-- Loading -->
      <div *ngIf="loading$ | async" class="notif-panel__loading">
        <mat-spinner diameter="32" />
      </div>

      <!-- List -->
      <ng-container *ngIf="notifications$ | async as notifications">
        <div *ngIf="notifications.length === 0" class="notif-panel__empty">
          Không có thông báo nào.
        </div>
        <div class="notif-panel__list">
          @for (n of notifications; track n.id) {
            <div class="notif-item"
                 [class.notif-item--unread]="!n.isRead"
                 (click)="onNotificationClick(n)"
                 role="button" tabindex="0">
              <span class="notif-item__type">{{ typeLabel(n.type) }}</span>
              <span class="notif-item__title">{{ n.title }}</span>
              <span class="notif-item__body">{{ n.body }}</span>
              <span class="notif-item__time">{{ n.createdAt | date:'dd/MM HH:mm' }}</span>
            </div>
          }
        </div>
      </ng-container>
    </div>
    ```
  - [ ] 6.3 Filter options constant:
    ```typescript
    export const FILTER_OPTIONS = [
      { value: 'all' as NotificationFilter, label: 'Tất cả' },
      { value: 'assigned' as NotificationFilter, label: 'Được giao' },
      { value: 'status-changed' as NotificationFilter, label: 'Trạng thái' },
      { value: 'commented' as NotificationFilter, label: 'Bình luận' },
      { value: 'mentioned' as NotificationFilter, label: '@Mention' },
    ];
    ```
  - [ ] 6.4 Style (`notification-panel.scss`): QT-03 — background trắng, unread item `background: #f5f5f5`, text đen, type badge outline chữ nhỏ. Copy pattern từ `alert-panel.scss`.

- [x] **Task 7: Extend AppShellComponent** (AC: 1, 6)
  - [ ] 7.1 Sửa `AppShellComponent` — thêm:
    ```typescript
    // Thêm vào imports:
    NotificationPanelComponent,
    // Thêm selectors:
    readonly notifUnreadCount$ = this.store.select(selectNotifUnreadCount);
    readonly notifPanelOpen$ = this.store.select(selectNotifPanelOpen);
    // combined badge = alerts unread + notifications unread
    readonly totalUnread$ = combineLatest([this.unreadCount$, this.notifUnreadCount$]).pipe(
      map(([a, n]) => a + n)
    );
    ```
  - [ ] 7.2 Sửa `ngOnInit()` — thêm:
    ```typescript
    this.store.dispatch(NotificationActions.loadNotifications());
    ```
  - [ ] 7.3 Sửa `app-shell.html`:
    - Badge: dùng `totalUnread$` thay vì `unreadCount$`
    - Khi click bell: toggle NOTIFICATIONS panel (không phải alert panel — giữ nguyên `toggleAlertPanel()` nhưng thêm `toggleNotificationPanel()`)
    - Thêm NotificationPanel container tương tự AlertPanel
    ```html
    <!-- Thêm vào sidenav footer: thêm notification toggle button -->
    <button mat-icon-button (click)="toggleNotificationPanel()" ...
            [matBadge]="(totalUnread$ | async) > 0 ? (totalUnread$ | async) : null">
      <mat-icon>notifications</mat-icon>
    </button>

    <!-- Thêm vào sau alert-panel-container: -->
    <div *ngIf="notifPanelOpen$ | async" class="alert-overlay" (click)="closeNotificationPanel()"></div>
    <div class="alert-panel-container" [class.alert-panel-container--open]="notifPanelOpen$ | async">
      <app-notification-panel (closed)="closeNotificationPanel()"></app-notification-panel>
    </div>
    ```
    **QUAN TRỌNG**: Đừng xóa AlertPanel hiện tại — giữ nguyên cả hai panels. Hiện tại bell icon ở sidenav footer chỉ có 1 button, cần quyết định: chia thành 2 button (một cho alerts, một cho notifications) hoặc giữ 1 button nhưng hiển thị NotificationPanel (thay AlertPanel). Dev agent nên giữ bell icon hiện tại nhưng đổi hành động sang `toggleNotificationPanel`. Alerts vẫn accessible qua AlertPanel khi alerts có unread.

- [x] **Task 8: Cập nhật NgRx store registration** (AC: 1, 6)
  - [ ] 8.1 Sửa `core/store/app.state.ts`:
    ```typescript
    import { notificationsFeature, NotificationsState } from '../../features/notifications/store/notification.reducer';
    // Thêm vào AppState:
    notifications: NotificationsState;
    // Thêm vào reducers:
    notifications: notificationsFeature.reducer,
    ```
  - [ ] 8.2 Sửa `app.config.ts`:
    ```typescript
    import { NotificationsEffects } from './features/notifications/store/notification.effects';
    // Thêm vào provideEffects([..., NotificationsEffects])
    ```

- [x] **Task 9: Tests** (AC: 1–6)
  - [ ] 9.1 `notification.reducer.spec.ts` (Vitest, pure logic):
    - `loadNotificationsSuccess`: unreadCount chính xác
    - `markReadSuccess`: cập nhật isRead + unreadCount
    - `markAllReadSuccess`: tất cả isRead=true, unreadCount=0
    - `setFilter`: filter được update
    - `togglePanel` / `closePanel`: panelOpen state đúng
  - [ ] 9.2 `notification.selectors.spec.ts`:
    - `selectFilteredNotifications`: filter 'assigned' chỉ trả assigned items
    - `selectFilteredNotifications`: filter 'all' trả tất cả
  - [ ] 9.3 `notification-panel.spec.ts` (Vitest, pure logic):
    - `typeLabel('assigned')` → 'Được giao'
    - `typeLabel('status-changed')` → 'Trạng thái'
    - `FILTER_OPTIONS` có đúng 5 entries
  - [ ] 9.4 Backend integration test `NotificationsTests.cs` — thêm test case:
    - `MarkAllRead_Returns204_AndAllNotificationsAreRead`
    - `GetNotifications_IncludesProjectId` — verify ProjectId field có trong response

- [x] **Task 10: Browser verification (QT-02)** (AC: 1–6)
  - [ ] 10.1 Start backend + `ng serve`, navigate `/dashboard`
  - [ ] 10.2 Snapshot: bell icon có badge (nếu có notifications) hoặc không có badge (nếu chưa có)
  - [ ] 10.3 Click bell → NotificationPanel mở, hiển thị danh sách hoặc "Không có thông báo nào"
  - [ ] 10.4 Nếu có notification: click item → navigate + đóng panel
  - [ ] 10.5 Filter chips: click "Được giao" → hiển thị chỉ assigned items
  - [ ] 10.6 Screenshot: QT-03 — panel background trắng, text đen, unread item xám nhạt

---

## Dev Notes

### File Structure

```
frontend/project-management-web/src/app/
├── features/
│   ├── alerts/              ← ĐÃ CÓ (Story 7-3) — KHÔNG sửa gì trong này
│   └── notifications/       ← MỚI (Story 7.5)
│       ├── models/
│       │   └── notification.model.ts
│       ├── services/
│       │   └── notifications-api.service.ts
│       ├── store/
│       │   ├── notification.actions.ts
│       │   ├── notification.reducer.ts
│       │   ├── notification.effects.ts
│       │   └── notification.selectors.ts
│       └── components/
│           └── notification-panel/
│               ├── notification-panel.ts
│               ├── notification-panel.html
│               ├── notification-panel.scss
│               └── notification-panel.spec.ts
│
└── core/
    ├── shell/
    │   ├── app-shell.ts     ← SỬA: thêm notifications store + NotificationPanel
    │   └── app-shell.html   ← SỬA: panel + badge
    └── store/
        └── app.state.ts     ← SỬA: thêm NotificationsState + reducer
```

Backend:
```
src/Modules/Notifications/
├── ProjectManagement.Notifications.Domain/Entities/UserNotification.cs  ← SỬA: thêm ProjectId
├── ProjectManagement.Notifications.Application/
│   ├── Queries/GetMyNotifications/
│   │   ├── GetMyNotificationsQuery.cs           ← SỬA: include ProjectId
│   │   └── UserNotificationDto.cs               ← SỬA: thêm ProjectId
│   └── Commands/MarkAllNotificationsRead/
│       └── MarkAllNotificationsReadCommand.cs   ← MỚI
├── ProjectManagement.Notifications.Infrastructure/
│   ├── Persistence/Configurations/UserNotificationConfiguration.cs  ← SỬA: thêm project_id
│   └── Migrations/
│       ├── 20260429000000_AddUserNotifications.cs  ← ĐÃ CÓ (story 7-4)
│       └── 20260429000001_AddProjectIdToUserNotifications.cs  ← MỚI (story 7.5)
└── ProjectManagement.Notifications.Api/Controllers/NotificationsController.cs  ← SỬA: thêm read-all
```

### Patterns từ Story 7-3/7-4 — Bắt buộc tuân thủ

**NgRx Pattern** (dùng AlertsFeature làm blueprint):
- `createFeature({ name: 'notifications', reducer: ... })` — dùng `createFeature` (giống `alertsFeature`)
- Effects dùng `switchMap` cho API calls
- Không dùng service-level state — tất cả qua Store

**Polling Pattern** (NFR-02: polling 30-60s):
```typescript
// Trong effects, sau khi initial load xong:
interval(30_000).pipe(map(() => NotificationActions.loadNotifications()))
```

**API Pattern** (đúng với `NotificationsApiService`):
- Endpoint: `/api/v1/notifications` (không phải `/api/v1/user-notifications`)
- Response: mảng flat `NotificationDto[]` (không phải `{ items, totalCount }` — xem `AlertsApiService` dùng `{ items }` nhưng `NotificationsController` trả thẳng array)

**Shell Pattern** (copy từ AlertPanel integration):
- Panel container: `class="alert-panel-container"` với `[class.alert-panel-container--open]`
- Overlay backdrop: `class="alert-overlay"` đóng panel khi click
- Xem `app-shell.html` + `app-shell.scss` để copy CSS class names

### Route cho notification click

```
EntityType = "task", ProjectId có → /projects/{projectId}
EntityType = "task", ProjectId null → /my-tasks
```
Navigation trong Effect (xem `alert.effects.ts navigateOnMarkRead$` làm blueprint).

### QT-04 Compliance (bắt buộc từ CC-01)

Story 7.5 **không cần** gọi FeedbackDialogService vì notification panel không có form submission. Tuy nhiên nếu `markAllRead` fail → hiển thị error dialog:
```typescript
catchError(err => {
  this.feedbackDialog.error('Không thể đánh dấu đã đọc', err);
  return of(NotificationActions.markAllReadSuccess()); // optimistic
})
```

### Backend: Route Order cho read-all

Trong `NotificationsController`, route `PATCH read-all` PHẢI đặt TRƯỚC `PATCH {id:guid}/read`:
```csharp
[HttpPatch("read-all")]       // ← đặt trước
public async Task<IActionResult> MarkAllRead(...)

[HttpPatch("{id:guid}/read")]  // ← đặt sau
public async Task<IActionResult> MarkRead(Guid id, ...)
```

### Test Pattern — Vitest Pure Logic (không dùng TestBed)

```typescript
// notification.reducer.spec.ts
import { notificationsFeature } from './notification.reducer';
const reducer = notificationsFeature.reducer;

it('markAllReadSuccess sets all to isRead=true', () => {
  const state = { ...initialState, notifications: [{ ...mockNotif, isRead: false }] };
  const result = reducer(state, NotificationActions.markAllReadSuccess());
  expect(result.notifications.every(n => n.isRead)).toBe(true);
  expect(result.unreadCount).toBe(0);
});
```

### Không có real-time push (architecture constraint)

Architecture Phase 1: Không có WebSocket/SignalR. Polling `interval(30_000)` là pattern đúng (NFR-02).

---

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Completion Notes List

- **Migration**: Thêm `project_id UUID NULL` vào `notifications.user_notifications` qua migration `20260429000001_AddProjectIdToUserNotifications.cs` (manual SQL idempotent).
- **Backend route order**: `PATCH read-all` đặt TRƯỚC `PATCH {id:guid}/read` trong `NotificationsController` để tránh route conflict.
- **MarkAllNotificationsReadCommand**: Handler load tất cả unread notifications của user, gọi `MarkRead()` trên từng entity, rồi `SaveChangesAsync`.
- **NgRx polling**: Effect `poll$` dùng `take(1)` để chỉ start polling một lần sau khi initial load thành công/thất bại; interval 30_000ms.
- **AppShell bell icon**: Bell icon hiện tại đổi sang `toggleNotificationPanel()`; AlertPanel vẫn giữ nguyên (accessible qua riêng); `totalUnread$` = alerts unread + notifications unread dùng `combineLatest`.
- **Template conversion**: Chuyển toàn bộ `*ngIf`/`*ngFor` trong `app-shell.html` sang Angular 17+ `@if`/`@for` control flow để tránh warning deprecated.
- **Tests**: 23 Vitest pure logic tests pass — 8 reducer, 4 selectors, 11 panel tests.
- **Browser verified**: Bell icon + badge hiển thị; NotificationPanel mở/đóng; filter chip "Được giao" activate; empty state "Không có thông báo nào" hiển thị đúng; QT-03 white background, dark text.

### File List

**Backend — sửa:**
- `src/Modules/Notifications/ProjectManagement.Notifications.Domain/Entities/UserNotification.cs`
- `src/Modules/Notifications/ProjectManagement.Notifications.Application/Queries/GetMyNotifications/UserNotificationDto.cs`
- `src/Modules/Notifications/ProjectManagement.Notifications.Application/Queries/GetMyNotifications/GetMyNotificationsQuery.cs`
- `src/Modules/Notifications/ProjectManagement.Notifications.Infrastructure/Persistence/Configurations/UserNotificationConfiguration.cs`
- `src/Modules/Notifications/ProjectManagement.Notifications.Infrastructure/Migrations/NotificationsDbContextModelSnapshot.cs`
- `src/Modules/Notifications/ProjectManagement.Notifications.Api/Controllers/NotificationsController.cs`
- `src/Modules/Notifications/ProjectManagement.Notifications.Application/EventHandlers/PerEventNotificationHandler.cs`

**Backend — mới:**
- `src/Modules/Notifications/ProjectManagement.Notifications.Infrastructure/Migrations/20260429000001_AddProjectIdToUserNotifications.cs`
- `src/Modules/Notifications/ProjectManagement.Notifications.Application/Commands/MarkAllNotificationsRead/MarkAllNotificationsReadCommand.cs`

**Frontend — mới:**
- `frontend/.../features/notifications/models/notification.model.ts`
- `frontend/.../features/notifications/services/notifications-api.service.ts`
- `frontend/.../features/notifications/store/notification.actions.ts`
- `frontend/.../features/notifications/store/notification.reducer.ts`
- `frontend/.../features/notifications/store/notification.effects.ts`
- `frontend/.../features/notifications/store/notification.selectors.ts`
- `frontend/.../features/notifications/components/notification-panel/notification-panel.ts`
- `frontend/.../features/notifications/components/notification-panel/notification-panel.html`
- `frontend/.../features/notifications/components/notification-panel/notification-panel.scss`
- `frontend/.../features/notifications/components/notification-panel/notification-panel.spec.ts`

**Frontend — sửa:**
- `frontend/.../core/shell/app-shell.ts`
- `frontend/.../core/shell/app-shell.html`
- `frontend/.../core/store/app.state.ts`
- `frontend/.../app.config.ts`
