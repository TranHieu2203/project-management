using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectManagement.Projects.Infrastructure.Migrations
{
    public partial class V008_001_ExpandIssueColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                -- V008_001: Add new columns to project_tasks (non-breaking — all nullable)
                -- Safe to run on live database.

                -- 1. Create issue_type_definitions lookup table first (FK target)
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

                -- 2. Add new columns to project_tasks (all nullable — backward compat)
                ALTER TABLE project_tasks
                    ADD COLUMN IF NOT EXISTS discriminator      VARCHAR(50)  NULL,
                    ADD COLUMN IF NOT EXISTS issue_type_id     UUID         NULL REFERENCES issue_type_definitions(id),
                    ADD COLUMN IF NOT EXISTS issue_key         VARCHAR(20)  NULL,
                    ADD COLUMN IF NOT EXISTS parent_issue_id   UUID         NULL REFERENCES project_tasks(id),
                    ADD COLUMN IF NOT EXISTS custom_fields     JSONB        NULL,
                    ADD COLUMN IF NOT EXISTS workflow_state_id UUID         NULL,
                    ADD COLUMN IF NOT EXISTS story_points      INT          NULL,
                    ADD COLUMN IF NOT EXISTS reporter_user_id  UUID         NULL REFERENCES users(id);

                -- 3. Unique index on issue_key (partial — only non-null rows)
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
}
