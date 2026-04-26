using MediatR;
using ProjectManagement.Capacity.Application.Common.Interfaces;
using ProjectManagement.Capacity.Domain.Entities;

namespace ProjectManagement.Capacity.Application.Commands.LogCapacityOverride;

public sealed record LogCapacityOverrideCommand(
    Guid ResourceId,
    DateOnly DateFrom,
    DateOnly DateTo,
    string TrafficLight,
    string OverriddenBy) : IRequest;

public sealed class LogCapacityOverrideHandler : IRequestHandler<LogCapacityOverrideCommand>
{
    private readonly ICapacityDbContext _db;
    public LogCapacityOverrideHandler(ICapacityDbContext db) => _db = db;

    public async Task Handle(LogCapacityOverrideCommand cmd, CancellationToken ct)
    {
        var record = CapacityOverride.Create(
            cmd.ResourceId, cmd.DateFrom, cmd.DateTo, cmd.TrafficLight, cmd.OverriddenBy);
        _db.CapacityOverrides.Add(record);
        await _db.SaveChangesAsync(ct);
    }
}
