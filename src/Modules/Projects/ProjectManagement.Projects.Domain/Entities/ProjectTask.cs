using ProjectManagement.Projects.Domain.Enums;
using ProjectManagement.Shared.Domain.Entities;

namespace ProjectManagement.Projects.Domain.Entities;

public class ProjectTask : AuditableEntity
{
    public Guid ProjectId { get; private set; }
    public Guid? ParentId { get; private set; }    // null = root node (Phase trực tiếp dưới project)
    public TaskType Type { get; private set; }
    public string Vbs { get; private set; } = string.Empty;    // e.g. "1.2.3", user-entered
    public string Name { get; private set; } = string.Empty;
    public TaskPriority Priority { get; private set; }
    public ProjectTaskStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public DateOnly? PlannedStartDate { get; private set; }
    public DateOnly? PlannedEndDate { get; private set; }
    public DateOnly? ActualStartDate { get; private set; }
    public DateOnly? ActualEndDate { get; private set; }
    public decimal? PlannedEffortHours { get; private set; }
    // actualEffortHours: computed từ TimeEntries (Epic 3) — KHÔNG lưu ở đây
    public decimal? PercentComplete { get; private set; }    // 0.00–100.00
    public Guid? AssigneeUserId { get; private set; }
    public int SortOrder { get; private set; }              // thứ tự hiển thị trong cùng parent
    public int Version { get; private set; }

    // Phase 8.0 — Issue model expansion (all nullable until Phase 4 contract)
    public string? Discriminator { get; private set; }
    public Guid? IssueTypeId { get; private set; }
    public string? IssueKey { get; private set; }
    public Guid? ParentIssueId { get; private set; }
    public string? CustomFields { get; private set; }   // JSONB stored as string
    public Guid? WorkflowStateId { get; private set; }
    public int? StoryPoints { get; private set; }
    public Guid? ReporterUserId { get; private set; }

    // Navigation properties
    public ICollection<TaskDependency> Predecessors { get; private set; } = [];
    public ICollection<TaskDependency> Successors { get; private set; } = [];

    public static ProjectTask Create(
        Guid projectId, Guid? parentId, TaskType type,
        string vbs, string name, TaskPriority priority, ProjectTaskStatus status,
        string? notes, DateOnly? plannedStartDate, DateOnly? plannedEndDate,
        DateOnly? actualStartDate, DateOnly? actualEndDate,
        decimal? plannedEffortHours, decimal? percentComplete,
        Guid? assigneeUserId, int sortOrder, string createdBy,
        string? issueKey = null, Guid? reporterUserId = null) => new()
    {
        Id = Guid.NewGuid(),
        ProjectId = projectId,
        ParentId = parentId,
        Type = type,
        Vbs = vbs,
        Name = name,
        Priority = priority,
        Status = status,
        Notes = notes,
        PlannedStartDate = plannedStartDate,
        PlannedEndDate = plannedEndDate,
        ActualStartDate = actualStartDate,
        ActualEndDate = actualEndDate,
        PlannedEffortHours = plannedEffortHours,
        PercentComplete = percentComplete,
        AssigneeUserId = assigneeUserId,
        SortOrder = sortOrder,
        Version = 1,
        CreatedAt = DateTime.UtcNow,
        CreatedBy = createdBy,
        Discriminator = type.ToString(),
        IssueKey = issueKey,
        ReporterUserId = reporterUserId,
    };

    public void Update(
        Guid? parentId, TaskType type, string vbs, string name,
        TaskPriority priority, ProjectTaskStatus status, string? notes,
        DateOnly? plannedStartDate, DateOnly? plannedEndDate,
        DateOnly? actualStartDate, DateOnly? actualEndDate,
        decimal? plannedEffortHours, decimal? percentComplete,
        Guid? assigneeUserId, int sortOrder, string updatedBy)
    {
        ParentId = parentId;
        Type = type;
        Vbs = vbs;
        Name = name;
        Priority = priority;
        Status = status;
        Notes = notes;
        PlannedStartDate = plannedStartDate;
        PlannedEndDate = plannedEndDate;
        ActualStartDate = actualStartDate;
        ActualEndDate = actualEndDate;
        PlannedEffortHours = plannedEffortHours;
        PercentComplete = percentComplete;
        AssigneeUserId = assigneeUserId;
        SortOrder = sortOrder;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
        Version++;
    }

    public void Delete(string updatedBy)
    {
        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
        Version++;
    }
}
