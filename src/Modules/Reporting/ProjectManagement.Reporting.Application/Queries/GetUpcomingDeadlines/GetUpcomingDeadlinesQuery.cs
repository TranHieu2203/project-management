using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Projects.Application.Common.Interfaces;
using ProjectManagement.Projects.Domain.Enums;

namespace ProjectManagement.Reporting.Application.Queries.GetUpcomingDeadlines;

public sealed record DeadlineDto(
    Guid TaskId,
    Guid ProjectId,
    string ProjectName,
    string EntityType,
    string Name,
    DateOnly DueDate,
    int DaysRemaining);

public sealed record GetUpcomingDeadlinesQuery(
    Guid CurrentUserId,
    int DaysAhead = 7,
    IReadOnlyList<Guid>? ProjectIds = null)
    : IRequest<List<DeadlineDto>>;

public sealed class GetUpcomingDeadlinesHandler
    : IRequestHandler<GetUpcomingDeadlinesQuery, List<DeadlineDto>>
{
    private readonly IProjectsDbContext _db;

    public GetUpcomingDeadlinesHandler(IProjectsDbContext db) => _db = db;

    public async Task<List<DeadlineDto>> Handle(
        GetUpcomingDeadlinesQuery request, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var horizon = today.AddDays(request.DaysAhead);

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

        // Load active projects for name lookup
        var projects = await _db.Projects
            .AsNoTracking()
            .Where(p => memberProjectIds.Contains(p.Id) && !p.IsDeleted)
            .Select(p => new { p.Id, p.Name })
            .ToListAsync(ct);

        var projectNameMap = projects.ToDictionary(p => p.Id, p => p.Name);

        // Query upcoming deadlines: tasks/milestones due in next DaysAhead days
        var upcomingTasks = await _db.ProjectTasks
            .AsNoTracking()
            .Where(t =>
                memberProjectIds.Contains(t.ProjectId) &&
                !t.IsDeleted &&
                t.Type != TaskType.Phase &&
                t.Status != ProjectTaskStatus.Completed &&
                t.Status != ProjectTaskStatus.Cancelled &&
                t.PlannedEndDate.HasValue &&
                t.PlannedEndDate.Value >= today &&
                t.PlannedEndDate.Value <= horizon)
            .OrderBy(t => t.PlannedEndDate)
            .Take(7)
            .Select(t => new
            {
                t.Id,
                t.ProjectId,
                t.Name,
                t.Type,
                t.PlannedEndDate
            })
            .ToListAsync(ct);

        return upcomingTasks
            .Select(t => new DeadlineDto(
                TaskId: t.Id,
                ProjectId: t.ProjectId,
                ProjectName: projectNameMap.TryGetValue(t.ProjectId, out var pname) ? pname : "Unknown",
                EntityType: t.Type == TaskType.Milestone ? "Milestone" : "Task",
                Name: t.Name,
                DueDate: t.PlannedEndDate!.Value,
                DaysRemaining: t.PlannedEndDate!.Value.DayNumber - today.DayNumber))
            .ToList();
    }
}
