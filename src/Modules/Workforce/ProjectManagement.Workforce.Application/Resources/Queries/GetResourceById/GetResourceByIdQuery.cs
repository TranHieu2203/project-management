using MediatR;
using ProjectManagement.Workforce.Application.DTOs;

namespace ProjectManagement.Workforce.Application.Resources.Queries.GetResourceById;

public sealed record GetResourceByIdQuery(Guid ResourceId) : IRequest<ResourceDto>;
