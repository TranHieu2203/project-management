using MediatR;
using ProjectManagement.Projects.Application.DTOs;

namespace ProjectManagement.Projects.Application.Commands.UpdateProject;

public sealed record UpdateProjectCommand(
    Guid ProjectId,
    string Name,
    string? Description,
    int ExpectedVersion,
    Guid CurrentUserId
) : IRequest<ProjectDto>;
