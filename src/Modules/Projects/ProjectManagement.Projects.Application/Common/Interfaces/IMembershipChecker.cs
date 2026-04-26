namespace ProjectManagement.Projects.Application.Common.Interfaces;

/// <summary>
/// Reusable membership check abstraction — used by Story 1.2 and reused in Story 1.3+ sub-resource protection.
/// EnsureMemberAsync throws NotFoundException if the user is not a member (→ 404, prevents existence leak).
/// </summary>
public interface IMembershipChecker
{
    Task<bool> IsMemberAsync(Guid projectId, Guid userId, CancellationToken ct = default);
    Task EnsureMemberAsync(Guid projectId, Guid userId, CancellationToken ct = default);
}
