using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ProjectManagement.Projects.Infrastructure.Persistence;

#nullable disable

namespace ProjectManagement.Projects.Infrastructure.Migrations
{
    [DbContext(typeof(ProjectsDbContext))]
    [Migration("20260429224715_V008_002_BackfillDiscriminatorAndIssueKey")]
    public partial class V008_002_BackfillDiscriminatorAndIssueKey : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                -- V008_002: Idempotent backfill — safe to run multiple times.
                -- Step 1: Set discriminator for all existing rows where NULL
                -- Maps existing TaskType enum values to discriminator strings
                UPDATE project_tasks
                SET discriminator = type
                WHERE discriminator IS NULL;

                -- Verify: no nulls remain
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM project_tasks WHERE discriminator IS NULL) THEN
                        RAISE EXCEPTION 'Backfill incomplete: discriminator still NULL on some rows';
                    END IF;
                END $$;

                -- Step 2: Generate issue_key = {project.code}-{per-project sequence}
                WITH rn AS (
                    SELECT
                        id,
                        project_id,
                        ROW_NUMBER() OVER (PARTITION BY project_id ORDER BY created_at ASC, id ASC) AS row_num
                    FROM project_tasks
                    WHERE issue_key IS NULL
                )
                UPDATE project_tasks pt
                SET issue_key = p.code || '-' || rn.row_num
                FROM rn
                JOIN projects p ON p.id = rn.project_id
                WHERE pt.id = rn.id
                  AND pt.issue_key IS NULL;

                -- Verify: no nulls remain
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM project_tasks WHERE issue_key IS NULL) THEN
                        RAISE EXCEPTION 'Backfill incomplete: issue_key still NULL on some rows';
                    END IF;
                END $$;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                -- Reset backfilled values (reverts to pre-backfill state)
                UPDATE project_tasks SET discriminator = NULL, issue_key = NULL;
                """);
        }
    }
}
