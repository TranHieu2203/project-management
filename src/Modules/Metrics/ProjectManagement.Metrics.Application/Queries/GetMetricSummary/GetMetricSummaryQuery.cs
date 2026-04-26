using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Metrics.Application.Common.Interfaces;

namespace ProjectManagement.Metrics.Application.Queries.GetMetricSummary;

public record GetMetricSummaryQuery(
    DateTime? From,
    DateTime? To,
    string? EventType
) : IRequest<MetricSummaryDto>;

public record MetricSummaryDto(
    int TotalEvents,
    List<MetricCountByTypeDto> ByType,
    Dictionary<string, int> ByDay
);

public record MetricCountByTypeDto(string EventType, int Count);

public class GetMetricSummaryHandler : IRequestHandler<GetMetricSummaryQuery, MetricSummaryDto>
{
    private readonly IMetricsDbContext _db;

    public GetMetricSummaryHandler(IMetricsDbContext db) => _db = db;

    public async Task<MetricSummaryDto> Handle(GetMetricSummaryQuery q, CancellationToken ct)
    {
        var query = _db.MetricEvents.AsNoTracking();

        if (q.From.HasValue)
            query = query.Where(e => e.OccurredAt >= q.From.Value);
        if (q.To.HasValue)
            query = query.Where(e => e.OccurredAt <= q.To.Value);
        if (!string.IsNullOrEmpty(q.EventType))
            query = query.Where(e => e.EventType == q.EventType);

        // Load only required fields into memory to avoid EF translation issues
        var events = await query
            .Select(e => new { e.EventType, e.OccurredAt })
            .ToListAsync(ct);

        var byType = events
            .GroupBy(e => e.EventType)
            .Select(g => new MetricCountByTypeDto(g.Key, g.Count()))
            .ToList();

        var byDay = events
            .GroupBy(e => e.OccurredAt.ToString("yyyy-MM-dd"))
            .ToDictionary(g => g.Key, g => g.Count());

        return new MetricSummaryDto(events.Count, byType, byDay);
    }
}
