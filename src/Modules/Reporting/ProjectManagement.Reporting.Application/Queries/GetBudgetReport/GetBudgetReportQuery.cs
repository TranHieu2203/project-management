using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Projects.Application.Common.Interfaces;
using ProjectManagement.TimeTracking.Application.Common.Interfaces;
using ProjectManagement.Workforce.Application.Common.Interfaces;

namespace ProjectManagement.Reporting.Application.Queries.GetBudgetReport;

public sealed record BudgetVendorRow(
    Guid? VendorId,
    string VendorName,
    decimal PlannedHours,
    decimal ActualHours,
    decimal PlannedCost,
    decimal ActualCost,
    decimal ConfirmedPct,
    bool HasAnomaly);

public sealed record BudgetProjectSection(
    Guid ProjectId,
    string ProjectName,
    decimal TotalPlannedCost,
    decimal TotalActualCost,
    IReadOnlyList<BudgetVendorRow> Vendors);

public sealed record BudgetReportDto(
    string Month,
    int WorkingDaysInMonth,
    decimal GrandTotalPlanned,
    decimal GrandTotalActual,
    IReadOnlyList<BudgetProjectSection> Projects);

public sealed record GetBudgetReportQuery(
    Guid CurrentUserId,
    string Month,
    IReadOnlyList<Guid>? ProjectIds = null)
    : IRequest<BudgetReportDto>;

public sealed class GetBudgetReportHandler
    : IRequestHandler<GetBudgetReportQuery, BudgetReportDto>
{
    private readonly IProjectsDbContext _projectsDb;
    private readonly ITimeTrackingDbContext _timeTrackingDb;
    private readonly IWorkforceDbContext _workforceDb;

    public GetBudgetReportHandler(
        IProjectsDbContext projectsDb,
        ITimeTrackingDbContext timeTrackingDb,
        IWorkforceDbContext workforceDb)
    {
        _projectsDb = projectsDb;
        _timeTrackingDb = timeTrackingDb;
        _workforceDb = workforceDb;
    }

    public async Task<BudgetReportDto> Handle(
        GetBudgetReportQuery request, CancellationToken ct)
    {
        // Parse YYYY-MM
        if (!TryParseMonth(request.Month, out var year, out var month))
            return EmptyReport(request.Month);

        var workingDays = CalculateWorkingDays(year, month);
        var dateFrom = new DateOnly(year, month, 1);
        var dateTo = dateFrom.AddMonths(1).AddDays(-1);

        // 1. Membership-scoped project IDs
        var memberProjectIds = await _projectsDb.ProjectMemberships
            .AsNoTracking()
            .Where(m => m.UserId == request.CurrentUserId)
            .Select(m => m.ProjectId)
            .Distinct()
            .ToListAsync(ct);

        if (request.ProjectIds?.Count > 0)
            memberProjectIds = memberProjectIds.Intersect(request.ProjectIds).ToList();

        if (memberProjectIds.Count == 0)
            return EmptyReport(request.Month);

        // 2. Load project names
        var projects = await _projectsDb.Projects
            .AsNoTracking()
            .Where(p => memberProjectIds.Contains(p.Id) && !p.IsDeleted)
            .Select(p => new { p.Id, p.Name })
            .ToListAsync(ct);

        var projectNameMap = projects.ToDictionary(p => p.Id, p => p.Name);

        // 3. Load time entries for the month
        var entries = await _timeTrackingDb.TimeEntries
            .AsNoTracking()
            .Where(e =>
                memberProjectIds.Contains(e.ProjectId) &&
                !e.IsVoided &&
                e.Date >= dateFrom &&
                e.Date <= dateTo)
            .Select(e => new EntryData(e.ResourceId, e.ProjectId, e.EntryType, e.CostAtTime, e.Hours))
            .ToListAsync(ct);

        if (entries.Count == 0)
            return EmptyReport(request.Month);

        // 4. Resolve resource → vendor mapping
        var resourceIds = entries.Select(e => e.ResourceId).Distinct().ToList();
        var resources = await _workforceDb.Resources
            .AsNoTracking()
            .Where(r => resourceIds.Contains(r.Id))
            .Select(r => new { r.Id, r.VendorId })
            .ToListAsync(ct);

        var resourceVendorMap = resources.ToDictionary(r => r.Id, r => r.VendorId);

        var vendorIds = resources
            .Where(r => r.VendorId.HasValue)
            .Select(r => r.VendorId!.Value)
            .Distinct()
            .ToList();

        var vendorNames = vendorIds.Count > 0
            ? await _workforceDb.Vendors
                .AsNoTracking()
                .Where(v => vendorIds.Contains(v.Id))
                .Select(v => new { v.Id, v.Name })
                .ToListAsync(ct)
            : [];

        var vendorNameMap = vendorNames.ToDictionary(v => v.Id, v => v.Name);

        // 5. Build budget sections per project
        var sections = new List<BudgetProjectSection>();
        foreach (var projectId in memberProjectIds.Where(id => projectNameMap.ContainsKey(id)))
        {
            var projectEntries = entries.Where(e => e.ProjectId == projectId).ToList();
            if (projectEntries.Count == 0) continue;

            // Group by vendor (null VendorId = Inhouse)
            var vendorGroups = projectEntries
                .GroupBy(e => resourceVendorMap.TryGetValue(e.ResourceId, out var vid) ? vid : null)
                .ToList();

            var vendorRows = vendorGroups.Select(g =>
            {
                var vendorId = g.Key;
                var vendorName = vendorId.HasValue && vendorNameMap.TryGetValue(vendorId.Value, out var vn)
                    ? vn : "Inhouse";

                var plannedHours = g.Where(e => e.EntryType == "Estimated").Sum(e => e.Hours);
                var actualHours = g.Where(e => e.EntryType is "PmAdjusted" or "VendorConfirmed").Sum(e => e.Hours);
                var plannedCost = g.Where(e => e.EntryType == "Estimated").Sum(e => e.CostAtTime);
                var actualCost = g.Where(e => e.EntryType is "PmAdjusted" or "VendorConfirmed").Sum(e => e.CostAtTime);
                var totalForPct = plannedCost + actualCost;
                var confirmedPct = totalForPct > 0 ? Math.Round(actualCost / totalForPct * 100m, 1) : 0m;
                var hasAnomaly = actualHours > workingDays * 8m;

                return new BudgetVendorRow(vendorId, vendorName, plannedHours, actualHours,
                    plannedCost, actualCost, confirmedPct, hasAnomaly);
            })
            .OrderBy(r => r.VendorName)
            .ToList();

            var totalPlanned = vendorRows.Sum(r => r.PlannedCost);
            var totalActual = vendorRows.Sum(r => r.ActualCost);

            sections.Add(new BudgetProjectSection(
                projectId,
                projectNameMap[projectId],
                totalPlanned,
                totalActual,
                vendorRows));
        }

        var grandPlanned = sections.Sum(s => s.TotalPlannedCost);
        var grandActual = sections.Sum(s => s.TotalActualCost);

        return new BudgetReportDto(
            request.Month,
            workingDays,
            grandPlanned,
            grandActual,
            sections.OrderBy(s => s.ProjectName).ToList());
    }

    private static bool TryParseMonth(string month, out int year, out int monthNum)
    {
        year = 0; monthNum = 0;
        if (string.IsNullOrWhiteSpace(month) || month.Length != 7) return false;
        var parts = month.Split('-');
        return parts.Length == 2
            && int.TryParse(parts[0], out year)
            && int.TryParse(parts[1], out monthNum)
            && monthNum is >= 1 and <= 12;
    }

    private static int CalculateWorkingDays(int year, int month)
    {
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var count = 0;
        for (var day = 1; day <= daysInMonth; day++)
        {
            var dow = new DateTime(year, month, day).DayOfWeek;
            if (dow != DayOfWeek.Saturday && dow != DayOfWeek.Sunday)
                count++;
        }
        return count;
    }

    private static BudgetReportDto EmptyReport(string month) =>
        new(month, 0, 0m, 0m, []);

    private sealed record EntryData(
        Guid ResourceId,
        Guid ProjectId,
        string EntryType,
        decimal CostAtTime,
        decimal Hours);
}
