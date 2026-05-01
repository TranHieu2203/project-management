using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Notifications.Application.Common.Interfaces;

namespace ProjectManagement.Notifications.Application.Commands.MarkNotificationRead;

public record MarkNotificationReadCommand(Guid NotificationId, Guid RequestingUserId)
    : IRequest<bool>;

public sealed class MarkNotificationReadHandler
    : IRequestHandler<MarkNotificationReadCommand, bool>
{
    private readonly INotificationsDbContext _db;

    public MarkNotificationReadHandler(INotificationsDbContext db) => _db = db;

    public async Task<bool> Handle(MarkNotificationReadCommand cmd, CancellationToken ct)
    {
        var n = await _db.UserNotifications
            .FirstOrDefaultAsync(x => x.Id == cmd.NotificationId
                                   && x.RecipientUserId == cmd.RequestingUserId, ct);
        if (n is null) return false;

        n.MarkRead();
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
