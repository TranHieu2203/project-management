namespace ProjectManagement.Workforce.Domain.Entities;

public class AuditEvent
{
    public Guid Id { get; private set; }
    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string Actor { get; private set; } = string.Empty;
    public string Summary { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    public static AuditEvent Create(
        string entityType, Guid entityId, string action, string actor, string summary)
        => new()
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            Actor = actor,
            Summary = summary,
            CreatedAt = DateTime.UtcNow,
        };
}
