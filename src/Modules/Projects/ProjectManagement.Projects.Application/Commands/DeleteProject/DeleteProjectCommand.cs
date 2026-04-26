using MediatR;

namespace ProjectManagement.Projects.Application.Commands.DeleteProject;

public sealed record DeleteProjectCommand(
    Guid ProjectId,
    int ExpectedVersion,
    Guid CurrentUserId
) : IRequest;
