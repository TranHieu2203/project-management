# Story 8.0: Issue Table Migration — Expand-Contract (tasks → issues + view backward compat)

Status: ready-for-dev

**Story ID:** 8.0
**Epic:** Epic 8 — Issue Model Migration + Agile Foundation
**Sprint:** Sprint 10
**Date Created:** 2026-04-29
**BLOCKER:** Tất cả Epic 8-15 stories phụ thuộc vào story này phải complete trước

---

## Story

As a developer,
I want migrate bảng `project_tasks` sang bảng `issues` theo pattern expand-contract mà không gián đoạn dữ liệu hiện có,
So that hệ thống hỗ trợ đa kiểu issue trong khi các module cũ (Gantt, Reports, TimeTracking) vẫn hoạt động bình thường qua backward-compat view.

---

## Acceptance Criteria

1. **Given** bảng `project_tasks` tồn tại với dữ liệu hiện có
   **When** chạy Phase 1 migration (V008_001)
   **Then** bảng `issue_type_definitions` được tạo và bảng `project_tasks` có thêm 8 cột mới (discriminator, issue_type_id, issue_key, parent_issue_id, custom_fields, workflow_state_id, story_points, reporter_user_id) — tất cả nullable
   **And** mọi integration test vẫn pass (không regression)

2. **Given** Phase 1 đã chạy
   **When** chạy Phase 2 migration (V008_002) backfill
   **Then** mọi row có `discriminator` được set từ cột `type` hiện tại
   **And** mọi row có `issue_key` dạng `{project.code}-{sequence_number}` (e.g. "PM-1", "PM-2")
   **And** không còn row nào có `discriminator IS NULL` hoặc `issue_key IS NULL`

3. **Given** Phase 2 đã chạy
   **When** chạy Phase 3 migration (V008_003)
   **Then** bảng `project_tasks` được rename thành `issues`
   **And** view `project_tasks` được tạo — trả toàn bộ data từ `issues` (backward compat)
   **And** cột `task_id` trong `time_entries` được rename thành `issue_id` (nếu tồn tại)
   **And** `TaskConfiguration.cs` map sang `b.ToTable("issues")`
   **And** mọi handler dùng `context.Issues` (thay vì `context.ProjectTasks`)
   **And** mọi integration test vẫn pass

4. **Given** tất cả Phase 1-3 stable (>= 1 tuần trên staging) và pre-conditions met
   **When** chạy Phase 4 migration (V008_004)
   **Then** view `project_tasks` bị drop
   **And** `discriminator` và `issue_key` là NOT NULL
   **And** `issue_type_id` là NOT NULL (sau khi seed V008_005 và update rows)
   **And** GIN indexes cho `custom_fields` và `search_vector` được tạo
   **And** `search_vector` generated column được tạo (tsvector từ name + description + issue_key)

5. **Given** Phase 1 đã chạy (issue_type_definitions table tồn tại)
   **When** chạy Phase 5 seed (V008_005)
   **Then** 5 built-in issue types tồn tại: Epic (#7C3AED), Story (#059669), Task (#2563EB), Bug (#DC2626), Sub-task (#6B7280)
   **And** tất cả is_built_in=true, is_deletable=false

---

## ⚠️ CRITICAL: Hiện trạng codebase

### Files cần sửa trong Projects module:

**Domain:**
- `ProjectManagement.Projects.Domain/Entities/ProjectTask.cs` — thêm 8 properties mới (nullable)

**Application:**
- `ProjectManagement.Projects.Application/Common/Interfaces/IProjectsDbContext.cs` — rename `ProjectTasks` → `Issues`, thêm `IssueTypeDefinitions`
- `ProjectManagement.Projects.Application/DTOs/TaskDto.cs` — thêm `IssueKey`, `Discriminator`, `StoryPoints`, `IssueTypeId`
- `ProjectManagement.Projects.Application/Tasks/Commands/CreateTask/CreateTaskHandler.cs` — set `discriminator`, generate `issue_key` atomically
- `ProjectManagement.Projects.Application/Tasks/Commands/UpdateTask/UpdateTaskHandler.cs` — dùng `context.Issues`
- `ProjectManagement.Projects.Application/Tasks/Commands/DeleteTask/DeleteTaskHandler.cs` — dùng `context.Issues`
- `ProjectManagement.Projects.Application/Tasks/Queries/GetTasksByProject/GetTasksByProjectHandler.cs` — dùng `context.Issues`
- `ProjectManagement.Projects.Application/Tasks/Queries/GetTaskById/GetTaskByIdHandler.cs` — dùng `context.Issues`
- `ProjectManagement.Projects.Application/Tasks/Queries/GetMyTasks/GetMyTasksHandler.cs` — dùng `context.Issues`

**Infrastructure:**
- `ProjectManagement.Projects.Infrastructure/Persistence/ProjectsDbContext.cs` — rename DbSet, thêm IssueTypeDefinitions
- `ProjectManagement.Projects.Infrastructure/Persistence/Configurations/TaskConfiguration.cs` — `b.ToTable("issues")` + 8 properties mới + indexes mới
- `ProjectManagement.Projects.Infrastructure/Migrations/ProjectsDbContextModelSnapshot.cs` — cập nhật snapshot

**Mới tạo:**
- `ProjectManagement.Projects.Domain/Entities/IssueTypeDefinition.cs` — entity mới
- `ProjectManagement.Projects.Infrastructure/Persistence/Configurations/IssueTypeDefinitionConfiguration.cs` — EF config mới
- `ProjectManagement.Projects.Infrastructure/Migrations/YYYYMMDDHHMMSS_V008_001_ExpandIssueColumns.cs`
- `ProjectManagement.Projects.Infrastructure/Migrations/YYYYMMDDHHMMSS_V008_002_BackfillDiscriminatorAndIssueKey.cs`
- `ProjectManagement.Projects.Infrastructure/Migrations/YYYYMMDDHHMMSS_V008_003_RenameToIssues_CreateCompatView.cs`
- `ProjectManagement.Projects.Infrastructure/Migrations/YYYYMMDDHHMMSS_V008_004_ContractCleanup.cs`
- `ProjectManagement.Projects.Infrastructure/Migrations/YYYYMMDDHHMMSS_V008_005_SeedBuiltInIssueTypes.cs`

---

## Tasks / Subtasks

### Phase 1 — Expand (add columns, non-breaking)

- [ ] **Task 1: Tạo IssueTypeDefinition entity** (AC: 5)
  - [ ] 1.1 Tạo `ProjectManagement.Projects.Domain/Entities/IssueTypeDefinition.cs`:
    ```csharp
    namespace ProjectManagement.Projects.Domain.Entities;

    public class IssueTypeDefinition : AuditableEntity
    {
        public string Name { get; private set; } = string.Empty;
        public string IconKey { get; private set; } = string.Empty;
        public string Color { get; private set; } = string.Empty;    // #RRGGBB
        public bool IsBuiltIn { get; private set; }
        public bool IsDeletable { get; private set; }
        public Guid? ProjectId { get; private set; }    // NULL = global built-in
        public int SortOrder { get; private set; }

        public static IssueTypeDefinition CreateBuiltIn(
            Guid id, string name, string iconKey, string color, int sortOrder) => new()
        {
            Id = id,
            Name = name,
            IconKey = iconKey,
            Color = color,
            IsBuiltIn = true,
            IsDeletable = false,
            ProjectId = null,
            SortOrder = sortOrder,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "system",
        };
    }
    ```
  - [ ] 1.2 Verify `AuditableEntity` base có: `Id`, `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`, `IsDeleted` — xem `ProjectTask.cs` làm reference

- [ ] **Task 2: Thêm 8 properties mới vào ProjectTask entity** (AC: 1)
  - [ ] 2.1 Sửa `ProjectManagement.Projects.Domain/Entities/ProjectTask.cs` — thêm sau `public int Version`:
    ```csharp
    // Phase 8.0 — Issue model expansion (all nullable until Phase 4)
    public string? Discriminator { get; private set; }
    public Guid? IssueTypeId { get; private set; }
    public string? IssueKey { get; private set; }
    public Guid? ParentIssueId { get; private set; }
    public string? CustomFields { get; private set; }   // JSONB stored as string
    public Guid? WorkflowStateId { get; private set; }
    public int? StoryPoints { get; private set; }
    public Guid? ReporterUserId { get; private set; }
    ```
  - [ ] 2.2 Update `Create()` factory để set `Discriminator` từ `TaskType` enum:
    ```csharp
    Discriminator = type.ToString(),  // "Phase", "Milestone", "Task"
    ReporterUserId = assigneeUserId,  // tạm thời = creator, Story 8.7 sẽ refine
    ```

- [ ] **Task 3: Tạo EF configs cho IssueTypeDefinition và cập nhật TaskConfiguration** (AC: 1, 3)
  - [ ] 3.1 Tạo `IssueTypeDefinitionConfiguration.cs`:
    ```csharp
    public sealed class IssueTypeDefinitionConfiguration : IEntityTypeConfiguration<IssueTypeDefinition>
    {
        public void Configure(EntityTypeBuilder<IssueTypeDefinition> b)
        {
            b.ToTable("issue_type_definitions");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            b.Property(x => x.IconKey).HasColumnName("icon_key").HasMaxLength(50).IsRequired();
            b.Property(x => x.Color).HasColumnName("color").HasMaxLength(7).IsRequired();
            b.Property(x => x.IsBuiltIn).HasColumnName("is_built_in").HasDefaultValue(false);
            b.Property(x => x.IsDeletable).HasColumnName("is_deletable").HasDefaultValue(true);
            b.Property(x => x.ProjectId).HasColumnName("project_id").IsRequired(false);
            b.Property(x => x.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);
            b.Property(x => x.CreatedAt).HasColumnName("created_at");
            b.Property(x => x.CreatedBy).HasColumnName("created_by").HasMaxLength(450);
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            b.Property(x => x.UpdatedBy).HasColumnName("updated_by").HasMaxLength(450);
            b.Property(x => x.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
            b.HasQueryFilter(x => !x.IsDeleted);
        }
    }
    ```
  - [ ] 3.2 Cập nhật `TaskConfiguration.cs` — **PHASE 3** target: đổi `b.ToTable("project_tasks")` → `b.ToTable("issues")`, thêm 8 property mappings:
    ```csharp
    // QUAN TRỌNG: đổi sang "issues" ngay từ bây giờ (Phase 3 forward)
    b.ToTable("issues");

    // 8 properties mới — nullable
    b.Property(x => x.Discriminator).HasColumnName("discriminator").HasMaxLength(50).IsRequired(false);
    b.Property(x => x.IssueTypeId).HasColumnName("issue_type_id").IsRequired(false);
    b.Property(x => x.IssueKey).HasColumnName("issue_key").HasMaxLength(20).IsRequired(false);
    b.Property(x => x.ParentIssueId).HasColumnName("parent_issue_id").IsRequired(false);
    b.Property(x => x.CustomFields).HasColumnName("custom_fields").HasColumnType("jsonb").IsRequired(false);
    b.Property(x => x.WorkflowStateId).HasColumnName("workflow_state_id").IsRequired(false);
    b.Property(x => x.StoryPoints).HasColumnName("story_points").IsRequired(false);
    b.Property(x => x.ReporterUserId).HasColumnName("reporter_user_id").IsRequired(false);

    // Indexes — đổi tên sang issues
    b.HasIndex(x => x.ProjectId).HasDatabaseName("ix_issues_project_id");
    b.HasIndex(x => x.ParentId).HasDatabaseName("ix_issues_parent_id");
    b.HasIndex(x => new { x.ProjectId, x.SortOrder }).HasDatabaseName("ix_issues_project_sort");
    ```
  - [ ] 3.3 **LƯU Ý QUAN TRỌNG**: Thứ tự apply migrations quyết định thứ tự của database:
    - EF Core config sẽ target `issues` table ngay sau khi Phase 3 migration chạy
    - Trước khi Phase 3 migration chạy, EF vẫn đọc qua view `project_tasks` (backward compat)

- [ ] **Task 4: Cập nhật IProjectsDbContext và ProjectsDbContext** (AC: 3)
  - [ ] 4.1 Sửa `IProjectsDbContext.cs`:
    ```csharp
    DbSet<ProjectTask> Issues { get; }              // was: ProjectTasks
    DbSet<IssueTypeDefinition> IssueTypeDefinitions { get; }   // mới
    // Giữ nguyên: Projects, ProjectMemberships, TaskDependencies
    ```
  - [ ] 4.2 Sửa `ProjectsDbContext.cs`:
    ```csharp
    public DbSet<ProjectTask> Issues => Set<ProjectTask>();          // was: ProjectTasks
    public DbSet<IssueTypeDefinition> IssueTypeDefinitions => Set<IssueTypeDefinition>();
    // OnModelCreating: thêm ApplyConfiguration(new IssueTypeDefinitionConfiguration())
    ```

- [ ] **Task 5: Cập nhật tất cả handlers để dùng context.Issues** (AC: 3)
  - [ ] 5.1 `CreateTaskHandler.cs` — đổi `_db.ProjectTasks` → `_db.Issues`; thêm logic generate `issue_key`:
    ```csharp
    // Generate issue_key — pattern: {project.code}-{count+1}
    // PHẢI dùng database transaction để tránh race condition
    // Cách đơn giản (acceptable cho Phase 1): count existing + 1
    var existingCount = await _db.Issues
        .IgnoreQueryFilters()  // count kể cả deleted
        .CountAsync(t => t.ProjectId == cmd.ProjectId, ct);
    var issueKey = $"{project.Code}-{existingCount + 1}";
    ```
    Lưu ý: Cần load `Project` để lấy `code`. Query project trước:
    ```csharp
    var project = await _db.Projects.FirstAsync(p => p.Id == cmd.ProjectId, ct);
    ```
  - [ ] 5.2 `UpdateTaskHandler.cs` — đổi `_db.ProjectTasks` → `_db.Issues`
  - [ ] 5.3 `DeleteTaskHandler.cs` — đổi `_db.ProjectTasks` → `_db.Issues`
  - [ ] 5.4 `GetTasksByProjectHandler.cs` — đổi `_db.ProjectTasks` → `_db.Issues`
  - [ ] 5.5 `GetTaskByIdHandler.cs` — đổi `_db.ProjectTasks` → `_db.Issues`
  - [ ] 5.6 `GetMyTasksHandler.cs` — đổi `_db.ProjectTasks` → `_db.Issues`

- [ ] **Task 6: Cập nhật TaskDto và MapToDto** (AC: 3)
  - [ ] 6.1 Sửa `TaskDto.cs` — thêm fields:
    ```csharp
    public record TaskDto(
        Guid Id,
        Guid ProjectId,
        Guid? ParentId,
        string Type,
        string? Vbs,
        string Name,
        string Priority,
        string Status,
        string? Notes,
        DateOnly? PlannedStartDate,
        DateOnly? PlannedEndDate,
        DateOnly? ActualStartDate,
        DateOnly? ActualEndDate,
        decimal? PlannedEffortHours,
        decimal? ActualEffortHours,
        decimal? PercentComplete,
        Guid? AssigneeUserId,
        int SortOrder,
        int Version,
        List<TaskDependencyDto> Predecessors,
        // Phase 8.0 — new fields (nullable until Phase 4 completes)
        string? IssueKey = null,
        string? Discriminator = null,
        int? StoryPoints = null,
        Guid? IssueTypeId = null,
        Guid? ReporterUserId = null,
        bool IsFilterMatch = true);
    ```
  - [ ] 6.2 Cập nhật `MapToDto()` trong `CreateTaskHandler`, `GetTasksByProjectHandler`, `GetTaskByIdHandler`, `GetMyTasksHandler` — thêm các fields mới vào mapping

---

### Phase 2 — Migrations (SQL scripts)

- [ ] **Task 7: Tạo Migration V008_001 — Expand columns** (AC: 1)
  - [ ] 7.1 Tạo file migration với timestamp hiện tại (format: `YYYYMMDDHHmmss_V008_001_ExpandIssueColumns.cs`):
    ```csharp
    public partial class V008_001_ExpandIssueColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                -- V008_001: Add new columns to project_tasks (non-breaking — all nullable)
                CREATE TABLE IF NOT EXISTS issue_type_definitions (
                    id           UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                    name         VARCHAR(100)  NOT NULL,
                    icon_key     VARCHAR(50)   NOT NULL,
                    color        VARCHAR(7)    NOT NULL,
                    is_built_in  BOOLEAN       NOT NULL DEFAULT false,
                    is_deletable BOOLEAN       NOT NULL DEFAULT true,
                    project_id   UUID          NULL REFERENCES projects(id) ON DELETE CASCADE,
                    sort_order   INT           NOT NULL DEFAULT 0,
                    created_at   TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
                    created_by   VARCHAR(450)  NOT NULL DEFAULT 'system',
                    updated_at   TIMESTAMPTZ   NULL,
                    updated_by   VARCHAR(450)  NULL,
                    is_deleted   BOOLEAN       NOT NULL DEFAULT false
                );

                CREATE UNIQUE INDEX IF NOT EXISTS uq_issue_type_definitions_name_project
                    ON issue_type_definitions(COALESCE(project_id::text, ''), name)
                    WHERE is_deleted = false;

                ALTER TABLE project_tasks
                    ADD COLUMN IF NOT EXISTS discriminator     VARCHAR(50)  NULL,
                    ADD COLUMN IF NOT EXISTS issue_type_id    UUID         NULL REFERENCES issue_type_definitions(id),
                    ADD COLUMN IF NOT EXISTS issue_key        VARCHAR(20)  NULL,
                    ADD COLUMN IF NOT EXISTS parent_issue_id  UUID         NULL REFERENCES project_tasks(id),
                    ADD COLUMN IF NOT EXISTS custom_fields    JSONB        NULL,
                    ADD COLUMN IF NOT EXISTS workflow_state_id UUID        NULL,
                    ADD COLUMN IF NOT EXISTS story_points     INT          NULL,
                    ADD COLUMN IF NOT EXISTS reporter_user_id UUID         NULL REFERENCES users(id);

                CREATE UNIQUE INDEX IF NOT EXISTS uq_project_tasks_issue_key
                    ON project_tasks(issue_key)
                    WHERE issue_key IS NOT NULL;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS uq_project_tasks_issue_key;
                ALTER TABLE project_tasks
                    DROP COLUMN IF EXISTS reporter_user_id,
                    DROP COLUMN IF EXISTS story_points,
                    DROP COLUMN IF EXISTS workflow_state_id,
                    DROP COLUMN IF EXISTS custom_fields,
                    DROP COLUMN IF EXISTS parent_issue_id,
                    DROP COLUMN IF EXISTS issue_key,
                    DROP COLUMN IF EXISTS issue_type_id,
                    DROP COLUMN IF EXISTS discriminator;
                DROP INDEX IF EXISTS uq_issue_type_definitions_name_project;
                DROP TABLE IF EXISTS issue_type_definitions;
                """);
        }
    }
    ```

- [ ] **Task 8: Tạo Migration V008_002 — Backfill** (AC: 2)
  - [ ] 8.1 Tạo file migration `V008_002_BackfillDiscriminatorAndIssueKey.cs`:
    ```csharp
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            -- V008_002: Idempotent backfill — safe to run multiple times
            BEGIN;

            -- Step 1: Set discriminator for all existing rows where NULL
            UPDATE project_tasks
            SET discriminator = type
            WHERE discriminator IS NULL;

            -- Verify
            DO $$
            BEGIN
                IF EXISTS (SELECT 1 FROM project_tasks WHERE discriminator IS NULL) THEN
                    RAISE EXCEPTION 'Backfill incomplete: discriminator still NULL';
                END IF;
            END $$;

            -- Step 2: Generate issue_key = {project.code}-{per-project sequence}
            UPDATE project_tasks pt
            SET issue_key = p.code || '-' || rn.row_num
            FROM (
                SELECT
                    id,
                    ROW_NUMBER() OVER (PARTITION BY project_id ORDER BY created_at ASC, id ASC) AS row_num
                FROM project_tasks
                WHERE issue_key IS NULL
            ) rn
            JOIN projects p ON p.id = pt.project_id
            WHERE pt.id = rn.id
              AND pt.issue_key IS NULL;

            -- Verify
            DO $$
            BEGIN
                IF EXISTS (SELECT 1 FROM project_tasks WHERE issue_key IS NULL) THEN
                    RAISE EXCEPTION 'Backfill incomplete: issue_key still NULL';
                END IF;
            END $$;

            COMMIT;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE project_tasks SET discriminator = NULL, issue_key = NULL;
            """);
    }
    ```

- [ ] **Task 9: Tạo Migration V008_003 — Rename + Compat View** (AC: 3)
  - [ ] 9.1 Tạo file migration `V008_003_RenameToIssues_CreateCompatView.cs`:
    ```csharp
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            -- V008_003: Rename project_tasks → issues, create backward-compat view
            ALTER TABLE project_tasks RENAME TO issues;

            -- Rename indexes
            ALTER INDEX IF EXISTS ix_project_tasks_project_id   RENAME TO ix_issues_project_id;
            ALTER INDEX IF EXISTS ix_project_tasks_parent_id    RENAME TO ix_issues_parent_id;
            ALTER INDEX IF EXISTS ix_project_tasks_project_sort RENAME TO ix_issues_project_sort;
            ALTER INDEX IF EXISTS uq_project_tasks_issue_key    RENAME TO uq_issues_issue_key;

            -- Create backward-compat view (includes ALL columns)
            CREATE VIEW project_tasks AS
                SELECT * FROM issues;

            -- Rename task_id → issue_id in time_entries (if column exists)
            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_name = 'time_entries' AND column_name = 'task_id'
                ) THEN
                    ALTER TABLE time_entries RENAME COLUMN task_id TO issue_id;
                END IF;
            END $$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP VIEW IF EXISTS project_tasks;
            ALTER TABLE issues RENAME TO project_tasks;
            ALTER INDEX IF EXISTS ix_issues_project_id    RENAME TO ix_project_tasks_project_id;
            ALTER INDEX IF EXISTS ix_issues_parent_id     RENAME TO ix_project_tasks_parent_id;
            ALTER INDEX IF EXISTS ix_issues_project_sort  RENAME TO ix_project_tasks_project_sort;
            ALTER INDEX IF EXISTS uq_issues_issue_key     RENAME TO uq_project_tasks_issue_key;
            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_name = 'time_entries' AND column_name = 'issue_id'
                ) THEN
                    ALTER TABLE time_entries RENAME COLUMN issue_id TO task_id;
                END IF;
            END $$;
            """);
    }
    ```

- [ ] **Task 10: Tạo Migration V008_004 — Contract/Cleanup** (AC: 4)
  - [ ] 10.1 Tạo file migration `V008_004_ContractCleanup.cs`:
    ```csharp
    // ⚠️ PRE-CONDITIONS: chỉ chạy khi tất cả pre-conditions met (xem checklist bên dưới)
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            BEGIN;

            -- 1. Drop backward-compat view
            DROP VIEW IF EXISTS project_tasks;

            -- 2. Assign issue_type_id từ built-in types trước khi enforce NOT NULL
            UPDATE issues i
            SET issue_type_id = (
                SELECT id FROM issue_type_definitions
                WHERE name = i.discriminator AND is_built_in = true
                LIMIT 1
            )
            WHERE i.issue_type_id IS NULL;

            -- 3. Enforce NOT NULL constraints
            ALTER TABLE issues ALTER COLUMN discriminator  SET NOT NULL;
            ALTER TABLE issues ALTER COLUMN issue_key      SET NOT NULL;
            ALTER TABLE issues ALTER COLUMN issue_type_id  SET NOT NULL;

            -- 4. GIN index for JSONB custom_fields
            CREATE INDEX IF NOT EXISTS idx_issues_custom_fields_gin
                ON issues USING gin(custom_fields);

            -- 5. Generated tsvector for full-text search
            ALTER TABLE issues
                ADD COLUMN IF NOT EXISTS search_vector tsvector
                    GENERATED ALWAYS AS (
                        to_tsvector('simple',
                            coalesce(name, '')        || ' ' ||
                            coalesce(notes, '')       || ' ' ||
                            coalesce(issue_key, '')
                        )
                    ) STORED;

            CREATE INDEX IF NOT EXISTS idx_issues_search_gin
                ON issues USING gin(search_vector);

            -- 6. Additional structural indexes
            CREATE INDEX IF NOT EXISTS ix_issues_discriminator ON issues(discriminator);
            CREATE INDEX IF NOT EXISTS ix_issues_issue_type_id ON issues(issue_type_id);

            COMMIT;
            """);
    }
    ```
  - [ ] 10.2 **KHÔNG chạy V008_004 trong story này** — Phase 4 là cleanup chạy sau khi Phase 1-3 stable trên staging >= 1 tuần. Chỉ cần tạo migration file sẵn.

- [ ] **Task 11: Tạo Migration V008_005 — Seed built-in issue types** (AC: 5)
  - [ ] 11.1 Tạo file migration `V008_005_SeedBuiltInIssueTypes.cs` (chạy sau V008_001):
    ```csharp
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT INTO issue_type_definitions
                (id, name, icon_key, color, is_built_in, is_deletable, project_id, sort_order, created_by)
            VALUES
                ('00000000-0000-0000-0000-000000000001', 'Epic',     'epic',    '#7C3AED', true, false, NULL, 1, 'system'),
                ('00000000-0000-0000-0000-000000000002', 'Story',    'story',   '#059669', true, false, NULL, 2, 'system'),
                ('00000000-0000-0000-0000-000000000003', 'Task',     'task',    '#2563EB', true, false, NULL, 3, 'system'),
                ('00000000-0000-0000-0000-000000000004', 'Bug',      'bug',     '#DC2626', true, false, NULL, 4, 'system'),
                ('00000000-0000-0000-0000-000000000005', 'Sub-task', 'subtask', '#6B7280', true, false, NULL, 5, 'system')
            ON CONFLICT (id) DO NOTHING;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE FROM issue_type_definitions
            WHERE id IN (
                '00000000-0000-0000-0000-000000000001',
                '00000000-0000-0000-0000-000000000002',
                '00000000-0000-0000-0000-000000000003',
                '00000000-0000-0000-0000-000000000004',
                '00000000-0000-0000-0000-000000000005'
            );
            """);
    }
    ```

- [ ] **Task 12: Cập nhật ProjectsDbContextModelSnapshot** (AC: 1, 3)
  - [ ] 12.1 Cập nhật snapshot để reflect: table name → "issues", 8 new columns, IssueTypeDefinition entity
  - [ ] 12.2 **LƯU Ý**: Snapshot là manually-maintained trong project này (không dùng `dotnet ef migrations add`). Xem pattern trong `ProjectsDbContextModelSnapshot.cs` hiện tại để maintain đúng format.

---

### Phase 3 — Verification & Tests

- [ ] **Task 13: Tests** (AC: 1–5)
  - [ ] 13.1 Unit test — `ProjectTask.Create()` sets `Discriminator = type.ToString()`:
    ```csharp
    // ProjectTaskTests.cs
    [Fact]
    public void Create_SetsDiscriminatorFromType()
    {
        var task = ProjectTask.Create(projectId, null, TaskType.Task, ...);
        task.Discriminator.Should().Be("Task");
    }
    ```
  - [ ] 13.2 Unit test — `CreateTaskHandler` generates `issue_key` in format `{code}-{n}`:
    - Mock `_db.Issues` với 2 existing tasks cho project "PM"
    - Assert kết quả có `IssueKey == "PM-3"`
  - [ ] 13.3 Integration test — V008_001: 8 new columns exist và nullable:
    ```sql
    SELECT column_name, is_nullable FROM information_schema.columns
    WHERE table_name = 'issues' -- sau khi Phase 3
      AND column_name IN ('discriminator','issue_type_id','issue_key','parent_issue_id','custom_fields','workflow_state_id','story_points','reporter_user_id');
    -- Expect: 8 rows
    ```
  - [ ] 13.4 Integration test — V008_002: 0 rows với discriminator IS NULL sau backfill
  - [ ] 13.5 Integration test — V008_003: `issues` table tồn tại, view `project_tasks` hoạt động
  - [ ] 13.6 Integration test — V008_005: 5 built-in types với đúng ID và color
  - [ ] 13.7 Integration test — `POST /api/projects/{id}/tasks` trả response có `issueKey` field
  - [ ] 13.8 Integration test — `GET /api/projects/{id}/tasks` trả `issueKey`, `discriminator`

- [ ] **Task 14: Browser verification (QT-02)** (AC: 1, 3)
  - [ ] 14.1 `dotnet run` API + `ng serve` frontend
  - [ ] 14.2 Verify Gantt view vẫn load đúng tasks sau khi rename (view backward compat)
  - [ ] 14.3 Tạo task mới — verify `issue_key` có trong response (check DevTools Network tab)
  - [ ] 14.4 Chạy toàn bộ integration test suite — expect 0 failures

---

## Dev Notes

### Migration Order (PHẢI theo đúng thứ tự)

```
V008_001 → V008_005 → V008_002 → V008_003 → [V008_004 — DEFER]
```

- V008_001: tạo `issue_type_definitions` table + thêm columns (chạy trước để FK tồn tại)
- V008_005: seed built-in types (cần table từ V008_001)
- V008_002: backfill discriminator + issue_key (cần data đã có)
- V008_003: rename + compat view (cần backfill xong)
- V008_004: **DEFER** — chỉ chạy sau khi Phase 1-3 stable >= 1 tuần

### IssueKey Generation — Race Condition Concern

Phương pháp COUNT + 1 đủ tốt cho Phase 8.0 (không phải production high-concurrency). Nếu cần atomic:
- Option A: `SELECT nextval()` với per-project sequence (thêm phức tạp, dùng Story 8.1)
- Option B: DB advisory lock (`pg_try_advisory_xact_lock(project_id hashcode)`)
- Hiện tại dùng Option A đơn giản: COUNT `IgnoreQueryFilters()` + 1. Duplicate key có unique index → 409 conflict, xử lý ở caller.

```csharp
// CreateTaskHandler — thêm sau membership check:
var project = await _db.Projects.FirstAsync(p => p.Id == cmd.ProjectId, ct);
var issueCount = await _db.Issues
    .IgnoreQueryFilters()
    .CountAsync(t => t.ProjectId == cmd.ProjectId, ct);
var issueKey = $"{project.Code}-{issueCount + 1}";
```

### Breaking Changes Checklist (PHẢI verify trước khi mark review)

- [ ] `grep -r "ProjectTasks" src/` → 0 results (thay hết bằng `Issues`)
- [ ] `grep -r "project_tasks" src/` → chỉ còn trong migration files + comments
- [ ] `time_entries.task_id` → `issue_id` nếu TimeTracking module có query raw SQL
- [ ] `TaskDto` API response có `issueKey` field không null cho mọi task mới
- [ ] Swagger/OpenAPI spec cập nhật (chạy `dotnet run` và check `/swagger`)
- [ ] `TaskDependency` FK vẫn trỏ đúng table sau rename (PostgreSQL tự update FK target)

### Pattern: EF Migration với manual SQL

Xem `20260426101831_CreateProjectsTables.cs` làm reference. Dùng `migrationBuilder.Sql("""...""")` cho raw SQL. Không dùng `migrationBuilder.CreateTable()` API vì SQL phức tạp hơn.

### Pattern: DbSet rename

```csharp
// IProjectsDbContext — TRƯỚC
DbSet<ProjectTask> ProjectTasks { get; }

// IProjectsDbContext — SAU
DbSet<ProjectTask> Issues { get; }
```

Tất cả handlers đang dùng `_db.ProjectTasks.XXX` phải đổi sang `_db.Issues.XXX`. Dùng IDE Find+Replace.

### Không có frontend thay đổi trong story này

Story 8.0 là pure backend migration. Frontend chỉ nhận thêm fields mới trong `TaskDto` response (nullable, backward compat). Không có route mới, không có UI mới.

### TimeTracking module dependency

Nếu `time_entries` table tồn tại với `task_id` column:
- V008_003 tự động rename sang `issue_id` (conditional DO block)
- Check `GetTimeEntriesByProjectHandler`, `GetMyTimeEntriesHandler` cho references tới `task_id`

---

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Completion Notes List

*(Điền khi dev hoàn thành)*

### File List

**Domain — mới:**
- `src/Modules/Projects/ProjectManagement.Projects.Domain/Entities/IssueTypeDefinition.cs`

**Domain — sửa:**
- `src/Modules/Projects/ProjectManagement.Projects.Domain/Entities/ProjectTask.cs`

**Application — sửa:**
- `src/Modules/Projects/ProjectManagement.Projects.Application/Common/Interfaces/IProjectsDbContext.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Application/DTOs/TaskDto.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Application/Tasks/Commands/CreateTask/CreateTaskHandler.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Application/Tasks/Commands/UpdateTask/UpdateTaskHandler.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Application/Tasks/Commands/DeleteTask/DeleteTaskHandler.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Application/Tasks/Queries/GetTasksByProject/GetTasksByProjectHandler.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Application/Tasks/Queries/GetTaskById/GetTaskByIdHandler.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Application/Tasks/Queries/GetMyTasks/GetMyTasksHandler.cs`

**Infrastructure — mới:**
- `src/Modules/Projects/ProjectManagement.Projects.Infrastructure/Persistence/Configurations/IssueTypeDefinitionConfiguration.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Infrastructure/Migrations/YYYYMMDDHHMMSS_V008_001_ExpandIssueColumns.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Infrastructure/Migrations/YYYYMMDDHHMMSS_V008_002_BackfillDiscriminatorAndIssueKey.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Infrastructure/Migrations/YYYYMMDDHHMMSS_V008_003_RenameToIssues_CreateCompatView.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Infrastructure/Migrations/YYYYMMDDHHMMSS_V008_004_ContractCleanup.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Infrastructure/Migrations/YYYYMMDDHHMMSS_V008_005_SeedBuiltInIssueTypes.cs`

**Infrastructure — sửa:**
- `src/Modules/Projects/ProjectManagement.Projects.Infrastructure/Persistence/ProjectsDbContext.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Infrastructure/Persistence/Configurations/TaskConfiguration.cs`
- `src/Modules/Projects/ProjectManagement.Projects.Infrastructure/Migrations/ProjectsDbContextModelSnapshot.cs`
