using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Projects.Application.Common.Interfaces;
using ProjectManagement.Projects.Application.DTOs;
using ProjectManagement.Projects.Application.Notifications;
using ProjectManagement.Projects.Domain.Entities;
using ProjectManagement.Shared.Domain.Exceptions;

namespace ProjectManagement.Projects.Application.Tasks.Commands.UpdateTask;

public sealed class UpdateTaskHandler : IRequestHandler<UpdateTaskCommand, TaskDto>
{
    private readonly IProjectsDbContext _db;
    private readonly IMembershipChecker _membership;
    private readonly IMediator _mediator;

    public UpdateTaskHandler(IProjectsDbContext db, IMembershipChecker membership, IMediator mediator)
    {
        _db = db;
        _membership = membership;
        _mediator = mediator;
    }

    public async Task<TaskDto> Handle(UpdateTaskCommand cmd, CancellationToken ct)
    {
        // 1. Membership check
        await _membership.EnsureMemberAsync(cmd.ProjectId, cmd.CurrentUserId, ct);

        // 2. Load task — 404 nếu không tồn tại
        var task = await _db.Issues
            .Include(t => t.Predecessors)
            .FirstOrDefaultAsync(t => t.Id == cmd.TaskId && t.ProjectId == cmd.ProjectId, ct);
        if (task is null)
            throw new NotFoundException(nameof(ProjectTask), cmd.TaskId);

        // Capture trạng thái trước khi update để phát events
        var previousAssigneeUserId = task.AssigneeUserId;
        var previousStatus = task.Status;

        // 3. Version check → 409 Conflict
        if (task.Version != cmd.ExpectedVersion)
        {
            var currentDto = BuildDto(task);
            throw new ConflictException(
                "Task đã được chỉnh sửa bởi người khác. Vui lòng tải lại.",
                currentState: currentDto,
                currentETag: $"\"{task.Version}\"");
        }

        // 4. Hierarchy cycle detection nếu parentId thay đổi
        if (cmd.ParentId.HasValue && cmd.ParentId != task.ParentId)
        {
            // Verify newParent thuộc cùng project
            var parentExists = await _db.Issues
                .AnyAsync(t => t.Id == cmd.ParentId.Value && t.ProjectId == cmd.ProjectId, ct);
            if (!parentExists)
                throw new NotFoundException(nameof(ProjectTask), cmd.ParentId.Value);

            if (await WouldCreateHierarchyCycleAsync(cmd.TaskId, cmd.ParentId.Value, ct))
                throw new DomainException("Không thể đặt parent — sẽ tạo vòng lặp trong cây.");
        }

        // 5. Replace dependencies + validate cycles against the post-replacement graph
        _db.TaskDependencies.RemoveRange(task.Predecessors);

        foreach (var (predId, _) in cmd.Predecessors)
        {
            if (predId == cmd.TaskId)
                throw new DomainException("Task không thể là predecessor của chính nó.");
        }

        foreach (var (predId, depType) in cmd.Predecessors)
        {
            // Sau RemoveRange: nếu predId đã nằm downstream của taskId (có chuỗi task phụ thuộc taskId ... predId)
            // thì thêm edge predId → taskId sẽ tạo cycle.
            if (await WouldCreateDependencyCycleAsync(cmd.TaskId, predId, ct))
                throw new DomainException($"Thêm predecessor '{predId}' sẽ tạo dependency cycle.");

            _db.TaskDependencies.Add(TaskDependency.Create(cmd.TaskId, predId, depType));
        }

        // 7. Cập nhật task
        task.Update(cmd.ParentId, cmd.Type, cmd.Vbs, cmd.Name,
            cmd.Priority, cmd.Status, cmd.Notes,
            cmd.PlannedStartDate, cmd.PlannedEndDate,
            cmd.ActualStartDate, cmd.ActualEndDate,
            cmd.PlannedEffortHours, cmd.PercentComplete,
            cmd.AssigneeUserId, cmd.SortOrder,
            cmd.CurrentUserId.ToString());

        await _db.SaveChangesAsync(ct);

        // Publish per-event notifications (fire-and-forget via MediatR — handlers dùng try-catch, không propagate exception)
        var projectName = await _db.Projects
            .AsNoTracking()
            .Where(p => p.Id == cmd.ProjectId)
            .Select(p => p.Name)
            .FirstOrDefaultAsync(ct) ?? string.Empty;

        if (cmd.AssigneeUserId.HasValue && cmd.AssigneeUserId != previousAssigneeUserId)
        {
            await _mediator.Publish(new TaskAssignedNotification(
                TaskId: task.Id,
                TaskName: task.Name,
                ProjectId: task.ProjectId,
                ProjectName: projectName,
                PreviousAssigneeUserId: previousAssigneeUserId,
                NewAssigneeUserId: cmd.AssigneeUserId), ct);
        }

        if (cmd.Status != previousStatus)
        {
            await _mediator.Publish(new TaskStatusChangedNotification(
                TaskId: task.Id,
                TaskName: task.Name,
                ProjectId: task.ProjectId,
                ProjectName: projectName,
                PreviousStatus: previousStatus.ToString(),
                NewStatus: cmd.Status.ToString(),
                AssigneeUserId: task.AssigneeUserId), ct);
        }

        return BuildDto(task, cmd.Predecessors.Select(p =>
            new TaskDependencyDto(p.PredecessorId, p.DependencyType.ToString())).ToList());
    }

    // Kiểm tra: nếu đặt newParentId làm parent của taskId, có tạo cycle không?
    // Cycle = newParentId là descendant của taskId
    private async Task<bool> WouldCreateHierarchyCycleAsync(
        Guid taskId, Guid newParentId, CancellationToken ct)
    {
        // BFS từ taskId xuống qua children — nếu gặp newParentId thì là cycle
        var visited = new HashSet<Guid>();
        var queue = new Queue<Guid>();
        queue.Enqueue(taskId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!visited.Add(current)) continue;

            var children = await _db.Issues
                .Where(t => t.ParentId == current)
                .Select(t => t.Id)
                .ToListAsync(ct);

            foreach (var childId in children)
            {
                if (childId == newParentId) return true;
                queue.Enqueue(childId);
            }
        }
        return false;
    }

    // Kiểm tra: thêm taskId phụ thuộc predecessorId có tạo cycle không?
    // Cycle khi predecessorId đã phụ thuộc (trực tiếp/gián tiếp) vào taskId — tức tồnại đường taskId → … → predecessorId
    // theo các cạnh (predecessor P, task T) nghĩa là T phụ thuộc P.
    private async Task<bool> WouldCreateDependencyCycleAsync(Guid taskId, Guid predecessorId, CancellationToken ct)
    {
        var visited = new HashSet<Guid>();
        var queue = new Queue<Guid>();
        queue.Enqueue(taskId);

        while (queue.Count > 0)
        {
            Guid current = queue.Dequeue();
            if (!visited.Add(current))
                continue;

            List<Guid> dependents = await _db.TaskDependencies
                .Where(d => d.PredecessorId == current)
                .Select(d => d.TaskId)
                .ToListAsync(ct);

            foreach (Guid dependentTaskId in dependents)
            {
                if (dependentTaskId == predecessorId)
                    return true;
                queue.Enqueue(dependentTaskId);
            }
        }

        return false;
    }

    private static TaskDto BuildDto(ProjectTask task, List<TaskDependencyDto>? predecessors = null) => new(
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
        ActualEffortHours: null,
        task.PercentComplete,
        task.AssigneeUserId,
        task.SortOrder,
        task.Version,
        predecessors ?? task.Predecessors.Select(p =>
            new TaskDependencyDto(p.PredecessorId, p.DependencyType.ToString())).ToList(),
        IssueKey: task.IssueKey,
        Discriminator: task.Discriminator,
        StoryPoints: task.StoryPoints,
        IssueTypeId: task.IssueTypeId,
        ReporterUserId: task.ReporterUserId);
}
