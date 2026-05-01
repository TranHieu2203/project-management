using MediatR;

namespace ProjectManagement.Projects.Application.Notifications;

public record TaskStatusChangedNotification(
    Guid TaskId,
    string TaskName,
    Guid ProjectId,
    string ProjectName,
    string PreviousStatus,
    string NewStatus,
    Guid? AssigneeUserId
) : INotification;
