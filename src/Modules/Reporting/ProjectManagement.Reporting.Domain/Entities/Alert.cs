namespace ProjectManagement.Reporting.Domain.Entities;

public class Alert
{
    public Guid Id { get; private set; }
    public Guid? ProjectId { get; private set; }
    public Guid UserId { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string? EntityType { get; private set; }
    public Guid? EntityId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ReadAt { get; private set; }

    public static Alert Create(
        Guid userId,
        string type,
        string title,
        Guid? projectId = null,
        string? entityType = null,
        Guid? entityId = null,
        string? description = null)
        => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            Title = title,
            ProjectId = projectId,
            EntityType = entityType,
            EntityId = entityId,
            Description = description,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
        };

    public void MarkAsRead()
    {
        IsRead = true;
        ReadAt = DateTime.UtcNow;
    }
}
