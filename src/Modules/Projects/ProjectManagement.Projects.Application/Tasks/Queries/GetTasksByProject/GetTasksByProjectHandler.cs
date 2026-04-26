using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Projects.Application.Common.Interfaces;
using ProjectManagement.Projects.Application.DTOs;
using ProjectManagement.Projects.Domain.Entities;

namespace ProjectManagement.Projects.Application.Tasks.Queries.GetTasksByProject;

public sealed class GetTasksByProjectHandler : IRequestHandler<GetTasksByProjectQuery, List<TaskDto>>
{
    private readonly IProjectsDbContext _db;
    private readonly IMembershipChecker _membership;

    public GetTasksByProjectHandler(IProjectsDbContext db, IMembershipChecker membership)
    {
        _db = db;
        _membership = membership;
    }

    public async Task<List<TaskDto>> Handle(GetTasksByProjectQuery query, CancellationToken ct)
    {
        // Membership check (404 cho non-member)
        await _membership.EnsureMemberAsync(query.ProjectId, query.CurrentUserId, ct);

        // Trả flat list — client tự build tree bằng parentId
        var tasks = await _db.ProjectTasks
            .Where(t => t.ProjectId == query.ProjectId)
            .Include(t => t.Predecessors)
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.CreatedAt)
            .ToListAsync(ct);

        return tasks.Select(MapToDto).ToList();
    }

    private static TaskDto MapToDto(ProjectTask t) => new(
        t.Id, t.ProjectId, t.ParentId,
        t.Type.ToString(), t.Vbs, t.Name,
        t.Priority.ToString(), t.Status.ToString(),
        t.Notes, t.PlannedStartDate, t.PlannedEndDate,
        t.ActualStartDate, t.ActualEndDate,
        t.PlannedEffortHours,
        ActualEffortHours: null,   // computed từ TimeEntries — Epic 3
        t.PercentComplete, t.AssigneeUserId,
        t.SortOrder, t.Version,
        t.Predecessors.Select(p => new TaskDependencyDto(
            p.PredecessorId, p.DependencyType.ToString())).ToList());
}
