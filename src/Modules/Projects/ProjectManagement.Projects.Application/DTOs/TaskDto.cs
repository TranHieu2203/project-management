namespace ProjectManagement.Projects.Application.DTOs;

public record TaskDto(
    Guid Id,
    Guid ProjectId,
    Guid? ParentId,
    string Type,            // "Phase" | "Milestone" | "Task"
    string? Vbs,
    string Name,
    string Priority,        // "Low" | "Medium" | "High" | "Critical"
    string Status,          // "NotStarted" | "InProgress" | "Completed" | "OnHold" | "Cancelled" | "Delayed"
    string? Notes,
    DateOnly? PlannedStartDate,
    DateOnly? PlannedEndDate,
    DateOnly? ActualStartDate,
    DateOnly? ActualEndDate,
    decimal? PlannedEffortHours,
    decimal? ActualEffortHours,   // LUÔN null — computed từ TimeEntries ở Epic 3
    decimal? PercentComplete,
    Guid? AssigneeUserId,
    int SortOrder,
    int Version,
    List<TaskDependencyDto> Predecessors,
    // Phase 8.0 — new fields (nullable until Phase 4 contract phase)
    string? IssueKey = null,
    string? Discriminator = null,
    int? StoryPoints = null,
    Guid? IssueTypeId = null,
    Guid? ReporterUserId = null,
    bool? IsFilterMatch = null); // null = no active filter; true/false when filtering w/ ancestor context

public record TaskDependencyDto(Guid PredecessorId, string DependencyType);
