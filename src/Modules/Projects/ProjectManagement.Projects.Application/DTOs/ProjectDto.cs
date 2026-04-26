namespace ProjectManagement.Projects.Application.DTOs;

public sealed record ProjectDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string Status,
    string Visibility,
    int Version
);
