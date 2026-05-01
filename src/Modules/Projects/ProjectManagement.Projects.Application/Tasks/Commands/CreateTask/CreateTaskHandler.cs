using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Projects.Application.Common.Interfaces;
using ProjectManagement.Projects.Application.DTOs;
using ProjectManagement.Projects.Domain.Entities;
using ProjectManagement.Shared.Domain.Exceptions;

namespace ProjectManagement.Projects.Application.Tasks.Commands.CreateTask;

public sealed class CreateTaskHandler : IRequestHandler<CreateTaskCommand, TaskDto>
{
    private readonly IProjectsDbContext _db;
    private readonly IMembershipChecker _membership;

    public CreateTaskHandler(IProjectsDbContext db, IMembershipChecker membership)
    {
        _db = db;
        _membership = membership;
    }

    public async Task<TaskDto> Handle(CreateTaskCommand cmd, CancellationToken ct)
    {
        // 1. Membership check — 404 nếu không phải member (prevents existence leak)
        await _membership.EnsureMemberAsync(cmd.ProjectId, cmd.CurrentUserId, ct);

        // 2. Verify parentId thuộc cùng project (nếu có)
        if (cmd.ParentId.HasValue)
        {
            var parentExists = await _db.Issues
                .AnyAsync(t => t.Id == cmd.ParentId.Value && t.ProjectId == cmd.ProjectId, ct);
            if (!parentExists)
                throw new NotFoundException(nameof(ProjectTask), cmd.ParentId.Value);
        }

        // 3. Generate issue_key — {project.code}-{count+1}
        var project = await _db.Projects.FirstAsync(p => p.Id == cmd.ProjectId, ct);
        var issueCount = await _db.Issues
            .IgnoreQueryFilters()
            .CountAsync(t => t.ProjectId == cmd.ProjectId, ct);
        var issueKey = $"{project.Code}-{issueCount + 1}";

        // 4. Tạo task entity
        var task = ProjectTask.Create(
            cmd.ProjectId, cmd.ParentId, cmd.Type, cmd.Vbs, cmd.Name,
            cmd.Priority, cmd.Status, cmd.Notes,
            cmd.PlannedStartDate, cmd.PlannedEndDate,
            cmd.ActualStartDate, cmd.ActualEndDate,
            cmd.PlannedEffortHours, cmd.PercentComplete,
            cmd.AssigneeUserId, cmd.SortOrder,
            cmd.CurrentUserId.ToString(),
            issueKey: issueKey,
            reporterUserId: cmd.CurrentUserId);

        _db.Issues.Add(task);

        // 5. Thêm predecessors nếu có
        if (cmd.Predecessors.Count > 0)
        {
            // Verify tất cả predecessors thuộc cùng project
            var predIds = cmd.Predecessors.Select(p => p.PredecessorId).ToList();
            var validPredCount = await _db.Issues
                .CountAsync(t => predIds.Contains(t.Id) && t.ProjectId == cmd.ProjectId, ct);
            if (validPredCount != predIds.Count)
                throw new DomainException("Một hoặc nhiều predecessor không thuộc project này.");

            // Cycle detection không cần cho CREATE — task mới chưa có successors
            foreach (var (predId, depType) in cmd.Predecessors)
            {
                _db.TaskDependencies.Add(TaskDependency.Create(task.Id, predId, depType));
            }
        }

        await _db.SaveChangesAsync(ct);

        var predecessorDtos = cmd.Predecessors
            .Select(p => new TaskDependencyDto(p.PredecessorId, p.DependencyType.ToString()))
            .ToList();

        return MapToDto(task, predecessorDtos);
    }

    private static TaskDto MapToDto(ProjectTask task, List<TaskDependencyDto> predecessors) => new(
        task.Id,
        task.ProjectId,
        task.ParentId,
        task.Type.ToString(),
        task.Vbs,
        task.Name,
        task.Priority.ToString(),
        task.Status.ToString(),
        task.Notes,
        task.PlannedStartDate,
        task.PlannedEndDate,
        task.ActualStartDate,
        task.ActualEndDate,
        task.PlannedEffortHours,
        ActualEffortHours: null,  // computed từ TimeEntries — Epic 3
        task.PercentComplete,
        task.AssigneeUserId,
        task.SortOrder,
        task.Version,
        predecessors,
        IssueKey: task.IssueKey,
        Discriminator: task.Discriminator,
        StoryPoints: task.StoryPoints,
        IssueTypeId: task.IssueTypeId,
        ReporterUserId: task.ReporterUserId);
}
