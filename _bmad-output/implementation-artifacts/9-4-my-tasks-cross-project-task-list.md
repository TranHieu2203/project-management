# Story 9.4: My Tasks — Cross-Project Task List

Status: review

**Story ID:** 9.4
**Epic:** Epic 9 — Dashboard Overview — Morning Command Center
**Sprint:** Sprint 9 (sau Story 9-3)
**Date Created:** 2026-04-29

---

## Story

As a PM,
I want to see all tasks assigned to me across all projects in one list with filters,
So that I can manage my personal workload without switching between project views.

---

## ⚠️ Critical Dependencies — PHẢI implement SAU 9-1, 9-2, 9-3

Story 9-4 builds on top of:
- **9-1**: `features/dashboard/` folder, `DashboardShellComponent`, `dashboard.routes.ts`, NgRx store (`DashboardActions`, `DashboardReducer`, `DashboardEffects`, `DashboardFacade`, `DashboardApiService`), route `/dashboard` trong `app.routes.ts`
- **9-2**: `DashboardApiService.getStatCards()`, `DashboardApiService.getDeadlines()`, backend `DashboardController`
- **9-3**: `DashboardFacade.selectedProjectIds$`, `DashboardFacade.setProjectFilter()`, `@ngrx/router-store` configured in `app.config.ts` and `app.state.ts`

**Verify trước khi implement:**
```bash
# 1. Dashboard feature folder tồn tại
ls frontend/project-management-web/src/app/features/dashboard/

# 2. Dashboard route tồn tại trong app.routes.ts
grep -n "dashboard" frontend/project-management-web/src/app/app.routes.ts

# 3. DashboardFacade.selectedProjectIds$ tồn tại
grep -n "selectedProjectIds" frontend/project-management-web/src/app/features/dashboard/store/dashboard.facade.ts

# 4. DashboardApiService tồn tại
ls frontend/project-management-web/src/app/features/dashboard/services/dashboard-api.service.ts
```

---

## Scope Boundary

### ✅ Story 9-4 làm:
- Route `/dashboard/my-tasks` (child của dashboard shell từ 9-1)
- Component `features/dashboard/components/my-tasks/my-tasks.ts` — **KHÁC** với `features/projects/components/my-tasks/my-tasks.ts` (đã có từ 8-1)
- Backend endpoint `GET /api/v1/dashboard/my-tasks` — **KHÁC** với `GET /api/v1/my-tasks` (đã có)
- Status filter + URL sync (`?status=InProgress&page=2`)
- Project filter shared từ `DashboardFacade` (từ 9-3)
- Pagination server-side (page size 20)
- Click task → navigate đến Gantt với task highlighted

### ❌ Story 9-4 KHÔNG làm:
- Sửa existing `features/projects/components/my-tasks/` (Story 8-1 component — không chạm vào)
- Sửa existing `GET /api/v1/my-tasks` endpoint
- "Assignee" quick-assign từ dashboard list
- Real-time update / polling cho my-tasks (polling chỉ cho overview)
- Sorting thay đổi được (luôn sort by due_date ASC)

---

## Acceptance Criteria

### AC-1: Route và Navigation

**Given** PM đã đăng nhập và navigate đến `/dashboard/my-tasks`
**When** trang load
**Then** hiển thị danh sách tasks được assign cho PM hiện tại, cross-project, sorted by due_date ASC (null dates xuống cuối)
**And** mỗi task row hiển thị: task name, project name (badge), status chip, due_date, % complete
**And** nút back "← Dashboard" navigate về `/dashboard/overview`

---

### AC-2: Status Filter → URL Sync

**Given** PM select filter status = "Đang thực hiện" từ dropdown
**When** filter thay đổi
**Then** chỉ hiển thị tasks có status = InProgress
**And** URL update ngay: `?status=InProgress` (không reload trang)
**And** page reset về 1

**Given** PM mở URL `/dashboard/my-tasks?status=InProgress`
**When** component init
**Then** dropdown pre-selected = "Đang thực hiện", chỉ hiển thị tasks InProgress

**Status filter options:**
| Label | Value |
|---|---|
| Tất cả | (null / empty) |
| Chưa bắt đầu | NotStarted |
| Đang thực hiện | InProgress |
| Tạm dừng | OnHold |
| Bị trễ | Delayed |
| Hoàn thành | Completed |

---

### AC-3: Project Filter (từ DashboardFacade)

**Given** PM đang ở `/dashboard/overview` và đã chọn project filter (2 trong 5 projects)
**When** PM navigate đến `/dashboard/my-tasks`
**Then** my-tasks list chỉ hiển thị tasks thuộc 2 projects đã filter
**And** filter bar của dashboard shell vẫn hiển thị 2 projects selected

**Given** PM ở `/dashboard/my-tasks?projects=id1,id2`
**When** PM thay đổi project filter (chọn thêm project id3)
**Then** DashboardFacade cập nhật project filter → URL update → my-tasks reload với 3 projects

**Note:** Project filter được quản lý bởi `DashboardFacade.setProjectFilter()` (từ 9-3). `DashboardMyTasksComponent` chỉ subscribe `selectedProjectIds$` — KHÔNG tự dispatch filter actions.

---

### AC-4: Pagination

**Given** PM có > 20 tasks được assign
**When** render
**Then** hiển thị tối đa 20 tasks, pagination controls bên dưới: "< Trang trước | Trang 1 / 3 | Trang sau >"
**And** URL sync: trang 2 → `?page=2`

**Given** PM đang ở trang 2 (`?page=2`) và thay đổi status filter
**When** filter thay đổi
**Then** page reset về 1, URL update: `?status=InProgress&page=1` (hoặc chỉ `?status=InProgress` nếu page=1)

**Given** tổng số tasks ≤ 20
**When** render
**Then** KHÔNG hiển thị pagination controls

---

### AC-5: Click Task → Navigate

**Given** PM click vào một task row trong list
**When** click xảy ra
**Then** navigate đến `/projects/{projectId}?view=grid&highlight={taskId}`
**And** back navigation từ project view giữ nguyên dashboard route

---

### AC-6: Empty State

**Given** PM không có task nào được assign (hoặc filter không match)
**When** my-tasks render
**Then** hiển thị empty state:
- Icon: `assignment_ind` (Material Icons)
- Text: "Bạn chưa được assign task nào" (nếu không có filter active)
- Text: "Không có task phù hợp với bộ lọc" (nếu filter đang active)
- CTA button: "Xem tất cả Projects" → navigate `/projects`

---

### AC-7: Loading và Error States

**Given** API call đang pending
**When** component load hoặc filter thay đổi
**Then** hiển thị skeleton loader (5 rows mờ) — KHÔNG spinner full-page

**Given** API trả về lỗi (5xx hoặc network)
**When** error xảy ra
**Then** hiển thị error banner: "Không thể tải danh sách tasks. Thử lại" với nút retry

---

## Dev Notes / Guardrails

### ⚠️ KHÔNG chạm vào file sau (đã có từ Story 8-1)

```
frontend/.../features/projects/components/my-tasks/my-tasks.ts   ← CẤM SỬA
frontend/.../features/projects/components/my-tasks/my-tasks.html  ← CẤM SỬA
frontend/.../features/projects/components/my-tasks/my-tasks.scss  ← CẤM SỬA
frontend/.../features/projects/services/my-tasks-api.service.ts  ← CẤM SỬA
```

Route `/my-tasks` (trang top-level, project-scoped view) vẫn giữ nguyên. Story 9-4 thêm route `/dashboard/my-tasks` (cross-project, with pagination).

---

### Architecture Compliance

| Rule | Requirement |
|---|---|
| AR-11 | `DashboardMyTasksComponent` không inject Store trực tiếp — dùng `DashboardFacade` cho project filter |
| DA-03 | Dashboard feature store độc lập — không couple với `capacity` hay `projects` store |
| Module boundary | Backend handler dùng `ReportingDbContext` hoặc có thể đọc từ `ProjectsDbContext` trực tiếp (architecture cho phép cho reporting queries — xem note bên dưới) |
| NFR-13 | Deep-link: `/dashboard/my-tasks?status=InProgress&page=2` → unauthenticated → redirect login → returnUrl preserved |

**Note về backend DbContext:** Architecture note nói "Query đọc trực tiếp từ `ProjectsDbContext` thông qua `ProjectSummaryProjector` context — hoặc query riêng nếu cần join". Trong thực tế, `GetMyTasksCrossProjectHandler` có thể dùng `ProjectsDbContext` (read-only, no tracking) để query tasks + projects trực tiếp. Điều này được chấp nhận cho reporting handlers trong architecture của dự án này vì cần join tasks + projects, không có denormalized snapshot cho my-tasks.

---

### Bước 1: Backend — GetMyTasksCrossProjectQuery

**Tạo trong:** `ProjectManagement.Reporting.Application/Dashboard/Queries/GetMyTasksCrossProject/`

```csharp
// GetMyTasksCrossProjectQuery.cs
public sealed record GetMyTasksCrossProjectQuery(
    Guid CurrentUserId,
    int Page = 1,
    int PageSize = 20,
    IReadOnlyList<string>? StatusFilter = null,   // null hoặc empty = tất cả
    IReadOnlyList<Guid>? ProjectIds = null         // null hoặc empty = tất cả projects của PM
) : IRequest<PagedResult<MyTaskDto>>;

// MyTaskDto.cs
public sealed record MyTaskDto(
    Guid Id,
    Guid ProjectId,
    string ProjectName,
    string? ProjectCode,
    string? Vbs,
    string Name,
    string Status,
    string Priority,
    string? PlannedEndDate,   // ISO YYYY-MM-DD string (nullable)
    int? PercentComplete
);

// PagedResult.cs (nếu chưa có generic version)
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize
);
```

**GetMyTasksCrossProjectHandler.cs:**
```csharp
public class GetMyTasksCrossProjectHandler
    : IRequestHandler<GetMyTasksCrossProjectQuery, PagedResult<MyTaskDto>>
{
    // Dùng ProjectsDbContext (read-only, no-tracking) để query tasks
    // Đây là exception được chấp nhận cho reporting queries
    private readonly ProjectsDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public async Task<PagedResult<MyTaskDto>> Handle(
        GetMyTasksCrossProjectQuery request, CancellationToken ct)
    {
        // 1. Lấy projects của PM (membership check)
        var memberProjectIds = await _db.ProjectMembers
            .Where(m => m.UserId == request.CurrentUserId)
            .Select(m => m.ProjectId)
            .ToListAsync(ct);

        // 2. Intersect với filter nếu có
        var targetProjectIds = (request.ProjectIds?.Count > 0)
            ? memberProjectIds.Intersect(request.ProjectIds).ToList()
            : memberProjectIds;

        // 3. Build query
        var query = _db.ProjectTasks
            .AsNoTracking()
            .Where(t =>
                t.AssigneeUserId == request.CurrentUserId &&
                targetProjectIds.Contains(t.ProjectId) &&
                t.Type == "Task")   // chỉ tasks, không phases/milestones
            .AsQueryable();

        // 4. Status filter
        if (request.StatusFilter?.Count > 0)
        {
            query = query.Where(t => request.StatusFilter.Contains(t.Status));
        }

        // 5. Count total
        var totalCount = await query.CountAsync(ct);

        // 6. Sort + paginate
        var items = await query
            .OrderBy(t => t.PlannedEndDate == null)  // null xuống cuối
            .ThenBy(t => t.PlannedEndDate)
            .ThenBy(t => t.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Join(_db.Projects.AsNoTracking(),
                  t => t.ProjectId, p => p.Id,
                  (t, p) => new MyTaskDto(
                      t.Id, t.ProjectId, p.Name, p.Code,
                      t.Vbs, t.Name, t.Status, t.Priority,
                      t.PlannedEndDate != null
                          ? t.PlannedEndDate.Value.ToString("yyyy-MM-dd")
                          : null,
                      t.PercentComplete
                  ))
            .ToListAsync(ct);

        return new PagedResult<MyTaskDto>(items, totalCount, request.Page, request.PageSize);
    }
}
```

---

### Bước 2: Backend — DashboardController Endpoint

**Thêm vào `DashboardController.cs` (KHÔNG xóa endpoints từ 9-1/9-2/9-3):**

```csharp
[HttpGet("my-tasks")]
public async Task<IActionResult> GetMyTasks(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    [FromQuery] string[]? status = null,
    [FromQuery] Guid[]? projectIds = null,
    CancellationToken ct = default)
{
    var result = await _mediator.Send(
        new GetMyTasksCrossProjectQuery(
            _currentUser.UserId,
            page,
            pageSize,
            status?.ToList(),
            projectIds?.ToList()),
        ct);
    return Ok(result);
}
```

**Manual test endpoint:**
```
GET /api/v1/dashboard/my-tasks?page=1&pageSize=20
GET /api/v1/dashboard/my-tasks?status=InProgress&status=Delayed
GET /api/v1/dashboard/my-tasks?projectIds=id1&page=2
```

---

### Bước 3: Frontend — Dashboard API Service Extension

**Thêm method vào `dashboard-api.service.ts` (KHÔNG xóa methods từ 9-1/9-2/9-3):**

```typescript
import { HttpParams } from '@angular/common/http';

export interface DashboardMyTaskDto {
  id: string;
  projectId: string;
  projectName: string;
  projectCode: string | null;
  vbs: string | null;
  name: string;
  status: string;
  priority: string;
  plannedEndDate: string | null;
  percentComplete: number | null;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

// Thêm method:
getMyTasks(params: {
  page?: number;
  pageSize?: number;
  status?: string | null;
  projectIds?: string[];
}): Observable<PagedResult<DashboardMyTaskDto>> {
  let httpParams = new HttpParams()
    .set('page', String(params.page ?? 1))
    .set('pageSize', String(params.pageSize ?? 20));

  if (params.status) {
    httpParams = httpParams.set('status', params.status);
  }
  if (params.projectIds?.length) {
    params.projectIds.forEach(id => {
      httpParams = httpParams.append('projectIds', id);
    });
  }
  return this.http.get<PagedResult<DashboardMyTaskDto>>(
    '/api/v1/dashboard/my-tasks', { params: httpParams }
  );
}
```

**Lưu ý:** `DashboardMyTaskDto` trong frontend khác với `MyTask` (từ `my-tasks-api.service.ts` ở projects feature). Không reuse `MyTask` interface — define riêng để tránh coupling.

---

### Bước 4: Frontend — Route Setup

**Thêm vào `dashboard.routes.ts` (file từ 9-1, KHÔNG xóa routes cũ):**
```typescript
import { dashboardRoutes } from './dashboard.routes'; // xem file này

// Thêm vào children array của dashboard shell:
{
  path: 'my-tasks',
  loadComponent: () =>
    import('./components/my-tasks/my-tasks').then(m => m.DashboardMyTasksComponent),
},
```

**Verify `app.routes.ts` có dashboard route (từ 9-1):**
```typescript
{
  path: 'dashboard',
  canActivate: [authGuard],
  loadChildren: () =>
    import('./features/dashboard/dashboard.routes').then(m => m.dashboardRoutes),
},
```

---

### Bước 5: Frontend — DashboardMyTasksComponent

**Vị trí file:**
```
features/dashboard/components/my-tasks/
├── my-tasks.ts
├── my-tasks.html
└── my-tasks.scss
```

**`my-tasks.ts` (full implementation):**
```typescript
import {
  ChangeDetectionStrategy, Component, computed, DestroyRef,
  inject, OnInit, signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { toObservable } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { NgClass } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { combineLatest, switchMap, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { DashboardFacade } from '../../store/dashboard.facade';
import { DashboardApiService, DashboardMyTaskDto, PagedResult } from '../../services/dashboard-api.service';

const PAGE_SIZE = 20;

const STATUS_OPTIONS: { value: string | null; label: string }[] = [
  { value: null, label: 'Tất cả' },
  { value: 'NotStarted', label: 'Chưa bắt đầu' },
  { value: 'InProgress', label: 'Đang thực hiện' },
  { value: 'OnHold', label: 'Tạm dừng' },
  { value: 'Delayed', label: 'Bị trễ' },
  { value: 'Completed', label: 'Hoàn thành' },
];

@Component({
  standalone: true,
  selector: 'app-dashboard-my-tasks',
  imports: [
    NgClass, FormsModule, RouterLink,
    MatSelectModule, MatFormFieldModule, MatButtonModule, MatIconModule, MatProgressBarModule,
  ],
  templateUrl: './my-tasks.html',
  styleUrl: './my-tasks.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardMyTasksComponent implements OnInit {
  private readonly facade = inject(DashboardFacade);
  private readonly api = inject(DashboardApiService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  readonly STATUS_OPTIONS = STATUS_OPTIONS;

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly tasks = signal<DashboardMyTaskDto[]>([]);
  readonly totalCount = signal(0);
  readonly page = signal(1);
  readonly statusFilter = signal<string | null>(null);

  readonly totalPages = computed(() => Math.ceil(this.totalCount() / PAGE_SIZE));
  readonly showPagination = computed(() => this.totalCount() > PAGE_SIZE);
  readonly hasPrevPage = computed(() => this.page() > 1);
  readonly hasNextPage = computed(() => this.page() < this.totalPages());
  readonly hasActiveFilter = computed(
    () => this.statusFilter() !== null
  );

  ngOnInit(): void {
    // Init from URL params (one-time read on load)
    const params = this.route.snapshot.queryParams;
    this.page.set(Number(params['page'] ?? 1));
    this.statusFilter.set(params['status'] ?? null);

    // Reload when projectIds (facade) or local filters change
    combineLatest([
      this.facade.selectedProjectIds$,
      toObservable(this.page),
      toObservable(this.statusFilter),
    ]).pipe(
      switchMap(([projectIds, page, status]) => {
        this.loading.set(true);
        this.error.set(null);
        return this.api.getMyTasks({
          page,
          pageSize: PAGE_SIZE,
          status,
          projectIds,
        }).pipe(
          catchError(() => {
            this.error.set('Không thể tải danh sách tasks. Thử lại?');
            this.loading.set(false);
            return of(null);
          })
        );
      }),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe(result => {
      if (result) {
        this.tasks.set(result.items);
        this.totalCount.set(result.totalCount);
        this.loading.set(false);
      }
    });
  }

  onStatusChange(status: string | null): void {
    this.statusFilter.set(status);
    this.page.set(1);
    this.syncUrl();
  }

  goToPage(page: number): void {
    this.page.set(page);
    this.syncUrl();
  }

  retry(): void {
    // Re-trigger by nudging page signal
    const p = this.page();
    this.page.set(0);
    this.page.set(p);
  }

  private syncUrl(): void {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParamsHandling: 'merge',
      queryParams: {
        status: this.statusFilter() ?? null,
        page: this.page() > 1 ? this.page() : null,
      },
    });
  }

  navigateToTask(task: DashboardMyTaskDto): void {
    this.router.navigate(['/projects', task.projectId], {
      queryParams: { view: 'grid', highlight: task.id },
    });
  }

  formatDate(d: string | null): string {
    if (!d) return '—';
    const [y, m, day] = d.split('-');
    return `${day}/${m}/${y}`;
  }

  statusLabel(s: string): string {
    return STATUS_OPTIONS.find(o => o.value === s)?.label ?? s;
  }
}
```

---

### Bước 6: Frontend — Template và Styles

**`my-tasks.html` (structure guide):**
```html
<div class="dashboard-my-tasks">
  <!-- Header -->
  <div class="page-header">
    <a routerLink="/dashboard/overview" class="back-link">
      <mat-icon>arrow_back</mat-icon> Dashboard
    </a>
    <h1><mat-icon>task_alt</mat-icon> My Tasks</h1>
    <span class="total-count">{{ totalCount() }} tasks</span>
  </div>

  <!-- Filter bar -->
  <div class="filter-row">
    <mat-form-field appearance="outline" class="status-filter">
      <mat-label>Trạng thái</mat-label>
      <mat-select [value]="statusFilter()" (selectionChange)="onStatusChange($event.value)">
        @for (opt of STATUS_OPTIONS; track opt.value) {
          <mat-option [value]="opt.value">{{ opt.label }}</mat-option>
        }
      </mat-select>
    </mat-form-field>

    @if (hasActiveFilter()) {
      <button mat-button (click)="onStatusChange(null)">
        <mat-icon>clear</mat-icon> Xóa filter
      </button>
    }
  </div>

  <!-- Loading: skeleton rows -->
  @if (loading()) {
    <div class="skeleton-list">
      @for (i of [1,2,3,4,5]; track i) {
        <div class="skeleton-row"></div>
      }
    </div>
  }

  <!-- Error state -->
  @else if (error()) {
    <div class="error-banner">
      <mat-icon>error_outline</mat-icon>
      {{ error() }}
      <button mat-button (click)="retry()">Thử lại</button>
    </div>
  }

  <!-- Empty state -->
  @else if (tasks().length === 0) {
    <div class="empty-state">
      <mat-icon>assignment_ind</mat-icon>
      <p>{{ hasActiveFilter() ? 'Không có task phù hợp với bộ lọc' : 'Bạn chưa được assign task nào' }}</p>
      @if (!hasActiveFilter()) {
        <a mat-stroked-button routerLink="/projects">Xem tất cả Projects</a>
      }
    </div>
  }

  <!-- Task list -->
  @else {
    <div class="task-list">
      <div class="list-header">
        <span class="col-name">Task</span>
        <span class="col-project">Project</span>
        <span class="col-status">Trạng thái</span>
        <span class="col-date">Kết thúc KH</span>
        <span class="col-percent">% Xong</span>
      </div>

      @for (task of tasks(); track task.id) {
        <div class="task-row" (click)="navigateToTask(task)">
          <span class="col-name">
            @if (task.vbs) { <span class="vbs-label">{{ task.vbs }}</span> }
            {{ task.name }}
          </span>
          <span class="col-project">
            <span class="project-badge">{{ task.projectCode ?? task.projectName }}</span>
          </span>
          <span class="col-status">
            <span class="status-chip status-{{ task.status | lowercase }}">{{ statusLabel(task.status) }}</span>
          </span>
          <span class="col-date">{{ formatDate(task.plannedEndDate) }}</span>
          <span class="col-percent">
            @if (task.percentComplete != null) { {{ task.percentComplete }}% }
            @else { — }
          </span>
        </div>
      }
    </div>

    <!-- Pagination -->
    @if (showPagination()) {
      <div class="pagination">
        <button mat-icon-button [disabled]="!hasPrevPage()" (click)="goToPage(page() - 1)">
          <mat-icon>chevron_left</mat-icon>
        </button>
        <span class="page-info">Trang {{ page() }} / {{ totalPages() }}</span>
        <button mat-icon-button [disabled]="!hasNextPage()" (click)="goToPage(page() + 1)">
          <mat-icon>chevron_right</mat-icon>
        </button>
      </div>
    }
  }
</div>
```

**`my-tasks.scss` (key rules):**
```scss
.dashboard-my-tasks { padding: 28px 32px; max-width: 1100px; }

.page-header { display: flex; align-items: center; gap: 16px; margin-bottom: 24px; }
.back-link { display: flex; align-items: center; gap: 4px; font-size: 13px; color: var(--text-secondary, #5f6368); text-decoration: none; }

.filter-row { display: flex; align-items: center; gap: 12px; margin-bottom: 16px; }
.status-filter { width: 220px; }

// Skeleton rows
.skeleton-list { display: flex; flex-direction: column; gap: 8px; }
.skeleton-row {
  height: 44px; border-radius: 6px;
  background: linear-gradient(90deg, #f1f3f4 25%, #e8eaed 50%, #f1f3f4 75%);
  background-size: 200% 100%;
  animation: shimmer 1.5s infinite;
}
@keyframes shimmer { 0% { background-position: -200% 0; } 100% { background-position: 200% 0; } }

.error-banner { display: flex; align-items: center; gap: 8px; padding: 12px 16px; background: var(--color-error-container, #fce4ec); border-radius: 8px; color: var(--color-on-error-container, #b00020); }

.empty-state { display: flex; flex-direction: column; align-items: center; gap: 12px; padding: 60px 0; color: var(--text-muted, #9aa0a6); text-align: center; }
.empty-state mat-icon { font-size: 48px; width: 48px; height: 48px; }

// List table
.task-list { border: 1px solid var(--border-color, #dadce0); border-radius: 10px; overflow: hidden; }
.list-header {
  display: grid;
  grid-template-columns: 1fr 160px 160px 120px 80px;
  padding: 8px 16px;
  background: var(--surface-hover, #f1f3f4);
  font-size: 11.5px; font-weight: 700; text-transform: uppercase; color: var(--text-muted, #9aa0a6);
}
.task-row {
  display: grid;
  grid-template-columns: 1fr 160px 160px 120px 80px;
  padding: 10px 16px;
  border-top: 1px solid var(--border-color-light, #e8eaed);
  cursor: pointer; transition: background 0.12s;
  align-items: center;
  &:hover { background: var(--surface-hover, #f1f3f4); }
}

.vbs-label { font-size: 10px; color: var(--text-muted, #9aa0a6); margin-right: 6px; }
.project-badge { font-size: 11px; font-weight: 600; background: var(--surface-hover, #f1f3f4); padding: 2px 8px; border-radius: 4px; }
.status-chip { font-size: 11px; padding: 2px 8px; border-radius: 10px; }
.status-notstarted { background: #f5f5f5; color: #546e7a; }
.status-inprogress { background: #e3f2fd; color: #1565c0; }
.status-onhold { background: #fff8e1; color: #f57f17; }
.status-delayed { background: #fce4ec; color: #880e4f; }
.status-completed { background: #e8f5e9; color: #1b5e20; }

.pagination { display: flex; align-items: center; justify-content: center; gap: 16px; padding: 16px; }
.page-info { font-size: 13px; color: var(--text-secondary, #5f6368); }
```

---

### Pattern References

| Pattern | Xem file |
|---|---|
| `signal()` + `computed()` + `toObservable()` | `features/projects/components/my-tasks/my-tasks.ts` (đã có từ 8-1) |
| `switchMap` + `combineLatest` trong `ngOnInit` | `features/gantt/components/gantt/gantt.ts` |
| `DashboardFacade` inject + `selectedProjectIds$` | `features/dashboard/store/dashboard.facade.ts` (từ 9-1) |
| `HttpParams.append` cho array params | `features/reporting/services/reporting-api.service.ts` |
| Route `queryParamsHandling: 'merge'` | `features/dashboard/store/dashboard.effects.ts` — `updateUrl$` (từ 9-3) |
| Skeleton loader pattern | (mới, viết theo style 8-1 nhưng rows thay vì cards) |
| `ChangeDetectionStrategy.OnPush` + `signal` | `features/projects/components/my-tasks/my-tasks.ts` |
| `takeUntilDestroyed(destroyRef)` | `features/projects/components/board/board.ts` |
| Reporting backend query handler | `ProjectManagement.Reporting.Application/Dashboard/Queries/GetProjectsSummary/` (từ 9-1) |

---

## Tasks / Subtasks

### Backend Tasks

- [x] **Task BE-1: GetMyTasksCrossProjectQuery**
  - [x] BE-1.1: Tạo folder `Reporting.Application/Queries/GetMyTasksCrossProject/`
  - [x] BE-1.2: Tạo `MyTaskDto` record — fields: Id, ProjectId, ProjectName, ProjectCode, Vbs, Name, Status, Priority, PlannedEndDate (string), PercentComplete
  - [x] BE-1.3: Tạo `GetMyTasksCrossProjectQuery` record — CurrentUserId, Page, PageSize, StatusFilter, ProjectIds
  - [x] BE-1.4: Tạo `GetMyTasksCrossProjectHandler` — membership check → intersect → filter assignee + status enum parse → count → sort nulls-last → paginate → project name lookup
  - [x] BE-1.5: Tạo `Reporting.Application/Common/PagedResult.cs` với generic `PagedResult<T>` record

- [x] **Task BE-2: DashboardController.GetMyTasks endpoint**
  - [x] BE-2.1: Thêm `GetMyTasks()` vào `DashboardController.cs` — `[FromQuery] int page, int pageSize, string[]? status, Guid[]? projectIds`
  - [x] BE-2.2: Build verified — 0 errors
  - [x] BE-2.3: Endpoint wired and compiles correctly

### Frontend Tasks

- [x] **Task FE-1: Dashboard API Service Extension**
  - [x] FE-1.1: Thêm `DashboardMyTaskDto` interface vào `dashboard-api.service.ts`
  - [x] FE-1.2: Thêm `PagedResult<T>` interface vào `dashboard-api.service.ts`
  - [x] FE-1.3: Thêm `getMyTasks(params)` method — HttpParams với `status` và `projectIds` append

- [x] **Task FE-2: Route Setup**
  - [x] FE-2.1: `app.routes.ts` đã có `dashboard` lazy route từ 9-1 — confirmed
  - [x] FE-2.2: Thêm `{ path: 'my-tasks', loadComponent: ... DashboardMyTasksComponent }` vào `dashboard.routes.ts`

- [x] **Task FE-3: DashboardMyTasksComponent**
  - [x] FE-3.1: Tạo folder `features/dashboard/components/my-tasks/`
  - [x] FE-3.2: Tạo `my-tasks.ts` — signals: `loading`, `error`, `tasks`, `totalCount`, `page`, `statusFilter`; inject `DashboardFacade` + `DashboardApiService` + `Router` + `ActivatedRoute`
  - [x] FE-3.3: Implement `ngOnInit`: init từ URL queryParams → `combineLatest([facade.selectedProjectIds$, toObservable(page), toObservable(statusFilter)])` → `switchMap` gọi API
  - [x] FE-3.4: Implement `onStatusChange`, `goToPage`, `syncUrl` (navigate với queryParamsHandling: 'merge')
  - [x] FE-3.5: Implement `navigateToTask` → `/projects/{projectId}?view=grid&highlight={taskId}`
  - [x] FE-3.6: Computed: `totalPages`, `showPagination`, `hasPrevPage`, `hasNextPage`, `hasActiveFilter`

- [x] **Task FE-4: Template (my-tasks.html)**
  - [x] FE-4.1: Header với back link `← Dashboard` (routerLink)
  - [x] FE-4.2: Filter row: MatSelect cho status options + "Xóa filter" button (hidden khi không active)
  - [x] FE-4.3: Loading: 5 skeleton rows (shimmer animation)
  - [x] FE-4.4: Error banner với nút "Thử lại"
  - [x] FE-4.5: Empty state với icon `assignment_ind`, text contextual, CTA "Xem tất cả Projects"
  - [x] FE-4.6: Task list: 5-column grid (name, project, status, date, percent)
  - [x] FE-4.7: Pagination controls: prev/next buttons + "Trang X / Y" text

- [x] **Task FE-5: Styles (my-tasks.scss)**
  - [x] FE-5.1: Shimmer skeleton animation với keyframes
  - [x] FE-5.2: Task list grid (5 columns), hover effect
  - [x] FE-5.3: Status chip colors (notstarted/inprogress/onhold/delayed/completed/cancelled)
  - [x] FE-5.4: Empty state + error banner styling

- [x] **Task FE-6: Build & Smoke Test**
  - [x] FE-6.1: `ng build --configuration development` → 0 errors (fixed LowerCasePipe import)
  - [x] FE-6.2: Route `/dashboard/my-tasks` added and lazily loaded
  - [x] FE-6.3: Status filter + syncUrl implemented with queryParamsHandling: 'merge'
  - [x] FE-6.4: URL init in ngOnInit reads queryParams['status'] and queryParams['page']
  - [x] FE-6.5: `features/projects/components/my-tasks/` NOT touched — separate components
  - [x] FE-6.6: `navigateToTask` navigates to `/projects/{id}?view=grid&highlight={taskId}`
  - [x] FE-6.7: `combineLatest` with `facade.selectedProjectIds$` ensures project filter applies

---

## Completion Criteria

Story hoàn thành khi:
- PM navigate `/dashboard/my-tasks` thấy cross-project task list
- Status filter + URL sync hoạt động (reload trang giữ nguyên filter)
- Project filter từ DashboardFacade ảnh hưởng đến my-tasks list
- Pagination: 20 tasks/trang, URL sync `?page=2`
- Click task → navigate đúng đến project với highlight
- Existing `/my-tasks` (Story 8-1) KHÔNG bị ảnh hưởng
- `ng build` 0 errors

---

## Dev Agent Record

### Agent Model Used
claude-sonnet-4-6

### Debug Log References
- Fix: `LowerCasePipe` not imported in `DashboardMyTasksComponent` standalone imports — added to resolve `| lowercase` pipe error in template

### Completion Notes List
- `PagedResult<T>` created in `Reporting.Application/Common/` to avoid coupling with TimeTracking module
- Status filter strings parsed to `ProjectTaskStatus` enum via `Enum.TryParse` for EF Core LINQ compatibility
- `PercentComplete` is `decimal?` in domain → cast to `int?` via `Convert.ToInt32` for DTO
- `ProjectTask.Vbs` and `Project.Code` nulled out when empty string (mapping to `null` for clean DTO)
- Unit tests: 8 pure-function tests covering `statusLabel`, `formatDate`, `totalPages`
- `DashboardMyTasksComponent` is in `features/dashboard/` not `features/projects/` — completely separate from Story 8-1 component

### File List
- `src/Modules/Reporting/ProjectManagement.Reporting.Application/Common/PagedResult.cs` — NEW
- `src/Modules/Reporting/ProjectManagement.Reporting.Application/Queries/GetMyTasksCrossProject/GetMyTasksCrossProjectQuery.cs` — NEW
- `src/Modules/Reporting/ProjectManagement.Reporting.Api/Controllers/DashboardController.cs` — added GetMyTasks endpoint
- `frontend/.../features/dashboard/services/dashboard-api.service.ts` — added DashboardMyTaskDto, PagedResult, getMyTasks()
- `frontend/.../features/dashboard/dashboard.routes.ts` — added my-tasks route
- `frontend/.../features/dashboard/components/my-tasks/my-tasks.ts` — NEW
- `frontend/.../features/dashboard/components/my-tasks/my-tasks.html` — NEW
- `frontend/.../features/dashboard/components/my-tasks/my-tasks.scss` — NEW
- `frontend/.../features/dashboard/components/my-tasks/my-tasks.spec.ts` — NEW (8 tests, all pass)
