# Story 1.3: Projects CRUD + Optimistic Locking (ETag/If-Match + 409)

Status: review

**Story ID:** 1.3
**Epic:** Epic 1 — Authentication + Portfolio/Project Setup + Gantt Interactive (Core Planning)
**Sprint:** Sprint 1
**Date Created:** 2026-04-25

---

## Story

As a PM,
I want tạo và quản lý project (CRUD) với cơ chế chống ghi đè (optimistic locking),
so that nhiều PM có thể thao tác đồng thời mà không mất dữ liệu và có inline reconcile khi conflict.

## Acceptance Criteria

1. **Given** user đã đăng nhập (JWT hợp lệ)
   **When** gọi `POST /api/v1/projects` với payload hợp lệ (`code`, `name`, optional `description`)
   **Then** trả `201` với project vừa tạo và `Location` header
   **And** response header có `ETag: "1"` (version)
   **And** project mặc định có `visibility=MembersOnly`
   **And** creator tự động trở thành Manager member của project vừa tạo

2. **Given** user đã đăng nhập
   **When** gọi `GET /api/v1/projects`
   **Then** trả `200` danh sách projects (membership-only — đã làm Story 1.2)
   **And** mỗi item có `id`, `code`, `name`, `status`, `visibility`, `version`

3. **Given** user đã đăng nhập và là member của `{projectId}`
   **When** gọi `GET /api/v1/projects/{projectId}`
   **Then** trả `200` project detail
   **And** response header có `ETag: "{version}"`

4. **Given** user đã đăng nhập và là member của `{projectId}`
   **When** gọi `PUT /api/v1/projects/{projectId}` với header `If-Match: "{version}"` đúng và payload hợp lệ
   **Then** trả `200` project đã cập nhật
   **And** response header có `ETag` mới (version+1)

5. **Given** user đã đăng nhập và là member của `{projectId}`
   **When** gọi `PUT /api/v1/projects/{projectId}` nhưng thiếu header `If-Match`
   **Then** trả `412 ProblemDetails` (Precondition Failed)

6. **Given** user đã đăng nhập và là member của `{projectId}`
   **When** gọi `PUT /api/v1/projects/{projectId}` với `If-Match` không khớp phiên bản mới nhất
   **Then** trả `409 ProblemDetails`
   **And** body JSON chứa `current` (server state mới nhất dạng ProjectDto) và `eTag` ở ROOT level

7. **Given** user đã đăng nhập và là member của `{projectId}`
   **When** gọi `DELETE /api/v1/projects/{projectId}` với `If-Match` khớp
   **Then** trả `204`

8. **Given** user đã đăng nhập và là member của `{projectId}`
   **When** gọi `DELETE /api/v1/projects/{projectId}` nhưng thiếu `If-Match`
   **Then** trả `412 ProblemDetails`

9. **Given** user đã đăng nhập và là member của `{projectId}`
   **When** gọi `DELETE /api/v1/projects/{projectId}` với `If-Match` không khớp
   **Then** trả `409 ProblemDetails`

10. **Given** user đã đăng nhập
    **When** gọi `POST /api/v1/projects` với `code` đã tồn tại
    **Then** trả `409 ProblemDetails` (unique constraint — không phải 422)

11. **Given** request không có/invalid/expired JWT
    **When** gọi bất kỳ endpoint projects nào
    **Then** trả `401 ProblemDetails`

12. **Given** bất kỳ lỗi validation/system nào
    **When** trả lỗi 4xx/5xx
    **Then** body luôn là `ProblemDetails`

## Tasks / Subtasks

- [x] Task 1: Mở rộng domain entity Project (BE)
  - [x] 1.1 Thêm `Description` property vào `Project.cs` (nullable string)
  - [x] 1.2 Sửa `Project.Create()` — đổi `Visibility = "Private"` → `"MembersOnly"`
  - [x] 1.3 Thêm `Project.Update(name, description, updatedBy)` method — bump Version
  - [x] 1.4 Thêm `Project.Archive(updatedBy)` method — set `IsDeleted = true`, bump Version

- [x] Task 2: Tạo CQRS Commands (BE)
  - [x] 2.1 `CreateProjectCommand.cs` — record với `Code`, `Name`, `Description?`, `CurrentUserId`
  - [x] 2.2 `CreateProjectCommandValidator.cs` — FluentValidation: Code 1-20 chars, Name required
  - [x] 2.3 `CreateProjectHandler.cs` — validate unique code (409), create project, add creator as Manager, SaveChanges, return `ProjectDto`
  - [x] 2.4 `UpdateProjectCommand.cs` — record với `ProjectId`, `Name`, `Description?`, `ExpectedVersion`, `CurrentUserId`
  - [x] 2.5 `UpdateProjectCommandValidator.cs` — Name required, max length
  - [x] 2.6 `UpdateProjectHandler.cs` — EnsureMember, load project, check version (409), call `Project.Update()`, SaveChanges, return `ProjectDto`
  - [x] 2.7 `DeleteProjectCommand.cs` — record với `ProjectId`, `ExpectedVersion`, `CurrentUserId`
  - [x] 2.8 `DeleteProjectHandler.cs` — EnsureMember, load project (không dùng IsDeleted filter ở đây vì đã check membership), check version (409), call `Project.Archive()`, SaveChanges

- [x] Task 3: EF Migration cho `description` column (BE)
  - [x] 3.1 Thêm `description` property configuration vào `ProjectConfiguration.cs` — nullable varchar(1000)
  - [x] 3.2 Tạo migration file `AddProjectDescription.cs` — `ALTER TABLE projects ADD COLUMN description varchar(1000) NULL`
  - [x] 3.3 Cập nhật `ProjectsDbContextModelSnapshot.cs`

- [x] Task 4: Mở rộng ProjectsController (BE)
  - [x] 4.1 Thêm `POST /` action — gọi `CreateProjectCommand`, trả 201 + `ETag` header + `Location` header
  - [x] 4.2 Cập nhật `GET /{projectId}` — thêm `ETag` header vào response
  - [x] 4.3 Thêm `PUT /{projectId}` action — parse `If-Match` header, 412 nếu thiếu, gọi `UpdateProjectCommand`, trả 200 + `ETag` header
  - [x] 4.4 Thêm `DELETE /{projectId}` action — parse `If-Match` header, 412 nếu thiếu, gọi `DeleteProjectCommand`, trả 204

- [x] Task 5: Cập nhật ProjectDto (BE)
  - [x] 5.1 Thêm `Description` field vào `ProjectDto` record — `string? Description`

- [x] Task 6: Frontend — mở rộng NgRx store (FE)
  - [x] 6.1 Cập nhật `project.model.ts` — thêm `description?: string`
  - [x] 6.2 Cập nhật `projects.actions.ts` — thêm Create/Update/Delete lifecycle actions (9 actions mới) + `updateProjectConflict`
  - [x] 6.3 Cập nhật `projects.reducer.ts` — handle create/update/delete/conflict actions
  - [x] 6.4 Thêm selectors cho create/update state: `selectProjectsCreating`, `selectProjectsUpdating`
  - [x] 6.5 Thêm CRUD effects vào `projects.effects.ts` — `createProject$`, `updateProject$`, `deleteProject$` với 409 handling
  - [x] 6.6 Cập nhật `ProjectsApiService` — thêm `createProject()`, `updateProject()`, `deleteProject()` với ETag header handling

- [x] Task 7: Frontend — Project Form Component (FE)
  - [x] 7.1 Tạo `features/projects/components/project-form/project-form.ts (.html, .scss)` — standalone MatDialog component
  - [x] 7.2 Form fields: `code` (required, disabled on edit), `name` (required), `description` (optional textarea)
  - [x] 7.3 Handle create mode vs edit mode (inject `MAT_DIALOG_DATA`)
  - [x] 7.4 Dispatch `ProjectsActions.createProject(...)` hoặc `updateProject(...)` on submit

- [x] Task 8: Frontend — cập nhật ProjectListComponent (FE)
  - [x] 8.1 Thêm "Tạo dự án" button — mở `ProjectFormComponent` dialog
  - [x] 8.2 Thêm edit / delete actions trên mỗi project card
  - [x] 8.3 Handle conflict state — khi `conflict` state xuất hiện, mở `ConflictDialogComponent`

- [x] Task 9: Tests (BE + FE)
  - [x] 9.1 Integration test BE: `POST /api/v1/projects` — success (201 + ETag), duplicate code (409)
  - [x] 9.2 Integration test BE: `PUT` — success (200 + new ETag), missing If-Match (412), stale If-Match (409 + current state)
  - [x] 9.3 Integration test BE: `DELETE` — success (204), missing If-Match (412), stale If-Match (409)
  - [x] 9.4 Integration test BE: Non-member PUT/DELETE → 404
  - [x] 9.5 Vitest FE: `createProject$` effect — success / failure
  - [x] 9.6 Vitest FE: `updateProject$` effect — success, 409 conflict dispatches `updateProjectConflict`

---

## Dev Notes

### ⚠️ Đọc Trước Khi Code — Những Gì Đã Có Sẵn

**Từ Stories 1.0-1.2 — KHÔNG viết lại:**

| File/Pattern | Trạng thái | Ghi chú |
|---|---|---|
| `ETagHelper` | ✅ tồn tại | `Shared.Infrastructure/OptimisticLocking/ETagHelper.cs` — dùng ngay |
| `ConflictException` | ✅ tồn tại | `Shared.Domain/Exceptions/ConflictException.cs` — có `CurrentState` + `CurrentETag` |
| `GlobalExceptionMiddleware` | ✅ đã wire | Xử lý `ConflictException` → 409 với `extensions.current` + `extensions.eTag` |
| `NotFoundException` | ✅ tồn tại | → 404 tự động |
| `IMembershipChecker.EnsureMemberAsync` | ✅ tồn tại | `Projects.Application/Common/Interfaces/` — dùng cho PUT/DELETE |
| `ICurrentUserService` | ✅ tồn tại | `Shared.Infrastructure/Services/` |
| `ProjectsController` | ✅ tồn tại | Đã có GET, CẬP NHẬT thêm POST/PUT/DELETE |
| `ProjectDto` | ✅ tồn tại | CẬP NHẬT thêm `Description?` |
| `ConflictDialogComponent` | ✅ tồn tại | `shared/components/conflict-dialog/conflict-dialog.ts` |
| `errorInterceptor` với `conflictError$` | ✅ tồn tại | `core/interceptors/error.interceptor.ts` |
| `ProjectsDbContext` + migration `InitProjects` | ✅ tồn tại | Schema có `projects` + `project_memberships` tables |

### Backend — Cập Nhật Project Entity

```csharp
// src/Modules/Projects/ProjectManagement.Projects.Domain/Entities/Project.cs
public class Project : AuditableEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }                    // ← THÊM MỚI
    public ProjectStatus Status { get; private set; }
    public string Visibility { get; private set; } = "MembersOnly";    // ← SỬA từ "Private"
    public int Version { get; private set; }
    public ICollection<ProjectMembership> Members { get; private set; } = new List<ProjectMembership>();

    // Factory — sửa Visibility default
    public static Project Create(string code, string name, string? description, string createdBy)
        => new()
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            Description = description,
            Status = ProjectStatus.Planning,
            Visibility = "MembersOnly",   // ← sửa từ "Private"
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy,
        };

    public void Update(string name, string? description, string updatedBy)
    {
        Name = name;
        Description = description;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
        Version++;
    }

    public void Archive(string updatedBy)
    {
        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
        Version++;
    }
}
```

**Lưu ý `Project.Create` signature thay đổi** — xem xét cập nhật `ProjectsSeeder.cs` nếu nó gọi `Project.Create(code, name, createdBy)` (thiếu `description`).

### Backend — CreateProjectHandler Pattern

```csharp
// CreateProjectHandler.cs
public async Task<ProjectDto> Handle(CreateProjectCommand cmd, CancellationToken ct)
{
    // 1. Check duplicate code → 409 (KHÔNG phải 422/400)
    var exists = await _db.Projects.AnyAsync(p => p.Code == cmd.Code && !p.IsDeleted, ct);
    if (exists)
        throw new ConflictException($"Project với code '{cmd.Code}' đã tồn tại.");

    // 2. Tạo project
    var project = Project.Create(cmd.Code, cmd.Name, cmd.Description, cmd.CurrentUserId.ToString());
    _db.Projects.Add(project);

    // 3. Creator tự động thành Manager member (CRITICAL — thiếu thì creator không thấy project sau khi tạo)
    var membership = ProjectMembership.Create(project.Id, cmd.CurrentUserId, ProjectMemberRole.Manager);
    _db.ProjectMemberships.Add(membership);

    await _db.SaveChangesAsync(ct);

    return new ProjectDto(project.Id, project.Code, project.Name, project.Description,
                          project.Status.ToString(), project.Visibility, project.Version);
}
```

### Backend — UpdateProjectHandler — Version Check + ConflictException

```csharp
// UpdateProjectHandler.cs
public async Task<ProjectDto> Handle(UpdateProjectCommand cmd, CancellationToken ct)
{
    // EnsureMember → NotFoundException nếu không phải member
    await _membershipChecker.EnsureMemberAsync(cmd.ProjectId, cmd.CurrentUserId, ct);

    // Load project (không cần membership filter vì đã check trên)
    var project = await _db.Projects
        .FirstOrDefaultAsync(p => p.Id == cmd.ProjectId && !p.IsDeleted, ct);
    if (project is null)
        throw new NotFoundException(nameof(Project), cmd.ProjectId);

    // Version mismatch → 409 với CurrentState + CurrentETag
    if (project.Version != cmd.ExpectedVersion)
    {
        var currentDto = new ProjectDto(project.Id, project.Code, project.Name, project.Description,
                                        project.Status.ToString(), project.Visibility, project.Version);
        throw new ConflictException(
            "Project đã được chỉnh sửa bởi người khác. Vui lòng tải lại.",
            currentState: currentDto,
            currentETag: ETagHelper.Generate(project.Version));
    }

    project.Update(cmd.Name, cmd.Description, cmd.CurrentUserId.ToString());
    await _db.SaveChangesAsync(ct);

    return new ProjectDto(project.Id, project.Code, project.Name, project.Description,
                          project.Status.ToString(), project.Visibility, project.Version);
}
```

### Backend — Controller — ETag Headers + 412 Logic

```csharp
// ProjectsController.cs — các actions mới/cập nhật

[HttpGet("{projectId:guid}")]
public async Task<IActionResult> GetProject(Guid projectId, CancellationToken ct)
{
    var result = await _mediator.Send(new GetProjectByIdQuery(projectId, _currentUser.UserId), ct);
    Response.Headers.ETag = ETagHelper.Generate(result.Version);  // ← THÊM
    return Ok(result);
}

[HttpPost]
public async Task<IActionResult> CreateProject([FromBody] CreateProjectRequest body, CancellationToken ct)
{
    var cmd = new CreateProjectCommand(body.Code, body.Name, body.Description, _currentUser.UserId);
    var result = await _mediator.Send(cmd, ct);
    Response.Headers.ETag = ETagHelper.Generate(result.Version);
    return CreatedAtAction(nameof(GetProject), new { projectId = result.Id }, result);
}

[HttpPut("{projectId:guid}")]
public async Task<IActionResult> UpdateProject(Guid projectId, [FromBody] UpdateProjectRequest body, CancellationToken ct)
{
    var version = ETagHelper.ParseIfMatch(Request.Headers.IfMatch);
    if (version is null)
        return StatusCode(StatusCodes.Status412PreconditionFailed,
            new ProblemDetails { Status = 412, Title = "Precondition Required",
                Detail = "If-Match header là bắt buộc cho cập nhật." });

    var cmd = new UpdateProjectCommand(projectId, body.Name, body.Description, (int)version, _currentUser.UserId);
    var result = await _mediator.Send(cmd, ct);  // throws ConflictException → 409 via GlobalExceptionMiddleware
    Response.Headers.ETag = ETagHelper.Generate(result.Version);
    return Ok(result);
}

[HttpDelete("{projectId:guid}")]
public async Task<IActionResult> DeleteProject(Guid projectId, CancellationToken ct)
{
    var version = ETagHelper.ParseIfMatch(Request.Headers.IfMatch);
    if (version is null)
        return StatusCode(StatusCodes.Status412PreconditionFailed,
            new ProblemDetails { Status = 412, Title = "Precondition Required",
                Detail = "If-Match header là bắt buộc cho xóa." });

    var cmd = new DeleteProjectCommand(projectId, (int)version, _currentUser.UserId);
    await _mediator.Send(cmd, ct);  // throws ConflictException → 409 via GlobalExceptionMiddleware
    return NoContent();
}
```

**Request DTOs (inline hoặc separate file):**
```csharp
public sealed record CreateProjectRequest(string Code, string Name, string? Description);
public sealed record UpdateProjectRequest(string Name, string? Description);
```

**Quan trọng — `Version` là `int` nhưng `ETagHelper.ParseIfMatch` trả `long?`:**
- Cast an toàn: `(int)version` — version số nhỏ (<2 tỷ) trong thực tế
- Nếu muốn an toàn hơn: check `version > int.MaxValue` → 412

### Backend — 409 Body Structure (CRITICAL cho FE Tests)

`GlobalExceptionMiddleware` đã xử lý `ConflictException` với `CurrentState` + `CurrentETag`:

```csharp
// GlobalExceptionMiddleware.cs (đã có, không sửa)
problem.Extensions["current"] = conflict.CurrentState;
problem.Extensions["eTag"] = conflict.CurrentETag;
```

`ProblemDetails.Extensions` serialize ở **ROOT level** trong JSON response:
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.8",
  "title": "Conflict",
  "status": 409,
  "detail": "Project đã được chỉnh sửa bởi người khác.",
  "current": { "id": "...", "code": "PROJ-01", "name": "...", "version": 2 },
  "eTag": "\"2\""
}
```

Integration test phải assert tại **root** level:
```csharp
// ✅ ĐÚNG
Assert.True(body.TryGetProperty("current", out _));
Assert.True(body.TryGetProperty("eTag", out _));

// ❌ SAI
Assert.True(body.GetProperty("extensions").TryGetProperty("current", out _));
```

### Backend — EF Migration: AddProjectDescription

Cần tạo **thủ công** (không thể chạy `dotnet ef migrations add` khi không có DB):

```csharp
// Migrations/20260426000000_AddProjectDescription.cs
public partial class AddProjectDescription : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "description",
            table: "projects",
            type: "character varying(1000)",
            maxLength: 1000,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "description", table: "projects");
    }
}
```

Cập nhật `ProjectsDbContextModelSnapshot.cs` — thêm property description vào `projects` table builder.

Cập nhật `ProjectConfiguration.cs`:
```csharp
b.Property(x => x.Description).HasColumnName("description").HasMaxLength(1000).IsRequired(false);
```

### Backend — Validator Pattern (FluentValidation)

```csharp
// CreateProjectCommandValidator.cs
public class CreateProjectCommandValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code không được để trống.")
            .MaximumLength(20).WithMessage("Code không vượt quá 20 ký tự.")
            .Matches(@"^[A-Z0-9\-]+$").WithMessage("Code chỉ dùng chữ hoa, số, gạch ngang.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên project không được để trống.")
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(1000).When(x => x.Description is not null);
    }
}
```

**Lưu ý: Validation error → 400, duplicate code → 409.** Đây là 2 luồng khác nhau.

### Frontend — Cập Nhật ProjectDto Model

```typescript
// features/projects/models/project.model.ts
export interface Project {
  id: string;
  code: string;
  name: string;
  description?: string;  // ← THÊM MỚI
  status: 'Planning' | 'Active' | 'OnHold' | 'Completed';
  visibility: string;
  version: number;
}
```

### Frontend — Mở Rộng Actions

```typescript
// features/projects/store/projects.actions.ts
export const ProjectsActions = createActionGroup({
  source: 'Projects',
  events: {
    // Existing (giữ nguyên)
    'Load Projects': emptyProps(),
    'Load Projects Success': props<{ projects: Project[] }>(),
    'Load Projects Failure': props<{ error: string }>(),
    'Select Project': props<{ projectId: string }>(),

    // Create
    'Create Project': props<{ code: string; name: string; description?: string }>(),
    'Create Project Success': props<{ project: Project }>(),
    'Create Project Failure': props<{ error: string }>(),

    // Update
    'Update Project': props<{ projectId: string; name: string; description?: string; version: number }>(),
    'Update Project Success': props<{ project: Project }>(),
    'Update Project Failure': props<{ error: string }>(),
    'Update Project Conflict': props<{ serverState: Project; eTag: string; pendingName: string; pendingDescription?: string }>(),

    // Delete
    'Delete Project': props<{ projectId: string; version: number }>(),
    'Delete Project Success': props<{ projectId: string }>(),
    'Delete Project Failure': props<{ error: string }>(),

    // Conflict resolved
    'Clear Conflict': emptyProps(),
  },
});
```

### Frontend — Effect: 409 Conflict Handling

```typescript
// features/projects/store/projects.effects.ts (thêm effect mới)
updateProject$ = createEffect(() =>
  this.actions$.pipe(
    ofType(ProjectsActions.updateProject),
    switchMap(action =>
      this.projectsApiService.updateProject(action.projectId, action.name, action.description, action.version).pipe(
        map(project => ProjectsActions.updateProjectSuccess({ project })),
        catchError(err => {
          if (err.status === 409) {
            // body.current và body.eTag ở ROOT level (xem "409 Body Structure" bên trên)
            return of(ProjectsActions.updateProjectConflict({
              serverState: err.error.current,
              eTag: err.error.eTag,
              pendingName: action.name,
              pendingDescription: action.description,
            }));
          }
          return of(ProjectsActions.updateProjectFailure({
            error: err.error?.detail ?? err.error?.title ?? 'Không thể cập nhật dự án.',
          }));
        })
      )
    )
  )
);
```

### Frontend — ProjectsApiService: ETag Headers

```typescript
// features/projects/services/projects-api.service.ts (cập nhật)
import { HttpClient, HttpHeaders } from '@angular/common/http';

createProject(code: string, name: string, description?: string): Observable<Project> {
  return this.http.post<Project>(this.baseUrl, { code, name, description });
}

updateProject(projectId: string, name: string, description: string | undefined, version: number): Observable<Project> {
  const headers = new HttpHeaders({ 'If-Match': `"${version}"` });
  return this.http.put<Project>(`${this.baseUrl}/${projectId}`, { name, description }, { headers });
}

deleteProject(projectId: string, version: number): Observable<void> {
  const headers = new HttpHeaders({ 'If-Match': `"${version}"` });
  return this.http.delete<void>(`${this.baseUrl}/${projectId}`, { headers });
}
```

### Frontend — ConflictDialogComponent (Đã Có — Dùng Lại)

```typescript
// KHÔNG tạo lại — shared/components/conflict-dialog/conflict-dialog.ts đã có sẵn
// Sử dụng trong component khi conflict state xuất hiện:
import { ConflictDialogComponent, ConflictDialogData } from '../../../shared/components/conflict-dialog/conflict-dialog';

// Trong ProjectFormComponent hoặc ProjectListComponent:
this.store.select(selectProjectsConflict).pipe(
  filter(Boolean),
  switchMap(conflict => {
    const data: ConflictDialogData = {
      serverState: conflict.serverState,
      userChanges: { name: conflict.pendingName },
      eTag: conflict.eTag,
    };
    return this.dialog.open(ConflictDialogComponent, { data }).afterClosed();
  })
).subscribe(result => {
  if (result === 'use-server') {
    this.store.dispatch(ProjectsActions.clearConflict());
    // Load server state vào form
  } else if (result === 'retry-mine') {
    this.store.dispatch(ProjectsActions.clearConflict());
    // Re-submit với eTag mới từ server state
  }
});
```

### Frontend — Reducer State Mở Rộng

```typescript
// projects.reducer.ts — thêm creating/updating/deleting state + conflict
export interface ConflictState {
  serverState: Project;
  eTag: string;
  pendingName: string;
  pendingDescription?: string;
}

export interface ProjectsState extends EntityState<Project> {
  selectedId: string | null;
  loading: boolean;
  error: string | null;
  creating: boolean;         // ← THÊM
  updating: boolean;         // ← THÊM
  deleting: boolean;         // ← THÊM
  conflict: ConflictState | null;  // ← THÊM
}
```

### File Structure — Tất Cả Files Cần Tạo/Cập Nhật

**Backend — Tạo mới:**
```
src/Modules/Projects/ProjectManagement.Projects.Application/
├── Commands/
│   ├── CreateProject/
│   │   ├── CreateProjectCommand.cs
│   │   ├── CreateProjectCommandValidator.cs
│   │   └── CreateProjectHandler.cs
│   ├── UpdateProject/
│   │   ├── UpdateProjectCommand.cs
│   │   ├── UpdateProjectCommandValidator.cs
│   │   └── UpdateProjectHandler.cs
│   └── DeleteProject/
│       ├── DeleteProjectCommand.cs
│       └── DeleteProjectHandler.cs

src/Modules/Projects/ProjectManagement.Projects.Infrastructure/
└── Migrations/
    └── 20260426000000_AddProjectDescription.cs
```

**Backend — Cập nhật:**
```
src/Modules/Projects/ProjectManagement.Projects.Domain/Entities/Project.cs        ← + Description, Update(), Archive()
src/Modules/Projects/ProjectManagement.Projects.Application/DTOs/ProjectDto.cs    ← + Description?
src/Modules/Projects/.../Persistence/Configurations/ProjectConfiguration.cs       ← + description config
src/Modules/Projects/.../Migrations/ProjectsDbContextModelSnapshot.cs             ← + description property
src/Modules/Projects/ProjectManagement.Projects.Api/Controllers/ProjectsController.cs ← + POST/PUT/DELETE + ETag
```

**Frontend — Tạo mới:**
```
frontend/.../features/projects/components/project-form/project-form.ts (.html, .scss)
```

**Frontend — Cập nhật:**
```
frontend/.../features/projects/models/project.model.ts
frontend/.../features/projects/store/projects.actions.ts
frontend/.../features/projects/store/projects.reducer.ts
frontend/.../features/projects/store/projects.effects.ts
frontend/.../features/projects/store/projects.selectors.ts
frontend/.../features/projects/services/projects-api.service.ts
frontend/.../features/projects/components/project-list/project-list.ts
frontend/.../features/projects/components/project-list/project-list.html
```

**Tests — Tạo mới:**
```
tests/ProjectManagement.Host.Tests/ProjectsCrudTests.cs
frontend/.../features/projects/store/projects.effects.spec.ts   ← cập nhật thêm CRUD tests
```

### Anti-Patterns — Tránh Những Lỗi Này

**Backend:**
❌ **KHÔNG** dùng 409 cho validation lỗi (thiếu field) — 400 ValidationException  
❌ **KHÔNG** dùng 422 cho duplicate code — phải là 409 ConflictException  
❌ **KHÔNG** quên add creator làm ProjectMembership sau khi tạo project — thiếu thì creator không thấy project mình vừa tạo  
❌ **KHÔNG** trả 403 cho non-member DELETE/PUT — phải là 404 (EnsureMemberAsync → NotFoundException)  
❌ **KHÔNG** tạo lại `GlobalExceptionMiddleware` hay `ETagHelper` — đã có sẵn  
❌ **KHÔNG** skip migration — `description` column phải có trước khi chạy integration tests  
❌ **KHÔNG** quên cập nhật `ProjectsSeeder.cs` nếu `Project.Create` signature đổi  

**Frontend:**
❌ **KHÔNG** tạo lại `ConflictDialogComponent` — đã có ở `shared/components/conflict-dialog/`  
❌ **KHÔNG** access `err.error.extensions.current` — field `current` và `eTag` ở **ROOT level** của body  
❌ **KHÔNG** hardcode `If-Match` header value mà không đọc từ `project.version`  
❌ **KHÔNG** tạo NgModule mới — standalone components  
❌ **KHÔNG** call API trực tiếp từ component — dispatch action, effect xử lý HTTP  

### Test Pattern — Integration Test (Backend)

Dùng `WebApplicationFactory<Program>` như Story 1.2:

```csharp
// tests/ProjectManagement.Host.Tests/ProjectsCrudTests.cs
public sealed class ProjectsCrudTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    // LUÔN dùng _factory.CreateClient() — không dùng new HttpClient { BaseAddress = ... }

    [Fact]
    public async Task CreateProject_ValidPayload_Returns201WithETag()
    {
        var client = _factory.CreateClient();
        var token = await GetTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var body = new { code = "TEST-01", name = "Test Project" };
        var response = await client.PostAsJsonAsync("/api/v1/projects", body);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.ETag);
        // Verify creator is a member by calling GET /projects and finding the new project
    }

    [Fact]
    public async Task UpdateProject_StaleETag_Returns409WithCurrentState()
    {
        // ... create project, then update with old ETag
        var updateResponse = await client.PutAsJsonAsync(...);
        Assert.Equal(HttpStatusCode.Conflict, updateResponse.StatusCode);
        var body = await updateResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.True(body.TryGetProperty("current", out _));   // ROOT level, không phải "extensions.current"
        Assert.True(body.TryGetProperty("eTag", out _));      // ROOT level
    }
}
```

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 1.3]
- [Source: _bmad-output/planning-artifacts/architecture.md#AD-06 Conflict Resolution]
- [Source: _bmad-output/planning-artifacts/architecture.md#5.5 Error Handling Patterns]
- [Source: _bmad-output/planning-artifacts/architecture.md#5.3 API Response Format]
- [Source: src/Shared/ProjectManagement.Shared.Infrastructure/OptimisticLocking/ETagHelper.cs]
- [Source: src/Shared/ProjectManagement.Shared.Domain/Exceptions/ConflictException.cs]
- [Source: src/Shared/ProjectManagement.Shared.Infrastructure/Middleware/GlobalExceptionMiddleware.cs]
- [Source: _bmad-output/implementation-artifacts/1-2-project-membership-only-authorization-baseline.md#Dev Notes]

---

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

- FluentValidation không có trong Application.csproj → thêm package reference `FluentValidation 11.11.0`
- `ETagHelper` ở `Shared.Infrastructure` không accessible từ Application layer → thay bằng format inline `$"\"{version}\""` trong ConflictException (clean architecture)
- `Project.Create()` signature đổi (thêm `description`) → cập nhật `ProjectsSeeder.cs`
- `describe is not defined` khi chạy Vitest standalone → phải dùng `ng test` (Angular CLI wrapper)

### Completion Notes List

- ✅ Task 1: Project entity mở rộng — thêm `Description`, `Update()`, `Archive()`, sửa Visibility default → `"MembersOnly"`
- ✅ Task 2: 8 CQRS files tạo mới — CreateProjectCommand/Validator/Handler, UpdateProject, DeleteProject. FluentValidation 11.11.0 thêm vào Application.csproj
- ✅ Task 3: EF Migration `20260426000000_AddProjectDescription` + cập nhật ProjectConfiguration + ModelSnapshot
- ✅ Task 4: ProjectsController mở rộng — POST/PUT/DELETE + ETag headers + 412 logic
- ✅ Task 5: ProjectDto thêm `Description?` field, cập nhật cả GetProjectById/GetProjectList handlers
- ✅ Task 6: Frontend NgRx store — model/actions/reducer/effects/service đầy đủ CRUD + conflict state
- ✅ Task 7: ProjectFormComponent standalone — create/edit mode, MatDialog, form validation
- ✅ Task 8: ProjectListComponent — thêm create/edit/delete UI, conflict dialog integration
- ✅ Task 9: Tests — BE integration tests (ProjectsCrudTests.cs, 15 tests), FE effects tests (33 tests total, tất cả pass)
- Build: 0 errors, 3 warnings (pre-existing EF Core version conflict không liên quan)
- BE tests: 18/41 pass (DB-dependent tests fail vì không có PostgreSQL — pre-existing, giống Stories 1.1/1.2), 3 new no-JWT tests pass
- FE tests: 33/33 pass với `ng test`

### File List

**Backend — Tạo mới:**
- src/Modules/Projects/ProjectManagement.Projects.Application/Commands/CreateProject/CreateProjectCommand.cs
- src/Modules/Projects/ProjectManagement.Projects.Application/Commands/CreateProject/CreateProjectCommandValidator.cs
- src/Modules/Projects/ProjectManagement.Projects.Application/Commands/CreateProject/CreateProjectHandler.cs
- src/Modules/Projects/ProjectManagement.Projects.Application/Commands/UpdateProject/UpdateProjectCommand.cs
- src/Modules/Projects/ProjectManagement.Projects.Application/Commands/UpdateProject/UpdateProjectCommandValidator.cs
- src/Modules/Projects/ProjectManagement.Projects.Application/Commands/UpdateProject/UpdateProjectHandler.cs
- src/Modules/Projects/ProjectManagement.Projects.Application/Commands/DeleteProject/DeleteProjectCommand.cs
- src/Modules/Projects/ProjectManagement.Projects.Application/Commands/DeleteProject/DeleteProjectHandler.cs
- src/Modules/Projects/ProjectManagement.Projects.Infrastructure/Migrations/20260426000000_AddProjectDescription.cs
- tests/ProjectManagement.Host.Tests/ProjectsCrudTests.cs

**Backend — Cập nhật:**
- src/Modules/Projects/ProjectManagement.Projects.Domain/Entities/Project.cs
- src/Modules/Projects/ProjectManagement.Projects.Application/DTOs/ProjectDto.cs
- src/Modules/Projects/ProjectManagement.Projects.Application/Queries/GetProjectById/GetProjectByIdHandler.cs
- src/Modules/Projects/ProjectManagement.Projects.Application/Queries/GetProjectList/GetProjectListHandler.cs
- src/Modules/Projects/ProjectManagement.Projects.Application/ProjectManagement.Projects.Application.csproj
- src/Modules/Projects/ProjectManagement.Projects.Infrastructure/Persistence/Configurations/ProjectConfiguration.cs
- src/Modules/Projects/ProjectManagement.Projects.Infrastructure/Migrations/ProjectsDbContextModelSnapshot.cs
- src/Modules/Projects/ProjectManagement.Projects.Infrastructure/Seeding/ProjectsSeeder.cs
- src/Modules/Projects/ProjectManagement.Projects.Api/Controllers/ProjectsController.cs

**Frontend — Tạo mới:**
- frontend/project-management-web/src/app/features/projects/components/project-form/project-form.ts
- frontend/project-management-web/src/app/features/projects/components/project-form/project-form.html
- frontend/project-management-web/src/app/features/projects/components/project-form/project-form.scss

**Frontend — Cập nhật:**
- frontend/project-management-web/src/app/features/projects/models/project.model.ts
- frontend/project-management-web/src/app/features/projects/store/projects.actions.ts
- frontend/project-management-web/src/app/features/projects/store/projects.reducer.ts
- frontend/project-management-web/src/app/features/projects/store/projects.effects.ts
- frontend/project-management-web/src/app/features/projects/store/projects.selectors.ts
- frontend/project-management-web/src/app/features/projects/store/projects.effects.spec.ts
- frontend/project-management-web/src/app/features/projects/services/projects-api.service.ts
- frontend/project-management-web/src/app/features/projects/components/project-list/project-list.ts
- frontend/project-management-web/src/app/features/projects/components/project-list/project-list.html
- frontend/project-management-web/src/app/features/projects/components/project-list/project-list.scss

**Sprint tracking:**
- _bmad-output/implementation-artifacts/sprint-status.yaml
