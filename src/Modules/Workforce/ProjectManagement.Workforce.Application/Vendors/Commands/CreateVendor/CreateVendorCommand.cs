using MediatR;
using ProjectManagement.Workforce.Application.DTOs;

namespace ProjectManagement.Workforce.Application.Vendors.Commands.CreateVendor;

public sealed record CreateVendorCommand(
    string Code,
    string Name,
    string? Description,
    string CreatedBy
) : IRequest<VendorDto>;
