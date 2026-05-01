using ProjectManagement.Projects.Application.IssueTypes.Models;

namespace ProjectManagement.Projects.Application.IssueTypes.Services;

public interface IProjectIssueTypeSettingsService
{
    Task<IReadOnlyList<IssueTypeSettingDto>> GetAsync(Guid projectId, Guid currentUserId, CancellationToken ct);
    Task<IssueTypeSettingDto> SetEnabledAsync(
        Guid projectId,
        Guid typeId,
        bool isEnabled,
        Guid currentUserId,
        CancellationToken ct);
}

