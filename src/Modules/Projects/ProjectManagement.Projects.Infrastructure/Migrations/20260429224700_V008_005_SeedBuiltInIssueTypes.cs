using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectManagement.Projects.Infrastructure.Migrations
{
    public partial class V008_005_SeedBuiltInIssueTypes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                -- V008_005: Seed 5 built-in issue types (global, project_id = NULL, not deletable)
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
    }
}
