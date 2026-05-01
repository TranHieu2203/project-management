using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Projects.Application.Common.Interfaces;
using ProjectManagement.Projects.Application.DTOs;
using ProjectManagement.Projects.Domain.Entities;
using ProjectManagement.Shared.Domain.Exceptions;

namespace ProjectManagement.Projects.Application.Tasks.Queries.GetTaskById;

public sealed class GetTaskByIdHandler : IRequestHandler<GetTaskByIdQuery, TaskDto>
{
    private readonly IProjectsDbContext _db;
    private readonly IMembershipChecker _membership;

    public GetTaskByIdHandler(IProjectsDbContext db, IMembershipChecker membership)
    {
        _db = db;
        _membership = membership;
    }

    public async Task<TaskDto> Handle(GetTaskByIdQuery query, CancellationToken ct)
    {
        // Membership check (404 cho non-member)
        await _membership.EnsureMemberAsync(query.ProjectId, query.CurrentUserId, ct);

        var task = await _db.Issues
            .Include(t => t.Predecessors)
            .FirstOrDefaultAsync(t => t.Id == query.TaskId && t.ProjectId == query.ProjectId, ct);

        if (task is null)
            throw new NotFoundException(nameof(ProjectTask), query.TaskId);

        return new TaskDto(
            task.Id, task.ProjectId, task.ParentId,
            task.Type.ToString(), task.Vbs, task.Name,
            task.Priority.ToString(), task.Status.ToString(),
            task.Notes, task.PlannedStartDate, task.PlannedEndDate,
            task.ActualStartDate, task.ActualEndDate,
            task.PlannedEffortHours,
            ActualEffortHours: null,
            task.PercentComplete, task.AssigneeUserId,
            task.SortOrder, task.Version,
            task.Predecessors.Select(p => new TaskDependencyDto(
                p.PredecessorId, p.DependencyType.ToString())).ToList(),
            IssueKey: task.IssueKey,
            Discriminator: task.Discriminator,
            StoryPoints: task.StoryPoints,
            IssueTypeId: task.IssueTypeId,
            ReporterUserId: task.ReporterUserId);
    }
}
