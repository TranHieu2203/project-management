using MediatR;
using ProjectManagement.Projects.Application.DTOs;

namespace ProjectManagement.Projects.Application.Tasks.Queries.GetTaskById;

public sealed record GetTaskByIdQuery(
    Guid TaskId,
    Guid ProjectId,
    Guid CurrentUserId
) : IRequest<TaskDto>;
