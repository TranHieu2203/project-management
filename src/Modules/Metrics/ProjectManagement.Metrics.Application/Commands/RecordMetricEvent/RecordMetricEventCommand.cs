using MediatR;
using ProjectManagement.Metrics.Application.Common.Interfaces;
using ProjectManagement.Metrics.Domain.Entities;
using ProjectManagement.Metrics.Domain.Enums;

namespace ProjectManagement.Metrics.Application.Commands.RecordMetricEvent;

public record RecordMetricEventCommand(
    string EventType,
    Guid ActorId,
    string? ContextJson,
    string? CorrelationId
) : IRequest;

public class RecordMetricEventHandler : IRequestHandler<RecordMetricEventCommand>
{
    private readonly IMetricsDbContext _db;

    public RecordMetricEventHandler(IMetricsDbContext db) => _db = db;

    public async Task Handle(RecordMetricEventCommand cmd, CancellationToken ct)
    {
        if (!MetricEventType.IsValid(cmd.EventType))
            throw new ArgumentException($"Invalid eventType: '{cmd.EventType}'", nameof(cmd.EventType));

        var ev = MetricEvent.Create(cmd.EventType, cmd.ActorId, cmd.ContextJson, cmd.CorrelationId);
        _db.MetricEvents.Add(ev);
        await _db.SaveChangesAsync(ct);
    }
}
