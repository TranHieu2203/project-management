namespace ProjectManagement.Projects.Application.IssueTypes.Models;

public sealed record IssueTypeSettingDto(
    Guid Id,
    string Name,
    string IconKey,
    string Color,
    bool IsBuiltIn,
    bool IsDeletable,
    Guid? ProjectId,
    int SortOrder,
    bool IsEnabled);

