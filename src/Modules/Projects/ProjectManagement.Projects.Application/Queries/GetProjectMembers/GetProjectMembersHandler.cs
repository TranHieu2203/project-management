using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Projects.Application.Common.Interfaces;
using ProjectManagement.Projects.Application.DTOs;
using ProjectManagement.Shared.Infrastructure.Services;

namespace ProjectManagement.Projects.Application.Queries.GetProjectMembers;

public sealed class GetProjectMembersHandler
    : IRequestHandler<GetProjectMembersQuery, List<ProjectMemberDto>>
{
    private readonly IProjectsDbContext _db;
    private readonly IMembershipChecker _membership;
    private readonly IUserLookupService _userLookup;

    public GetProjectMembersHandler(
        IProjectsDbContext db,
        IMembershipChecker membership,
        IUserLookupService userLookup)
    {
        _db = db;
        _membership = membership;
        _userLookup = userLookup;
    }

    public async Task<List<ProjectMemberDto>> Handle(
        GetProjectMembersQuery query, CancellationToken ct)
    {
        await _membership.EnsureMemberAsync(query.ProjectId, query.CurrentUserId, ct);

        var memberships = await _db.ProjectMemberships
            .Where(m => m.ProjectId == query.ProjectId)
            .ToListAsync(ct);

        var userIds = memberships.Select(m => m.UserId);
        var users = await _userLookup.GetUsersByIdsAsync(userIds, ct);
        var userMap = users.ToDictionary(u => u.Id);

        return memberships.Select(m =>
        {
            var user = userMap.GetValueOrDefault(m.UserId);
            return new ProjectMemberDto(
                m.UserId,
                user?.Username ?? m.UserId.ToString("N"),
                user?.DisplayName,
                m.Role.ToString(),
                m.JoinedAt);
        }).ToList();
    }
}
