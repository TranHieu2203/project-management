using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Capacity.Application.Commands.TriggerForecastCompute;
using ProjectManagement.Capacity.Application.Common.Interfaces;

namespace ProjectManagement.Capacity.Application.Queries.GetForecastDelta;

public sealed record ForecastDeltaItem(
    Guid ResourceId,
    string WeekStart,
    decimal PreviousUtilizationPct,
    decimal CurrentUtilizationPct,
    decimal DeltaPct,
    string CurrentTrafficLight,
    string Hint);

public sealed record ForecastDeltaResult(
    int CurrentVersion,
    int PreviousVersion,
    IReadOnlyList<ForecastDeltaItem> TopChanges,
    bool HasData);

public sealed record GetForecastDeltaQuery : IRequest<ForecastDeltaResult>;

public sealed class GetForecastDeltaHandler
    : IRequestHandler<GetForecastDeltaQuery, ForecastDeltaResult>
{
    private readonly ICapacityDbContext _capacityDb;

    public GetForecastDeltaHandler(ICapacityDbContext capacityDb)
    {
        _capacityDb = capacityDb;
    }

    public async Task<ForecastDeltaResult> Handle(
        GetForecastDeltaQuery query, CancellationToken ct)
    {
        var artifacts = await _capacityDb.ForecastArtifacts
            .Where(a => a.Status == "Succeeded")
            .OrderByDescending(a => a.Version)
            .Take(2)
            .ToListAsync(ct);

        if (artifacts.Count < 2)
            return new ForecastDeltaResult(
                artifacts.Count == 1 ? artifacts[0].Version : 0, 0, [], false);

        var current  = JsonSerializer.Deserialize<ForecastPayload>(artifacts[0].Payload!)!;
        var previous = JsonSerializer.Deserialize<ForecastPayload>(artifacts[1].Payload!)!;

        var prevLookup = previous.Resources
            .SelectMany(r => r.Cells.Select(c => (r.ResourceId, c.WeekStart, c.ForecastedUtilizationPct)))
            .ToDictionary(x => (x.ResourceId, x.WeekStart), x => x.ForecastedUtilizationPct);

        var deltas = current.Resources
            .SelectMany(r => r.Cells.Select(c =>
            {
                var prevPct = prevLookup.TryGetValue((r.ResourceId, c.WeekStart), out var p) ? p : 0m;
                var deltaPct = Math.Round(c.ForecastedUtilizationPct - prevPct, 1);
                var hint = BuildHint(c.ForecastedUtilizationPct, c.TrafficLight, prevPct, deltaPct);
                return new ForecastDeltaItem(
                    r.ResourceId, c.WeekStart, prevPct,
                    c.ForecastedUtilizationPct, deltaPct, c.TrafficLight, hint);
            }))
            .OrderByDescending(d => Math.Abs(d.DeltaPct))
            .Take(10)
            .ToList();

        return new ForecastDeltaResult(artifacts[0].Version, artifacts[1].Version, deltas, true);
    }

    private static string BuildHint(
        decimal currentPct, string currentLight, decimal prevPct, decimal deltaPct)
    {
        if (currentLight is "Red" or "Orange")
            return "Nguy cơ overload — cân nhắc điều chỉnh phân công";

        if (prevPct >= 95m && currentPct < 95m)
            return "Cải thiện capacity";

        if (Math.Abs(deltaPct) > 20m)
            return deltaPct > 0 ? "Tăng đáng kể" : "Giảm đáng kể";

        return "Theo dõi";
    }
}
