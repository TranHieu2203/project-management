using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ProjectManagement.Projects.Infrastructure.Persistence;

#nullable disable

namespace ProjectManagement.Projects.Infrastructure.Migrations
{
    [DbContext(typeof(ProjectsDbContext))]
    [Migration("20260429224730_V008_003_RenameToIssues_CreateCompatView")]
    public partial class V008_003_RenameToIssues_CreateCompatView : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                -- V008_003: Rename project_tasks → issues, create backward-compat view.
                -- Application code continues to work unchanged after this migration (via view).

                -- 1. Rename the table
                ALTER TABLE project_tasks RENAME TO issues;

                -- 2. Rename indexes to match new table name
                ALTER INDEX IF EXISTS ix_project_tasks_project_id   RENAME TO ix_issues_project_id;
                ALTER INDEX IF EXISTS ix_project_tasks_parent_id    RENAME TO ix_issues_parent_id;
                ALTER INDEX IF EXISTS ix_project_tasks_project_sort RENAME TO ix_issues_project_sort;
                ALTER INDEX IF EXISTS uq_project_tasks_issue_key    RENAME TO uq_issues_issue_key;

                -- 3. Create backward-compat view so legacy queries still work
                CREATE VIEW project_tasks AS
                    SELECT * FROM issues;

                -- 4. Rename FK column in time_entries (if column exists)
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
    }
}
