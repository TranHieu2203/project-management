using MediatR;
using ProjectManagement.Projects.Application.DTOs;

namespace ProjectManagement.Projects.Application.Queries.GetProjectList;

public sealed record GetProjectListQuery(Guid CurrentUserId) : IRequest<List<ProjectDto>>;
