using MediatR;
using ProjectManagement.Projects.Application.DTOs;

namespace ProjectManagement.Projects.Application.Tasks.Queries.GetTasksByProject;

public sealed record GetTasksByProjectQuery(
    Guid ProjectId,
    Guid CurrentUserId
) : IRequest<List<TaskDto>>;
