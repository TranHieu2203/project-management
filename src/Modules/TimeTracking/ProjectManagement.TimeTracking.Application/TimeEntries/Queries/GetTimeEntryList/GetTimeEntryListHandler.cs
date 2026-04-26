using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.TimeTracking.Application.Common.Interfaces;
using ProjectManagement.TimeTracking.Application.DTOs;

namespace ProjectManagement.TimeTracking.Application.TimeEntries.Queries.GetTimeEntryList;

public sealed class GetTimeEntryListHandler : IRequestHandler<GetTimeEntryListQuery, PagedResult<TimeEntryDto>>
{
    private readonly ITimeTrackingDbContext _db;

    public GetTimeEntryListHandler(ITimeTrackingDbContext db) => _db = db;

    public async Task<PagedResult<TimeEntryDto>> Handle(GetTimeEntryListQuery query, CancellationToken ct)
    {
        var q = _db.TimeEntries.AsNoTracking().AsQueryable();

        if (query.DateFrom.HasValue) q = q.Where(e => e.Date >= query.DateFrom.Value);
        if (query.DateTo.HasValue) q = q.Where(e => e.Date <= query.DateTo.Value);
        if (query.ResourceId.HasValue) q = q.Where(e => e.ResourceId == query.ResourceId.Value);
        if (query.ProjectId.HasValue) q = q.Where(e => e.ProjectId == query.ProjectId.Value);
        if (!string.IsNullOrEmpty(query.EntryType)) q = q.Where(e => e.EntryType == query.EntryType);

        var totalCount = await q.CountAsync(ct);
        var pageSize = Math.Min(Math.Max(query.PageSize, 1), 200);
        var page = Math.Max(query.Page, 1);

        var items = await q
            .OrderByDescending(e => e.Date)
            .ThenByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new TimeEntryDto(
                e.Id, e.ResourceId, e.ProjectId, e.TaskId,
                e.Date, e.Hours, e.EntryType, e.Note,
                e.RateAtTime, e.CostAtTime, e.EnteredBy, e.CreatedAt,
                e.IsVoided, e.VoidReason, e.VoidedBy, e.VoidedAt, e.SupersedesId))
            .ToListAsync(ct);

        return new PagedResult<TimeEntryDto>(items, totalCount, page, pageSize);
    }
}
