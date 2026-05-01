using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Projects.Application.Common.Interfaces;
using ProjectManagement.Projects.Application.DTOs;
using ProjectManagement.Projects.Domain.Entities;
using ProjectManagement.Shared.Domain.Exceptions;

namespace ProjectManagement.Projects.Application.Tasks.Commands.DeleteTask;

public sealed class DeleteTaskHandler : IRequestHandler<DeleteTaskCommand>
{
    private readonly IProjectsDbContext _db;
    private readonly IMembershipChecker _membership;

    public DeleteTaskHandler(IProjectsDbContext db, IMembershipChecker membership)
    {
        _db = db;
        _membership = membership;
    }

    public async Task Handle(DeleteTaskCommand cmd, CancellationToken ct)
    {
        // 1. Membership check
        await _membership.EnsureMemberAsync(cmd.ProjectId, cmd.CurrentUserId, ct);

        // 2. Load task — 404 nếu không tồn tại
        var task = await _db.Issues
            .FirstOrDefaultAsync(t => t.Id == cmd.TaskId && t.ProjectId == cmd.ProjectId, ct);
        if (task is null)
            throw new NotFoundException(nameof(ProjectTask), cmd.TaskId);

        // 3. Version check → 409 Conflict
        if (task.Version != cmd.ExpectedVersion)
        {
            var currentDto = new TaskDto(
                task.Id, task.ProjectId, task.ParentId,
                task.Type.ToString(), task.Vbs, task.Name,
                task.Priority.ToString(), task.Status.ToString(),
                task.Notes, task.PlannedStartDate, task.PlannedEndDate,
                task.ActualStartDate, task.ActualEndDate,
                task.PlannedEffortHours, ActualEffortHours: null,
                task.PercentComplete, task.AssigneeUserId,
                task.SortOrder, task.Version, []);
            throw new ConflictException(
                "Task đã thay đổi. Vui lòng tải lại.",
                currentState: currentDto,
                currentETag: $"\"{task.Version}\"");
        }

        // 4. Không cho phép xóa task có children còn active
        var hasChildren = await _db.Issues
            .AnyAsync(t => t.ParentId == cmd.TaskId, ct);
        if (hasChildren)
            throw new DomainException("Không thể xóa task có child tasks. Xóa child tasks trước.");

        // 5. Soft delete
        task.Delete(cmd.CurrentUserId.ToString());
        await _db.SaveChangesAsync(ct);
    }
}
