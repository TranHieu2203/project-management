using MediatR;
using ProjectManagement.Workforce.Application.DTOs;

namespace ProjectManagement.Workforce.Application.Vendors.Queries.GetVendorById;

public sealed record GetVendorByIdQuery(Guid VendorId) : IRequest<VendorDto>;
