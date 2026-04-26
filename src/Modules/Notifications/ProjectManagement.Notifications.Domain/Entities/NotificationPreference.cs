namespace ProjectManagement.Notifications.Domain.Entities;

public class NotificationPreference
{
    public Guid UserId { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public bool IsEnabled { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public static NotificationPreference Create(Guid userId, string type, bool isEnabled)
        => new() { UserId = userId, Type = type, IsEnabled = isEnabled, UpdatedAt = DateTime.UtcNow };

    public void SetEnabled(bool isEnabled) { IsEnabled = isEnabled; UpdatedAt = DateTime.UtcNow; }
}
