using MediatR;
using ProjectManagement.Projects.Application.DTOs;

namespace ProjectManagement.Projects.Application.Tasks.Queries.GetTasksByProject;

public sealed record GetTasksByProjectQuery(
    Guid ProjectId,
    Guid CurrentUserId,
    // ── Filter params (all optional; null = no filter on that field) ──────────
    string? Keyword = null,
    string[]? Statuses = null,
    string[]? Priorities = null,
    string[]? NodeTypes = null,
    Guid[]? AssigneeIds = null,
    bool IncludeUnassigned = false,
    Guid? MilestoneId = null,
    DateOnly? DueDateFrom = null,
    DateOnly? DueDateTo = null,
    bool OverdueOnly = false,
    /// <summary>
    /// When true: return all ancestor nodes with IsFilterMatch=false so client
    /// can render tree context. When false: return only matching tasks (flat).
    /// </summary>
    bool IncludeAncestors = true
) : IRequest<List<TaskDto>>;
