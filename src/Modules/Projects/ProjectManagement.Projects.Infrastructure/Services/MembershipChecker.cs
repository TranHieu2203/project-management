using Microsoft.EntityFrameworkCore;
using ProjectManagement.Projects.Application.Common.Interfaces;
using ProjectManagement.Projects.Domain.Entities;
using ProjectManagement.Projects.Infrastructure.Persistence;
using ProjectManagement.Shared.Domain.Exceptions;

namespace ProjectManagement.Projects.Infrastructure.Services;

public sealed class MembershipChecker : IMembershipChecker
{
    private readonly ProjectsDbContext _db;

    public MembershipChecker(ProjectsDbContext db) => _db = db;

    public async Task<bool> IsMemberAsync(Guid projectId, Guid userId, CancellationToken ct = default)
    {
        return await _db.ProjectMemberships
            .AnyAsync(m => m.ProjectId == projectId && m.UserId == userId, ct);
    }

    public async Task EnsureMemberAsync(Guid projectId, Guid userId, CancellationToken ct = default)
    {
        // 1-query: combines existence + membership → always 404, never leaks project existence
        var projectExists = await _db.Projects
            .Where(p => p.Id == projectId
                     && !p.IsDeleted
                     && p.Members.Any(m => m.UserId == userId))
            .AnyAsync(ct);

        if (!projectExists)
            throw new NotFoundException(nameof(Project), projectId);
    }
}
