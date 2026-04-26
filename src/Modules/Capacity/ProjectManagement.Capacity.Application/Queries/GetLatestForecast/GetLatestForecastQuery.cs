using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Capacity.Application.Commands.TriggerForecastCompute;
using ProjectManagement.Capacity.Application.Common.Interfaces;

namespace ProjectManagement.Capacity.Application.Queries.GetLatestForecast;

public sealed record ForecastResultDto(
    int Version,
    DateTime? ComputedAt,
    List<string> Weeks,
    List<ForecastResourceRow> Rows);

public sealed record GetLatestForecastQuery : IRequest<ForecastResultDto>;

public sealed class GetLatestForecastHandler
    : IRequestHandler<GetLatestForecastQuery, ForecastResultDto>
{
    private readonly ICapacityDbContext _capacityDb;

    public GetLatestForecastHandler(ICapacityDbContext capacityDb)
    {
        _capacityDb = capacityDb;
    }

    public async Task<ForecastResultDto> Handle(
        GetLatestForecastQuery query, CancellationToken ct)
    {
        var artifact = await _capacityDb.ForecastArtifacts
            .Where(a => a.Status == "Succeeded")
            .OrderByDescending(a => a.Version)
            .FirstOrDefaultAsync(ct);

        if (artifact is null)
            return new ForecastResultDto(0, null, [], []);

        var payload = JsonSerializer.Deserialize<ForecastPayload>(artifact.Payload!)!;

        return new ForecastResultDto(
            artifact.Version,
            artifact.ComputedAt,
            payload.Weeks,
            payload.Resources);
    }
}
