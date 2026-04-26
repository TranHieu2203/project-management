using MediatR;
using ProjectManagement.Workforce.Application.DTOs;

namespace ProjectManagement.Workforce.Application.Resources.Commands.UpdateResource;

public sealed record UpdateResourceCommand(
    Guid ResourceId,
    string Name,
    string? Email,
    int ExpectedVersion,
    string UpdatedBy
) : IRequest<ResourceDto>;
