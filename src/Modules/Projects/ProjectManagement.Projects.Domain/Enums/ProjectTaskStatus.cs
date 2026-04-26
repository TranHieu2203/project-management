namespace ProjectManagement.Projects.Domain.Enums;

/// <summary>
/// Tên ProjectTaskStatus để tránh xung đột với System.Threading.Tasks.TaskStatus.
/// </summary>
public enum ProjectTaskStatus
{
    NotStarted,
    InProgress,
    Completed,
    OnHold,
    Cancelled,
    Delayed
}
