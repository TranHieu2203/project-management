using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Projects.Application.Common.Interfaces;
using ProjectManagement.TimeTracking.Application.Common.Interfaces;
using ProjectManagement.Workforce.Application.Common.Interfaces;

namespace ProjectManagement.Reporting.Application.Queries.GetCostBreakdown;

public sealed record CostBreakdownItem(
    string DimensionKey,
    string DimensionLabel,
    string? VendorId,
    string? VendorName,
    string? ResourceId,
    string? ResourceName,
    string? ProjectId,
    string? Month,
    decimal EstimatedCost,
    decimal OfficialCost,
    decimal ConfirmedPct,
    decimal TotalHours);

public sealed record CostBreakdownResult(
    string GroupBy,
    int TotalCount,
    int Page,
    int PageSize,
    IReadOnlyList<CostBreakdownItem> Items);

public sealed record GetCostBreakdownQuery(
    Guid CurrentUserId,
    string GroupBy,
    string? Month,
    Guid? VendorId,
    Guid? ProjectId,
    Guid? ResourceId,
    int Page = 1,
    int PageSize = 50)
    : IRequest<CostBreakdownResult>;

public sealed class GetCostBreakdownHandler
    : IRequestHandler<GetCostBreakdownQuery, CostBreakdownResult>
{
    private static readonly string[] ValidGroupByValues = ["vendor", "project", "resource", "month"];

    private readonly IProjectsDbContext _projectsDb;
    private readonly ITimeTrackingDbContext _timeTrackingDb;
    private readonly IWorkforceDbContext _workforceDb;

    public GetCostBreakdownHandler(
        IProjectsDbContext projectsDb,
        ITimeTrackingDbContext timeTrackingDb,
        IWorkforceDbContext workforceDb)
    {
        _projectsDb = projectsDb;
        _timeTrackingDb = timeTrackingDb;
        _workforceDb = workforceDb;
    }

    public async Task<CostBreakdownResult> Handle(
        GetCostBreakdownQuery query, CancellationToken ct)
    {
        var groupBy = query.GroupBy.ToLowerInvariant();
        if (!ValidGroupByValues.Contains(groupBy))
            throw new ArgumentException(
                $"GroupBy '{query.GroupBy}' không hợp lệ. Chấp nhận: vendor, project, resource, month.");

        var pageSize = Math.Min(Math.Max(query.PageSize, 1), 200);
        var page = Math.Max(query.Page, 1);

        // 1. Membership-scope
        var projectIds = await _projectsDb.ProjectMemberships
            .Where(m => m.UserId == query.CurrentUserId)
            .Select(m => m.ProjectId)
            .Distinct()
            .ToListAsync(ct);

        if (query.ProjectId.HasValue)
        {
            if (!projectIds.Contains(query.ProjectId.Value))
                return EmptyResult(groupBy, page, pageSize);
            projectIds = [query.ProjectId.Value];
        }

        if (projectIds.Count == 0)
            return EmptyResult(groupBy, page, pageSize);

        // 2. Parse month → date range
        DateOnly? dateFrom = null, dateTo = null;
        if (!string.IsNullOrWhiteSpace(query.Month) &&
            DateOnly.TryParseExact(query.Month, "yyyy-MM",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var parsedMonth))
        {
            dateFrom = parsedMonth;
            dateTo = parsedMonth.AddMonths(1).AddDays(-1);
        }

        // 3. Build entry query
        var entryQuery = _timeTrackingDb.TimeEntries.AsNoTracking()
            .Where(e => projectIds.Contains(e.ProjectId) && !e.IsVoided);

        if (dateFrom.HasValue) entryQuery = entryQuery.Where(e => e.Date >= dateFrom.Value);
        if (dateTo.HasValue)   entryQuery = entryQuery.Where(e => e.Date <= dateTo.Value);
        if (query.ResourceId.HasValue)
            entryQuery = entryQuery.Where(e => e.ResourceId == query.ResourceId.Value);

        var entries = await entryQuery
            .Select(e => new EntryData(e.ResourceId, e.ProjectId, e.Date, e.EntryType, e.CostAtTime, e.Hours))
            .ToListAsync(ct);

        // 4. Apply vendor filter (resolve via resource lookup)
        if (query.VendorId.HasValue)
        {
            var vendorResourceIds = await _workforceDb.Resources.AsNoTracking()
                .Where(r => r.VendorId == query.VendorId.Value)
                .Select(r => r.Id)
                .ToListAsync(ct);
            var vendorResourceSet = vendorResourceIds.ToHashSet();
            entries = entries.Where(e => vendorResourceSet.Contains(e.ResourceId)).ToList();
        }

        if (entries.Count == 0)
            return EmptyResult(groupBy, page, pageSize);

        // 5. Load resource/vendor lookups for labelling
        var resourceIds = entries.Select(e => e.ResourceId).Distinct().ToList();
        var resources = await _workforceDb.Resources.AsNoTracking()
            .Where(r => resourceIds.Contains(r.Id))
            .Select(r => new { r.Id, r.Name, r.VendorId })
            .ToListAsync(ct);
        var resourceMap = resources.ToDictionary(r => r.Id);

        var vendorIds = resources.Where(r => r.VendorId.HasValue).Select(r => r.VendorId!.Value).Distinct().ToList();
        var vendors = vendorIds.Count > 0
            ? await _workforceDb.Vendors.AsNoTracking()
                .Where(v => vendorIds.Contains(v.Id))
                .Select(v => new { v.Id, v.Name })
                .ToListAsync(ct)
            : new List<dynamic>().Select(x => new { Id = Guid.Empty, Name = "" }).ToList();
        var vendorMap = vendors.ToDictionary(v => v.Id, v => v.Name);

        // 6. Group, compute, page
        List<CostBreakdownItem> allItems = groupBy switch
        {
            "vendor" => entries
                .GroupBy(e =>
                {
                    var hasVendor = resourceMap.TryGetValue(e.ResourceId, out var r) && r.VendorId.HasValue;
                    return hasVendor ? r!.VendorId!.Value.ToString() : "__inhouse__";
                })
                .Select(g =>
                {
                    if (g.Key == "__inhouse__")
                        return BuildItem(g, "__inhouse__", "Inhouse", null, null, null, null, null, null);
                    var vid = Guid.Parse(g.Key);
                    var vname = vendorMap.TryGetValue(vid, out var n) ? n : g.Key;
                    return BuildItem(g, g.Key, vname, g.Key, vname, null, null, null, null);
                })
                .ToList(),

            "project" => entries
                .GroupBy(e => e.ProjectId.ToString())
                .Select(g => BuildItem(g, g.Key, g.Key, null, null, null, null, g.Key, null))
                .ToList(),

            "resource" => entries
                .GroupBy(e => e.ResourceId.ToString())
                .Select(g =>
                {
                    var rid = Guid.Parse(g.Key);
                    var rname = resourceMap.TryGetValue(rid, out var r) ? r.Name : g.Key;
                    var vId = r != null && r.VendorId.HasValue ? r.VendorId!.Value.ToString() : null;
                    var vname = vId != null && vendorMap.TryGetValue(Guid.Parse(vId), out var vn) ? vn : null;
                    return BuildItem(g, g.Key, rname, vId, vname, g.Key, rname, null, null);
                })
                .ToList(),

            "month" => entries
                .GroupBy(e => $"{e.Date.Year:D4}-{e.Date.Month:D2}")
                .OrderBy(g => g.Key)
                .Select(g => BuildItem(g, g.Key, g.Key, null, null, null, null, null, g.Key))
                .ToList(),

            _ => []
        };

        if (groupBy != "month")
            allItems = [.. allItems.OrderByDescending(i => i.OfficialCost)];

        var totalCount = allItems.Count;
        var paged = allItems.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return new CostBreakdownResult(groupBy, totalCount, page, pageSize, paged);
    }

    private static CostBreakdownItem BuildItem(
        IEnumerable<EntryData> group,
        string dimKey, string dimLabel,
        string? vendorId, string? vendorName,
        string? resourceId, string? resourceName,
        string? projectId, string? month)
    {
        var list = group.ToList();
        var estimated  = list.Where(e => e.EntryType == "Estimated").Sum(e => e.CostAtTime);
        var pmAdj      = list.Where(e => e.EntryType == "PmAdjusted").Sum(e => e.CostAtTime);
        var vendorConf = list.Where(e => e.EntryType == "VendorConfirmed").Sum(e => e.CostAtTime);
        var official   = pmAdj + vendorConf;
        var total      = official + estimated;
        var pct        = total == 0m ? 0m : Math.Round(official / total * 100m, 1);
        var hours      = list.Sum(e => e.Hours);
        return new CostBreakdownItem(
            dimKey, dimLabel, vendorId, vendorName, resourceId, resourceName, projectId, month,
            estimated, official, pct, hours);
    }

    private static CostBreakdownResult EmptyResult(string groupBy, int page, int pageSize) =>
        new(groupBy, 0, page, pageSize, []);

    private sealed record EntryData(
        Guid ResourceId,
        Guid ProjectId,
        DateOnly Date,
        string EntryType,
        decimal CostAtTime,
        decimal Hours);
}
