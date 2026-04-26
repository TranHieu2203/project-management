using MediatR;
using ProjectManagement.Projects.Application.DTOs;
using ProjectManagement.Projects.Domain.Enums;

namespace ProjectManagement.Projects.Application.Tasks.Commands.CreateTask;

public sealed record CreateTaskCommand(
    Guid ProjectId,
    Guid? ParentId,
    TaskType Type,
    string Vbs,
    string Name,
    TaskPriority Priority,
    ProjectTaskStatus Status,
    string? Notes,
    DateOnly? PlannedStartDate,
    DateOnly? PlannedEndDate,
    DateOnly? ActualStartDate,
    DateOnly? ActualEndDate,
    decimal? PlannedEffortHours,
    decimal? PercentComplete,
    Guid? AssigneeUserId,
    int SortOrder,
    List<(Guid PredecessorId, DependencyType DependencyType)> Predecessors,
    Guid CurrentUserId
) : IRequest<TaskDto>;
