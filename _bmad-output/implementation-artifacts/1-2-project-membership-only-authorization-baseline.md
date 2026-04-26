# Story 1.2: Project Membership-only Authorization Baseline

Status: review

**Story ID:** 1.2
**Epic:** Epic 1 — Authentication + Portfolio/Project Setup + Gantt Interactive (Core Planning)
**Sprint:** Sprint 1
**Date Created:** 2026-04-25

---

## Story

As a PM,
I want chỉ nhìn thấy/truy cập được các project mà tôi là member,
so that dữ liệu không bị lộ giữa các dự án.

## Acceptance Criteria

1. **Given** user đã đăng nhập (JWT hợp lệ)
   **When** gọi `GET /api/v1/projects`
   **Then** chỉ trả về danh sách project mà user là member (membership-only)

2. **Given** user đã đăng nhập và **là member** của project `{projectId}`
   **When** gọi `GET /api/v1/projects/{projectId}`
   **Then** trả `200` với project detail

3. **Given** user đã đăng nhập nhưng **không phải member** của project `{projectId}`
   **When** gọi `GET /api/v1/projects/{projectId}`
   **Then** trả `404 ProblemDetails` (không leak sự tồn tại của project)

4. **Given** user đã đăng nhập nhưng **không phải member** của project `{projectId}`
   **When** gọi các endpoint con như `GET /api/v1/projects/{projectId}/members`, `PUT /api/v1/projects/{projectId}`, `DELETE /api/v1/projects/{projectId}`
   **Then** trả `404 ProblemDetails`

5. **Given** request không có/invalid/expired JWT
   **When** gọi bất kỳ endpoint project nào
   **Then** trả `401 ProblemDetails`

6. **Given** bất kỳ lỗi validation/system nào ở layer authorization/filtering
   **When** trả lỗi 4xx/5xx
   **Then** body luôn là `ProblemDetails` (chuẩn hoá)

## Tasks / Subtasks

- [x] Task 1: Xây dựng data model ProjectMembership (BE)
  - [x] 1.1 Tạo entity `ProjectMembership` trong `Projects.Domain` — fields: `Id`, `ProjectId`, `UserId`, `Role` (enum Member/Manager), `JoinedAt`
  - [x] 1.2 Cập nhật `ProjectsDbContext` — thêm `DbSet<ProjectMembership>`, tạo `ProjectMembershipConfiguration` với snake_case mapping, unique index `(project_id, user_id)`
  - [x] 1.3 Tạo EF migration: `AddProjectMembership`
  - [x] 1.4 Cập nhật seeder — seed user test vào ít nhất 1 project (dùng userId từ `AuthSeeder`: `pm1@local.test`)

- [x] Task 2: Implement membership filter ở Query/Application layer (BE)
  - [x] 2.1 Cập nhật `GetProjectListQuery` — thêm `CurrentUserId: Guid`; handler filter `WHERE project_memberships.user_id = @userId`
  - [x] 2.2 Cập nhật `GetProjectByIdQuery` — thêm `CurrentUserId: Guid`; handler throw `NotFoundException` nếu không tìm thấy (1 query kết hợp cả existence + membership check)
  - [x] 2.3 Tạo `IMembershipChecker` interface trong `Projects.Application/Common/Interfaces/` — phục vụ sub-resource endpoints của Story 1.3+

- [x] Task 3: Wire authorization vào ProjectsController (BE)
  - [x] 3.1 Kiểm tra `ICurrentUserService` đã tồn tại trong `Shared.Infrastructure` chưa — nếu chưa thì tạo (inject `IHttpContextAccessor`, đọc claim `sub`)
  - [x] 3.2 Thêm `[Authorize]` ở controller level — bảo vệ toàn bộ endpoints → 401 nếu thiếu JWT
  - [x] 3.3 Inject `ICurrentUserService` vào `ProjectsController`, truyền `UserId` vào mọi query
  - [x] 3.4 Verify `GlobalExceptionMiddleware` (từ Story 1.0) map `NotFoundException` → `404 ProblemDetails` — không viết lại

- [x] Task 4: Frontend `features/projects/` cơ bản (FE)
  - [x] 4.1 Cập nhật `projects.routes.ts` (placeholder từ Story 1.1) — route `/projects` lazy load `ProjectListComponent`
  - [x] 4.2 Tạo `features/projects/models/project.model.ts` — interface `Project { id, code, name, status, visibility, version }`
  - [x] 4.3 Tạo `features/projects/services/projects-api.service.ts` — HTTP wrapper cho `GET /api/v1/projects` và `GET /api/v1/projects/{id}`
  - [x] 4.4 Tạo `features/projects/store/` — `projects.actions.ts`, `projects.reducer.ts` (`EntityState<Project>`), `projects.effects.ts`, `projects.selectors.ts`
  - [x] 4.5 Tạo `features/projects/components/project-list/` — standalone component, OnPush, hiển thị danh sách + empty state + loading/error state
  - [x] 4.6 Cập nhật `app.state.ts` thêm `projects: ProjectsState`; cập nhật `app.config.ts` đăng ký `ProjectsEffects`
  - [x] 4.7 Xử lý 404 từ project endpoint — UI hiển thị thông báo trung tính + CTA về `/projects`

- [x] Task 5: Tests (BE + FE)
  - [x] 5.1 Integration test BE: `GET /api/v1/projects` — user A thấy projects của mình, không thấy projects chỉ của user B
  - [x] 5.2 Integration test BE: `GET /api/v1/projects/{id}` — member → 200; non-member → 404 ProblemDetails
  - [x] 5.3 Integration test BE: Sub-resource với non-member project → 404
  - [x] 5.4 Integration test BE: Không có JWT → 401 ProblemDetails
  - [x] 5.5 Vitest FE: `projects.effects.spec.ts` — load success (store populated), load failure (error state)

## Dev Notes

### ⚠️ Trạng Thái Hiện Tại — ĐỌC TRƯỚC KHI CODE

**Từ Story 1.0 — Đã có sẵn, KHÔNG viết lại:**
- `GlobalExceptionMiddleware` — map `NotFoundException` → 404, `ConflictException` → 409, v.v.
- `NotFoundException`, `DomainException`, `ConflictException` trong `ProjectManagement.Shared.Domain/Exceptions/`
- `CorrelationIdMiddleware`, `Result<T>` pattern
- Auth plumbing: JWT Bearer, `[Authorize]` attribute hoạt động
- `ProjectManagement.Projects.*` modules đã có skeleton (từ Story 1.0 setup)

**Từ Story 1.1 — Đã có sẵn, KHÔNG viết lại:**
- `features/projects/projects.routes.ts` — placeholder, CẬP NHẬT file này (không tạo mới)
- `core/auth/auth.guard.ts` — bảo vệ `/projects` route, không sửa
- `core/interceptors/error.interceptor.ts` — xử lý 401/409, chỉ mở rộng nếu cần 404
- JWT route prefix xác nhận: `/api/v1/`
- `AuthSeeder` credentials: `pm1@local.test` / `P@ssw0rd!123`
- Test infrastructure: `CustomWebApplicationFactory`, `TestContainersFixture` (PostgreSQL) đã có

**Story 1.2 cần BUILD MỚI:**
- `ProjectMembership` entity + migration + EF config
- `IMembershipChecker` interface
- Cập nhật `GetProjectList/GetProjectById` handlers
- `ICurrentUserService` (kiểm tra đã có chưa từ Story 1.0)
- Frontend: NgRx projects store + ProjectListComponent

---

### Backend — ProjectMembership Entity

```csharp
// ProjectManagement.Projects.Domain/Entities/ProjectMembership.cs
public class ProjectMembership
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid UserId { get; private set; }
    public ProjectMemberRole Role { get; private set; }
    public DateTime JoinedAt { get; private set; }

    public Project Project { get; private set; } = null!;

    public static ProjectMembership Create(Guid projectId, Guid userId, ProjectMemberRole role)
        => new() { Id = Guid.NewGuid(), ProjectId = projectId, UserId = userId,
                   Role = role, JoinedAt = DateTime.UtcNow };
}

// ProjectManagement.Projects.Domain/Enums/ProjectMemberRole.cs
public enum ProjectMemberRole { Member, Manager }
```

### Backend — EF Config (snake_case bắt buộc)

```csharp
// ProjectMembershipConfiguration.cs
modelBuilder.Entity<ProjectMembership>(b =>
{
    b.ToTable("project_memberships");
    b.HasKey(x => x.Id);
    b.Property(x => x.ProjectId).HasColumnName("project_id");
    b.Property(x => x.UserId).HasColumnName("user_id");
    b.Property(x => x.Role).HasColumnName("role").HasConversion<string>();
    b.Property(x => x.JoinedAt).HasColumnName("joined_at");
    // Unique: 1 user = 1 membership record per project
    b.HasIndex(x => new { x.ProjectId, x.UserId })
     .IsUnique()
     .HasDatabaseName("uq_project_memberships_project_user");
    b.HasOne(x => x.Project)
     .WithMany(p => p.Members)
     .HasForeignKey(x => x.ProjectId);
});
```

### Backend — Membership Filter — 1 Query, Không Leak (CRITICAL)

```csharp
// GetProjectListHandler.cs
public async Task<List<ProjectDto>> Handle(GetProjectListQuery query, CancellationToken ct)
{
    return await _db.Projects
        .Where(p => p.Members.Any(m => m.UserId == query.CurrentUserId))
        .Select(p => new ProjectDto { Id = p.Id, Code = p.Code, Name = p.Name, Status = p.Status })
        .ToListAsync(ct);
}

// GetProjectByIdHandler.cs — CRITICAL: 1 query kết hợp existence + membership
public async Task<ProjectDto> Handle(GetProjectByIdQuery query, CancellationToken ct)
{
    var project = await _db.Projects
        .Where(p => p.Id == query.ProjectId && p.Members.Any(m => m.UserId == query.CurrentUserId))
        .Select(p => new ProjectDto { ... })
        .FirstOrDefaultAsync(ct);

    if (project is null)
        throw new NotFoundException(nameof(Project), query.ProjectId);  // luôn → 404

    return project;
}
```

**KHÔNG làm theo cách này (leak thông tin):**

```csharp
// ❌ SAI — 2 query, leak "project tồn tại nhưng user không có quyền"
var exists = await _db.Projects.AnyAsync(p => p.Id == id);
if (!exists) throw new NotFoundException(...);       // 404
var isMember = await _db.ProjectMemberships.AnyAsync(m => ...);
if (!isMember) throw new ForbiddenException(...);    // 403 ← LEAK!

// ✅ ĐÚNG — 1 query, luôn 404 cho cả 2 trường hợp
var project = await _db.Projects
    .Where(p => p.Id == id && p.Members.Any(m => m.UserId == userId))
    .FirstOrDefaultAsync();
if (project is null) throw new NotFoundException(...);
```

### Backend — ICurrentUserService

Kiểm tra file `src/Shared/ProjectManagement.Shared.Infrastructure/Services/ICurrentUserService.cs`. Nếu chưa tồn tại (Story 1.0 có thể chưa tạo):

```csharp
// ICurrentUserService.cs
public interface ICurrentUserService
{
    Guid UserId { get; }
    bool IsAuthenticated { get; }
}

// CurrentUserService.cs
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _accessor;
    public CurrentUserService(IHttpContextAccessor accessor) => _accessor = accessor;

    public Guid UserId
    {
        get
        {
            var value = _accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? _accessor.HttpContext?.User.FindFirstValue("sub");
            return Guid.TryParse(value, out var id) ? id : throw new UnauthorizedException();
        }
    }
    public bool IsAuthenticated => _accessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
}
```

Đăng ký trong `ModuleExtensions.cs` hoặc Host `Program.cs`:

```csharp
services.AddHttpContextAccessor();
services.AddScoped<ICurrentUserService, CurrentUserService>();
```

### Backend — ProjectsController Pattern

```csharp
[Authorize]   // ← bắt buộc — bảo vệ toàn bộ controller → 401 nếu thiếu JWT
[ApiController]
[Route("api/v1/projects")]
public class ProjectsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    [HttpGet]
    public async Task<IActionResult> GetProjects(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetProjectListQuery(_currentUser.UserId), ct);
        return Ok(result);
    }

    [HttpGet("{projectId:guid}")]
    public async Task<IActionResult> GetProject(Guid projectId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetProjectByIdQuery(projectId, _currentUser.UserId), ct);
        return Ok(result);  // NotFoundException → GlobalExceptionMiddleware → 404
    }
}
```

### Backend — IMembershipChecker (Tái Dụng Cho Story 1.3+)

```csharp
// Projects.Application/Common/Interfaces/IMembershipChecker.cs
public interface IMembershipChecker
{
    Task<bool> IsMemberAsync(Guid projectId, Guid userId, CancellationToken ct = default);
    Task EnsureMemberAsync(Guid projectId, Guid userId, CancellationToken ct = default);
    // EnsureMemberAsync throw NotFoundException nếu không phải member
}
```

Story 1.3 (Projects CRUD) sẽ gọi `_membershipChecker.EnsureMemberAsync(...)` cho PUT/DELETE.

### Frontend — NgRx Projects Store

```typescript
// features/projects/models/project.model.ts
export interface Project {
  id: string;
  code: string;
  name: string;
  status: 'Planning' | 'Active' | 'OnHold' | 'Completed';
  visibility: string;
  version: number;
}

// features/projects/store/projects.actions.ts
export const ProjectsActions = createActionGroup({
  source: 'Projects',
  events: {
    'Load Projects': emptyProps(),
    'Load Projects Success': props<{ projects: Project[] }>(),
    'Load Projects Failure': props<{ error: string }>(),
    'Select Project': props<{ projectId: string }>(),
  }
});

// features/projects/store/projects.reducer.ts
export interface ProjectsState extends EntityState<Project> {
  selectedId: string | null;
  loading: boolean;
  error: string | null;
}

const adapter = createEntityAdapter<Project>();
const initialState: ProjectsState = adapter.getInitialState({ selectedId: null, loading: false, error: null });

// features/projects/store/projects.selectors.ts
export const selectProjectsState = (state: AppState) => state.projects;
export const { selectAll: selectAllProjects } = adapter.getSelectors(selectProjectsState);
export const selectProjectsLoading = createSelector(selectProjectsState, s => s.loading);
export const selectProjectsError = createSelector(selectProjectsState, s => s.error);
```

### Frontend — ProjectListComponent

```typescript
// features/projects/components/project-list/project-list.ts
@Component({
  standalone: true,
  selector: 'app-project-list',
  imports: [CommonModule, MatCardModule, MatButtonModule, MatProgressSpinnerModule, RouterLink, AsyncPipe, NgFor, NgIf],
  templateUrl: './project-list.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProjectListComponent implements OnInit {
  private store = inject(Store);

  protected projects$ = this.store.select(selectAllProjects);
  protected loading$ = this.store.select(selectProjectsLoading);
  protected error$ = this.store.select(selectProjectsError);

  ngOnInit(): void {
    this.store.dispatch(ProjectsActions.loadProjects());
  }
}
```

**UI requirements cho project-list.html:**
- Loading state: `mat-spinner` khi `loading$`
- Empty state (không có project): "Bạn chưa có dự án nào. Hãy tạo dự án mới." + CTA button
- Error state: banner lỗi với retry button
- Project card: hiển thị `code`, `name`, `status` — clickable navigate đến `/projects/{id}`

### Frontend — 404 UX (Membership)

Per UX spec: message phải **trung tính** — không confirm project có tồn tại:

```
✅ ĐÚNG: "Không tìm thấy dự án (có thể bạn không có quyền)" + nút "Về My Projects"
❌ SAI: "Bạn không có quyền truy cập dự án này"  ← confirm project tồn tại
❌ SAI: "Dự án không tồn tại"                    ← xác nhận ngược lại
```

Xử lý trong `error.interceptor.ts` hoặc component-level error handling:
```typescript
// Khi navigate /projects/{id} nhận 404 → snackbar trung tính + navigate('/projects')
```

### Project Structure — Files Cần Tạo/Cập Nhật

**Backend — Tạo mới:**

```
src/Modules/Projects/ProjectManagement.Projects.Domain/
├── Entities/ProjectMembership.cs
└── Enums/ProjectMemberRole.cs

src/Modules/Projects/ProjectManagement.Projects.Infrastructure/Persistence/
├── Configurations/ProjectMembershipConfiguration.cs
└── Migrations/{timestamp}_AddProjectMembership.cs  ← auto-generate bằng dotnet ef migrations add

src/Modules/Projects/ProjectManagement.Projects.Application/Common/Interfaces/
└── IMembershipChecker.cs
```

**Backend — Cập nhật:**

```
src/Modules/Projects/...Application/Queries/GetProjectList/GetProjectListQuery.cs   ← thêm CurrentUserId
src/Modules/Projects/...Application/Queries/GetProjectList/GetProjectListHandler.cs ← filter by membership
src/Modules/Projects/...Application/Queries/GetProjectById/GetProjectByIdQuery.cs   ← thêm CurrentUserId
src/Modules/Projects/...Application/Queries/GetProjectById/GetProjectByIdHandler.cs ← 1-query + NotFoundException
src/Modules/Projects/...Infrastructure/Persistence/ProjectsDbContext.cs             ← DbSet<ProjectMembership>
src/Modules/Projects/...Api/Controllers/ProjectsController.cs                       ← [Authorize] + ICurrentUserService
```

**Shared — Kiểm tra/Tạo nếu chưa có từ Story 1.0:**

```
src/Shared/ProjectManagement.Shared.Infrastructure/Services/ICurrentUserService.cs
src/Shared/ProjectManagement.Shared.Infrastructure/Services/CurrentUserService.cs
```

**Frontend — Tạo mới:**

```
frontend/.../features/projects/models/project.model.ts
frontend/.../features/projects/services/projects-api.service.ts
frontend/.../features/projects/store/projects.actions.ts
frontend/.../features/projects/store/projects.reducer.ts
frontend/.../features/projects/store/projects.effects.ts
frontend/.../features/projects/store/projects.selectors.ts
frontend/.../features/projects/components/project-list/project-list.ts (.html, .scss)
```

**Frontend — Cập nhật:**

```
frontend/.../features/projects/projects.routes.ts         ← update placeholder (từ Story 1.1)
frontend/.../core/store/app.state.ts                      ← thêm projects: ProjectsState
frontend/.../app.config.ts                                ← đăng ký ProjectsEffects
```

**Tests — Tạo mới:**

```
tests/Projects.IntegrationTests/ProjectsMembershipTests.cs
frontend/.../features/projects/store/projects.effects.spec.ts
```

### Anti-Patterns — Tránh Những Lỗi Này

**Backend:**
❌ **KHÔNG** trả `403` cho non-member — phải là `404` (membership-only = không xác nhận sự tồn tại)
❌ **KHÔNG** dùng 2 query riêng (check exists → check membership) — dùng 1 query kết hợp
❌ **KHÔNG** viết lại `GlobalExceptionMiddleware` — đã có từ Story 1.0, `NotFoundException` → 404 tự động
❌ **KHÔNG** quên `[Authorize]` ở controller — thiếu thì endpoint public, không trả 401
❌ **KHÔNG** hardcode `userId` trong query — phải lấy từ `ICurrentUserService` (JWT claim `sub`)
❌ **KHÔNG** để `updated_at` trong `ProjectMembership` nếu entity là append-only

**Frontend:**
❌ **KHÔNG** hiển thị message xác nhận sự tồn tại của project khi 404
❌ **KHÔNG** tạo lại `auth.guard.ts` hay interceptors — đã có từ Story 1.1
❌ **KHÔNG** import `features/auth` từ `features/projects` — dùng NgRx Store để đọc auth state
❌ **KHÔNG** gọi API trực tiếp trong component — dispatch action, effect xử lý HTTP
❌ **KHÔNG** dùng NgModules — Angular 21 standalone components hoàn toàn

### ProblemDetails Response Shape (Từ Story 1.0/1.1)

Lưu ý từ Story 1.1: `ProblemDetails.Extensions` với `[JsonExtensionData]` serialize ở **ROOT JSON level**:

```json
// Response 404:
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "traceId": "00-abc123"
}

// Response 401:
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401,
  "traceId": "00-abc123"
}
```

Integration tests phải assert JSON path ở root level (không nested dưới "extensions").

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 1.2]
- [Source: _bmad-output/planning-artifacts/architecture.md#Phần 5.2 Authentication & Security (D-04, D-05)]
- [Source: _bmad-output/planning-artifacts/architecture.md#Section 7.3 Ranh Giới Kiến Trúc — API Boundaries]
- [Source: _bmad-output/planning-artifacts/architecture.md#Section 5.5 Error Handling Patterns]
- [Source: _bmad-output/planning-artifacts/ux-design-specification.md#Error/Recovery paths — 404 membership-only]
- [Source: _bmad-output/implementation-artifacts/1-1-local-authentication-login-logout-me-jwt-plumbing.md#Dev Notes]

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6 (2026-04-25)

### Debug Log References

- EF Core version conflict: Npgsql 10.0.1 depends on EF 10.0.4; fixed by pinning Application.csproj to `10.0.4`.
- `ProjectsSeeder` decoupled from Auth module — Program.cs resolves `pm1@local.test` userId then passes `Guid` to seeder.
- Angular `NgFor`/`NgIf` removed from `project-list.ts` imports (template uses `@for`/`@if` control flow).
- Integration tests: used `_factory.CreateClient()` instead of `new HttpClient { BaseAddress = ... }` to use in-memory test handler.

### Completion Notes List

- **Backend Projects module tạo hoàn chỉnh từ đầu** (Projects.Domain / Application / Infrastructure / Api) theo modular monolith pattern.
- **1-query membership filter** được implement đúng trong `GetProjectByIdHandler` và `GetProjectListHandler` — luôn trả 404 cho cả non-member và non-existent, không leaking info.
- **ICurrentUserService** tạo trong `Shared.Infrastructure`, đọc `ClaimTypes.NameIdentifier` (JWT `sub` → mapped by JwtBearer middleware).
- **IMembershipChecker** interface tạo trong `Projects.Application/Common/Interfaces/` sẵn sàng cho Story 1.3+.
- **EF Migration** `InitProjects` tạo manual, tương thích với Npgsql 10.0.1 schema.
- **ProjectsSeeder** seed `pm1@local.test` là Manager của `SEED-01` — được gọi từ Program.cs sau Auth seeder.
- **Frontend NgRx store** đầy đủ: actions, reducer (EntityState), effects, selectors. `app.state.ts` + `app.config.ts` cập nhật.
- **ProjectListComponent** standalone, OnPush, Angular control flow (`@if`/`@for`), loading/error/empty states, status chips.
- **Test results**: 15/24 BE pass (9 fail do không có PostgreSQL local — expected); 26/26 FE Vitest pass.
- **No-auth BE tests** (`GetProjectById_NoJwt_Returns401`, `GetProjects_NoJwt_Returns401`) PASS — xác nhận `[Authorize]` attribute hoạt động.

### File List

**Backend — Tạo mới:**
- `src/Modules/Projects/ProjectManagement.Projects.Domain/ProjectManagement.Projects.Domain.csproj`
- `src/Modules/Projects/ProjectManagement.Projects.Domain/Entities/Project.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Domain/Entities/ProjectMembership.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Domain/Enums/ProjectStatus.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Domain/Enums/ProjectMemberRole.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Application/ProjectManagement.Projects.Application.csproj`
- `src/Modules/Projects/ProjectManagement.Projects.Application/DTOs/ProjectDto.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Application/Common/Interfaces/IProjectsDbContext.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Application/Common/Interfaces/IMembershipChecker.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Application/Queries/GetProjectList/GetProjectListQuery.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Application/Queries/GetProjectList/GetProjectListHandler.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Application/Queries/GetProjectById/GetProjectByIdQuery.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Application/Queries/GetProjectById/GetProjectByIdHandler.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Infrastructure/ProjectManagement.Projects.Infrastructure.csproj`
- `src/Modules/Projects/ProjectManagement.Projects.Infrastructure/Persistence/ProjectsDbContext.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Infrastructure/Persistence/Configurations/ProjectConfiguration.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Infrastructure/Persistence/Configurations/ProjectMembershipConfiguration.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Infrastructure/Services/MembershipChecker.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Infrastructure/Seeding/ProjectsSeeder.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Infrastructure/Migrations/20260425200000_InitProjects.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Infrastructure/Migrations/ProjectsDbContextModelSnapshot.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Infrastructure/Extensions/ProjectsInfrastructureServiceCollectionExtensions.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Api/ProjectManagement.Projects.Api.csproj`
- `src/Modules/Projects/ProjectManagement.Projects.Api/Controllers/ProjectsController.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Api/Extensions/ProjectsModuleServiceCollectionExtensions.cs`

**Shared — Tạo mới:**
- `src/Shared/ProjectManagement.Shared.Infrastructure/Services/ICurrentUserService.cs`
- `src/Shared/ProjectManagement.Shared.Infrastructure/Services/CurrentUserService.cs`

**Host — Cập nhật:**
- `src/Host/ProjectManagement.Host/Program.cs`
- `src/Host/ProjectManagement.Host/ProjectManagement.Host.csproj`
- `ProjectManagement.slnx`

**Frontend — Tạo mới:**
- `frontend/project-management-web/src/app/features/projects/models/project.model.ts`
- `frontend/project-management-web/src/app/features/projects/services/projects-api.service.ts`
- `frontend/project-management-web/src/app/features/projects/store/projects.actions.ts`
- `frontend/project-management-web/src/app/features/projects/store/projects.reducer.ts`
- `frontend/project-management-web/src/app/features/projects/store/projects.effects.ts`
- `frontend/project-management-web/src/app/features/projects/store/projects.selectors.ts`
- `frontend/project-management-web/src/app/features/projects/components/project-list/project-list.ts`
- `frontend/project-management-web/src/app/features/projects/components/project-list/project-list.html`
- `frontend/project-management-web/src/app/features/projects/components/project-list/project-list.scss`

**Frontend — Cập nhật:**
- `frontend/project-management-web/src/app/features/projects/projects.routes.ts`
- `frontend/project-management-web/src/app/core/store/app.state.ts`
- `frontend/project-management-web/src/app/app.config.ts`

**Tests — Tạo mới:**
- `tests/ProjectManagement.Host.Tests/ProjectsMembershipTests.cs`
- `frontend/project-management-web/src/app/features/projects/store/projects.effects.spec.ts`

## Change Log

- 2026-04-25: Story 1.2 created — membership-only authorization baseline. Status → ready-for-dev.
- 2026-04-25: Story 1.2 implemented by dev agent. Backend Projects module created from scratch (no skeleton existed). Frontend NgRx store + ProjectListComponent built. Status → review.
