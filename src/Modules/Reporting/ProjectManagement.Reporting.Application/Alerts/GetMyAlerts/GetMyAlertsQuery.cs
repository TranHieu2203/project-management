using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Reporting.Application.Common.Interfaces;

namespace ProjectManagement.Reporting.Application.Alerts.GetMyAlerts;

public sealed record AlertDto(
    Guid Id,
    Guid? ProjectId,
    string Type,
    string? EntityType,
    Guid? EntityId,
    string Title,
    string? Description,
    bool IsRead,
    DateTime CreatedAt,
    DateTime? ReadAt);

public sealed record GetMyAlertsQuery(
    Guid CurrentUserId,
    bool UnreadOnly = false,
    int Page = 1,
    int PageSize = 20) : IRequest<GetMyAlertsResult>;

public sealed record GetMyAlertsResult(IReadOnlyList<AlertDto> Items, int TotalCount);

public sealed class GetMyAlertsHandler : IRequestHandler<GetMyAlertsQuery, GetMyAlertsResult>
{
    private readonly IReportingDbContext _db;

    public GetMyAlertsHandler(IReportingDbContext db) => _db = db;

    public async Task<GetMyAlertsResult> Handle(GetMyAlertsQuery request, CancellationToken ct)
    {
        var query = _db.Alerts
            .AsNoTracking()
            .Where(a => a.UserId == request.CurrentUserId);

        if (request.UnreadOnly)
            query = query.Where(a => !a.IsRead);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new AlertDto(
                a.Id,
                a.ProjectId,
                a.Type,
                a.EntityType,
                a.EntityId,
                a.Title,
                a.Description,
                a.IsRead,
                a.CreatedAt,
                a.ReadAt))
            .ToListAsync(ct);

        return new GetMyAlertsResult(items, totalCount);
    }
}
