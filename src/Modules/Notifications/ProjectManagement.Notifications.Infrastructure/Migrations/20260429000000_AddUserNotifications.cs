using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ProjectManagement.Notifications.Infrastructure.Persistence;

#nullable disable

namespace ProjectManagement.Notifications.Infrastructure.Migrations
{
    [DbContext(typeof(NotificationsDbContext))]
    [Migration("20260429000000_AddUserNotifications")]
    public partial class AddUserNotifications : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TABLE notifications.user_notifications (
                    id                UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                    recipient_user_id UUID NOT NULL,
                    type              VARCHAR(30) NOT NULL,
                    title             VARCHAR(200) NOT NULL,
                    body              TEXT NOT NULL DEFAULT '',
                    entity_type       VARCHAR(30) NULL,
                    entity_id         UUID NULL,
                    is_read           BOOLEAN NOT NULL DEFAULT FALSE,
                    created_at        TIMESTAMPTZ NOT NULL DEFAULT now(),
                    read_at           TIMESTAMPTZ NULL
                );
                CREATE INDEX ix_user_notifications_user_read_created
                    ON notifications.user_notifications(recipient_user_id, is_read, created_at DESC);
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS notifications.user_notifications;");
        }
    }
}
