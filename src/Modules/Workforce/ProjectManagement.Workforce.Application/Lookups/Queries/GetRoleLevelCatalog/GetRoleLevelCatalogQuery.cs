using MediatR;
using ProjectManagement.Workforce.Application.DTOs;

namespace ProjectManagement.Workforce.Application.Lookups.Queries.GetRoleLevelCatalog;

public sealed record GetRoleLevelCatalogQuery() : IRequest<RoleLevelCatalogDto>;
