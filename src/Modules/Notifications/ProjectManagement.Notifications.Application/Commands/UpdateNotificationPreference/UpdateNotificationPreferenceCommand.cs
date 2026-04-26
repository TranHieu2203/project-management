using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Notifications.Application.Common.Interfaces;
using ProjectManagement.Notifications.Domain.Entities;

namespace ProjectManagement.Notifications.Application.Commands.UpdateNotificationPreference;

public sealed record UpdateNotificationPreferenceCommand(
    Guid UserId, string Type, bool IsEnabled) : IRequest;

public sealed class UpdateNotificationPreferenceHandler : IRequestHandler<UpdateNotificationPreferenceCommand>
{
    private readonly INotificationsDbContext _db;

    public UpdateNotificationPreferenceHandler(INotificationsDbContext db) => _db = db;

    public async Task Handle(UpdateNotificationPreferenceCommand cmd, CancellationToken ct)
    {
        var pref = await _db.NotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == cmd.UserId && p.Type == cmd.Type, ct);

        if (pref is null)
        {
            pref = NotificationPreference.Create(cmd.UserId, cmd.Type, cmd.IsEnabled);
            _db.NotificationPreferences.Add(pref);
        }
        else
        {
            pref.SetEnabled(cmd.IsEnabled);
        }

        await _db.SaveChangesAsync(ct);
    }
}
