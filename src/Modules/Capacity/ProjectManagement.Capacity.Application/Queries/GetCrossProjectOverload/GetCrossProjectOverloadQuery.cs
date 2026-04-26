using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Projects.Application.Common.Interfaces;
using ProjectManagement.TimeTracking.Application.Common.Interfaces;

namespace ProjectManagement.Capacity.Application.Queries.GetCrossProjectOverload;

public sealed record ResourceOverloadSummary(
    Guid ResourceId,
    decimal TotalHours,
    int OverloadedDays,
    int OverloadedWeeks,
    bool HasOverload);

public sealed record CrossProjectOverloadResult(
    IReadOnlyList<ResourceOverloadSummary> Resources,
    DateOnly DateFrom,
    DateOnly DateTo,
    int ProjectCount);

public sealed record GetCrossProjectOverloadQuery(
    Guid CurrentUserId, DateOnly DateFrom, DateOnly DateTo)
    : IRequest<CrossProjectOverloadResult>;

public sealed class GetCrossProjectOverloadHandler
    : IRequestHandler<GetCrossProjectOverloadQuery, CrossProjectOverloadResult>
{
    private readonly IProjectsDbContext _projectsDb;
    private readonly ITimeTrackingDbContext _timeTrackingDb;

    public GetCrossProjectOverloadHandler(
        IProjectsDbContext projectsDb,
        ITimeTrackingDbContext timeTrackingDb)
    {
        _projectsDb = projectsDb;
        _timeTrackingDb = timeTrackingDb;
    }

    public async Task<CrossProjectOverloadResult> Handle(
        GetCrossProjectOverloadQuery query, CancellationToken ct)
    {
        var projectIds = await _projectsDb.ProjectMemberships
            .Where(m => m.UserId == query.CurrentUserId)
            .Select(m => m.ProjectId)
            .Distinct()
            .ToListAsync(ct);

        if (projectIds.Count == 0)
            return new CrossProjectOverloadResult([], query.DateFrom, query.DateTo, 0);

        var entries = await _timeTrackingDb.TimeEntries.AsNoTracking()
            .Where(e => projectIds.Contains(e.ProjectId)
                     && !e.IsVoided
                     && e.Date >= query.DateFrom
                     && e.Date <= query.DateTo)
            .Select(e => new { e.ResourceId, e.Date, e.Hours })
            .ToListAsync(ct);

        var resources = entries
            .GroupBy(e => e.ResourceId)
            .Select(g =>
            {
                var totalHours = g.Sum(e => e.Hours);

                var overloadedDays = g
                    .GroupBy(e => e.Date)
                    .Count(d => d.Sum(e => e.Hours) > 8m);

                var overloadedWeeks = g
                    .GroupBy(e => GetMonday(e.Date))
                    .Count(w => w.Sum(e => e.Hours) > 40m);

                return new ResourceOverloadSummary(
                    g.Key, totalHours, overloadedDays, overloadedWeeks,
                    overloadedDays > 0 || overloadedWeeks > 0);
            })
            .OrderByDescending(r => r.TotalHours)
            .ToList();

        return new CrossProjectOverloadResult(resources, query.DateFrom, query.DateTo, projectIds.Count);
    }

    private static DateOnly GetMonday(DateOnly date)
    {
        var diff = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.AddDays(-diff);
    }
}
