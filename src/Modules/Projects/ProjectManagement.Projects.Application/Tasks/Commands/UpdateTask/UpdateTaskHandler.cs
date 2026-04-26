using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Projects.Application.Common.Interfaces;
using ProjectManagement.Projects.Application.DTOs;
using ProjectManagement.Projects.Domain.Entities;
using ProjectManagement.Shared.Domain.Exceptions;

namespace ProjectManagement.Projects.Application.Tasks.Commands.UpdateTask;

public sealed class UpdateTaskHandler : IRequestHandler<UpdateTaskCommand, TaskDto>
{
    private readonly IProjectsDbContext _db;
    private readonly IMembershipChecker _membership;

    public UpdateTaskHandler(IProjectsDbContext db, IMembershipChecker membership)
    {
        _db = db;
        _membership = membership;
    }

    public async Task<TaskDto> Handle(UpdateTaskCommand cmd, CancellationToken ct)
    {
        // 1. Membership check
        await _membership.EnsureMemberAsync(cmd.ProjectId, cmd.CurrentUserId, ct);

        // 2. Load task — 404 nếu không tồn tại
        var task = await _db.ProjectTasks
            .Include(t => t.Predecessors)
            .FirstOrDefaultAsync(t => t.Id == cmd.TaskId && t.ProjectId == cmd.ProjectId, ct);
        if (task is null)
            throw new NotFoundException(nameof(ProjectTask), cmd.TaskId);

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
            var parentExists = await _db.ProjectTasks
                .AnyAsync(t => t.Id == cmd.ParentId.Value && t.ProjectId == cmd.ProjectId, ct);
            if (!parentExists)
                throw new NotFoundException(nameof(ProjectTask), cmd.ParentId.Value);

            if (await WouldCreateHierarchyCycleAsync(cmd.TaskId, cmd.ParentId.Value, ct))
                throw new DomainException("Không thể đặt parent — sẽ tạo vòng lặp trong cây.");
        }

        // 5. Dependency cycle detection cho predecessors mới
        foreach (var (predId, _) in cmd.Predecessors)
        {
            if (predId == cmd.TaskId)
                throw new DomainException("Task không thể là predecessor của chính nó.");
            if (await WouldCreateDependencyCycleAsync(cmd.TaskId, predId, ct))
                throw new DomainException($"Thêm predecessor '{predId}' sẽ tạo dependency cycle.");
        }

        // 6. Replace tất cả dependencies
        _db.TaskDependencies.RemoveRange(task.Predecessors);
        foreach (var (predId, depType) in cmd.Predecessors)
            _db.TaskDependencies.Add(TaskDependency.Create(cmd.TaskId, predId, depType));

        // 7. Cập nhật task
        task.Update(cmd.ParentId, cmd.Type, cmd.Vbs, cmd.Name,
            cmd.Priority, cmd.Status, cmd.Notes,
            cmd.PlannedStartDate, cmd.PlannedEndDate,
            cmd.ActualStartDate, cmd.ActualEndDate,
            cmd.PlannedEffortHours, cmd.PercentComplete,
            cmd.AssigneeUserId, cmd.SortOrder,
            cmd.CurrentUserId.ToString());

        await _db.SaveChangesAsync(ct);

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

            var children = await _db.ProjectTasks
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

    // Kiểm tra: nếu thêm predecessorId làm predecessor của taskId, có tạo dependency cycle?
    // Cycle = taskId đã là (direct/indirect) predecessor của predecessorId
    private async Task<bool> WouldCreateDependencyCycleAsync(
        Guid taskId, Guid predecessorId, CancellationToken ct)
    {
        // BFS từ predecessorId đi theo chiều "successors"
        // Nếu gặp taskId → cycle
        var visited = new HashSet<Guid>();
        var queue = new Queue<Guid>();
        queue.Enqueue(predecessorId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!visited.Add(current)) continue;

            var successors = await _db.TaskDependencies
                .Where(d => d.PredecessorId == current)
                .Select(d => d.TaskId)
                .ToListAsync(ct);

            foreach (var s in successors)
            {
                if (s == taskId) return true;
                queue.Enqueue(s);
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
            new TaskDependencyDto(p.PredecessorId, p.DependencyType.ToString())).ToList());
}
