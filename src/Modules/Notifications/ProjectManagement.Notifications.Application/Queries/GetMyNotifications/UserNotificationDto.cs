namespace ProjectManagement.Notifications.Application.Queries.GetMyNotifications;

public record UserNotificationDto(
    Guid Id,
    string Type,
    string Title,
    string Body,
    string? EntityType,
    Guid? EntityId,
    Guid? ProjectId,
    bool IsRead,
    DateTime CreatedAt,
    DateTime? ReadAt
);
