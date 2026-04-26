namespace ProjectManagement.Metrics.Domain.Entities;

public class MetricEvent
{
    public Guid Id { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public Guid ActorId { get; private set; }
    public string? ContextJson { get; private set; }
    public string? CorrelationId { get; private set; }
    public DateTime OccurredAt { get; private set; }

    public static MetricEvent Create(
        string eventType, Guid actorId,
        string? contextJson, string? correlationId)
        => new()
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            ActorId = actorId,
            ContextJson = contextJson,
            CorrelationId = correlationId,
            OccurredAt = DateTime.UtcNow
        };
}
