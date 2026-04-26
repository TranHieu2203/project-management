using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Notifications.Application.Common.Interfaces;
using ProjectManagement.Notifications.Domain.Enums;

namespace ProjectManagement.Notifications.Application.Queries.GetNotificationPreferences;

public sealed record NotificationPreferenceDto(string Type, bool IsEnabled);

public sealed record GetNotificationPreferencesQuery(Guid UserId) : IRequest<List<NotificationPreferenceDto>>;

public sealed class GetNotificationPreferencesHandler
    : IRequestHandler<GetNotificationPreferencesQuery, List<NotificationPreferenceDto>>
{
    private readonly INotificationsDbContext _db;

    public GetNotificationPreferencesHandler(INotificationsDbContext db) => _db = db;

    public async Task<List<NotificationPreferenceDto>> Handle(
        GetNotificationPreferencesQuery query, CancellationToken ct)
    {
        var stored = await _db.NotificationPreferences
            .AsNoTracking()
            .Where(p => p.UserId == query.UserId)
            .ToListAsync(ct);

        var allTypes = new[] { NotificationType.Overload, NotificationType.Overdue };
        return allTypes.Select(type =>
        {
            var pref = stored.FirstOrDefault(p => p.Type == type);
            return new NotificationPreferenceDto(type, pref?.IsEnabled ?? true);
        }).ToList();
    }
}
