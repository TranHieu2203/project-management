---
type: technical-specification
story: 8.0
title: Issue Table Migration — Expand-Contract (tasks → issues)
epic: 8
status: ready-for-implementation
date: 2026-04-29
---

# Technical Specification: Story 8.0

## Overview

4-phase expand-contract migration renaming `project_tasks` → `issues`, adding discriminator, issue_type_id, issue_key, custom_fields, story_points, and full-text search. Zero-downtime: backward-compat `project_tasks` view maintained through Phase 3; only dropped in Phase 4 once all code migrated. Each phase is an independent EF Core migration script and can be rolled back independently (except Phase 4).

## Prerequisites

- Story 1.4 fully merged (ProjectTask CRUD live)
- `projects` table has a `code` column (VARCHAR, unique per project) used to generate `issue_key`
- `users` table exists (Auth module, Story 1.1)
- `time_entries` table has a `task_id` column (TimeTracking module — rename to `issue_id` in Phase 3)
- `issue_type_definitions` table does NOT yet exist (created in Phase 1)
- All integration tests green on `main` before starting Phase 1
- Database: PostgreSQL 15+

---

## Phase 1 — Expand (Sprint N, Day 1–2)

### Migration File

`src/Modules/Projects/ProjectManagement.Projects.Infrastructure/Migrations/YYYYMMDDHHMMSS_V008_001_ExpandIssueColumns.cs`

### SQL Script V008_001

```sql
-- V008_001: Add new columns to project_tasks (non-breaking — all nullable)
-- Safe to run on live database.

-- 1. Create issue_type_definitions lookup table first (FK target)
CREATE TABLE IF NOT EXISTS issue_type_definitions (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name        VARCHAR(100)  NOT NULL,
    icon_key    VARCHAR(50)   NOT NULL,
    color       VARCHAR(7)    NOT NULL,   -- hex #RRGGBB
    is_built_in BOOLEAN       NOT NULL DEFAULT false,
    is_deletable BOOLEAN      NOT NULL DEFAULT true,
    project_id  UUID          NULL REFERENCES projects(id) ON DELETE CASCADE,
    sort_order  INT           NOT NULL DEFAULT 0,
    created_at  TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    created_by  VARCHAR(450)  NOT NULL DEFAULT 'system',
    updated_at  TIMESTAMPTZ   NULL,
    updated_by  VARCHAR(450)  NULL,
    is_deleted  BOOLEAN       NOT NULL DEFAULT false
);

CREATE UNIQUE INDEX IF NOT EXISTS uq_issue_type_definitions_name_project
    ON issue_type_definitions(COALESCE(project_id::text, ''), name)
    WHERE is_deleted = false;

-- 2. Add new columns to project_tasks (all nullable — backward compat)
ALTER TABLE project_tasks
    ADD COLUMN IF NOT EXISTS discriminator       VARCHAR(50)  NULL,
    ADD COLUMN IF NOT EXISTS issue_type_id       UUID         NULL REFERENCES issue_type_definitions(id),
    ADD COLUMN IF NOT EXISTS issue_key           VARCHAR(20)  NULL,
    ADD COLUMN IF NOT EXISTS parent_issue_id     UUID         NULL REFERENCES project_tasks(id),
    ADD COLUMN IF NOT EXISTS custom_fields       JSONB        NULL,
    ADD COLUMN IF NOT EXISTS workflow_state_id   UUID         NULL,
    ADD COLUMN IF NOT EXISTS story_points        INT          NULL,
    ADD COLUMN IF NOT EXISTS reporter_user_id    UUID         NULL REFERENCES users(id);

-- 3. Unique index on issue_key (partial — only non-null rows)
CREATE UNIQUE INDEX IF NOT EXISTS uq_project_tasks_issue_key
    ON project_tasks(issue_key)
    WHERE issue_key IS NOT NULL;
```

### EF Core Migration (C# skeleton)

```csharp
// Migrations/YYYYMMDDHHMMSS_V008_001_ExpandIssueColumns.cs
public partial class V008_001_ExpandIssueColumns : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS issue_type_definitions ( ... );
            -- (full SQL above)
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            ALTER TABLE project_tasks
                DROP COLUMN IF EXISTS reporter_user_id,
                DROP COLUMN IF EXISTS story_points,
                DROP COLUMN IF EXISTS workflow_state_id,
                DROP COLUMN IF EXISTS custom_fields,
                DROP COLUMN IF EXISTS parent_issue_id,
                DROP COLUMN IF EXISTS issue_key,
                DROP COLUMN IF EXISTS issue_type_id,
                DROP COLUMN IF EXISTS discriminator;
            DROP INDEX IF EXISTS uq_project_tasks_issue_key;
            DROP TABLE IF EXISTS issue_type_definitions;
        ");
    }
}
```

### Validation

```sql
-- All new columns exist and are nullable
SELECT column_name, data_type, is_nullable
FROM information_schema.columns
WHERE table_name = 'project_tasks'
  AND column_name IN (
    'discriminator','issue_type_id','issue_key',
    'parent_issue_id','custom_fields','workflow_state_id',
    'story_points','reporter_user_id'
  );
-- Expected: 8 rows, all is_nullable = 'YES'

-- issue_type_definitions table exists
SELECT COUNT(*) FROM issue_type_definitions;
-- Expected: 0 (empty before seed)

-- Existing tests: dotnet test — expect 0 failures
```

### Rollback

```sql
-- Run V008_001 Down migration:
ALTER TABLE project_tasks
    DROP COLUMN IF EXISTS reporter_user_id,
    DROP COLUMN IF EXISTS story_points,
    DROP COLUMN IF EXISTS workflow_state_id,
    DROP COLUMN IF EXISTS custom_fields,
    DROP COLUMN IF EXISTS parent_issue_id,
    DROP COLUMN IF EXISTS issue_key,
    DROP COLUMN IF EXISTS issue_type_id,
    DROP COLUMN IF EXISTS discriminator;
DROP INDEX IF EXISTS uq_project_tasks_issue_key;
DROP TABLE IF EXISTS issue_type_definitions;
```

---

## Phase 2 — Backfill (Sprint N, Day 2–3)

### Migration File

`...Migrations/YYYYMMDDHHMMSS_V008_002_BackfillDiscriminatorAndIssueKey.cs`

### SQL Script V008_002 (idempotent)

```sql
-- V008_002: Idempotent backfill — safe to run multiple times.
-- Must run inside a transaction.

BEGIN;

-- Step 1: Set discriminator for all existing rows where NULL
-- Map existing TaskType enum values to discriminator strings
UPDATE project_tasks
SET discriminator = type   -- type column stores 'Phase','Milestone','Task' as strings
WHERE discriminator IS NULL;

-- Verify: no nulls remain
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM project_tasks WHERE discriminator IS NULL) THEN
        RAISE EXCEPTION 'Backfill incomplete: discriminator still NULL on some rows';
    END IF;
END $$;

-- Step 2: Generate issue_key = {project.code}-{per-project sequence}
-- Uses ROW_NUMBER partitioned by project_id ordered by created_at (stable, deterministic)
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

-- Verify: no nulls remain
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM project_tasks WHERE issue_key IS NULL) THEN
        RAISE EXCEPTION 'Backfill incomplete: issue_key still NULL on some rows';
    END IF;
END $$;

COMMIT;
```

### Validation

```sql
-- Both assertions below must return 0
SELECT COUNT(*) AS discriminator_nulls FROM project_tasks WHERE discriminator IS NULL;
-- Expected: 0

SELECT COUNT(*) AS issue_key_nulls FROM project_tasks WHERE issue_key IS NULL;
-- Expected: 0

-- Spot-check: issue_keys look correct
SELECT project_id, issue_key, discriminator FROM project_tasks LIMIT 20;

-- Run full test suite
-- dotnet test — expect 0 failures
```

### Rollback

```sql
-- Reset backfilled values (reverts to pre-backfill state)
UPDATE project_tasks SET discriminator = NULL, issue_key = NULL;
-- Then run V008_001 Down if needed to remove columns entirely.
```

---

## Phase 3 — Rename + Compat View (Sprint N, Day 3–4)

### Migration File

`...Migrations/YYYYMMDDHHMMSS_V008_003_RenameToIssues_CreateCompatView.cs`

### SQL Script V008_003

```sql
-- V008_003: Rename project_tasks → issues, create backward-compat view.
-- Application code continues to work unchanged after this migration.

-- 1. Rename the table
ALTER TABLE project_tasks RENAME TO issues;

-- 2. Rename sequences, if any (auto-named by Postgres)
-- (No sequences on project_tasks — id is UUID, no serial)

-- 3. Rename indexes to match new table name
ALTER INDEX IF EXISTS ix_project_tasks_project_id    RENAME TO ix_issues_project_id;
ALTER INDEX IF EXISTS ix_project_tasks_parent_id     RENAME TO ix_issues_parent_id;
ALTER INDEX IF EXISTS ix_project_tasks_project_sort  RENAME TO ix_issues_project_sort;
ALTER INDEX IF EXISTS uq_project_tasks_issue_key     RENAME TO uq_issues_issue_key;

-- 4. Create backward-compat view so existing queries still work
--    Includes ALL columns so SELECT * and named-column queries are both safe.
CREATE VIEW project_tasks AS
    SELECT * FROM issues
    WHERE discriminator IN ('Task', 'Phase', 'Milestone');

-- 5. Rename FK column in time_entries (if time_entries already exists)
--    If time_entries does not exist yet, skip and handle in TimeTracking module init.
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'time_entries' AND column_name = 'task_id'
    ) THEN
        ALTER TABLE time_entries RENAME COLUMN task_id TO issue_id;
        -- Rename corresponding FK constraint
        ALTER TABLE time_entries
            RENAME CONSTRAINT fk_time_entries_task_id TO fk_time_entries_issue_id;
    END IF;
END $$;

-- 6. Rename FK in task_dependencies if it references project_tasks
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'task_dependencies' AND column_name = 'predecessor_task_id'
    ) THEN
        -- task_dependencies FKs now point to issues (renamed table); no column rename needed
        -- but update constraint names for clarity:
        ALTER INDEX IF EXISTS ix_task_dependencies_predecessor
            RENAME TO ix_task_dependencies_predecessor_issue;
        ALTER INDEX IF EXISTS ix_task_dependencies_successor
            RENAME TO ix_task_dependencies_successor_issue;
    END IF;
END $$;
```

### EF Core Configuration Change (immediate — Phase 3)

In `TaskConfiguration.cs`, update the table mapping so EF Core targets `issues` and the compat view is not used by the ORM:

```csharp
// src/Modules/Projects/ProjectManagement.Projects.Infrastructure/Persistence/Configurations/TaskConfiguration.cs
public sealed class TaskConfiguration : IEntityTypeConfiguration<ProjectTask>
{
    public void Configure(EntityTypeBuilder<ProjectTask> b)
    {
        // CHANGED: was "project_tasks", now "issues"
        b.ToTable("issues");

        // Add new columns (nullable — Phase 3 entity update)
        b.Property(x => x.Discriminator)
            .HasColumnName("discriminator")
            .HasMaxLength(50)
            .IsRequired(false);   // becomes required in Phase 4

        b.Property(x => x.IssueTypeId)
            .HasColumnName("issue_type_id")
            .IsRequired(false);   // becomes required in Phase 4

        b.Property(x => x.IssueKey)
            .HasColumnName("issue_key")
            .HasMaxLength(20)
            .IsRequired(false);

        b.Property(x => x.ParentIssueId)
            .HasColumnName("parent_issue_id")
            .IsRequired(false);

        b.Property(x => x.CustomFields)
            .HasColumnName("custom_fields")
            .HasColumnType("jsonb")
            .IsRequired(false);

        b.Property(x => x.WorkflowStateId)
            .HasColumnName("workflow_state_id")
            .IsRequired(false);

        b.Property(x => x.StoryPoints)
            .HasColumnName("story_points")
            .IsRequired(false);

        b.Property(x => x.ReporterUserId)
            .HasColumnName("reporter_user_id")
            .IsRequired(false);

        // Indexes — renamed to match new table
        b.HasIndex(x => x.ProjectId).HasDatabaseName("ix_issues_project_id");
        b.HasIndex(x => x.ParentId).HasDatabaseName("ix_issues_parent_id");
        b.HasIndex(x => new { x.ProjectId, x.SortOrder })
            .HasDatabaseName("ix_issues_project_sort");

        // ... existing columns unchanged (id, project_id, parent_id, type, etc.)
    }
}
```

### Domain Entity Changes (Phase 3)

```csharp
// src/Modules/Projects/ProjectManagement.Projects.Domain/Entities/ProjectTask.cs
// Add new properties (all nullable — Phase 4 enforces NOT NULL)

public string? Discriminator { get; private set; }
public Guid? IssueTypeId { get; private set; }
public string? IssueKey { get; private set; }
public Guid? ParentIssueId { get; private set; }     // replaces ParentId for cross-type hierarchy
public string? CustomFields { get; private set; }    // stored as JSON string; deserialize in app layer
public Guid? WorkflowStateId { get; private set; }
public int? StoryPoints { get; private set; }
public Guid? ReporterUserId { get; private set; }
```

### Validation

```sql
-- issues table exists
SELECT COUNT(*) FROM issues;

-- compat view works
SELECT COUNT(*) FROM project_tasks;

-- Row counts match (view filters to Task/Phase/Milestone only)
SELECT
    (SELECT COUNT(*) FROM issues) AS total_issues,
    (SELECT COUNT(*) FROM project_tasks) AS compat_view_count;
-- compat_view_count <= total_issues (equal if no new issue types yet)

-- time_entries column renamed (if applicable)
SELECT column_name FROM information_schema.columns
WHERE table_name = 'time_entries';
-- Should contain 'issue_id', NOT 'task_id'
```

```bash
# Run full test suite — expect 0 failures
dotnet test --configuration Release
```

### Rollback

```sql
DROP VIEW IF EXISTS project_tasks;
ALTER TABLE issues RENAME TO project_tasks;

-- Rename indexes back
ALTER INDEX IF EXISTS ix_issues_project_id    RENAME TO ix_project_tasks_project_id;
ALTER INDEX IF EXISTS ix_issues_parent_id     RENAME TO ix_project_tasks_parent_id;
ALTER INDEX IF EXISTS ix_issues_project_sort  RENAME TO ix_project_tasks_project_sort;
ALTER INDEX IF EXISTS uq_issues_issue_key     RENAME TO uq_project_tasks_issue_key;

-- Restore time_entries column name
ALTER TABLE time_entries RENAME COLUMN issue_id TO task_id;
```

---

## Phase 4 — Contract / Cleanup (Sprint N+1 or later)

### Pre-conditions

All of the following MUST be true before running Phase 4:

- [ ] All application code targets `issues` table directly (no query via compat view)
- [ ] `TaskConfiguration.cs` maps `b.ToTable("issues")`
- [ ] `IProjectsDbContext` uses `DbSet<ProjectTask> Issues` (or renamed `Issues`)
- [ ] `time_entries` code uses `issue_id` column
- [ ] Zero references to `project_tasks` string remain in application code (`grep -r "project_tasks" src/`)
- [ ] All 4 phases deployed and stable in staging for >= 1 week
- [ ] Full regression suite passes on staging

### Migration File

`...Migrations/YYYYMMDDHHMMSS_V008_004_ContractCleanup.cs`

### SQL Script V008_004

```sql
-- V008_004: Contract phase — enforce NOT NULL, drop compat view, add GIN indexes.
-- IRREVERSIBLE without blue/green deployment. Only run after all pre-conditions met.

BEGIN;

-- 1. Drop the backward-compat view
DROP VIEW IF EXISTS project_tasks;

-- 2. Enforce NOT NULL on discriminator and issue_type_id
--    (backfill in V008_002 guarantees no NULLs)
ALTER TABLE issues ALTER COLUMN discriminator    SET NOT NULL;
ALTER TABLE issues ALTER COLUMN issue_key        SET NOT NULL;

-- Note: issue_type_id NOT NULL enforcement depends on V008_005 seed running first
--       and all existing rows having been assigned an issue_type_id.
--       Run the assignment below first:
UPDATE issues i
SET issue_type_id = (
    SELECT id FROM issue_type_definitions
    WHERE name = i.discriminator   -- 'Task', 'Phase', 'Milestone' match built-in names
      AND is_built_in = true
    LIMIT 1
)
WHERE i.issue_type_id IS NULL;

ALTER TABLE issues ALTER COLUMN issue_type_id SET NOT NULL;

-- 3. GIN index on custom_fields JSONB for fast key/value queries
CREATE INDEX IF NOT EXISTS idx_issues_custom_fields_gin
    ON issues USING gin(custom_fields);

-- 4. Full-text search: generated tsvector column
ALTER TABLE issues
    ADD COLUMN IF NOT EXISTS search_vector tsvector
        GENERATED ALWAYS AS (
            to_tsvector('simple',
                coalesce(name, '')        || ' ' ||
                coalesce(description, '') || ' ' ||
                coalesce(issue_key, '')
            )
        ) STORED;

CREATE INDEX IF NOT EXISTS idx_issues_search_gin
    ON issues USING gin(search_vector);

-- 5. Additional structural indexes
CREATE INDEX IF NOT EXISTS ix_issues_discriminator
    ON issues(discriminator);

CREATE INDEX IF NOT EXISTS ix_issues_issue_type_id
    ON issues(issue_type_id);

COMMIT;
```

### Rollback

Phase 4 cannot be safely rolled back in-place. Required strategy:

1. Blue/green deployment: keep old version running while new version is deployed.
2. To reverse Phase 4 manually (emergency only, data loss risk):

```sql
-- Emergency rollback (only if no new-type rows exist):
ALTER TABLE issues DROP COLUMN IF EXISTS search_vector;
DROP INDEX IF EXISTS idx_issues_custom_fields_gin;
DROP INDEX IF EXISTS idx_issues_search_gin;
DROP INDEX IF EXISTS ix_issues_discriminator;
DROP INDEX IF EXISTS ix_issues_issue_type_id;
ALTER TABLE issues ALTER COLUMN discriminator DROP NOT NULL;
ALTER TABLE issues ALTER COLUMN issue_type_id DROP NOT NULL;
ALTER TABLE issues ALTER COLUMN issue_key DROP NOT NULL;
-- Re-create compat view:
CREATE VIEW project_tasks AS
    SELECT * FROM issues WHERE discriminator IN ('Task','Phase','Milestone');
```

---

## Issue Type Definitions Seed Data

### Migration File

`...Migrations/YYYYMMDDHHMMSS_V008_005_SeedBuiltInIssueTypes.cs`

Run this after V008_001 (issue_type_definitions table exists) and before V008_004 (issue_type_id NOT NULL).

### SQL Script V008_005

```sql
-- V008_005: Seed 5 built-in issue types (global, project_id = NULL, not deletable)
INSERT INTO issue_type_definitions (id, name, icon_key, color, is_built_in, is_deletable, project_id, sort_order, created_by)
VALUES
    ('00000000-0000-0000-0000-000000000001', 'Epic',     'epic',    '#7C3AED', true, false, NULL, 1, 'system'),
    ('00000000-0000-0000-0000-000000000002', 'Story',    'story',   '#059669', true, false, NULL, 2, 'system'),
    ('00000000-0000-0000-0000-000000000003', 'Task',     'task',    '#2563EB', true, false, NULL, 3, 'system'),
    ('00000000-0000-0000-0000-000000000004', 'Bug',      'bug',     '#DC2626', true, false, NULL, 4, 'system'),
    ('00000000-0000-0000-0000-000000000005', 'Sub-task', 'subtask', '#6B7280', true, false, NULL, 5, 'system')
ON CONFLICT (id) DO NOTHING;
```

### Validation

```sql
SELECT id, name, icon_key, color, is_built_in, is_deletable
FROM issue_type_definitions
ORDER BY sort_order;
-- Expected: 5 rows, all is_built_in=true, is_deletable=false
```

---

## EF Core Configuration Changes

### Summary of changes by file

| File | Change |
|---|---|
| `TaskConfiguration.cs` | `b.ToTable("issues")` + 8 new property mappings |
| `ProjectsDbContext.cs` | `DbSet<ProjectTask> Issues` (rename DbSet) |
| `IProjectsDbContext.cs` | Update interface to match |
| `ProjectTask.cs` (domain) | Add 8 new properties (nullable until Phase 4) |
| New: `IssueTypeDefinitionConfiguration.cs` | EF config for `issue_type_definitions` table |
| New: `IssueTypeDefinition.cs` (domain) | Entity for issue type lookup |

### IssueTypeDefinition entity

```csharp
// src/Modules/Projects/ProjectManagement.Projects.Domain/Entities/IssueTypeDefinition.cs
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
}
```

### IssueTypeDefinitionConfiguration

```csharp
// src/Modules/Projects/ProjectManagement.Projects.Infrastructure/Persistence/Configurations/IssueTypeDefinitionConfiguration.cs
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

### Updated ProjectsDbContext

```csharp
// ProjectsDbContext.cs — updated
public DbSet<Project> Projects => Set<Project>();
public DbSet<ProjectMembership> ProjectMemberships => Set<ProjectMembership>();
public DbSet<ProjectTask> Issues => Set<ProjectTask>();          // renamed: was ProjectTasks
public DbSet<TaskDependency> TaskDependencies => Set<TaskDependency>();
public DbSet<IssueTypeDefinition> IssueTypeDefinitions => Set<IssueTypeDefinition>();

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplyConfiguration(new ProjectConfiguration());
    modelBuilder.ApplyConfiguration(new ProjectMembershipConfiguration());
    modelBuilder.ApplyConfiguration(new TaskConfiguration());            // maps to "issues"
    modelBuilder.ApplyConfiguration(new TaskDependencyConfiguration());
    modelBuilder.ApplyConfiguration(new IssueTypeDefinitionConfiguration());
}
```

---

## Breaking Changes Checklist

- [ ] All handlers that use `context.ProjectTasks` must be updated to `context.Issues`
- [ ] `IProjectsDbContext` interface: rename `ProjectTasks` → `Issues`
- [ ] `time_entries.task_id` renamed to `issue_id` — TimeTracking module queries must update
- [ ] Any raw SQL in `GetTasksByProjectHandler`, `GetMyTasksHandler`, etc. referencing `project_tasks` string
- [ ] API response DTOs: add `issueKey`, `discriminator`, `storyPoints`, `issueTypeId` fields
- [ ] `CreateTaskCommand` / `CreateTaskHandler`: set `discriminator` from `TaskType` enum, generate `issue_key`
- [ ] `IssueKey` generation: must be atomic (use DB sequence or advisory lock to avoid duplicates under concurrency)
- [ ] Any integration test using raw table name `"project_tasks"` must update to `"issues"`
- [ ] Swagger/OpenAPI spec: update schema names if `ProjectTask` DTO is renamed to `IssueDto`
- [ ] `TaskDependency` FK columns still reference correct table after rename (verify FK constraints)

---

## Test Coverage Requirements

| Layer | What to test | Expected result |
|---|---|---|
| Unit — Domain | `ProjectTask.Create()` sets `Discriminator` from `TaskType` | Pass |
| Unit — Domain | `IssueKey` format validation: matches `^[A-Z]+-\d+$` | Pass |
| Unit — Domain | Null `IssueTypeId` allowed pre-Phase 4, rejected after | Pass (conditional) |
| Unit — Application | `CreateTaskHandler` sets `discriminator` and generates `issue_key` | Pass |
| Unit — Application | `CreateTaskHandler` with duplicate `issue_key` returns `409` | Pass |
| Unit — Application | `GetTasksByProjectHandler` queries `Issues` DbSet (not raw view) | Pass |
| Integration — DB | V008_001 migration: 8 new columns exist, all nullable | Pass |
| Integration — DB | V008_002 migration: 0 rows with `discriminator IS NULL` after backfill | Pass |
| Integration — DB | V008_002 migration: `issue_key` format correct for all rows | Pass |
| Integration — DB | V008_003 migration: `issues` table exists, `project_tasks` view exists | Pass |
| Integration — DB | V008_003 migration: `project_tasks` view returns same rows as before rename | Pass |
| Integration — DB | V008_004 migration: `discriminator NOT NULL` enforced | Pass |
| Integration — DB | V008_004 migration: `search_vector` column populated on existing rows | Pass |
| Integration — DB | V008_005 seed: 5 built-in types with correct IDs and colors | Pass |
| Integration — API | `POST /api/projects/{id}/tasks` — response includes `issueKey` field | Pass |
| Integration — API | `GET /api/projects/{id}/tasks` — returns `issueKey`, `discriminator` | Pass |
| Integration — API | `GET /api/issues?search=keyword` — uses `search_vector` GIN index | Pass |
| Integration — API | `PATCH /api/issues/{id}` — `storyPoints`, `issueTypeId`, `customFields` updatable | Pass |
| E2E — Migration | Apply all 5 migrations in sequence on empty DB, seed, verify counts | Pass |
| E2E — Migration | Apply V008_001–003, run legacy code path via compat view, verify no errors | Pass |

---

# Technical Note: Resource → User Identity Bridge (Epic 10 Prerequisite)

## Problem Statement

The `resources` table (Workforce module) represents people (inhouse + outsource) but has no link to `users` table (Auth module). Epic 10 features (@mentions, notifications, assignment suggestions, user-linked timesheets) require knowing which `user` account corresponds to which `resource`. Currently the link is implicit at best (matching by `email`), which is fragile. This migration adds an explicit, audited `user_id` foreign key to `resources`.

---

## Migration Script V010_001

### Migration File

`src/Modules/Workforce/ProjectManagement.Workforce.Infrastructure/Migrations/YYYYMMDDHHMMSS_V010_001_ResourceUserBridge.cs`

```sql
-- V010_001: Add user_id FK to resources table.
-- Non-breaking: user_id is nullable. Resources without linked users continue to work.

ALTER TABLE resources
    ADD COLUMN IF NOT EXISTS user_id UUID NULL REFERENCES users(id) ON DELETE SET NULL;

-- Partial unique index: one resource per user, but only when linked
CREATE UNIQUE INDEX IF NOT EXISTS uq_resources_user_id
    ON resources(user_id)
    WHERE user_id IS NOT NULL AND is_deleted = false;

-- Audit log table for link/unlink events
CREATE TABLE IF NOT EXISTS resource_user_link_audit (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    resource_id     UUID NOT NULL REFERENCES resources(id),
    user_id         UUID NOT NULL REFERENCES users(id),
    action          VARCHAR(10) NOT NULL CHECK (action IN ('LINKED', 'UNLINKED')),
    performed_by    VARCHAR(450) NOT NULL,
    performed_at    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    note            VARCHAR(500) NULL
);

CREATE INDEX IF NOT EXISTS ix_resource_user_link_audit_resource_id
    ON resource_user_link_audit(resource_id);
CREATE INDEX IF NOT EXISTS ix_resource_user_link_audit_user_id
    ON resource_user_link_audit(user_id);
```

### Rollback

```sql
DROP TABLE IF EXISTS resource_user_link_audit;
DROP INDEX IF EXISTS uq_resources_user_id;
ALTER TABLE resources DROP COLUMN IF EXISTS user_id;
```

---

## API Endpoints

### PATCH /api/resources/{resourceId}/link-user

Links a `resource` to a `user` account. Only Admin role.

**Request:**
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "note": "Linked after SSO onboarding"
}
```

**Response:**
- `200 OK` with updated `ResourceDto` (including `userId`)
- `404` if resource not found
- `409` if `userId` already linked to another resource
- `422` if `userId` does not exist in users table

**Handler logic:**
```csharp
// 1. Validate resource exists and is not deleted
// 2. Validate user exists (cross-module: call IUserExistenceChecker or direct DB read via shared context)
// 3. Check no other active resource has this user_id (unique constraint — catch DB conflict or pre-check)
// 4. Set resource.UserId = userId
// 5. Write audit log: action='LINKED', performedBy=currentUser, note
// 6. Publish ResourceLinkedToUserEvent (for Epic 10 subscribers)
// 7. Return updated ResourceDto
```

### PATCH /api/resources/{resourceId}/unlink-user

Removes the user link. Only Admin role.

**Request:**
```json
{
  "note": "User left organisation"
}
```

**Response:**
- `200 OK` with updated `ResourceDto` (`userId: null`)
- `404` if resource not found or already unlinked

**Handler logic:**
```csharp
// 1. Validate resource exists and has user_id set
// 2. Capture old user_id for audit
// 3. Set resource.UserId = null
// 4. Write audit log: action='UNLINKED', performedBy=currentUser, note
// 5. Publish ResourceUnlinkedFromUserEvent
// 6. Return updated ResourceDto
```

---

## Audit Requirements

Every `link-user` and `unlink-user` call writes one row to `resource_user_link_audit`:

| Column | Source |
|---|---|
| `resource_id` | Path param `{resourceId}` |
| `user_id` | Body `userId` (LINKED) or previous value (UNLINKED) |
| `action` | `'LINKED'` or `'UNLINKED'` |
| `performed_by` | JWT claim `sub` (current user email or ID) |
| `performed_at` | `DateTime.UtcNow` |
| `note` | Optional body field |

Audit rows are immutable — no UPDATE/DELETE allowed on `resource_user_link_audit`.

---

## Impact on @mentions Feature (Epic 10)

| Scenario | `user_id` column | Behaviour |
|---|---|---|
| Resource has linked user | `IS NOT NULL` | @mention resolves to user; notification dispatched via user's email/preferences |
| Resource has no linked user | `IS NULL` | @mention shows resource name only; no push notification; warning shown in UI "User account not linked" |
| User deactivated but resource active | `user_id` present but `users.is_active = false` | @mention still resolves name; notification silently dropped; UI badge shows "inactive user" |
| Multiple resources, same user | Blocked by `uq_resources_user_id` unique index | `409 Conflict` on second link attempt |

### Query pattern for @mention resolution

```sql
-- Resolve @mention by display name or resource code
SELECT r.id, r.name, r.code, r.user_id, u.email
FROM resources r
LEFT JOIN users u ON u.id = r.user_id
WHERE r.is_deleted = false
  AND r.is_active  = true
  AND (
    r.name ILIKE '%' || $1 || '%'
    OR r.code ILIKE '%' || $1 || '%'
  )
ORDER BY r.name
LIMIT 10;
```

### ResourceDto update (Phase 10)

```csharp
public record ResourceDto(
    Guid Id,
    string Code,
    string Name,
    string? Email,
    string Type,
    Guid? VendorId,
    bool IsActive,
    Guid? UserId,         // NEW: null if not linked
    bool HasUserAccount   // NEW: computed = UserId != null
);
```

---

## Cross-Module Dependency Note

`resources` lives in `Workforce` schema/module. `users` lives in `Auth` module. Direct FK across module boundaries is acceptable here because:

1. Auth module is a stable, low-churn dependency.
2. `ON DELETE SET NULL` prevents cascade failures if a user is deleted.
3. An `IUserExistenceChecker` interface (Workforce.Application) backed by a cross-DB read or an internal event ensures loose coupling at the application layer.

If strict module isolation is required, replace the FK with an application-enforced check and remove the DB-level FK constraint — audit trail remains regardless.
