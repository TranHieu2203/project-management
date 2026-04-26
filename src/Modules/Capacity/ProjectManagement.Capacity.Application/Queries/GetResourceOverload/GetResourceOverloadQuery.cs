using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.TimeTracking.Application.Common.Interfaces;

namespace ProjectManagement.Capacity.Application.Queries.GetResourceOverload;

public sealed record GetResourceOverloadQuery(Guid ResourceId, DateOnly DateFrom, DateOnly DateTo)
    : IRequest<ResourceOverloadResult>;

public sealed record OverloadDayResult(DateOnly Date, decimal Hours, bool IsOverloaded);

public sealed record OverloadWeekResult(
    DateOnly WeekStart,
    decimal TotalHours,
    bool IsOverloaded,
    IReadOnlyList<OverloadDayResult> Days);

public sealed record ResourceOverloadResult(
    Guid ResourceId,
    IReadOnlyList<OverloadDayResult> DailyBreakdown,
    IReadOnlyList<OverloadWeekResult> WeeklyBreakdown,
    bool HasOverload);

public sealed class GetResourceOverloadHandler : IRequestHandler<GetResourceOverloadQuery, ResourceOverloadResult>
{
    private readonly ITimeTrackingDbContext _db;
    public GetResourceOverloadHandler(ITimeTrackingDbContext db) => _db = db;

    public async Task<ResourceOverloadResult> Handle(GetResourceOverloadQuery query, CancellationToken ct)
    {
        var entries = await _db.TimeEntries.AsNoTracking()
            .Where(e => e.ResourceId == query.ResourceId
                     && !e.IsVoided
                     && e.Date >= query.DateFrom
                     && e.Date <= query.DateTo)
            .Select(e => new { e.Date, e.Hours })
            .ToListAsync(ct);

        // OL-01: >8h/day
        var dailyBreakdown = entries
            .GroupBy(e => e.Date)
            .Select(g =>
            {
                var total = g.Sum(e => e.Hours);
                return new OverloadDayResult(g.Key, total, total > 8m);
            })
            .OrderBy(d => d.Date)
            .ToList();

        // OL-02: >40h/week (Mon–Sun)
        var weeklyBreakdown = entries
            .GroupBy(e => GetMonday(e.Date))
            .Select(g =>
            {
                var days = g.GroupBy(e => e.Date)
                    .Select(d =>
                    {
                        var dh = d.Sum(e => e.Hours);
                        return new OverloadDayResult(d.Key, dh, dh > 8m);
                    })
                    .OrderBy(d => d.Date)
                    .ToList();
                var total = g.Sum(e => e.Hours);
                return new OverloadWeekResult(g.Key, total, total > 40m, days);
            })
            .OrderBy(w => w.WeekStart)
            .ToList();

        return new ResourceOverloadResult(
            query.ResourceId,
            dailyBreakdown,
            weeklyBreakdown,
            dailyBreakdown.Any(d => d.IsOverloaded) || weeklyBreakdown.Any(w => w.IsOverloaded));
    }

    private static DateOnly GetMonday(DateOnly date)
    {
        var diff = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.AddDays(-diff);
    }
}
