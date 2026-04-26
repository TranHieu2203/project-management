using MediatR;
using ProjectManagement.Projects.Application.DTOs;

namespace ProjectManagement.Projects.Application.Queries.GetProjectMembers;

public sealed record GetProjectMembersQuery(Guid ProjectId, Guid CurrentUserId)
    : IRequest<List<ProjectMemberDto>>;
