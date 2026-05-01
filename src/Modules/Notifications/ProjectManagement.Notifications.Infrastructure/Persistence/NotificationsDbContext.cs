using Microsoft.EntityFrameworkCore;
using ProjectManagement.Notifications.Application.Common.Interfaces;
using ProjectManagement.Notifications.Domain.Entities;
using ProjectManagement.Notifications.Infrastructure.Persistence.Configurations;

namespace ProjectManagement.Notifications.Infrastructure.Persistence;

public class NotificationsDbContext : DbContext, INotificationsDbContext
{
    public NotificationsDbContext(DbContextOptions<NotificationsDbContext> options) : base(options) { }

    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public DbSet<DigestLog> DigestLogs => Set<DigestLog>();
    public DbSet<UserNotification> UserNotifications => Set<UserNotification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("notifications");
        modelBuilder.ApplyConfiguration(new NotificationPreferenceConfiguration());
        modelBuilder.ApplyConfiguration(new DigestLogConfiguration());
        modelBuilder.ApplyConfiguration(new UserNotificationConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}
