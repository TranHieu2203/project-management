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
        await _membership.EnsureMemberAsync(query.ProjectId, query.CurrentUserId, ct);

        var tasks = await _db.Issues
            .Where(t => t.ProjectId == query.ProjectId)
            .Include(t => t.Predecessors)
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.CreatedAt)
            .ToListAsync(ct);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var hasFilter = HasActiveFilter(query);

        if (!hasFilter)
            return tasks.Select(t => MapToDto(t)).ToList();

        var taskMap = tasks.ToDictionary(t => t.Id);

        // Pre-compute milestone subtree if filter is set
        HashSet<Guid>? milestoneSubtreeIds = null;
        if (query.MilestoneId.HasValue && taskMap.ContainsKey(query.MilestoneId.Value))
            milestoneSubtreeIds = GetSubtreeIds(taskMap, query.MilestoneId.Value);

        // Server-side filter: compute matches, then include ancestors as context nodes
        var matchingIds = new HashSet<Guid>();
        foreach (var t in tasks)
        {
            if (Matches(t, query, today, milestoneSubtreeIds))
                matchingIds.Add(t.Id);
        }

        var visibleMap = new Dictionary<Guid, bool>(); // true=match, false=ancestor

        foreach (var id in matchingIds)
        {
            visibleMap[id] = true;
            // Walk up ancestor chain
            var current = taskMap.GetValueOrDefault(id);
            while (current?.ParentId is { } parentId)
            {
                if (!visibleMap.ContainsKey(parentId))
                    visibleMap[parentId] = false; // ancestor context
                current = taskMap.GetValueOrDefault(parentId);
            }
        }

        if (!query.IncludeAncestors)
        {
            // Return only matching tasks (no ancestor context)
            return tasks
                .Where(t => matchingIds.Contains(t.Id))
                .Select(t => MapToDto(t, isMatch: true))
                .ToList();
        }

        return tasks
            .Where(t => visibleMap.ContainsKey(t.Id))
            .Select(t => MapToDto(t, isMatch: visibleMap[t.Id]))
            .ToList();
    }

    private static bool HasActiveFilter(GetTasksByProjectQuery q) =>
        !string.IsNullOrWhiteSpace(q.Keyword)
        || q.Statuses?.Length > 0
        || q.Priorities?.Length > 0
        || q.NodeTypes?.Length > 0
        || q.AssigneeIds?.Length > 0
        || q.IncludeUnassigned
        || q.MilestoneId.HasValue
        || q.DueDateFrom.HasValue
        || q.DueDateTo.HasValue
        || q.OverdueOnly;

    private static bool Matches(
        ProjectTask t,
        GetTasksByProjectQuery q,
        DateOnly today,
        HashSet<Guid>? milestoneSubtreeIds = null)
    {
        // Keyword: name or vbs
        if (!string.IsNullOrWhiteSpace(q.Keyword))
        {
            var kw = q.Keyword.Trim();
            if (!t.Name.Contains(kw, StringComparison.OrdinalIgnoreCase)
                && !(t.Vbs?.Contains(kw, StringComparison.OrdinalIgnoreCase) ?? false))
                return false;
        }

        // Status
        if (q.Statuses?.Length > 0)
        {
            if (!q.Statuses.Contains(t.Status.ToString(), StringComparer.OrdinalIgnoreCase))
                return false;
        }

        // Priority
        if (q.Priorities?.Length > 0)
        {
            if (!q.Priorities.Contains(t.Priority.ToString(), StringComparer.OrdinalIgnoreCase))
                return false;
        }

        // Node type
        if (q.NodeTypes?.Length > 0)
        {
            if (!q.NodeTypes.Contains(t.Type.ToString(), StringComparer.OrdinalIgnoreCase))
                return false;
        }

        // Assignee: explicit IDs OR unassigned flag
        if (q.AssigneeIds?.Length > 0 || q.IncludeUnassigned)
        {
            bool match = false;
            if (q.IncludeUnassigned && t.AssigneeUserId is null) match = true;
            if (q.AssigneeIds?.Length > 0 && t.AssigneeUserId.HasValue
                && q.AssigneeIds.Contains(t.AssigneeUserId.Value))
                match = true;
            if (!match) return false;
        }

        // Due date range
        if (q.DueDateFrom.HasValue && t.PlannedEndDate.HasValue)
        {
            if (t.PlannedEndDate.Value < q.DueDateFrom.Value) return false;
        }
        if (q.DueDateTo.HasValue && t.PlannedEndDate.HasValue)
        {
            if (t.PlannedEndDate.Value > q.DueDateTo.Value) return false;
        }

        // Overdue only
        if (q.OverdueOnly)
        {
            var isOverdue = t.PlannedEndDate.HasValue
                && t.PlannedEndDate.Value < today
                && t.Status.ToString() != "Completed"
                && t.Status.ToString() != "Cancelled";
            if (!isOverdue) return false;
        }

        // Milestone subtree filter (pre-computed by caller)
        if (milestoneSubtreeIds is not null && !milestoneSubtreeIds.Contains(t.Id))
            return false;

        return true;
    }

    private static HashSet<Guid> GetSubtreeIds(Dictionary<Guid, ProjectTask> taskMap, Guid rootId)
    {
        var result = new HashSet<Guid>();
        var queue = new Queue<Guid>();
        queue.Enqueue(rootId);
        while (queue.Count > 0)
        {
            var id = queue.Dequeue();
            result.Add(id);
            foreach (var child in taskMap.Values.Where(t => t.ParentId == id))
                queue.Enqueue(child.Id);
        }
        return result;
    }

    private static TaskDto MapToDto(ProjectTask t, bool? isMatch = null) => new(
        t.Id, t.ProjectId, t.ParentId,
        t.Type.ToString(), t.Vbs, t.Name,
        t.Priority.ToString(), t.Status.ToString(),
        t.Notes, t.PlannedStartDate, t.PlannedEndDate,
        t.ActualStartDate, t.ActualEndDate,
        t.PlannedEffortHours,
        ActualEffortHours: null,
        t.PercentComplete, t.AssigneeUserId,
        t.SortOrder, t.Version,
        t.Predecessors.Select(p => new TaskDependencyDto(
            p.PredecessorId, p.DependencyType.ToString())).ToList(),
        IssueKey: t.IssueKey,
        Discriminator: t.Discriminator,
        StoryPoints: t.StoryPoints,
        IssueTypeId: t.IssueTypeId,
        ReporterUserId: t.ReporterUserId,
        IsFilterMatch: isMatch);
}
