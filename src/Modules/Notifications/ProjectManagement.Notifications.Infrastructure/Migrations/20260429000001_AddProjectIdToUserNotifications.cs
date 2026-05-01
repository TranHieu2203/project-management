using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ProjectManagement.Notifications.Infrastructure.Persistence;

#nullable disable

namespace ProjectManagement.Notifications.Infrastructure.Migrations
{
    [DbContext(typeof(NotificationsDbContext))]
    [Migration("20260429000001_AddProjectIdToUserNotifications")]
    public partial class AddProjectIdToUserNotifications : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE notifications.user_notifications
                ADD COLUMN IF NOT EXISTS project_id UUID NULL;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE notifications.user_notifications
                DROP COLUMN IF EXISTS project_id;
                """);
        }
    }
}
