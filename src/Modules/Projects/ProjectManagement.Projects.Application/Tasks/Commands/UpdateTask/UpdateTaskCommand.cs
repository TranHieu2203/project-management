using MediatR;
using ProjectManagement.Projects.Application.DTOs;
using ProjectManagement.Projects.Domain.Enums;

namespace ProjectManagement.Projects.Application.Tasks.Commands.UpdateTask;

public sealed record UpdateTaskCommand(
    Guid TaskId,
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
    int ExpectedVersion,
    Guid CurrentUserId
) : IRequest<TaskDto>;
