using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.TimeTracking.Application.Common.Interfaces;
using ProjectManagement.TimeTracking.Application.PeriodLocks.Commands;
using ProjectManagement.TimeTracking.Application.PeriodLocks.DTOs;

namespace ProjectManagement.TimeTracking.Application.PeriodLocks.Queries;

public sealed record GetPeriodLocksQuery(Guid VendorId) : IRequest<IReadOnlyList<PeriodLockDto>>;

public sealed class GetPeriodLocksHandler : IRequestHandler<GetPeriodLocksQuery, IReadOnlyList<PeriodLockDto>>
{
    private readonly ITimeTrackingDbContext _db;
    public GetPeriodLocksHandler(ITimeTrackingDbContext db) => _db = db;

    public async Task<IReadOnlyList<PeriodLockDto>> Handle(GetPeriodLocksQuery query, CancellationToken ct)
    {
        return await _db.PeriodLocks.AsNoTracking()
            .Where(p => p.VendorId == query.VendorId)
            .OrderByDescending(p => p.Year).ThenByDescending(p => p.Month)
            .Select(p => LockPeriodHandler.ToDto(p))
            .ToListAsync(ct);
    }
}

public sealed record GetPeriodReconcileQuery(Guid VendorId, int Year, int Month) : IRequest<PeriodReconcileDto>;

public sealed class GetPeriodReconcileHandler : IRequestHandler<GetPeriodReconcileQuery, PeriodReconcileDto>
{
    private readonly ITimeTrackingDbContext _db;
    public GetPeriodReconcileHandler(ITimeTrackingDbContext db) => _db = db;

    public async Task<PeriodReconcileDto> Handle(GetPeriodReconcileQuery query, CancellationToken ct)
    {
        var firstDay = new DateOnly(query.Year, query.Month, 1);
        var lastDay = firstDay.AddMonths(1).AddDays(-1);

        var grouped = await _db.TimeEntries.AsNoTracking()
            .Where(e => !e.IsVoided && e.Date >= firstDay && e.Date <= lastDay)
            .GroupBy(e => e.EntryType)
            .Select(g => new { EntryType = g.Key, Hours = g.Sum(e => e.Hours), Cost = g.Sum(e => e.CostAtTime), Count = g.Count() })
            .ToListAsync(ct);

        var periodLock = await _db.PeriodLocks.AsNoTracking()
            .FirstOrDefaultAsync(p => p.VendorId == query.VendorId && p.Year == query.Year && p.Month == query.Month, ct);

        var estimated = grouped.FirstOrDefault(g => g.EntryType == "Estimated")?.Hours ?? 0m;
        var pmAdj = grouped.FirstOrDefault(g => g.EntryType == "PmAdjusted")?.Hours ?? 0m;
        var confirmed = grouped.FirstOrDefault(g => g.EntryType == "VendorConfirmed");
        var totalEntries = grouped.Sum(g => g.Count);

        return new PeriodReconcileDto(
            query.VendorId, query.Year, query.Month,
            periodLock != null, periodLock?.LockedAt,
            estimated, pmAdj,
            confirmed?.Hours ?? 0m,
            confirmed?.Cost ?? 0m,
            totalEntries);
    }
}
