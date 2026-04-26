using MediatR;
using ProjectManagement.Projects.Application.DTOs;

namespace ProjectManagement.Projects.Application.Queries.GetProjectById;

public sealed record GetProjectByIdQuery(Guid ProjectId, Guid CurrentUserId) : IRequest<ProjectDto>;
