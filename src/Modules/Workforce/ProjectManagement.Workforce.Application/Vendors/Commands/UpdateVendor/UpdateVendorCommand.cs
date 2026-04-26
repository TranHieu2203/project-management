using MediatR;
using ProjectManagement.Workforce.Application.DTOs;

namespace ProjectManagement.Workforce.Application.Vendors.Commands.UpdateVendor;

public sealed record UpdateVendorCommand(
    Guid VendorId,
    string Name,
    string? Description,
    int ExpectedVersion,
    string UpdatedBy
) : IRequest<VendorDto>;
