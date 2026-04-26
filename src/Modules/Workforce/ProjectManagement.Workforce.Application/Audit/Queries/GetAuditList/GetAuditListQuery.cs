using MediatR;
using ProjectManagement.Workforce.Application.DTOs;

namespace ProjectManagement.Workforce.Application.Audit.Queries.GetAuditList;

public sealed record GetAuditListQuery(
    string? EntityType = null,
    Guid? EntityId = null,
    int PageSize = 50
) : IRequest<List<AuditEventDto>>;
