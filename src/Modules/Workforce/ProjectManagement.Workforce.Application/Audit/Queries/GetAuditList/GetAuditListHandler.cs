using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Workforce.Application.Common.Interfaces;
using ProjectManagement.Workforce.Application.DTOs;

namespace ProjectManagement.Workforce.Application.Audit.Queries.GetAuditList;

public sealed class GetAuditListHandler : IRequestHandler<GetAuditListQuery, List<AuditEventDto>>
{
    private readonly IWorkforceDbContext _db;

    public GetAuditListHandler(IWorkforceDbContext db) => _db = db;

    public async Task<List<AuditEventDto>> Handle(GetAuditListQuery query, CancellationToken ct)
    {
        var q = _db.AuditEvents.AsNoTracking().AsQueryable();
        if (!string.IsNullOrEmpty(query.EntityType))
            q = q.Where(e => e.EntityType == query.EntityType);
        if (query.EntityId.HasValue)
            q = q.Where(e => e.EntityId == query.EntityId.Value);

        var pageSize = Math.Min(query.PageSize, 200);
        return await q
            .OrderByDescending(e => e.CreatedAt)
            .Take(pageSize)
            .Select(e => new AuditEventDto(e.Id, e.EntityType, e.EntityId, e.Action, e.Actor, e.Summary, e.CreatedAt))
            .ToListAsync(ct);
    }
}
