using ProjectManagement.Projects.Domain.Enums;
using ProjectManagement.Shared.Domain.Entities;

namespace ProjectManagement.Projects.Domain.Entities;

public class TaskDependency : BaseEntity
{
    public Guid TaskId { get; private set; }          // task "successor"
    public Guid PredecessorId { get; private set; }   // task predecessor

    public DependencyType DependencyType { get; private set; }

    public static TaskDependency Create(Guid taskId, Guid predecessorId, DependencyType type) => new()
    {
        Id = Guid.NewGuid(),
        TaskId = taskId,
        PredecessorId = predecessorId,
        DependencyType = type,
        CreatedAt = DateTime.UtcNow,
        CreatedBy = string.Empty,
    };
}
