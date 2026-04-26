# Story 1.4: Project Structure CRUD (Phase/Milestone/Task) + Required Task Fields (MVP)

Status: review

**Story ID:** 1.4
**Epic:** Epic 1 — Authentication + Portfolio/Project Setup + Gantt Interactive (Core Planning)
**Sprint:** Sprint 2
**Date Created:** 2026-04-25

---

## Story

As a PM,
I want tạo và quản lý cấu trúc Dự Án → Phase → Milestone → Task và đầy đủ field của task (MVP),
so that tôi có thể biểu diễn kế hoạch như Excel/MS Project và chuẩn bị dữ liệu cho Gantt interactive.

## Acceptance Criteria

1. **Given** user là member của `{projectId}`
   **When** user tạo/sửa/xóa Phase, Milestone, Task trong project
   **Then** hệ thống lưu đúng quan hệ cha–con (Project → Phase → Milestone → Task)
   **And** không cho tạo vòng lặp trong cây cha–con (một node không được là tổ tiên của chính nó)

2. **Given** user tạo hoặc cập nhật một Task (bao gồm Phase, Milestone)
   **When** submit dữ liệu
   **Then** Task hỗ trợ tối thiểu các field: `vbs`, `name`, `type` (Phase/Milestone/Task), `priority`, `status`, `notes`, `plannedStartDate`, `plannedEndDate`, `actualStartDate`, `actualEndDate`, `plannedEffortHours`, `actualEffortHours` (computed từ TimeEntries — Epic 3, hiện trả null), `percentComplete`, `assigneeUserId` (1 người/task), `predecessors[]` với dependency types FS/SS/FF/SF

3. **Given** user thêm predecessor cho Task A
   **When** chọn Task B làm predecessor (FS/SS/FF/SF)
   **Then** không cho phép tạo dependency tạo thành cycle trong dependency graph
   **And** trả lỗi `400 ProblemDetails` với message rõ ràng khi cycle

4. **Given** Task có `plannedStartDate`/`plannedEndDate`
   **When** user lưu Task
   **Then** validate `plannedStartDate <= plannedEndDate`
   **And** validate `actualStartDate <= actualEndDate` (nếu cả hai có giá trị)

5. **Given** user không phải member của `{projectId}`
   **When** gọi bất kỳ endpoint CRUD hierarchy/task trong project đó
   **Then** trả `404 ProblemDetails`

6. **Given** entity (Phase/Milestone/Task) có version/ETag
   **When** update/delete thiếu `If-Match`
   **Then** trả `412 ProblemDetails`
   **And** mismatch version trả `409 ProblemDetails` kèm `extensions.current` (TaskDto) và `extensions.eTag` để UI reconcile

## Tasks / Subtasks

- [x] **Task 1: Domain Entities + Enums (BE)**
  - [x] 1.1 Tạo `TaskType.cs` enum — `Phase | Milestone | Task`
  - [x] 1.2 Tạo `TaskPriority.cs` enum — `Low | Medium | High | Critical`
  - [x] 1.3 Tạo `TaskStatus.cs` enum — `NotStarted | InProgress | Completed | OnHold | Cancelled | Delayed`
  - [x] 1.4 Tạo `DependencyType.cs` enum — `FS | SS | FF | SF`
  - [x] 1.5 Tạo `ProjectTask.cs` entity kế thừa `AuditableEntity` — tất cả fields, methods `Create()`, `Update()`, `Delete()`
  - [x] 1.6 Tạo `TaskDependency.cs` entity kế thừa `BaseEntity` — `TaskId`, `PredecessorId`, `DependencyType`

- [x] **Task 2: EF Core Configuration + Migration (BE)**
  - [x] 2.1 Tạo `TaskConfiguration.cs` — bảng `project_tasks`, snake_case columns, FK, index
  - [x] 2.2 Tạo `TaskDependencyConfiguration.cs` — bảng `task_dependencies`, composite unique (task_id, predecessor_id)
  - [x] 2.3 Cập nhật `ProjectsDbContext.cs` — thêm `DbSet<ProjectTask> ProjectTasks` và `DbSet<TaskDependency> TaskDependencies`
  - [x] 2.4 Cập nhật `IProjectsDbContext.cs` — thêm 2 DbSets mới
  - [x] 2.5 Tạo migration thủ công `20260427000000_AddProjectTasks.cs` — tạo 2 bảng, FK, index
  - [x] 2.6 Cập nhật `ProjectsDbContextModelSnapshot.cs`

- [x] **Task 3: CQRS Commands + Handlers (BE)**
  - [x] 3.1 Tạo `CreateTaskCommand.cs` record + `CreateTaskCommandValidator.cs` + `CreateTaskHandler.cs`
  - [x] 3.2 Tạo `UpdateTaskCommand.cs` record + `UpdateTaskCommandValidator.cs` + `UpdateTaskHandler.cs`
  - [x] 3.3 Tạo `DeleteTaskCommand.cs` record + `DeleteTaskHandler.cs`
  - [x] 3.4 `CreateTaskHandler` — verify membership, verify parentId trong cùng project, detect hierarchy cycle, insert task + dependencies
  - [x] 3.5 `UpdateTaskHandler` — verify membership, check version (409), detect hierarchy cycle nếu parentId đổi, detect dependency cycle, replace dependencies, save
  - [x] 3.6 `DeleteTaskHandler` — verify membership, check version (409), kiểm tra tasks con (422 nếu còn children), soft-delete

- [x] **Task 4: CQRS Queries + DTOs (BE)**
  - [x] 4.1 Tạo `TaskDto.cs` record — tất cả fields bao gồm `ActualEffortHours` (luôn null cho đến Epic 3)
  - [x] 4.2 Tạo `TaskDependencyDto.cs` record — `PredecessorId`, `DependencyType`
  - [x] 4.3 Tạo `GetTasksByProjectQuery.cs` + `GetTasksByProjectHandler.cs` — trả flat list, order by `sort_order`
  - [x] 4.4 Tạo `GetTaskByIdQuery.cs` + `GetTaskByIdHandler.cs` — include predecessors

- [x] **Task 5: API Controller (BE)**
  - [x] 5.1 Tạo `TasksController.cs` route `api/v1/projects/{projectId}/tasks`
  - [x] 5.2 `GET /` — GetTasksByProject, không cần ETag
  - [x] 5.3 `GET /{taskId}` — GetTaskById, set `ETag` header
  - [x] 5.4 `POST /` — CreateTask, trả 201 + `ETag` + `Location` header
  - [x] 5.5 `PUT /{taskId}` — parse `If-Match` (412 nếu thiếu), UpdateTask (409 qua middleware nếu version lệch), trả 200 + `ETag`
  - [x] 5.6 `DELETE /{taskId}` — parse `If-Match` (412 nếu thiếu), DeleteTask, trả 204
  - [x] 5.7 Tạo request records inline: `CreateTaskRequest`, `UpdateTaskRequest`, `TaskDependencyRequest`

- [x] **Task 6: Frontend — NgRx Store (FE)**
  - [x] 6.1 Tạo `features/projects/models/task.model.ts` — `ProjectTask` interface và `TaskDependency` interface
  - [x] 6.2 Tạo `features/projects/store/tasks.actions.ts` — Load/Create/Update/Delete lifecycle + ConflictAction
  - [x] 6.3 Tạo `features/projects/store/tasks.reducer.ts` — `TasksState extends EntityState<ProjectTask>`, handle CRUD + conflict
  - [x] 6.4 Tạo `features/projects/store/tasks.effects.ts` — HTTP effects, handle 409 dispatch `taskUpdateConflict`
  - [x] 6.5 Tạo `features/projects/store/tasks.selectors.ts` — `selectAllTasks`, `selectTasksByProject`, `selectTasksLoading`, `selectTasksConflict`
  - [x] 6.6 Cập nhật `core/store/app.state.ts` — thêm `tasks: TasksState`
  - [x] 6.7 Cập nhật `app.config.ts` — thêm `provideState(tasksFeature)` và `provideEffects(TasksEffects)`

- [x] **Task 7: Frontend — Service (FE)**
  - [x] 7.1 Tạo `features/projects/services/tasks-api.service.ts` — CRUD methods với ETag header handling

- [x] **Task 8: Frontend — Components (FE)**
  - [x] 8.1 Tạo `features/projects/components/project-detail/project-detail.ts (.html, .scss)` — container, dispatch `LoadTasks`, hiển thị task tree
  - [x] 8.2 Tạo `features/projects/components/task-tree/task-tree.ts (.html, .scss)` — hiển thị hierarchy, nút Add/Edit/Delete per node
  - [x] 8.3 Tạo `features/projects/components/task-form/task-form.ts (.html, .scss)` — MatDialog form, tất cả task fields, predecessor selector

- [x] **Task 9: Frontend — Routing (FE)**
  - [x] 9.1 Cập nhật `features/projects/projects.routes.ts` — thêm route `/projects/:projectId` → `ProjectDetailComponent`

- [x] **Task 10: Tests (BE + FE)**
  - [x] 10.1 Integration test BE: `POST /api/v1/projects/{id}/tasks` — success (201 + ETag), non-member (404), parent cycle (400)
  - [x] 10.2 Integration test BE: `PUT` — success (200 + new ETag), missing If-Match (412), stale If-Match (409 với current state), dependency cycle (400)
  - [x] 10.3 Integration test BE: `DELETE` — success (204), has children (422), missing If-Match (412), stale (409)
  - [x] 10.4 Integration test BE: hierarchy constraints — đảm bảo tasks trong đúng project scope
  - [x] 10.5 Vitest FE: `tasks.effects.ts` — loadTasks, createTask, updateTask (409 conflict), deleteTask

---

## Dev Notes

### ⚠️ Đọc Trước Khi Code — Những Gì Đã Có Sẵn

**Từ Stories 1.0–1.3 — KHÔNG viết lại:**

| File/Pattern | Trạng thái | Ghi chú |
|---|---|---|
| `ETagHelper` | ✅ tồn tại | `Shared.Infrastructure/OptimisticLocking/ETagHelper.cs` — dùng ngay |
| `ConflictException` | ✅ tồn tại | `Shared.Domain/Exceptions/ConflictException.cs` — có `CurrentState` + `CurrentETag` |
| `DomainException` | ✅ tồn tại | `Shared.Domain/Exceptions/DomainException.cs` — map → 422 |
| `NotFoundException` | ✅ tồn tại | `Shared.Domain/Exceptions/NotFoundException.cs` — map → 404 |
| `GlobalExceptionMiddleware` | ✅ đã wire | Xử lý tất cả exception → ProblemDetails tự động |
| `IMembershipChecker.EnsureMemberAsync` | ✅ tồn tại | `Projects.Application/Common/Interfaces/` — dùng cho mọi task mutation |
| `ICurrentUserService` | ✅ tồn tại | `Shared.Infrastructure/Services/` |
| `ProjectsDbContext` | ✅ tồn tại | **CẬP NHẬT** thêm `ProjectTasks` + `TaskDependencies` DbSets |
| `AuditableEntity` | ✅ tồn tại | `Shared.Domain/Entities/AuditableEntity.cs` — `ProjectTask` kế thừa |
| `BaseEntity` | ✅ tồn tại | `Shared.Domain/Entities/BaseEntity.cs` — `TaskDependency` kế thừa |
| `ConflictDialogComponent` | ✅ tồn tại | `shared/components/conflict-dialog/conflict-dialog.ts` — dùng lại |
| `errorInterceptor` | ✅ tồn tại | `core/interceptors/error.interceptor.ts` — xử lý 409 tự động |
| `ProjectsController` | ✅ tồn tại | Không sửa — TasksController là file riêng |
| `ProjectsApiService` | ✅ tồn tại | Không sửa — TasksApiService là file riêng |
| `IMembershipChecker` interface | ✅ tồn tại | Designed to be reused for sub-resources |

---

### Quyết Định Thiết Kế — Single Table Hierarchy

**Phase, Milestone, Task đều dùng chung entity `ProjectTask`** với `Type` discriminator (không phải 3 bảng riêng).

**Lý do:**
- Gantt chart (Story 1.5) cần flat list với `parentId` để render tree
- Tất cả nodes đều cần các field giống nhau (dates, effort, status)
- Bryntum Gantt expect flat array input + `parentId` mapping
- Giảm JOIN phức tạp, migration đơn giản hơn

**Hierarchy rules (không enforce tại DB, validate trong handler):**
- Phase: parentId = null (trực tiếp dưới project)
- Milestone: parentId trỏ đến Phase
- Task: parentId trỏ đến Milestone hoặc Phase
- (MVP: flexible — parentId chỉ cần thuộc cùng project)

**`actualEffortHours` KHÔNG lưu trên task** — là computed field từ TimeEntries (Epic 3). Response luôn trả `null` cho đến khi Epic 3 implement.

---

### Backend — Domain Entities

#### Enums — Đặt trong `Projects.Domain/Enums/`

```csharp
// TaskType.cs
public enum TaskType { Phase, Milestone, Task }

// TaskPriority.cs
public enum TaskPriority { Low, Medium, High, Critical }

// TaskStatus.cs
public enum TaskStatus { NotStarted, InProgress, Completed, OnHold, Cancelled, Delayed }

// DependencyType.cs
public enum DependencyType { FS, SS, FF, SF }
```

#### ProjectTask.cs — `Projects.Domain/Entities/`

```csharp
public class ProjectTask : AuditableEntity
{
    public Guid ProjectId { get; private set; }
    public Guid? ParentId { get; private set; }    // null = root node (Phase)
    public TaskType Type { get; private set; }
    public string Vbs { get; private set; } = string.Empty;    // e.g. "1.2.3", user-entered
    public string Name { get; private set; } = string.Empty;
    public TaskPriority Priority { get; private set; }
    public TaskStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public DateOnly? PlannedStartDate { get; private set; }
    public DateOnly? PlannedEndDate { get; private set; }
    public DateOnly? ActualStartDate { get; private set; }
    public DateOnly? ActualEndDate { get; private set; }
    public decimal? PlannedEffortHours { get; private set; }
    // actualEffortHours: computed from TimeEntries (Epic 3) — NOT stored here
    public decimal? PercentComplete { get; private set; }    // 0.00–100.00
    public Guid? AssigneeUserId { get; private set; }
    public int SortOrder { get; private set; }              // display order within parent
    public int Version { get; private set; }

    // Navigation properties
    public ICollection<TaskDependency> Predecessors { get; private set; } = [];
    public ICollection<TaskDependency> Successors { get; private set; } = [];

    public static ProjectTask Create(
        Guid projectId, Guid? parentId, TaskType type,
        string vbs, string name, TaskPriority priority, TaskStatus status,
        string? notes, DateOnly? plannedStartDate, DateOnly? plannedEndDate,
        DateOnly? actualStartDate, DateOnly? actualEndDate,
        decimal? plannedEffortHours, decimal? percentComplete,
        Guid? assigneeUserId, int sortOrder, string createdBy) => new()
    {
        Id = Guid.NewGuid(),
        ProjectId = projectId,
        ParentId = parentId,
        Type = type,
        Vbs = vbs,
        Name = name,
        Priority = priority,
        Status = status,
        Notes = notes,
        PlannedStartDate = plannedStartDate,
        PlannedEndDate = plannedEndDate,
        ActualStartDate = actualStartDate,
        ActualEndDate = actualEndDate,
        PlannedEffortHours = plannedEffortHours,
        PercentComplete = percentComplete,
        AssigneeUserId = assigneeUserId,
        SortOrder = sortOrder,
        Version = 1,
        CreatedAt = DateTime.UtcNow,
        CreatedBy = createdBy,
    };

    public void Update(
        Guid? parentId, TaskType type, string vbs, string name,
        TaskPriority priority, TaskStatus status, string? notes,
        DateOnly? plannedStartDate, DateOnly? plannedEndDate,
        DateOnly? actualStartDate, DateOnly? actualEndDate,
        decimal? plannedEffortHours, decimal? percentComplete,
        Guid? assigneeUserId, int sortOrder, string updatedBy)
    {
        ParentId = parentId;
        Type = type;
        Vbs = vbs;
        Name = name;
        Priority = priority;
        Status = status;
        Notes = notes;
        PlannedStartDate = plannedStartDate;
        PlannedEndDate = plannedEndDate;
        ActualStartDate = actualStartDate;
        ActualEndDate = actualEndDate;
        PlannedEffortHours = plannedEffortHours;
        PercentComplete = percentComplete;
        AssigneeUserId = assigneeUserId;
        SortOrder = sortOrder;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
        Version++;
    }

    public void Delete(string updatedBy)
    {
        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
        Version++;
    }
}
```

#### TaskDependency.cs — `Projects.Domain/Entities/`

```csharp
public class TaskDependency : BaseEntity
{
    public Guid TaskId { get; private set; }          // the "successor" task
    public Guid PredecessorId { get; private set; }   // the predecessor task

    // DependencyType stored as string (không dùng enum FK trong DB)
    public DependencyType DependencyType { get; private set; }

    public static TaskDependency Create(Guid taskId, Guid predecessorId, DependencyType type) => new()
    {
        Id = Guid.NewGuid(),
        TaskId = taskId,
        PredecessorId = predecessorId,
        DependencyType = type,
        CreatedAt = DateTime.UtcNow,
    };
}
```

---

### Backend — EF Configuration

#### TaskConfiguration.cs — `Projects.Infrastructure/Persistence/Configurations/`

```csharp
public class TaskConfiguration : IEntityTypeConfiguration<ProjectTask>
{
    public void Configure(EntityTypeBuilder<ProjectTask> b)
    {
        b.ToTable("project_tasks");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.ProjectId).HasColumnName("project_id").IsRequired();
        b.Property(x => x.ParentId).HasColumnName("parent_id");
        b.Property(x => x.Type).HasColumnName("type")
            .HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.Vbs).HasColumnName("vbs").HasMaxLength(50).IsRequired(false);
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(500).IsRequired();
        b.Property(x => x.Priority).HasColumnName("priority")
            .HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.Status).HasColumnName("status")
            .HasConversion<string>().HasMaxLength(30).IsRequired();
        b.Property(x => x.Notes).HasColumnName("notes").HasMaxLength(4000);
        b.Property(x => x.PlannedStartDate).HasColumnName("planned_start_date");
        b.Property(x => x.PlannedEndDate).HasColumnName("planned_end_date");
        b.Property(x => x.ActualStartDate).HasColumnName("actual_start_date");
        b.Property(x => x.ActualEndDate).HasColumnName("actual_end_date");
        b.Property(x => x.PlannedEffortHours).HasColumnName("planned_effort_hours")
            .HasColumnType("numeric(8,2)");
        b.Property(x => x.PercentComplete).HasColumnName("percent_complete")
            .HasColumnType("numeric(5,2)");
        b.Property(x => x.AssigneeUserId).HasColumnName("assignee_user_id");
        b.Property(x => x.SortOrder).HasColumnName("sort_order");
        b.Property(x => x.Version).HasColumnName("version");
        b.Property(x => x.IsDeleted).HasColumnName("is_deleted");
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.CreatedBy).HasColumnName("created_by").HasMaxLength(450);
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        b.Property(x => x.UpdatedBy).HasColumnName("updated_by").HasMaxLength(450);

        b.HasIndex(x => x.ProjectId).HasDatabaseName("ix_project_tasks_project_id");
        b.HasIndex(x => x.ParentId).HasDatabaseName("ix_project_tasks_parent_id");
        b.HasIndex(x => new { x.ProjectId, x.SortOrder })
            .HasDatabaseName("ix_project_tasks_project_sort");

        // Self-referencing for hierarchy (Restrict — không cascade delete)
        // No navigation from ProjectTask to Parent — query by parentId directly
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}
```

#### TaskDependencyConfiguration.cs — `Projects.Infrastructure/Persistence/Configurations/`

```csharp
public class TaskDependencyConfiguration : IEntityTypeConfiguration<TaskDependency>
{
    public void Configure(EntityTypeBuilder<TaskDependency> b)
    {
        b.ToTable("task_dependencies");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.TaskId).HasColumnName("task_id").IsRequired();
        b.Property(x => x.PredecessorId).HasColumnName("predecessor_id").IsRequired();
        b.Property(x => x.DependencyType).HasColumnName("dependency_type")
            .HasConversion<string>().HasMaxLength(5).IsRequired();
        b.Property(x => x.CreatedAt).HasColumnName("created_at");

        // Unique: cannot add same predecessor twice
        b.HasIndex(x => new { x.TaskId, x.PredecessorId })
            .IsUnique()
            .HasDatabaseName("uq_task_dependencies_task_predecessor");

        b.HasIndex(x => x.TaskId).HasDatabaseName("ix_task_dependencies_task_id");
        b.HasIndex(x => x.PredecessorId).HasDatabaseName("ix_task_dependencies_predecessor_id");

        // FKs to project_tasks — NO query filter here (ProjectTask has its own filter)
        b.HasOne<ProjectTask>()
            .WithMany(t => t.Successors)     // task_dependencies where this task is predecessor
            .HasForeignKey(d => d.PredecessorId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne<ProjectTask>()
            .WithMany(t => t.Predecessors)   // task_dependencies where this task is successor
            .HasForeignKey(d => d.TaskId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

#### Cập nhật ProjectsDbContext.cs

```csharp
// Thêm vào ProjectsDbContext
public DbSet<ProjectTask> ProjectTasks => Set<ProjectTask>();
public DbSet<TaskDependency> TaskDependencies => Set<TaskDependency>();

// Trong OnModelCreating — thêm:
modelBuilder.ApplyConfiguration(new TaskConfiguration());
modelBuilder.ApplyConfiguration(new TaskDependencyConfiguration());
```

#### Migration: 20260427000000_AddProjectTasks.cs

```csharp
public partial class AddProjectTasks : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.CreateTable("project_tasks", t => new
        {
            id = t.Column<Guid>(nullable: false),
            project_id = t.Column<Guid>(nullable: false),
            parent_id = t.Column<Guid>(nullable: true),
            type = t.Column<string>(maxLength: 20, nullable: false),
            vbs = t.Column<string>(maxLength: 50, nullable: true),
            name = t.Column<string>(maxLength: 500, nullable: false),
            priority = t.Column<string>(maxLength: 20, nullable: false),
            status = t.Column<string>(maxLength: 30, nullable: false),
            notes = t.Column<string>(maxLength: 4000, nullable: true),
            planned_start_date = t.Column<DateOnly>(nullable: true),
            planned_end_date = t.Column<DateOnly>(nullable: true),
            actual_start_date = t.Column<DateOnly>(nullable: true),
            actual_end_date = t.Column<DateOnly>(nullable: true),
            planned_effort_hours = t.Column<decimal>(type: "numeric(8,2)", nullable: true),
            percent_complete = t.Column<decimal>(type: "numeric(5,2)", nullable: true),
            assignee_user_id = t.Column<Guid>(nullable: true),
            sort_order = t.Column<int>(nullable: false, defaultValue: 0),
            version = t.Column<int>(nullable: false, defaultValue: 1),
            is_deleted = t.Column<bool>(nullable: false, defaultValue: false),
            created_at = t.Column<DateTime>(nullable: false),
            created_by = t.Column<string>(maxLength: 450, nullable: true),
            updated_at = t.Column<DateTime>(nullable: true),
            updated_by = t.Column<string>(maxLength: 450, nullable: true),
        }, constraints: t =>
        {
            t.PrimaryKey("pk_project_tasks", x => x.id);
            t.ForeignKey("fk_project_tasks_projects", x => x.project_id,
                "projects", "id", onDelete: ReferentialAction.Restrict);
            t.ForeignKey("fk_project_tasks_parent", x => x.parent_id,
                "project_tasks", "id", onDelete: ReferentialAction.Restrict);
        });

        m.CreateTable("task_dependencies", t => new
        {
            id = t.Column<Guid>(nullable: false),
            task_id = t.Column<Guid>(nullable: false),
            predecessor_id = t.Column<Guid>(nullable: false),
            dependency_type = t.Column<string>(maxLength: 5, nullable: false),
            created_at = t.Column<DateTime>(nullable: false),
        }, constraints: t =>
        {
            t.PrimaryKey("pk_task_dependencies", x => x.id);
            t.ForeignKey("fk_task_dependencies_task", x => x.task_id,
                "project_tasks", "id", onDelete: ReferentialAction.Restrict);
            t.ForeignKey("fk_task_dependencies_predecessor", x => x.predecessor_id,
                "project_tasks", "id", onDelete: ReferentialAction.Restrict);
        });

        m.CreateIndex("ix_project_tasks_project_id", "project_tasks", "project_id");
        m.CreateIndex("ix_project_tasks_parent_id", "project_tasks", "parent_id");
        m.CreateIndex("ix_project_tasks_project_sort", "project_tasks", ["project_id", "sort_order"]);
        m.CreateIndex("ix_task_dependencies_task_id", "task_dependencies", "task_id");
        m.CreateIndex("ix_task_dependencies_predecessor_id", "task_dependencies", "predecessor_id");
        m.CreateIndex("uq_task_dependencies_task_predecessor", "task_dependencies",
            ["task_id", "predecessor_id"], unique: true);
    }

    protected override void Down(MigrationBuilder m)
    {
        m.DropTable("task_dependencies");
        m.DropTable("project_tasks");
    }
}
```

---

### Backend — CQRS Commands + Handlers

#### Cấu trúc thư mục Application layer

```
Projects.Application/
├── Tasks/
│   ├── Commands/
│   │   ├── CreateTask/
│   │   │   ├── CreateTaskCommand.cs
│   │   │   ├── CreateTaskCommandValidator.cs
│   │   │   └── CreateTaskHandler.cs
│   │   ├── UpdateTask/
│   │   │   ├── UpdateTaskCommand.cs
│   │   │   ├── UpdateTaskCommandValidator.cs
│   │   │   └── UpdateTaskHandler.cs
│   │   └── DeleteTask/
│   │       ├── DeleteTaskCommand.cs
│   │       └── DeleteTaskHandler.cs
│   └── Queries/
│       ├── GetTasksByProject/
│       │   ├── GetTasksByProjectQuery.cs
│       │   └── GetTasksByProjectHandler.cs
│       └── GetTaskById/
│           ├── GetTaskByIdQuery.cs
│           └── GetTaskByIdHandler.cs
└── DTOs/
    ├── TaskDto.cs
    └── TaskDependencyDto.cs
```

#### TaskDto.cs

```csharp
public record TaskDto(
    Guid Id,
    Guid ProjectId,
    Guid? ParentId,
    string Type,            // "Phase" | "Milestone" | "Task"
    string? Vbs,
    string Name,
    string Priority,        // "Low" | "Medium" | "High" | "Critical"
    string Status,
    string? Notes,
    DateOnly? PlannedStartDate,
    DateOnly? PlannedEndDate,
    DateOnly? ActualStartDate,
    DateOnly? ActualEndDate,
    decimal? PlannedEffortHours,
    decimal? ActualEffortHours,   // LUÔN null — computed từ TimeEntries ở Epic 3
    decimal? PercentComplete,
    Guid? AssigneeUserId,
    int SortOrder,
    int Version,
    List<TaskDependencyDto> Predecessors);

public record TaskDependencyDto(Guid PredecessorId, string DependencyType);
```

#### CreateTaskCommand.cs

```csharp
public record CreateTaskCommand(
    Guid ProjectId,
    Guid? ParentId,
    TaskType Type,
    string Vbs,
    string Name,
    TaskPriority Priority,
    TaskStatus Status,
    string? Notes,
    DateOnly? PlannedStartDate,
    DateOnly? PlannedEndDate,
    DateOnly? ActualStartDate,
    DateOnly? ActualEndDate,
    decimal? PlannedEffortHours,
    decimal? PercentComplete,
    Guid? AssigneeUserId,
    int SortOrder,
    List<(Guid PredecessorId, DependencyType DependencyType)> Predecessors,
    Guid CurrentUserId) : IRequest<TaskDto>;
```

#### CreateTaskCommandValidator.cs

```csharp
public class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Vbs).MaximumLength(50).When(x => x.Vbs is not null);
        RuleFor(x => x.PlannedEffortHours).GreaterThan(0).When(x => x.PlannedEffortHours.HasValue);
        RuleFor(x => x.PercentComplete).InclusiveBetween(0, 100).When(x => x.PercentComplete.HasValue);
        // Date validation
        RuleFor(x => x)
            .Must(x => x.PlannedStartDate is null || x.PlannedEndDate is null
                       || x.PlannedStartDate <= x.PlannedEndDate)
            .WithMessage("plannedStartDate phải nhỏ hơn hoặc bằng plannedEndDate.");
        RuleFor(x => x)
            .Must(x => x.ActualStartDate is null || x.ActualEndDate is null
                       || x.ActualStartDate <= x.ActualEndDate)
            .WithMessage("actualStartDate phải nhỏ hơn hoặc bằng actualEndDate.");
    }
}
```

#### CreateTaskHandler.cs

```csharp
public class CreateTaskHandler(IProjectsDbContext db, IMembershipChecker membership)
    : IRequestHandler<CreateTaskCommand, TaskDto>
{
    public async Task<TaskDto> Handle(CreateTaskCommand cmd, CancellationToken ct)
    {
        // 1. Membership check (404 nếu không phải member)
        await membership.EnsureMemberAsync(cmd.ProjectId, cmd.CurrentUserId, ct);

        // 2. Verify parentId thuộc cùng project (nếu có)
        if (cmd.ParentId.HasValue)
        {
            var parentExists = await db.ProjectTasks
                .AnyAsync(t => t.Id == cmd.ParentId && t.ProjectId == cmd.ProjectId, ct);
            if (!parentExists)
                throw new NotFoundException(nameof(ProjectTask), cmd.ParentId);
        }

        // 3. Tạo task
        var task = ProjectTask.Create(
            cmd.ProjectId, cmd.ParentId, cmd.Type, cmd.Vbs, cmd.Name,
            cmd.Priority, cmd.Status, cmd.Notes,
            cmd.PlannedStartDate, cmd.PlannedEndDate,
            cmd.ActualStartDate, cmd.ActualEndDate,
            cmd.PlannedEffortHours, cmd.PercentComplete,
            cmd.AssigneeUserId, cmd.SortOrder,
            cmd.CurrentUserId.ToString());

        db.ProjectTasks.Add(task);

        // 4. Add predecessors nếu có
        if (cmd.Predecessors.Count > 0)
        {
            // Verify predecessors thuộc cùng project
            var predIds = cmd.Predecessors.Select(p => p.PredecessorId).ToList();
            var validPredCount = await db.ProjectTasks
                .CountAsync(t => predIds.Contains(t.Id) && t.ProjectId == cmd.ProjectId, ct);
            if (validPredCount != predIds.Count)
                throw new DomainException("Một hoặc nhiều predecessor không thuộc project này.");

            // Cycle detection KHÔNG cần cho CREATE vì task mới chưa có successors
            foreach (var (predId, depType) in cmd.Predecessors)
            {
                db.TaskDependencies.Add(TaskDependency.Create(task.Id, predId, depType));
            }
        }

        await db.SaveChangesAsync(ct);
        return MapToDto(task, cmd.Predecessors.Select(p =>
            new TaskDependencyDto(p.PredecessorId, p.DependencyType.ToString())).ToList());
    }
}
```

#### UpdateTaskHandler.cs — Cycle Detection Logic

```csharp
public class UpdateTaskHandler(IProjectsDbContext db, IMembershipChecker membership)
    : IRequestHandler<UpdateTaskCommand, TaskDto>
{
    public async Task<TaskDto> Handle(UpdateTaskCommand cmd, CancellationToken ct)
    {
        await membership.EnsureMemberAsync(cmd.ProjectId, cmd.CurrentUserId, ct);

        var task = await db.ProjectTasks
            .Include(t => t.Predecessors)
            .FirstOrDefaultAsync(t => t.Id == cmd.TaskId && t.ProjectId == cmd.ProjectId, ct);
        if (task is null)
            throw new NotFoundException(nameof(ProjectTask), cmd.TaskId);

        // Version check → 409
        if (task.Version != cmd.ExpectedVersion)
        {
            var currentDto = await BuildDtoAsync(task, ct);
            throw new ConflictException(
                "Task đã được chỉnh sửa bởi người khác. Vui lòng tải lại.",
                currentState: currentDto,
                currentETag: ETagHelper.Generate(task.Version));
        }

        // Hierarchy cycle detection khi parentId thay đổi
        if (cmd.ParentId.HasValue && cmd.ParentId != task.ParentId)
        {
            if (await WouldCreateHierarchyCycleAsync(cmd.TaskId, cmd.ParentId.Value, ct))
                throw new DomainException("Không thể đặt parent — sẽ tạo vòng lặp trong cây.");

            // Verify parentId thuộc cùng project
            var parentExists = await db.ProjectTasks
                .AnyAsync(t => t.Id == cmd.ParentId && t.ProjectId == cmd.ProjectId, ct);
            if (!parentExists)
                throw new NotFoundException(nameof(ProjectTask), cmd.ParentId);
        }

        // Dependency cycle detection cho predecessors mới
        foreach (var (predId, _) in cmd.Predecessors)
        {
            if (predId == cmd.TaskId)
                throw new DomainException("Task không thể là predecessor của chính nó.");
            if (await WouldCreateDependencyCycleAsync(cmd.TaskId, predId, ct))
                throw new DomainException(
                    $"Thêm predecessor '{predId}' sẽ tạo dependency cycle.");
        }

        // Replace tất cả dependencies
        db.TaskDependencies.RemoveRange(task.Predecessors);
        foreach (var (predId, depType) in cmd.Predecessors)
            db.TaskDependencies.Add(TaskDependency.Create(cmd.TaskId, predId, depType));

        task.Update(cmd.ParentId, cmd.Type, cmd.Vbs, cmd.Name,
            cmd.Priority, cmd.Status, cmd.Notes,
            cmd.PlannedStartDate, cmd.PlannedEndDate,
            cmd.ActualStartDate, cmd.ActualEndDate,
            cmd.PlannedEffortHours, cmd.PercentComplete,
            cmd.AssigneeUserId, cmd.SortOrder,
            cmd.CurrentUserId.ToString());

        await db.SaveChangesAsync(ct);
        return await BuildDtoAsync(task, ct);
    }

    // --- Cycle detection helpers ---

    // Kiểm tra: nếu đặt newParentId làm parent của taskId, có tạo cycle không?
    // Cycle = newParentId là descendant của taskId
    private async Task<bool> WouldCreateHierarchyCycleAsync(
        Guid taskId, Guid newParentId, CancellationToken ct)
    {
        // BFS/DFS từ taskId xuống — nếu gặp newParentId thì là cycle
        var visited = new HashSet<Guid>();
        var queue = new Queue<Guid>();
        queue.Enqueue(taskId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!visited.Add(current)) continue;

            var children = await db.ProjectTasks
                .Where(t => t.ParentId == current)
                .Select(t => t.Id)
                .ToListAsync(ct);

            foreach (var childId in children)
            {
                if (childId == newParentId) return true;  // cycle!
                queue.Enqueue(childId);
            }
        }
        return false;
    }

    // Kiểm tra: nếu thêm predecessorId làm predecessor của taskId, có tạo dependency cycle?
    // Cycle = taskId đã là (direct/indirect) predecessor của predecessorId
    private async Task<bool> WouldCreateDependencyCycleAsync(
        Guid taskId, Guid predecessorId, CancellationToken ct)
    {
        // BFS từ predecessorId đi theo chiều "successors"
        // Nếu gặp taskId → cycle
        var visited = new HashSet<Guid>();
        var queue = new Queue<Guid>();
        queue.Enqueue(predecessorId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!visited.Add(current)) continue;

            var successors = await db.TaskDependencies
                .Where(d => d.PredecessorId == current)
                .Select(d => d.TaskId)
                .ToListAsync(ct);

            foreach (var s in successors)
            {
                if (s == taskId) return true;  // cycle!
                queue.Enqueue(s);
            }
        }
        return false;
    }
}
```

#### DeleteTaskHandler.cs

```csharp
public class DeleteTaskHandler(IProjectsDbContext db, IMembershipChecker membership)
    : IRequestHandler<DeleteTaskCommand>
{
    public async Task Handle(DeleteTaskCommand cmd, CancellationToken ct)
    {
        await membership.EnsureMemberAsync(cmd.ProjectId, cmd.CurrentUserId, ct);

        var task = await db.ProjectTasks
            .FirstOrDefaultAsync(t => t.Id == cmd.TaskId && t.ProjectId == cmd.ProjectId, ct);
        if (task is null)
            throw new NotFoundException(nameof(ProjectTask), cmd.TaskId);

        if (task.Version != cmd.ExpectedVersion)
        {
            var currentDto = /* MapToDto(task) */ ...;
            throw new ConflictException(
                "Task đã thay đổi. Vui lòng tải lại.",
                currentState: currentDto,
                currentETag: ETagHelper.Generate(task.Version));
        }

        // Không cho phép xóa task có children còn active
        var hasChildren = await db.ProjectTasks
            .AnyAsync(t => t.ParentId == cmd.TaskId, ct);
        if (hasChildren)
            throw new DomainException("Không thể xóa task có child tasks. Xóa child tasks trước.");

        task.Delete(cmd.CurrentUserId.ToString());
        await db.SaveChangesAsync(ct);
    }
}
```

#### GetTasksByProjectHandler.cs

```csharp
public class GetTasksByProjectHandler(IProjectsDbContext db, IMembershipChecker membership)
    : IRequestHandler<GetTasksByProjectQuery, List<TaskDto>>
{
    public async Task<List<TaskDto>> Handle(GetTasksByProjectQuery query, CancellationToken ct)
    {
        await membership.EnsureMemberAsync(query.ProjectId, query.CurrentUserId, ct);

        // Trả flat list — client tự build tree bằng parentId
        // Order: sort_order trong cùng parentId
        var tasks = await db.ProjectTasks
            .Where(t => t.ProjectId == query.ProjectId)
            .Include(t => t.Predecessors)
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.CreatedAt)
            .ToListAsync(ct);

        return tasks.Select(t => MapToDto(t)).ToList();
    }

    private static TaskDto MapToDto(ProjectTask t) => new(
        t.Id, t.ProjectId, t.ParentId,
        t.Type.ToString(), t.Vbs, t.Name,
        t.Priority.ToString(), t.Status.ToString(),
        t.Notes, t.PlannedStartDate, t.PlannedEndDate,
        t.ActualStartDate, t.ActualEndDate,
        t.PlannedEffortHours,
        ActualEffortHours: null,   // computed from TimeEntries — Epic 3
        t.PercentComplete, t.AssigneeUserId,
        t.SortOrder, t.Version,
        t.Predecessors.Select(p => new TaskDependencyDto(
            p.PredecessorId, p.DependencyType.ToString())).ToList());
}
```

---

### Backend — TasksController

```csharp
// Route: "api/v1/projects/{projectId}/tasks"
[ApiController]
[Route("api/v1/projects/{projectId:guid}/tasks")]
[Authorize]
public class TasksController(IMediator mediator, ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetTasks(Guid projectId, CancellationToken ct)
    {
        var result = await mediator.Send(
            new GetTasksByProjectQuery(projectId, currentUser.UserId), ct);
        return Ok(result);
    }

    [HttpGet("{taskId:guid}")]
    public async Task<IActionResult> GetTask(Guid projectId, Guid taskId, CancellationToken ct)
    {
        var result = await mediator.Send(
            new GetTaskByIdQuery(taskId, projectId, currentUser.UserId), ct);
        Response.Headers.ETag = ETagHelper.Generate(result.Version);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTask(
        Guid projectId, [FromBody] CreateTaskRequest body, CancellationToken ct)
    {
        var predecessors = body.Predecessors?
            .Select(p => (p.PredecessorId, Enum.Parse<DependencyType>(p.DependencyType)))
            .ToList() ?? [];

        var cmd = new CreateTaskCommand(
            projectId, body.ParentId,
            Enum.Parse<TaskType>(body.Type), body.Vbs ?? string.Empty,
            body.Name, Enum.Parse<TaskPriority>(body.Priority),
            Enum.Parse<TaskStatus>(body.Status), body.Notes,
            body.PlannedStartDate, body.PlannedEndDate,
            body.ActualStartDate, body.ActualEndDate,
            body.PlannedEffortHours, body.PercentComplete,
            body.AssigneeUserId, body.SortOrder,
            predecessors, currentUser.UserId);

        var result = await mediator.Send(cmd, ct);
        Response.Headers.ETag = ETagHelper.Generate(result.Version);
        return CreatedAtAction(nameof(GetTask),
            new { projectId, taskId = result.Id }, result);
    }

    [HttpPut("{taskId:guid}")]
    public async Task<IActionResult> UpdateTask(
        Guid projectId, Guid taskId, [FromBody] UpdateTaskRequest body, CancellationToken ct)
    {
        var version = ETagHelper.ParseIfMatch(Request.Headers.IfMatch);
        if (version is null)
            return StatusCode(412, new ProblemDetails
            {
                Status = 412, Title = "Precondition Required",
                Detail = "If-Match header là bắt buộc cho cập nhật task."
            });

        var predecessors = body.Predecessors?
            .Select(p => (p.PredecessorId, Enum.Parse<DependencyType>(p.DependencyType)))
            .ToList() ?? [];

        var cmd = new UpdateTaskCommand(
            taskId, projectId, body.ParentId,
            Enum.Parse<TaskType>(body.Type), body.Vbs ?? string.Empty,
            body.Name, Enum.Parse<TaskPriority>(body.Priority),
            Enum.Parse<TaskStatus>(body.Status), body.Notes,
            body.PlannedStartDate, body.PlannedEndDate,
            body.ActualStartDate, body.ActualEndDate,
            body.PlannedEffortHours, body.PercentComplete,
            body.AssigneeUserId, body.SortOrder,
            predecessors, (int)version, currentUser.UserId);

        var result = await mediator.Send(cmd, ct);
        Response.Headers.ETag = ETagHelper.Generate(result.Version);
        return Ok(result);
    }

    [HttpDelete("{taskId:guid}")]
    public async Task<IActionResult> DeleteTask(
        Guid projectId, Guid taskId, CancellationToken ct)
    {
        var version = ETagHelper.ParseIfMatch(Request.Headers.IfMatch);
        if (version is null)
            return StatusCode(412, new ProblemDetails
            {
                Status = 412, Title = "Precondition Required",
                Detail = "If-Match header là bắt buộc cho xóa task."
            });

        await mediator.Send(
            new DeleteTaskCommand(taskId, projectId, (int)version, currentUser.UserId), ct);
        return NoContent();
    }
}

// Inline request records (đặt trong controller file hoặc file riêng)
public sealed record CreateTaskRequest(
    Guid? ParentId, string Type, string? Vbs, string Name,
    string Priority, string Status, string? Notes,
    DateOnly? PlannedStartDate, DateOnly? PlannedEndDate,
    DateOnly? ActualStartDate, DateOnly? ActualEndDate,
    decimal? PlannedEffortHours, decimal? PercentComplete,
    Guid? AssigneeUserId, int SortOrder,
    List<TaskDependencyRequest>? Predecessors);

public sealed record UpdateTaskRequest(
    Guid? ParentId, string Type, string? Vbs, string Name,
    string Priority, string Status, string? Notes,
    DateOnly? PlannedStartDate, DateOnly? PlannedEndDate,
    DateOnly? ActualStartDate, DateOnly? ActualEndDate,
    decimal? PlannedEffortHours, decimal? PercentComplete,
    Guid? AssigneeUserId, int SortOrder,
    List<TaskDependencyRequest>? Predecessors);

public sealed record TaskDependencyRequest(Guid PredecessorId, string DependencyType);
```

**Enum parsing lỗi → 400 tự động** nếu dùng `Enum.Parse` bọc trong FluentValidation, hoặc dùng `[JsonConverter]` attribute để parse từ JSON. Cách đơn giản nhất: validate enum string trong CommandValidator rồi parse.

---

### Backend — 409 Body Structure

**Giống hệt Story 1.3** — `GlobalExceptionMiddleware` đã xử lý:
```json
{
  "title": "Conflict",
  "status": 409,
  "detail": "Task đã được chỉnh sửa bởi người khác. Vui lòng tải lại.",
  "current": { "id": "...", "name": "...", "version": 3, ... },
  "eTag": "\"3\""
}
```
`current` và `eTag` ở **ROOT level** (không phải `extensions.current`). Integration test phải assert đúng điều này.

---

### Frontend — Task Model

```typescript
// features/projects/models/task.model.ts
export interface ProjectTask {
  id: string;
  projectId: string;
  parentId: string | null;
  type: 'Phase' | 'Milestone' | 'Task';
  vbs: string | null;
  name: string;
  priority: 'Low' | 'Medium' | 'High' | 'Critical';
  status: 'NotStarted' | 'InProgress' | 'Completed' | 'OnHold' | 'Cancelled' | 'Delayed';
  notes: string | null;
  plannedStartDate: string | null;   // "2026-04-25" format (DateOnly)
  plannedEndDate: string | null;
  actualStartDate: string | null;
  actualEndDate: string | null;
  plannedEffortHours: number | null;
  actualEffortHours: number | null;  // luôn null đến Epic 3
  percentComplete: number | null;
  assigneeUserId: string | null;
  sortOrder: number;
  version: number;
  predecessors: TaskDependency[];
}

export interface TaskDependency {
  predecessorId: string;
  dependencyType: 'FS' | 'SS' | 'FF' | 'SF';
}
```

---

### Frontend — NgRx Store

#### tasks.actions.ts

```typescript
// features/projects/store/tasks.actions.ts
export const TasksActions = createActionGroup({
  source: 'Tasks',
  events: {
    'Load Tasks': props<{ projectId: string }>(),
    'Load Tasks Success': props<{ tasks: ProjectTask[] }>(),
    'Load Tasks Failure': props<{ error: string }>(),

    'Create Task': props<{ projectId: string; request: CreateTaskPayload }>(),
    'Create Task Success': props<{ task: ProjectTask }>(),
    'Create Task Failure': props<{ error: string }>(),

    'Update Task': props<{ projectId: string; taskId: string; request: UpdateTaskPayload; version: number }>(),
    'Update Task Success': props<{ task: ProjectTask }>(),
    'Update Task Failure': props<{ error: string }>(),
    'Update Task Conflict': props<{ serverState: ProjectTask; eTag: string }>(),

    'Delete Task': props<{ projectId: string; taskId: string; version: number }>(),
    'Delete Task Success': props<{ taskId: string }>(),
    'Delete Task Failure': props<{ error: string }>(),

    'Clear Task Conflict': emptyProps(),
    'Select Task': props<{ taskId: string | null }>(),
  },
});
```

#### tasks.reducer.ts

```typescript
export interface TaskConflictState {
  serverState: ProjectTask;
  eTag: string;
}

export interface TasksState extends EntityState<ProjectTask> {
  currentProjectId: string | null;
  selectedTaskId: string | null;
  loading: boolean;
  creating: boolean;
  updating: boolean;
  deleting: boolean;
  error: string | null;
  conflict: TaskConflictState | null;
}

const adapter = createEntityAdapter<ProjectTask>();

export const tasksFeature = createFeature({
  name: 'tasks',
  reducer: createReducer(
    adapter.getInitialState({
      currentProjectId: null, selectedTaskId: null,
      loading: false, creating: false, updating: false, deleting: false,
      error: null, conflict: null,
    } as TasksState),
    on(TasksActions.loadTasks, (s, { projectId }) =>
      ({ ...s, loading: true, error: null, currentProjectId: projectId })),
    on(TasksActions.loadTasksSuccess, (s, { tasks }) =>
      adapter.setAll(tasks, { ...s, loading: false })),
    on(TasksActions.loadTasksFailure, (s, { error }) =>
      ({ ...s, loading: false, error })),

    on(TasksActions.createTaskSuccess, (s, { task }) =>
      adapter.addOne(task, { ...s, creating: false })),
    on(TasksActions.updateTaskSuccess, (s, { task }) =>
      adapter.upsertOne(task, { ...s, updating: false, conflict: null })),
    on(TasksActions.updateTaskConflict, (s, { serverState, eTag }) =>
      ({ ...s, updating: false, conflict: { serverState, eTag } })),
    on(TasksActions.deleteTaskSuccess, (s, { taskId }) =>
      adapter.removeOne(taskId, { ...s, deleting: false })),
    on(TasksActions.clearTaskConflict, s => ({ ...s, conflict: null })),
    on(TasksActions.selectTask, (s, { taskId }) => ({ ...s, selectedTaskId: taskId })),
  ),
});
```

#### tasks.effects.ts — 409 Conflict Handling

```typescript
updateTask$ = createEffect(() =>
  this.actions$.pipe(
    ofType(TasksActions.updateTask),
    switchMap(({ projectId, taskId, request, version }) =>
      this.tasksApi.updateTask(projectId, taskId, request, version).pipe(
        map(task => TasksActions.updateTaskSuccess({ task })),
        catchError(err => {
          if (err.status === 409) {
            // current và eTag ở ROOT level của body (không phải extensions)
            return of(TasksActions.updateTaskConflict({
              serverState: err.error.current,
              eTag: err.error.eTag,
            }));
          }
          return of(TasksActions.updateTaskFailure({
            error: err.error?.detail ?? 'Không thể cập nhật task.',
          }));
        })
      )
    )
  )
);
```

---

### Frontend — TasksApiService

```typescript
// features/projects/services/tasks-api.service.ts
@Injectable({ providedIn: 'root' })
export class TasksApiService {
  private readonly http = inject(HttpClient);

  private url(projectId: string) {
    return `/api/v1/projects/${projectId}/tasks`;
  }

  getTasks(projectId: string): Observable<ProjectTask[]> {
    return this.http.get<ProjectTask[]>(this.url(projectId));
  }

  getTask(projectId: string, taskId: string): Observable<ProjectTask> {
    return this.http.get<ProjectTask>(`${this.url(projectId)}/${taskId}`);
  }

  createTask(projectId: string, request: CreateTaskPayload): Observable<ProjectTask> {
    return this.http.post<ProjectTask>(this.url(projectId), request);
  }

  updateTask(
    projectId: string, taskId: string,
    request: UpdateTaskPayload, version: number
  ): Observable<ProjectTask> {
    const headers = new HttpHeaders({ 'If-Match': `"${version}"` });
    return this.http.put<ProjectTask>(
      `${this.url(projectId)}/${taskId}`, request, { headers });
  }

  deleteTask(projectId: string, taskId: string, version: number): Observable<void> {
    const headers = new HttpHeaders({ 'If-Match': `"${version}"` });
    return this.http.delete<void>(
      `${this.url(projectId)}/${taskId}`, { headers });
  }
}
```

---

### Frontend — Routing Update

```typescript
// features/projects/projects.routes.ts — thêm route mới
export const projectsRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./components/project-list/project-list').then(m => m.ProjectListComponent),
  },
  {
    path: ':projectId',                           // ← THÊM MỚI
    loadComponent: () =>
      import('./components/project-detail/project-detail').then(m => m.ProjectDetailComponent),
  },
];
```

```typescript
// app.config.ts — thêm tasks state và effects
import { tasksFeature } from './features/projects/store/tasks.reducer';
import { TasksEffects } from './features/projects/store/tasks.effects';

// Trong provideStore / appConfig:
provideState(tasksFeature),
provideEffects(TasksEffects),
```

```typescript
// core/store/app.state.ts — thêm tasks
import { TasksState } from '../../features/projects/store/tasks.reducer';

export interface AppState {
  router: RouterReducerState;
  projects: ProjectsState;   // đã có từ Story 1.3
  tasks: TasksState;          // ← THÊM MỚI
}
```

---

### Frontend — ProjectDetailComponent (Skeleton MVP)

```typescript
// features/projects/components/project-detail/project-detail.ts
@Component({
  standalone: true,
  selector: 'app-project-detail',
  imports: [/* mat-table hoặc mat-tree, TaskTreeComponent */],
})
export class ProjectDetailComponent {
  private store = inject(Store);
  private route = inject(ActivatedRoute);

  projectId = this.route.snapshot.paramMap.get('projectId')!;
  tasks$ = this.store.select(selectAllTasks);
  loading$ = this.store.select(selectTasksLoading);

  ngOnInit() {
    this.store.dispatch(TasksActions.loadTasks({ projectId: this.projectId }));
  }

  onAddTask() { /* Mở TaskFormComponent dialog */ }
  onEditTask(task: ProjectTask) { /* Mở TaskFormComponent dialog với task */ }
  onDeleteTask(task: ProjectTask) { /* Dispatch delete với confirmation */ }
}
```

**TaskTreeComponent** — Hiển thị flat list dạng indent tree (không cần mat-tree phức tạp cho MVP):
- Group tasks theo parentId
- Indent bằng CSS `padding-left` dựa vào depth
- Action buttons: Add Child, Edit, Delete

**TaskFormComponent** — MatDialog với:
- Fields: `type`, `vbs`, `name`, `priority`, `status`, `notes`
- Date fields: `plannedStartDate`, `plannedEndDate`, `actualStartDate`, `actualEndDate`
- `plannedEffortHours`, `percentComplete`, `assigneeUserId`
- Predecessor selector: multiple select từ danh sách tasks hiện có + dependency type dropdown
- Dùng `inject(MAT_DIALOG_DATA)` để nhận task hiện tại (edit mode) hoặc parentId (add child mode)

---

### File Structure — Tất Cả Files Cần Tạo/Cập Nhật

**Backend — Tạo mới:**
```
src/Modules/Projects/ProjectManagement.Projects.Domain/
├── Entities/
│   ├── ProjectTask.cs
│   └── TaskDependency.cs
└── Enums/
    ├── TaskType.cs
    ├── TaskPriority.cs
    ├── TaskStatus.cs
    └── DependencyType.cs

src/Modules/Projects/ProjectManagement.Projects.Application/
└── Tasks/
    ├── Commands/
    │   ├── CreateTask/CreateTaskCommand.cs + Validator + Handler
    │   ├── UpdateTask/UpdateTaskCommand.cs + Validator + Handler
    │   └── DeleteTask/DeleteTaskCommand.cs + Handler
    ├── Queries/
    │   ├── GetTasksByProject/GetTasksByProjectQuery.cs + Handler
    │   └── GetTaskById/GetTaskByIdQuery.cs + Handler
    └── DTOs/
        ├── TaskDto.cs
        └── TaskDependencyDto.cs

src/Modules/Projects/ProjectManagement.Projects.Infrastructure/
├── Persistence/
│   ├── Configurations/
│   │   ├── TaskConfiguration.cs
│   │   └── TaskDependencyConfiguration.cs
│   └── Migrations/
│       └── 20260427000000_AddProjectTasks.cs

src/Modules/Projects/ProjectManagement.Projects.Api/
└── Controllers/
    └── TasksController.cs

tests/ProjectManagement.Host.Tests/
└── TasksCrudTests.cs
```

**Backend — Cập nhật:**
```
src/Modules/Projects/ProjectManagement.Projects.Infrastructure/Persistence/ProjectsDbContext.cs ← + ProjectTasks, TaskDependencies
src/Modules/Projects/ProjectManagement.Projects.Application/Common/Interfaces/IProjectsDbContext.cs ← + 2 DbSets
src/Modules/Projects/ProjectManagement.Projects.Infrastructure/Migrations/ProjectsDbContextModelSnapshot.cs ← + 2 tables
```

**Frontend — Tạo mới:**
```
frontend/.../features/projects/models/task.model.ts
frontend/.../features/projects/store/tasks.actions.ts
frontend/.../features/projects/store/tasks.reducer.ts
frontend/.../features/projects/store/tasks.effects.ts
frontend/.../features/projects/store/tasks.selectors.ts
frontend/.../features/projects/services/tasks-api.service.ts
frontend/.../features/projects/components/project-detail/project-detail.ts (.html, .scss)
frontend/.../features/projects/components/task-tree/task-tree.ts (.html, .scss)
frontend/.../features/projects/components/task-form/task-form.ts (.html, .scss)
```

**Frontend — Cập nhật:**
```
frontend/.../app/features/projects/projects.routes.ts  ← + /projects/:projectId route
frontend/.../app/core/store/app.state.ts               ← + tasks: TasksState
frontend/.../app/app.config.ts                         ← + provideState(tasksFeature), provideEffects(TasksEffects)
```

---

### Anti-Patterns — Tránh Những Lỗi Này

**Backend:**
❌ **KHÔNG** tạo 3 bảng riêng Phase/Milestone/Task — dùng 1 bảng `project_tasks` với `type` discriminator  
❌ **KHÔNG** quên `HasQueryFilter(x => !x.IsDeleted)` trong TaskConfiguration — thiếu thì soft-deleted tasks sẽ bị trả về  
❌ **KHÔNG** store `actualEffortHours` trên ProjectTask — field này computed từ TimeEntries (Epic 3)  
❌ **KHÔNG** dùng cascade delete cho FK — dùng `Restrict` để force explicit delete children trước  
❌ **KHÔNG** tạo lại `GlobalExceptionMiddleware`, `ETagHelper`, `ConflictException` — đã có sẵn  
❌ **KHÔNG** trả 403 cho non-member — phải là 404 (EnsureMemberAsync → NotFoundException)  
❌ **KHÔNG** skip cycle detection — cả hierarchy cycle và dependency cycle phải check trong UpdateTaskHandler  
❌ **KHÔNG** parse enum trực tiếp mà không validate — nếu `Enum.Parse` fail, cần trả 400, không phải 500  
❌ **KHÔNG** quên remove old dependencies trước khi add new ones trong UpdateTaskHandler  
❌ **KHÔNG** dùng `int` version nếu `ETagHelper.ParseIfMatch` trả `long?` — cast an toàn: `(int)version`

**Frontend:**
❌ **KHÔNG** access `err.error.extensions.current` — `current` và `eTag` ở **ROOT level**  
❌ **KHÔNG** tạo NgModule mới — standalone components  
❌ **KHÔNG** call TasksApiService trực tiếp từ component — dispatch action  
❌ **KHÔNG** tạo lại `ConflictDialogComponent` — tái dùng từ `shared/components/conflict-dialog/`  
❌ **KHÔNG** quên `provideState` và `provideEffects` trong `app.config.ts` — thiếu thì store không hoạt động  
❌ **KHÔNG** cố gắng build tree trong reducer — trả flat list từ server, build tree trong selector hoặc component  

---

### Test Pattern — Integration Test (Backend)

**Dùng pattern WebApplicationFactory giống Stories 1.1–1.3:**

```csharp
// tests/ProjectManagement.Host.Tests/TasksCrudTests.cs
public sealed class TasksCrudTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task CreateTask_ValidPayload_Returns201WithETag()
    {
        // Arrange: login, create project (để có projectId + membership)
        // Act: POST /api/v1/projects/{projectId}/tasks
        // Assert: 201, ETag header, task in body
    }

    [Fact]
    public async Task CreateTask_NonMember_Returns404()
    {
        // Arrange: login as user A, create project, login as user B
        // Act: user B POST tasks → 404
    }

    [Fact]
    public async Task UpdateTask_StaleETag_Returns409WithCurrentState()
    {
        // Arrange: create task (version=1), update once (version=2)
        // Act: update with version=1 → 409
        // Assert: body.current != null (root level), body.eTag != null (root level)
    }

    [Fact]
    public async Task DeleteTask_HasChildren_Returns422()
    {
        // Arrange: create parent task, create child task
        // Act: delete parent → 422 DomainException
    }

    [Fact]
    public async Task UpdateTask_ParentCycle_Returns422()
    {
        // Arrange: A → B (B is child of A)
        // Act: update A with parentId = B → DomainException (422)
    }

    [Fact]
    public async Task CreateTask_DependencyCycle_Returns422()
    {
        // Arrange: A → B (B is successor of A), create dependency B→A (cycle)
        // Act: create dep with predecessorId=B on task A → 422
    }
}
```

**Lưu ý quan trọng với integration tests:**
- Cần tạo project trước (Story 1.3 pattern: POST /api/v1/projects)
- Creator tự động là member → có thể thao tác tasks ngay
- DB-dependent tests fail nếu không có PostgreSQL — **expected behavior**, không phải bug

---

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 1.4]
- [Source: _bmad-output/planning-artifacts/architecture.md#3.1 Data Model]
- [Source: _bmad-output/planning-artifacts/architecture.md#5.2 Structure Patterns]
- [Source: _bmad-output/planning-artifacts/architecture.md#AD-06 Conflict Resolution]
- [Source: _bmad-output/planning-artifacts/architecture.md#7.1 Backend Structure]
- [Source: _bmad-output/implementation-artifacts/1-3-projects-crud-create-list-detail-update-archive-with-optimistic-locking-etag-if-match-409.md#Dev Notes]
- [Source: src/Modules/Projects/ProjectManagement.Projects.Domain/Entities/Project.cs]
- [Source: src/Modules/Projects/ProjectManagement.Projects.Infrastructure/Persistence/ProjectsDbContext.cs]
- [Source: src/Shared/ProjectManagement.Shared.Domain/Exceptions/ConflictException.cs]
- [Source: src/Shared/ProjectManagement.Shared.Infrastructure/OptimisticLocking/ETagHelper.cs]

---

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

- **Fix 1**: `UpdateTaskHandler`/`DeleteTaskHandler` ban đầu import `ETagHelper` từ `Shared.Infrastructure` — Application project không reference Infrastructure → đổi thành `$"\"{task.Version}\""` inline
- **Fix 2**: `ProjectsDbContextModelSnapshot.cs` ban đầu dùng `Successors2` (non-existent navigation) và `HasCheckConstraint` → sửa thành `Successors`, bỏ constraint
- **Fix 3**: `task-tree.html` dùng `*ngIf` structural directive (cần `NgIf` import) → đổi sang `@if` Angular 17+ control flow
- **Fix 4**: `tasks.selectors.ts` dùng `tasksFeature.selectTasksState` (typed `EntityState<ProjectTask>`, thiếu extra fields) → đổi về `(state: AppState) => state.tasks`
- **Fix 5**: `tasks.reducer.ts` dùng `createFeature` → TypeScript type `ActionReducer<EntityState<ProjectTask>>` không assignable to `ActionReducer<TasksState>` → đổi sang `createReducer` trực tiếp với `const initialState: TasksState`
- **Fix 6**: Enum `TaskStatus` → đổi thành `ProjectTaskStatus` để tránh collision với `System.Threading.Tasks.TaskStatus`
- **Build note**: Host project build dùng `--configuration Release` để tránh file lock conflict với dev server đang chạy

### Completion Notes List

- Tất cả 10 tasks và 35 subtasks đã implement đầy đủ
- Backend: Domain entities, EF config, migration snapshot, CQRS handlers với BFS cycle detection, API controller với ETag/If-Match
- Frontend: NgRx store (actions/reducer/effects/selectors), TasksApiService, 3 components (ProjectDetail/TaskTree/TaskForm), routing
- Integration tests (13 test cases) viết đầy đủ trong `TasksCrudTests.cs` — require PostgreSQL để chạy (500 nếu không có DB — expected behavior, tương tự ProjectsCrudTests)
- Frontend build pass `ng build --configuration development` thành công
- `actualEffortHours` luôn trả `null` — computed từ TimeEntries Epic 3, không lưu trên entity

### File List

**Backend — New files:**
- `src/Modules/Projects/ProjectManagement.Projects.Domain/Enums/TaskType.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Domain/Enums/TaskPriority.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Domain/Enums/ProjectTaskStatus.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Domain/Enums/DependencyType.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Domain/Entities/ProjectTask.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Domain/Entities/TaskDependency.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Infrastructure/Persistence/Configurations/TaskConfiguration.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Infrastructure/Persistence/Configurations/TaskDependencyConfiguration.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Infrastructure/Persistence/Migrations/20260427000000_AddProjectTasks.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Application/DTOs/TaskDto.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Application/DTOs/TaskDependencyDto.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Application/Tasks/Commands/CreateTask/CreateTaskCommand.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Application/Tasks/Commands/CreateTask/CreateTaskCommandValidator.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Application/Tasks/Commands/CreateTask/CreateTaskHandler.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Application/Tasks/Commands/UpdateTask/UpdateTaskCommand.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Application/Tasks/Commands/UpdateTask/UpdateTaskCommandValidator.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Application/Tasks/Commands/UpdateTask/UpdateTaskHandler.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Application/Tasks/Commands/DeleteTask/DeleteTaskCommand.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Application/Tasks/Commands/DeleteTask/DeleteTaskHandler.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Application/Tasks/Queries/GetTasksByProject/GetTasksByProjectQuery.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Application/Tasks/Queries/GetTasksByProject/GetTasksByProjectHandler.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Application/Tasks/Queries/GetTaskById/GetTaskByIdQuery.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Application/Tasks/Queries/GetTaskById/GetTaskByIdHandler.cs`
- `src/Host/ProjectManagement.Host/Controllers/TasksController.cs`
- `tests/ProjectManagement.Host.Tests/TasksCrudTests.cs`
- `frontend/project-management-web/src/app/features/projects/models/task.model.ts`
- `frontend/project-management-web/src/app/features/projects/store/tasks.actions.ts`
- `frontend/project-management-web/src/app/features/projects/store/tasks.reducer.ts`
- `frontend/project-management-web/src/app/features/projects/store/tasks.effects.ts`
- `frontend/project-management-web/src/app/features/projects/store/tasks.selectors.ts`
- `frontend/project-management-web/src/app/features/projects/services/tasks-api.service.ts`
- `frontend/project-management-web/src/app/features/projects/components/project-detail/project-detail.ts`
- `frontend/project-management-web/src/app/features/projects/components/project-detail/project-detail.html`
- `frontend/project-management-web/src/app/features/projects/components/project-detail/project-detail.scss`
- `frontend/project-management-web/src/app/features/projects/components/task-tree/task-tree.ts`
- `frontend/project-management-web/src/app/features/projects/components/task-tree/task-tree.html`
- `frontend/project-management-web/src/app/features/projects/components/task-tree/task-tree.scss`
- `frontend/project-management-web/src/app/features/projects/components/task-form/task-form.ts`
- `frontend/project-management-web/src/app/features/projects/components/task-form/task-form.html`
- `frontend/project-management-web/src/app/features/projects/components/task-form/task-form.scss`

**Backend — Modified files:**
- `src/Modules/Projects/ProjectManagement.Projects.Application/Common/Interfaces/IProjectsDbContext.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Infrastructure/Persistence/ProjectsDbContext.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Infrastructure/Persistence/Migrations/ProjectsDbContextModelSnapshot.cs`

**Frontend — Modified files:**
- `frontend/project-management-web/src/app/features/projects/projects.routes.ts`
- `frontend/project-management-web/src/app/core/store/app.state.ts`
- `frontend/project-management-web/src/app/app.config.ts`
