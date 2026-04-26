using MediatR;
using ProjectManagement.Workforce.Application.DTOs;

namespace ProjectManagement.Workforce.Application.Resources.Commands.CreateResource;

public sealed record CreateResourceCommand(
    string Code,
    string Name,
    string? Email,
    string Type,
    Guid? VendorId,
    string CreatedBy
) : IRequest<ResourceDto>;
