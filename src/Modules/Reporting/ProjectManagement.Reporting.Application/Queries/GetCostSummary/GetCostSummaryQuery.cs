using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Projects.Application.Common.Interfaces;
using ProjectManagement.TimeTracking.Application.Common.Interfaces;

namespace ProjectManagement.Reporting.Application.Queries.GetCostSummary;

public sealed record CostProjectBreakdown(
    Guid ProjectId,
    decimal EstimatedCost,
    decimal OfficialCost,
    decimal VendorConfirmedCost,
    decimal PmAdjustedCost,
    decimal ConfirmedPct);

public sealed record CostSummaryResult(
    DateOnly DateFrom,
    DateOnly DateTo,
    int ProjectCount,
    decimal TotalEstimatedCost,
    decimal TotalOfficialCost,
    decimal ConfirmedPct,
    IReadOnlyList<CostProjectBreakdown> ByProject);

public sealed record GetCostSummaryQuery(
    Guid CurrentUserId,
    DateOnly DateFrom,
    DateOnly DateTo,
    Guid? ProjectId = null)
    : IRequest<CostSummaryResult>;

public sealed class GetCostSummaryHandler
    : IRequestHandler<GetCostSummaryQuery, CostSummaryResult>
{
    private readonly IProjectsDbContext _projectsDb;
    private readonly ITimeTrackingDbContext _timeTrackingDb;

    public GetCostSummaryHandler(
        IProjectsDbContext projectsDb,
        ITimeTrackingDbContext timeTrackingDb)
    {
        _projectsDb = projectsDb;
        _timeTrackingDb = timeTrackingDb;
    }

    public async Task<CostSummaryResult> Handle(
        GetCostSummaryQuery query, CancellationToken ct)
    {
        var projectIds = await _projectsDb.ProjectMemberships
            .Where(m => m.UserId == query.CurrentUserId)
            .Select(m => m.ProjectId)
            .Distinct()
            .ToListAsync(ct);

        if (query.ProjectId.HasValue)
        {
            if (!projectIds.Contains(query.ProjectId.Value))
                return EmptyResult(query);
            projectIds = new List<Guid> { query.ProjectId.Value };
        }

        if (projectIds.Count == 0)
            return EmptyResult(query);

        var entries = await _timeTrackingDb.TimeEntries
            .AsNoTracking()
            .Where(e =>
                projectIds.Contains(e.ProjectId) &&
                e.Date >= query.DateFrom &&
                e.Date <= query.DateTo &&
                !e.IsVoided)
            .Select(e => new { e.ProjectId, e.EntryType, e.CostAtTime })
            .ToListAsync(ct);

        var byProject = entries
            .GroupBy(e => e.ProjectId)
            .Select(g =>
            {
                var estimated  = g.Where(e => e.EntryType == "Estimated").Sum(e => e.CostAtTime);
                var pmAdj      = g.Where(e => e.EntryType == "PmAdjusted").Sum(e => e.CostAtTime);
                var vendorConf = g.Where(e => e.EntryType == "VendorConfirmed").Sum(e => e.CostAtTime);
                var official   = pmAdj + vendorConf;
                var total      = official + estimated;
                var pct        = total == 0m ? 0m : Math.Round(official / total * 100m, 1);
                return new CostProjectBreakdown(g.Key, estimated, official, vendorConf, pmAdj, pct);
            })
            .OrderByDescending(p => p.OfficialCost)
            .ToList();

        var totalEstimated = byProject.Sum(p => p.EstimatedCost);
        var totalOfficial  = byProject.Sum(p => p.OfficialCost);
        var grandTotal     = totalOfficial + totalEstimated;
        var overallPct     = grandTotal == 0m ? 0m : Math.Round(totalOfficial / grandTotal * 100m, 1);

        return new CostSummaryResult(
            query.DateFrom, query.DateTo,
            byProject.Count,
            totalEstimated, totalOfficial, overallPct,
            byProject);
    }

    private static CostSummaryResult EmptyResult(GetCostSummaryQuery q) =>
        new(q.DateFrom, q.DateTo, 0, 0m, 0m, 0m, []);
}
