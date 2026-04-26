using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectManagement.Reporting.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "reporting");

            migrationBuilder.CreateTable(
                name: "export_jobs",
                schema: "reporting",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    triggered_by = table.Column<Guid>(type: "uuid", nullable: false),
                    format = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    group_by = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    filter_params = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    file_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    file_content = table.Column<byte[]>(type: "bytea", nullable: true),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_export_jobs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_export_jobs_status",
                schema: "reporting",
                table: "export_jobs",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_export_jobs_triggered_by",
                schema: "reporting",
                table: "export_jobs",
                column: "triggered_by");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "export_jobs",
                schema: "reporting");
        }
    }
}
