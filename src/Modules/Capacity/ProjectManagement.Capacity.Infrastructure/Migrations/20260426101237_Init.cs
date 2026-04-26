using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectManagement.Capacity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "capacity");

            migrationBuilder.CreateTable(
                name: "capacity_overrides",
                schema: "capacity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date_from = table.Column<DateOnly>(type: "date", nullable: false),
                    date_to = table.Column<DateOnly>(type: "date", nullable: false),
                    traffic_light = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    overridden_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    overridden_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_capacity_overrides", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "forecast_artifacts",
                schema: "capacity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    computed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    payload = table.Column<string>(type: "text", nullable: true),
                    error_message = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_forecast_artifacts", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_capacity_overrides_resource_id",
                schema: "capacity",
                table: "capacity_overrides",
                column: "resource_id");

            migrationBuilder.CreateIndex(
                name: "ix_forecast_artifacts_version",
                schema: "capacity",
                table: "forecast_artifacts",
                column: "version");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "capacity_overrides",
                schema: "capacity");

            migrationBuilder.DropTable(
                name: "forecast_artifacts",
                schema: "capacity");
        }
    }
}
