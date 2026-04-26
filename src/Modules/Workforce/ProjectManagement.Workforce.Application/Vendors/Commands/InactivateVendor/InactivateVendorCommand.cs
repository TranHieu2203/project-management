using MediatR;
using ProjectManagement.Workforce.Application.DTOs;

namespace ProjectManagement.Workforce.Application.Vendors.Commands.InactivateVendor;

public sealed record InactivateVendorCommand(
    Guid VendorId,
    int ExpectedVersion,
    string UpdatedBy
) : IRequest<VendorDto>;
