using MediatR;
using ProjectManagement.Projects.Application.DTOs;

namespace ProjectManagement.Projects.Application.Tasks.Queries.GetMyTasks;

public sealed record GetMyTasksQuery(
    Guid CurrentUserId,
    bool OverdueOnly = false,
    string? Keyword = null
) : IRequest<List<MyTaskDto>>;
