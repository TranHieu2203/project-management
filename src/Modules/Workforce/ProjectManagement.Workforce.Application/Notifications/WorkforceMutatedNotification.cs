using MediatR;

namespace ProjectManagement.Workforce.Application.Notifications;

public sealed record WorkforceMutatedNotification(
    string EntityType,
    Guid EntityId,
    string Action,
    string Actor,
    string Summary
) : INotification;
