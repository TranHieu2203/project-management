using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Notifications.Application.Common.Interfaces;

namespace ProjectManagement.Notifications.Application.Queries.GetMyNotifications;

public record GetMyNotificationsQuery(Guid UserId, bool UnreadOnly = false)
    : IRequest<List<UserNotificationDto>>;

public sealed class GetMyNotificationsHandler
    : IRequestHandler<GetMyNotificationsQuery, List<UserNotificationDto>>
{
    private readonly INotificationsDbContext _db;

    public GetMyNotificationsHandler(INotificationsDbContext db) => _db = db;

    public async Task<List<UserNotificationDto>> Handle(
        GetMyNotificationsQuery query, CancellationToken ct)
    {
        var q = _db.UserNotifications
            .AsNoTracking()
            .Where(n => n.RecipientUserId == query.UserId);

        if (query.UnreadOnly)
            q = q.Where(n => !n.IsRead);

        return await q
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(n => new UserNotificationDto(
                n.Id, n.Type, n.Title, n.Body,
                n.EntityType, n.EntityId, n.ProjectId,
                n.IsRead, n.CreatedAt, n.ReadAt))
            .ToListAsync(ct);
    }
}
