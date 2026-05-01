using MediatR;

namespace ProjectManagement.Projects.Application.Notifications;

public record TaskAssignedNotification(
    Guid TaskId,
    string TaskName,
    Guid ProjectId,
    string ProjectName,
    Guid? PreviousAssigneeUserId,
    Guid? NewAssigneeUserId
) : INotification;
