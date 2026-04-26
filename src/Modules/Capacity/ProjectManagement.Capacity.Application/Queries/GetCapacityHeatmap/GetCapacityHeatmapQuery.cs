using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Projects.Application.Common.Interfaces;
using ProjectManagement.TimeTracking.Application.Common.Interfaces;

namespace ProjectManagement.Capacity.Application.Queries.GetCapacityHeatmap;

public sealed record HeatmapCell(
    DateOnly WeekStart,
    decimal UtilizationPct,
    string TrafficLight,
    decimal ActualHours,
    decimal AvailableHours);

public sealed record HeatmapRow(
    Guid ResourceId,
    IReadOnlyList<HeatmapCell> Cells);

public sealed record CapacityHeatmapResult(
    IReadOnlyList<DateOnly> Weeks,
    IReadOnlyList<HeatmapRow> Rows,
    DateOnly DateFrom,
    DateOnly DateTo,
    int ProjectCount);

public sealed record GetCapacityHeatmapQuery(
    Guid CurrentUserId, DateOnly DateFrom, DateOnly DateTo)
    : IRequest<CapacityHeatmapResult>;

public sealed class GetCapacityHeatmapHandler
    : IRequestHandler<GetCapacityHeatmapQuery, CapacityHeatmapResult>
{
    private readonly IProjectsDbContext _projectsDb;
    private readonly ITimeTrackingDbContext _timeTrackingDb;

    public GetCapacityHeatmapHandler(
        IProjectsDbContext projectsDb,
        ITimeTrackingDbContext timeTrackingDb)
    {
        _projectsDb = projectsDb;
        _timeTrackingDb = timeTrackingDb;
    }

    public async Task<CapacityHeatmapResult> Handle(
        GetCapacityHeatmapQuery query, CancellationToken ct)
    {
        var projectIds = await _projectsDb.ProjectMemberships
            .Where(m => m.UserId == query.CurrentUserId)
            .Select(m => m.ProjectId)
            .Distinct()
            .ToListAsync(ct);

        if (projectIds.Count == 0)
            return new CapacityHeatmapResult([], [], query.DateFrom, query.DateTo, 0);

        var weeks = BuildWeeks(query.DateFrom, query.DateTo);

        var entries = await _timeTrackingDb.TimeEntries.AsNoTracking()
            .Where(e => projectIds.Contains(e.ProjectId)
                     && !e.IsVoided
                     && e.Date >= query.DateFrom
                     && e.Date <= query.DateTo)
            .Select(e => new { e.ResourceId, e.Date, e.Hours })
            .ToListAsync(ct);

        var rows = entries
            .GroupBy(e => e.ResourceId)
            .Select(resourceGroup =>
            {
                var byWeek = resourceGroup.ToLookup(e => GetMonday(e.Date));

                var cells = weeks.Select(weekStart =>
                {
                    var weekEnd = weekStart.AddDays(4); // Friday
                    var effectiveStart = weekStart < query.DateFrom ? query.DateFrom : weekStart;
                    var effectiveEnd = weekEnd > query.DateTo ? query.DateTo : weekEnd;

                    var weekdays = CountWeekdays(effectiveStart, effectiveEnd);
                    var available = weekdays * 8m;
                    var actual = byWeek[weekStart].Sum(e => e.Hours);

                    var utilizationPct = available > 0
                        ? Math.Round(actual / available * 100, 1)
                        : 0m;

                    var trafficLight = utilizationPct switch
                    {
                        >= 105m => "Red",
                        >= 95m  => "Orange",
                        >= 80m  => "Yellow",
                        _       => "Green",
                    };

                    return new HeatmapCell(weekStart, utilizationPct, trafficLight, actual, available);
                }).ToList();

                return new HeatmapRow(resourceGroup.Key, cells);
            })
            .OrderByDescending(r => r.Cells.Count(c => c.TrafficLight is "Red" or "Orange"))
            .ThenByDescending(r => r.Cells.Max(c => c.UtilizationPct))
            .ToList();

        return new CapacityHeatmapResult(weeks, rows, query.DateFrom, query.DateTo, projectIds.Count);
    }

    private static List<DateOnly> BuildWeeks(DateOnly dateFrom, DateOnly dateTo)
    {
        var weeks = new List<DateOnly>();
        var monday = GetMonday(dateFrom);
        while (monday <= dateTo)
        {
            weeks.Add(monday);
            monday = monday.AddDays(7);
        }
        return weeks;
    }

    private static DateOnly GetMonday(DateOnly date)
    {
        var diff = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.AddDays(-diff);
    }

    private static int CountWeekdays(DateOnly from, DateOnly to)
    {
        int count = 0;
        for (var d = from; d <= to; d = d.AddDays(1))
            if (d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday)
                count++;
        return count;
    }
}
