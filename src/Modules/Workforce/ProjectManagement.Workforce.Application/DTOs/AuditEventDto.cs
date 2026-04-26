namespace ProjectManagement.Workforce.Application.DTOs;

public sealed record AuditEventDto(
    Guid Id,
    string EntityType,
    Guid EntityId,
    string Action,
    string Actor,
    string Summary,
    DateTime CreatedAt
);
