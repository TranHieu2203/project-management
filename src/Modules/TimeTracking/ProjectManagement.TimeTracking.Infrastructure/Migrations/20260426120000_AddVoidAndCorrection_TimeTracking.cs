using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectManagement.TimeTracking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVoidAndCorrection_TimeTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_voided",
                schema: "time_tracking",
                table: "time_entries",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "void_reason",
                schema: "time_tracking",
                table: "time_entries",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "voided_by",
                schema: "time_tracking",
                table: "time_entries",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "voided_at",
                schema: "time_tracking",
                table: "time_entries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "supersedes_id",
                schema: "time_tracking",
                table: "time_entries",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_time_entries_supersedes_id",
                schema: "time_tracking",
                table: "time_entries",
                column: "supersedes_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_time_entries_supersedes_id",
                schema: "time_tracking",
                table: "time_entries");

            migrationBuilder.DropColumn(
                name: "is_voided",
                schema: "time_tracking",
                table: "time_entries");

            migrationBuilder.DropColumn(
                name: "void_reason",
                schema: "time_tracking",
                table: "time_entries");

            migrationBuilder.DropColumn(
                name: "voided_by",
                schema: "time_tracking",
                table: "time_entries");

            migrationBuilder.DropColumn(
                name: "voided_at",
                schema: "time_tracking",
                table: "time_entries");

            migrationBuilder.DropColumn(
                name: "supersedes_id",
                schema: "time_tracking",
                table: "time_entries");
        }
    }
}
