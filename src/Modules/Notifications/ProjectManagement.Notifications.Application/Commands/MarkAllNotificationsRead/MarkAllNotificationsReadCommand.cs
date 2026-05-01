using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Notifications.Application.Common.Interfaces;

namespace ProjectManagement.Notifications.Application.Commands.MarkAllNotificationsRead;

public record MarkAllNotificationsReadCommand(Guid RequestingUserId) : IRequest<int>;

public sealed class MarkAllNotificationsReadHandler
    : IRequestHandler<MarkAllNotificationsReadCommand, int>
{
    private readonly INotificationsDbContext _db;

    public MarkAllNotificationsReadHandler(INotificationsDbContext db) => _db = db;

    public async Task<int> Handle(MarkAllNotificationsReadCommand cmd, CancellationToken ct)
    {
        var notifications = await _db.UserNotifications
            .Where(n => n.RecipientUserId == cmd.RequestingUserId && !n.IsRead)
            .ToListAsync(ct);

        foreach (var n in notifications)
            n.MarkRead();

        await _db.SaveChangesAsync(ct);
        return notifications.Count;
    }
}
