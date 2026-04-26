using MediatR;

namespace ProjectManagement.Projects.Application.Tasks.Commands.DeleteTask;

public sealed record DeleteTaskCommand(
    Guid TaskId,
    Guid ProjectId,
    int ExpectedVersion,
    Guid CurrentUserId
) : IRequest;
