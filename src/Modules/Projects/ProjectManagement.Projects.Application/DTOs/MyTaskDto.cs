namespace ProjectManagement.Projects.Application.DTOs;

public record MyTaskDto(
    Guid Id,
    Guid ProjectId,
    string ProjectName,
    string ProjectCode,
    Guid? ParentId,
    string Type,
    string? Vbs,
    string Name,
    string Priority,
    string Status,
    DateOnly? PlannedEndDate,
    decimal? PercentComplete,
    Guid? AssigneeUserId,
    int SortOrder,
    int Version);
