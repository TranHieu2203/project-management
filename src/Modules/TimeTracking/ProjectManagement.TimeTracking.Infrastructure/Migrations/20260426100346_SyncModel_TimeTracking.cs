using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectManagement.TimeTracking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncModel_TimeTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "import_job_id",
                schema: "time_tracking",
                table: "time_entries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "row_fingerprint",
                schema: "time_tracking",
                table: "time_entries",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "import_job_errors",
                schema: "time_tracking",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    import_job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    row_index = table.Column<int>(type: "integer", nullable: false),
                    column_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    error_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_import_job_errors", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "import_jobs",
                schema: "time_tracking",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vendor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    file_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    raw_content = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    total_rows = table.Column<int>(type: "integer", nullable: false),
                    error_count = table.Column<int>(type: "integer", nullable: false),
                    entered_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_import_jobs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "period_locks",
                schema: "time_tracking",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vendor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    month = table.Column<int>(type: "integer", nullable: false),
                    locked_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    locked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_period_locks", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_time_entries_import_job_fingerprint",
                schema: "time_tracking",
                table: "time_entries",
                columns: new[] { "import_job_id", "row_fingerprint" },
                unique: true,
                filter: "import_job_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_import_job_errors_job_id",
                schema: "time_tracking",
                table: "import_job_errors",
                column: "import_job_id");

            migrationBuilder.CreateIndex(
                name: "ix_import_jobs_file_hash",
                schema: "time_tracking",
                table: "import_jobs",
                column: "file_hash");

            migrationBuilder.CreateIndex(
                name: "ix_import_jobs_vendor_id",
                schema: "time_tracking",
                table: "import_jobs",
                column: "vendor_id");

            migrationBuilder.CreateIndex(
                name: "ix_period_locks_vendor_year_month",
                schema: "time_tracking",
                table: "period_locks",
                columns: new[] { "vendor_id", "year", "month" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "import_job_errors",
                schema: "time_tracking");

            migrationBuilder.DropTable(
                name: "import_jobs",
                schema: "time_tracking");

            migrationBuilder.DropTable(
                name: "period_locks",
                schema: "time_tracking");

            migrationBuilder.DropIndex(
                name: "ix_time_entries_import_job_fingerprint",
                schema: "time_tracking",
                table: "time_entries");

            migrationBuilder.DropColumn(
                name: "import_job_id",
                schema: "time_tracking",
                table: "time_entries");

            migrationBuilder.DropColumn(
                name: "row_fingerprint",
                schema: "time_tracking",
                table: "time_entries");
        }
    }
}
