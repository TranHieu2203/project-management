using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Projects.Application.Common.Interfaces;
using ProjectManagement.Projects.Domain.Enums;

namespace ProjectManagement.Reporting.Application.Queries.GetStatCards;

public sealed record StatCardsDto(
    int OverdueTaskCount,
    int AtRiskProjectCount,
    int OverloadedResourceCount);

public sealed record GetStatCardsQuery(
    Guid CurrentUserId,
    IReadOnlyList<Guid>? ProjectIds = null
) : IRequest<StatCardsDto>;

public sealed class GetStatCardsHandler : IRequestHandler<GetStatCardsQuery, StatCardsDto>
{
    private readonly IProjectsDbContext _db;

    public GetStatCardsHandler(IProjectsDbContext db) => _db = db;

    public async Task<StatCardsDto> Handle(GetStatCardsQuery request, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var memberProjectIds = await _db.ProjectMemberships
            .AsNoTracking()
            .Where(m => m.UserId == request.CurrentUserId)
            .Select(m => m.ProjectId)
            .Distinct()
            .ToListAsync(ct);

        if (request.ProjectIds?.Count > 0)
            memberProjectIds = memberProjectIds.Intersect(request.ProjectIds).ToList();

        if (memberProjectIds.Count == 0)
            return new StatCardsDto(0, 0, 0);

        // overdueTaskCount: tasks past due, not completed/cancelled
        var overdueTaskCount = await _db.ProjectTasks
            .AsNoTracking()
            .Where(t =>
                memberProjectIds.Contains(t.ProjectId) &&
                !t.IsDeleted &&
                t.Type != TaskType.Phase &&
                t.Status != ProjectTaskStatus.Completed &&
                t.Status != ProjectTaskStatus.Cancelled &&
                t.PlannedEndDate.HasValue &&
                t.PlannedEndDate.Value < today)
            .CountAsync(ct);

        // atRiskProjectCount: projects where health = AtRisk or Delayed
        // Compute health inline (same logic as GetProjectsSummaryHandler)
        var tasks = await _db.ProjectTasks
            .AsNoTracking()
            .Where(t =>
                memberProjectIds.Contains(t.ProjectId) &&
                !t.IsDeleted &&
                t.Type != TaskType.Phase)
            .Select(t => new
            {
                t.ProjectId,
                t.Status,
                t.PlannedStartDate,
                t.PlannedEndDate,
                t.PercentComplete
            })
            .ToListAsync(ct);

        var tasksByProject = tasks.GroupBy(t => t.ProjectId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var atRiskProjectCount = 0;
        foreach (var projectId in memberProjectIds)
        {
            if (!tasksByProject.TryGetValue(projectId, out var projTasks) || projTasks.Count == 0)
                continue;

            var overdueCount = projTasks.Count(t =>
                t.PlannedEndDate.HasValue &&
                t.PlannedEndDate.Value < today &&
                t.Status != ProjectTaskStatus.Completed &&
                t.Status != ProjectTaskStatus.Cancelled);

            var activeTasks = projTasks.Where(t => t.Status != ProjectTaskStatus.Cancelled).ToList();
            var percentComplete = activeTasks.Count > 0
                ? (decimal)activeTasks.Sum(t => (double)(t.PercentComplete ?? 0m)) / activeTasks.Count
                : 0m;

            var startDates = projTasks
                .Where(t => t.PlannedStartDate.HasValue)
                .Select(t => t.PlannedStartDate!.Value).ToList();
            var endDates = projTasks
                .Where(t => t.PlannedEndDate.HasValue)
                .Select(t => t.PlannedEndDate!.Value).ToList();

            decimal percentTimeElapsed = 0m;
            if (startDates.Count > 0 && endDates.Count > 0)
            {
                var minStart = startDates.Min();
                var maxEnd = endDates.Max();
                var totalDays = (maxEnd.ToDateTime(TimeOnly.MinValue) - minStart.ToDateTime(TimeOnly.MinValue)).TotalDays;
                if (totalDays > 0)
                {
                    var elapsed = (today.ToDateTime(TimeOnly.MinValue) - minStart.ToDateTime(TimeOnly.MinValue)).TotalDays;
                    percentTimeElapsed = Math.Round(Math.Clamp((decimal)(elapsed / totalDays * 100), 0m, 100m), 1);
                }
            }

            if (overdueCount > 3 || percentComplete < percentTimeElapsed - 25m ||
                overdueCount >= 1 || percentComplete < percentTimeElapsed - 10m)
            {
                atRiskProjectCount++;
            }
        }

        return new StatCardsDto(overdueTaskCount, atRiskProjectCount, 0);
    }
}
