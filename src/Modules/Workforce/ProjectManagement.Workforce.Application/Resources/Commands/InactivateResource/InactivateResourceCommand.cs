using MediatR;
using ProjectManagement.Workforce.Application.DTOs;

namespace ProjectManagement.Workforce.Application.Resources.Commands.InactivateResource;

public sealed record InactivateResourceCommand(
    Guid ResourceId,
    int ExpectedVersion,
    string UpdatedBy
) : IRequest<ResourceDto>;
