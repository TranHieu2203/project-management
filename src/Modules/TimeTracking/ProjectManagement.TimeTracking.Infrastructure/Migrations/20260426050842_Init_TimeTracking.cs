using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectManagement.TimeTracking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Init_TimeTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "time_tracking");

            migrationBuilder.CreateTable(
                name: "time_entries",
                schema: "time_tracking",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_id = table.Column<Guid>(type: "uuid", nullable: true),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    hours = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    entry_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    rate_at_time = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    cost_at_time = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    entered_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_time_entries", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_time_entries_date",
                schema: "time_tracking",
                table: "time_entries",
                column: "date");

            migrationBuilder.CreateIndex(
                name: "ix_time_entries_project_date",
                schema: "time_tracking",
                table: "time_entries",
                columns: new[] { "project_id", "date" });

            migrationBuilder.CreateIndex(
                name: "ix_time_entries_resource_date",
                schema: "time_tracking",
                table: "time_entries",
                columns: new[] { "resource_id", "date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "time_entries",
                schema: "time_tracking");
        }
    }
}
