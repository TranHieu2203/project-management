using ProjectManagement.Projects.Application.IssueTypes.Models;

namespace ProjectManagement.Projects.Application.IssueTypes.Services;

public interface IIssueTypesService
{
    Task<IReadOnlyList<IssueTypeDto>> GetBuiltInAsync(CancellationToken ct);

    Task<IReadOnlyList<IssueTypeDto>> GetByProjectAsync(Guid projectId, Guid currentUserId, CancellationToken ct);

    Task<IssueTypeDto> CreateAsync(
        Guid projectId,
        string name,
        string? iconKey,
        string color,
        Guid currentUserId,
        CancellationToken ct);

    Task<IssueTypeDto> UpdateAsync(
        Guid projectId,
        Guid typeId,
        string name,
        string? iconKey,
        string color,
        Guid currentUserId,
        CancellationToken ct);

    Task DeleteAsync(
        Guid projectId,
        Guid typeId,
        Guid currentUserId,
        CancellationToken ct);
}

