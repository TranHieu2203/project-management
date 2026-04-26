using MediatR;
using ProjectManagement.Projects.Application.DTOs;

namespace ProjectManagement.Projects.Application.Commands.CreateProject;

public sealed record CreateProjectCommand(
    string Code,
    string Name,
    string? Description,
    Guid CurrentUserId
) : IRequest<ProjectDto>;
