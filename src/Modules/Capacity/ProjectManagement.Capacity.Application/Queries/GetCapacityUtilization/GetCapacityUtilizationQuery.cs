using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.TimeTracking.Application.Common.Interfaces;

namespace ProjectManagement.Capacity.Application.Queries.GetCapacityUtilization;

public sealed record TopContribution(DateOnly Date, decimal Hours);

public sealed record CapacityUtilizationResult(
    Guid ResourceId,
    decimal UtilizationPct,
    decimal AvailableHours,
    decimal ActualHours,
    string TrafficLight,
    IReadOnlyList<TopContribution> TopContributions);

public sealed record GetCapacityUtilizationQuery(Guid ResourceId, DateOnly DateFrom, DateOnly DateTo)
    : IRequest<CapacityUtilizationResult>;

public sealed class GetCapacityUtilizationHandler
    : IRequestHandler<GetCapacityUtilizationQuery, CapacityUtilizationResult>
{
    private readonly ITimeTrackingDbContext _db;
    public GetCapacityUtilizationHandler(ITimeTrackingDbContext db) => _db = db;

    public async Task<CapacityUtilizationResult> Handle(
        GetCapacityUtilizationQuery query, CancellationToken ct)
    {
        var entries = await _db.TimeEntries.AsNoTracking()
            .Where(e => e.ResourceId == query.ResourceId
                     && !e.IsVoided
                     && e.Date >= query.DateFrom
                     && e.Date <= query.DateTo)
            .Select(e => new { e.Date, e.Hours })
            .ToListAsync(ct);

        var availableHours = CountWeekdays(query.DateFrom, query.DateTo) * 8m;
        var actualHours = entries.Sum(e => e.Hours);
        var utilizationPct = availableHours > 0
            ? Math.Round(actualHours / availableHours * 100, 1)
            : 0m;

        var trafficLight = utilizationPct switch
        {
            >= 105m => "Red",
            >= 95m  => "Orange",
            >= 80m  => "Yellow",
            _       => "Green",
        };

        var topContributions = entries
            .GroupBy(e => e.Date)
            .Select(g => new TopContribution(g.Key, g.Sum(e => e.Hours)))
            .OrderByDescending(d => d.Hours)
            .Take(3)
            .ToList();

        return new CapacityUtilizationResult(
            query.ResourceId, utilizationPct, availableHours, actualHours, trafficLight, topContributions);
    }

    private static int CountWeekdays(DateOnly from, DateOnly to)
    {
        var count = 0;
        for (var d = from; d <= to; d = d.AddDays(1))
            if (d.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
                count++;
        return count;
    }
}
