using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ProjectManagement.Projects.Infrastructure.Persistence;

#nullable disable

namespace ProjectManagement.Projects.Infrastructure.Migrations
{
    [DbContext(typeof(ProjectsDbContext))]
    [Migration("20260501154300_V008_006_ProjectIssueTypeSettings")]
    public partial class V008_006_ProjectIssueTypeSettings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "project_issue_type_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    issue_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_project_issue_type_settings", x => x.id);
                    table.ForeignKey(
                        name: "fk_project_issue_type_settings_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_project_issue_type_settings_issue_type_definitions_issue_type_id",
                        column: x => x.issue_type_id,
                        principalTable: "issue_type_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "uq_project_issue_type_settings_project_type",
                table: "project_issue_type_settings",
                columns: new[] { "project_id", "issue_type_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_project_issue_type_settings_project_id",
                table: "project_issue_type_settings",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_project_issue_type_settings_issue_type_id",
                table: "project_issue_type_settings",
                column: "issue_type_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "project_issue_type_settings");
        }
    }
}

