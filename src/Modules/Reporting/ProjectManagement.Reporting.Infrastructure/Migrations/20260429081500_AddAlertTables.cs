using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectManagement.Reporting.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAlertTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS reporting.alerts (
                    id uuid NOT NULL,
                    project_id uuid,
                    user_id uuid NOT NULL,
                    type character varying(50) NOT NULL,
                    entity_type character varying(50),
                    entity_id uuid,
                    title character varying(500) NOT NULL,
                    description text,
                    is_read boolean NOT NULL DEFAULT false,
                    created_at timestamp with time zone NOT NULL,
                    read_at timestamp with time zone,
                    CONSTRAINT "PK_alerts" PRIMARY KEY (id)
                );
                CREATE TABLE IF NOT EXISTS reporting.alert_preferences (
                    id uuid NOT NULL,
                    user_id uuid NOT NULL,
                    alert_type character varying(50) NOT NULL,
                    enabled boolean NOT NULL DEFAULT true,
                    threshold_days integer,
                    CONSTRAINT "PK_alert_preferences" PRIMARY KEY (id)
                );
                CREATE INDEX IF NOT EXISTS ix_alerts_user_read ON reporting.alerts (user_id, is_read, created_at DESC);
                CREATE UNIQUE INDEX IF NOT EXISTS ix_alert_preferences_user_type ON reporting.alert_preferences (user_id, alert_type);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alert_preferences",
                schema: "reporting");

            migrationBuilder.DropTable(
                name: "alerts",
                schema: "reporting");
        }
    }
}
