using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Projects.Application.Common.Interfaces;
using ProjectManagement.Projects.Domain.Enums;

namespace ProjectManagement.Reporting.Application.Queries.GetProjectsSummary;

public sealed record GetProjectsSummaryQuery(
    Guid CurrentUserId,
    IReadOnlyList<Guid>? ProjectIds = null
) : IRequest<List<ProjectSummaryDto>>;

public sealed class GetProjectsSummaryHandler
    : IRequestHandler<GetProjectsSummaryQuery, List<ProjectSummaryDto>>
{
    private readonly IProjectsDbContext _db;

    public GetProjectsSummaryHandler(IProjectsDbContext db) => _db = db;

    public async Task<List<ProjectSummaryDto>> Handle(
        GetProjectsSummaryQuery request, CancellationToken ct)
    {
        // 1. Membership-scoped project IDs
        var memberProjectIds = await _db.ProjectMemberships
            .AsNoTracking()
            .Where(m => m.UserId == request.CurrentUserId)
            .Select(m => m.ProjectId)
            .Distinct()
            .ToListAsync(ct);

        if (request.ProjectIds?.Count > 0)
            memberProjectIds = memberProjectIds.Intersect(request.ProjectIds).ToList();

        if (memberProjectIds.Count == 0)
            return [];

        // 2. Load active projects
        var projects = await _db.Projects
            .AsNoTracking()
            .Where(p => memberProjectIds.Contains(p.Id) && !p.IsDeleted)
            .Select(p => new { p.Id, p.Name })
            .ToListAsync(ct);

        if (projects.Count == 0)
            return [];

        // 3. Load all tasks for these projects (only leaf tasks: Task + Milestone)
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var tasks = await _db.ProjectTasks
            .AsNoTracking()
            .Where(t =>
                memberProjectIds.Contains(t.ProjectId) &&
                !t.IsDeleted &&
                t.Type != TaskType.Phase)
            .Select(t => new TaskData(
                t.ProjectId,
                t.Status,
                t.PlannedStartDate,
                t.PlannedEndDate,
                t.PercentComplete))
            .ToListAsync(ct);

        // 4. Compute per-project health metrics
        var tasksByProject = tasks
            .GroupBy(t => t.ProjectId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = new List<ProjectSummaryDto>(projects.Count);
        foreach (var proj in projects)
        {
            var projTasks = tasksByProject.TryGetValue(proj.Id, out var tl) ? tl : [];
            var summary = ComputeSummary(proj.Id, proj.Name, projTasks, today);
            result.Add(summary);
        }

        return [.. result.OrderBy(s => s.Name)];
    }

    private static ProjectSummaryDto ComputeSummary(
        Guid projectId, string name,
        List<TaskData> tasks, DateOnly today)
    {
        if (tasks.Count == 0)
            return new ProjectSummaryDto(projectId, name, "OnTrack", 0m, 0m, 0, 0);

        var activeTasks = tasks.Where(t =>
            t.Status != ProjectTaskStatus.Cancelled).ToList();

        // % complete: average of non-cancelled tasks' percentComplete
        var percentComplete = activeTasks.Count > 0
            ? Math.Round(
                (decimal)activeTasks.Sum(t => (double)(t.PercentComplete ?? 0m))
                / activeTasks.Count, 1)
            : 0m;

        // % time elapsed: based on min/max planned dates across all tasks
        var startDates = tasks
            .Where(t => t.PlannedStartDate.HasValue)
            .Select(t => t.PlannedStartDate!.Value)
            .ToList();
        var endDates = tasks
            .Where(t => t.PlannedEndDate.HasValue)
            .Select(t => t.PlannedEndDate!.Value)
            .ToList();

        decimal percentTimeElapsed = 0m;
        if (startDates.Count > 0 && endDates.Count > 0)
        {
            var minStart = startDates.Min();
            var maxEnd = endDates.Max();
            var totalDays = (maxEnd.ToDateTime(TimeOnly.MinValue) - minStart.ToDateTime(TimeOnly.MinValue)).TotalDays;
            if (totalDays > 0)
            {
                var elapsed = (today.ToDateTime(TimeOnly.MinValue) - minStart.ToDateTime(TimeOnly.MinValue)).TotalDays;
                percentTimeElapsed = Math.Round(
                    Math.Clamp((decimal)(elapsed / totalDays * 100), 0m, 100m), 1);
            }
        }

        // Overdue: non-completed, non-cancelled tasks with past due date
        var overdueTaskCount = tasks.Count(t =>
            t.PlannedEndDate.HasValue &&
            t.PlannedEndDate.Value < today &&
            t.Status != ProjectTaskStatus.Completed &&
            t.Status != ProjectTaskStatus.Cancelled);

        // Remaining: not completed, not cancelled
        var remainingTaskCount = tasks.Count(t =>
            t.Status != ProjectTaskStatus.Completed &&
            t.Status != ProjectTaskStatus.Cancelled);

        // Health status (Delayed takes priority over AtRisk)
        string healthStatus;
        if (overdueTaskCount > 3 || percentComplete < percentTimeElapsed - 25m)
            healthStatus = "Delayed";
        else if (overdueTaskCount >= 1 || percentComplete < percentTimeElapsed - 10m)
            healthStatus = "AtRisk";
        else
            healthStatus = "OnTrack";

        return new ProjectSummaryDto(
            projectId, name, healthStatus,
            percentComplete, percentTimeElapsed,
            remainingTaskCount, overdueTaskCount);
    }

    private sealed record TaskData(
        Guid ProjectId,
        ProjectTaskStatus Status,
        DateOnly? PlannedStartDate,
        DateOnly? PlannedEndDate,
        decimal? PercentComplete);
}
