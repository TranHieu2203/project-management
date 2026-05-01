namespace ProjectManagement.Notifications.Domain.Entities;

public class UserNotification
{
    public Guid Id { get; private set; }
    public Guid RecipientUserId { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public string? EntityType { get; private set; }
    public Guid? EntityId { get; private set; }
    public Guid? ProjectId { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ReadAt { get; private set; }

    public static UserNotification Create(Guid recipientUserId, string type,
        string title, string body, string? entityType = null, Guid? entityId = null,
        Guid? projectId = null)
        => new()
        {
            Id = Guid.NewGuid(),
            RecipientUserId = recipientUserId,
            Type = type,
            Title = title,
            Body = body,
            EntityType = entityType,
            EntityId = entityId,
            ProjectId = projectId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
        };

    public void MarkRead()
    {
        IsRead = true;
        ReadAt = DateTime.UtcNow;
    }
}
