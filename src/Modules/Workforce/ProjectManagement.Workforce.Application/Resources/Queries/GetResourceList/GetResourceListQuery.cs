using MediatR;
using ProjectManagement.Workforce.Application.DTOs;

namespace ProjectManagement.Workforce.Application.Resources.Queries.GetResourceList;

public sealed record GetResourceListQuery(
    string? Type = null,
    Guid? VendorId = null,
    bool? ActiveOnly = null
) : IRequest<List<ResourceDto>>;
