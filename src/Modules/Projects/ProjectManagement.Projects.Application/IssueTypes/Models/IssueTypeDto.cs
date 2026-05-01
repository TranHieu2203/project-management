namespace ProjectManagement.Projects.Application.IssueTypes.Models;

public sealed record IssueTypeDto(
    Guid Id,
    string Name,
    string IconKey,
    string Color,
    bool IsBuiltIn,
    bool IsDeletable,
    Guid? ProjectId,
    int SortOrder);

