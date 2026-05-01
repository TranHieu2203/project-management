using Microsoft.EntityFrameworkCore;
using ProjectManagement.Projects.Application.Common.Interfaces;
using ProjectManagement.Projects.Domain.Entities;
using ProjectManagement.Projects.Domain.Enums;
using ProjectManagement.Projects.Infrastructure.Persistence;
using ProjectManagement.Shared.Domain.Exceptions;

namespace ProjectManagement.Projects.Infrastructure.Services;

public sealed class ProjectAdminChecker : IProjectAdminChecker
{
    private readonly ProjectsDbContext _db;
    private readonly IMembershipChecker _membershipChecker;

    public ProjectAdminChecker(ProjectsDbContext db, IMembershipChecker membershipChecker)
    {
        _db = db;
        _membershipChecker = membershipChecker;
    }

    public async Task EnsureProjectAdminAsync(Guid projectId, Guid userId, CancellationToken ct = default)
    {
        await _membershipChecker.EnsureMemberAsync(projectId, userId, ct);

        var isAdmin = await _db.ProjectMemberships
            .AnyAsync(
                m => m.ProjectId == projectId
                  && m.UserId == userId
                  && m.Role == ProjectMemberRole.Manager,
                ct);

        if (!isAdmin)
            throw new ForbiddenException("Bạn không có quyền cấu hình project này.");
    }
}

