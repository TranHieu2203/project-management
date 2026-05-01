using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectManagement.Projects.Infrastructure.Migrations
{
    public partial class V008_002_BackfillDiscriminatorAndIssueKey : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                -- V008_002: Idempotent backfill — safe to run multiple times.
                BEGIN;

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
