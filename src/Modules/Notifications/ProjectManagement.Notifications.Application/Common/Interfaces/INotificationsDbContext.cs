using Microsoft.EntityFrameworkCore;
using ProjectManagement.Notifications.Domain.Entities;

namespace ProjectManagement.Notifications.Application.Common.Interfaces;

public interface INotificationsDbContext
{
    DbSet<NotificationPreference> NotificationPreferences { get; }
    DbSet<DigestLog> DigestLogs { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
