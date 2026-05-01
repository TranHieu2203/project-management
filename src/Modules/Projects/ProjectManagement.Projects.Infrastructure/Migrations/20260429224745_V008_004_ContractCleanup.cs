using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectManagement.Projects.Infrastructure.Migrations
{
    /// <summary>
    /// Phase 4 — Contract/Cleanup. DEFER: Only run after Phase 1-3 stable on staging >= 1 week
    /// and ALL pre-conditions are met (see checklist in story 8-0).
    /// </summary>
    public partial class V008_004_ContractCleanup : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                -- V008_004: Contract phase — enforce NOT NULL, drop compat view, add GIN indexes.
                -- PREREQUISITE: V008_001–003 + V008_005 must be applied and stable.
                -- IRREVERSIBLE without blue/green deployment.

                BEGIN;

                -- 1. Drop the backward-compat view
                DROP VIEW IF EXISTS project_tasks;

                -- 2. Assign issue_type_id from built-in types before enforcing NOT NULL
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

                -- 4. GIN index on custom_fields JSONB for fast key/value queries
                CREATE INDEX IF NOT EXISTS idx_issues_custom_fields_gin
                    ON issues USING gin(custom_fields);

                -- 5. Full-text search: generated tsvector column
                ALTER TABLE issues
                    ADD COLUMN IF NOT EXISTS search_vector tsvector
                        GENERATED ALWAYS AS (
                            to_tsvector('simple',
                                coalesce(name, '')  || ' ' ||
                                coalesce(notes, '') || ' ' ||
                                coalesce(issue_key, '')
                            )
                        ) STORED;

                CREATE INDEX IF NOT EXISTS idx_issues_search_gin
                    ON issues USING gin(search_vector);

                -- 6. Structural indexes
                CREATE INDEX IF NOT EXISTS ix_issues_discriminator  ON issues(discriminator);
                CREATE INDEX IF NOT EXISTS ix_issues_issue_type_id  ON issues(issue_type_id);

                COMMIT;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
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
                    SELECT * FROM issues;
                """);
        }
    }
}
