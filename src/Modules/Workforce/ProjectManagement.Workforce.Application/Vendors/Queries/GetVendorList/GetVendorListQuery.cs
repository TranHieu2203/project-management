using MediatR;
using ProjectManagement.Workforce.Application.DTOs;

namespace ProjectManagement.Workforce.Application.Vendors.Queries.GetVendorList;

public sealed record GetVendorListQuery(bool? ActiveOnly = null) : IRequest<List<VendorDto>>;
