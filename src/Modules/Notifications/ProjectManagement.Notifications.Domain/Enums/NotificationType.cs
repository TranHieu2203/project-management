namespace ProjectManagement.Notifications.Domain.Enums;

public static class NotificationType
{
    public const string Overload = "overload";
    public const string Overdue  = "overdue";
    // Per-event types (Story 7-4)
    public const string Assigned      = "assigned";
    public const string Commented     = "commented";
    public const string StatusChanged = "status-changed";
    public const string Mentioned     = "mentioned";
}
