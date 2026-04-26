namespace ProjectManagement.Projects.Application.DTOs;

public sealed record ProjectMemberDto(
    Guid UserId,
    string Username,
    string? DisplayName,
    string Role,
    DateTime JoinedAt
);
