using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Projects.Application.Tasks.Commands.CreateTask;
using ProjectManagement.Projects.Application.Tasks.Commands.DeleteTask;
using ProjectManagement.Projects.Application.Tasks.Commands.UpdateTask;
using ProjectManagement.Projects.Application.Tasks.Queries.GetTaskById;
using ProjectManagement.Projects.Application.Tasks.Queries.GetTasksByProject;
using ProjectManagement.Projects.Domain.Enums;
using ProjectManagement.Shared.Infrastructure.OptimisticLocking;
using ProjectManagement.Shared.Infrastructure.Services;

namespace ProjectManagement.Projects.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/projects/{projectId:guid}/tasks")]
public sealed class TasksController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public TasksController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Trả flat list tasks trong project (member-only).
    /// Hỗ trợ filter phía server cho projects lớn (>500 tasks).
    /// Không có filter → trả tất cả (client tự filter).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetTasks(
        Guid projectId,
        [FromQuery] string? q = null,
        [FromQuery] string? status = null,
        [FromQuery] string? priority = null,
        [FromQuery] string? type = null,
        [FromQuery] string? assignee = null,
        [FromQuery] bool unassigned = false,
        [FromQuery] Guid? milestone = null,
        [FromQuery] DateOnly? dateFrom = null,
        [FromQuery] DateOnly? dateTo = null,
        [FromQuery] bool overdue = false,
        [FromQuery] bool includeAncestors = true,
        CancellationToken ct = default)
    {
        var statuses   = SplitParam(status);
        var priorities = SplitParam(priority);
        var nodeTypes  = SplitParam(type);
        var assigneeIds = SplitGuidParam(assignee);

        var query = new GetTasksByProjectQuery(
            projectId, _currentUser.UserId,
            Keyword: q,
            Statuses: statuses,
            Priorities: priorities,
            NodeTypes: nodeTypes,
            AssigneeIds: assigneeIds,
            IncludeUnassigned: unassigned,
            MilestoneId: milestone,
            DueDateFrom: dateFrom,
            DueDateTo: dateTo,
            OverdueOnly: overdue,
            IncludeAncestors: includeAncestors);

        var result = await _mediator.Send(query, ct);
        return Ok(result);
    }

    /// <summary>
    /// Trả chi tiết một task. Set ETag header.
    /// </summary>
    [HttpGet("{taskId:guid}")]
    public async Task<IActionResult> GetTask(Guid projectId, Guid taskId, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new GetTaskByIdQuery(taskId, projectId, _currentUser.UserId), ct);
        Response.Headers.ETag = ETagHelper.Generate(result.Version);
        return Ok(result);
    }

    /// <summary>
    /// Tạo task mới. Trả 201 + ETag + Location header.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateTask(
        Guid projectId, [FromBody] CreateTaskRequest body, CancellationToken ct)
    {
        var predecessors = ParsePredecessors(body.Predecessors);

        var cmd = new CreateTaskCommand(
            projectId,
            body.ParentId,
            ParseEnum<TaskType>(body.Type),
            body.Vbs ?? string.Empty,
            body.Name,
            ParseEnum<TaskPriority>(body.Priority),
            ParseEnum<ProjectTaskStatus>(body.Status),
            body.Notes,
            body.PlannedStartDate,
            body.PlannedEndDate,
            body.ActualStartDate,
            body.ActualEndDate,
            body.PlannedEffortHours,
            body.PercentComplete,
            body.AssigneeUserId,
            body.SortOrder,
            predecessors,
            _currentUser.UserId);

        var result = await _mediator.Send(cmd, ct);
        Response.Headers.ETag = ETagHelper.Generate(result.Version);
        return CreatedAtAction(nameof(GetTask), new { projectId, taskId = result.Id }, result);
    }

    /// <summary>
    /// Cập nhật task. Yêu cầu If-Match header.
    /// 412 nếu thiếu header, 409 nếu version mismatch.
    /// </summary>
    [HttpPut("{taskId:guid}")]
    public async Task<IActionResult> UpdateTask(
        Guid projectId, Guid taskId, [FromBody] UpdateTaskRequest body, CancellationToken ct)
    {
        var version = ETagHelper.ParseIfMatch(Request.Headers.IfMatch);
        if (version is null)
            return StatusCode(StatusCodes.Status412PreconditionFailed,
                new ProblemDetails
                {
                    Status = 412,
                    Title = "Precondition Required",
                    Detail = "If-Match header là bắt buộc cho cập nhật task."
                });

        var predecessors = ParsePredecessors(body.Predecessors);

        var cmd = new UpdateTaskCommand(
            taskId,
            projectId,
            body.ParentId,
            ParseEnum<TaskType>(body.Type),
            body.Vbs ?? string.Empty,
            body.Name,
            ParseEnum<TaskPriority>(body.Priority),
            ParseEnum<ProjectTaskStatus>(body.Status),
            body.Notes,
            body.PlannedStartDate,
            body.PlannedEndDate,
            body.ActualStartDate,
            body.ActualEndDate,
            body.PlannedEffortHours,
            body.PercentComplete,
            body.AssigneeUserId,
            body.SortOrder,
            predecessors,
            (int)version,
            _currentUser.UserId);

        var result = await _mediator.Send(cmd, ct);
        Response.Headers.ETag = ETagHelper.Generate(result.Version);
        return Ok(result);
    }

    /// <summary>
    /// Xóa (soft-delete) task. Yêu cầu If-Match header.
    /// 422 nếu task còn child tasks.
    /// </summary>
    [HttpDelete("{taskId:guid}")]
    public async Task<IActionResult> DeleteTask(
        Guid projectId, Guid taskId, CancellationToken ct)
    {
        var version = ETagHelper.ParseIfMatch(Request.Headers.IfMatch);
        if (version is null)
            return StatusCode(StatusCodes.Status412PreconditionFailed,
                new ProblemDetails
                {
                    Status = 412,
                    Title = "Precondition Required",
                    Detail = "If-Match header là bắt buộc cho xóa task."
                });

        await _mediator.Send(
            new DeleteTaskCommand(taskId, projectId, (int)version, _currentUser.UserId), ct);
        return NoContent();
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static string[]? SplitParam(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null
        : value.Split(',', StringSplitOptions.RemoveEmptyEntries);

    private static Guid[]? SplitGuidParam(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var parts = value.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var guids = new List<Guid>();
        foreach (var p in parts)
            if (Guid.TryParse(p.Trim(), out var g))
                guids.Add(g);
        return guids.Count > 0 ? [.. guids] : null;
    }

    private static T ParseEnum<T>(string value) where T : struct, Enum
        => Enum.Parse<T>(value, ignoreCase: true);

    private static List<(Guid PredecessorId, DependencyType DependencyType)> ParsePredecessors(
        List<TaskDependencyRequest>? items)
        => items?
            .Select(p => (p.PredecessorId, ParseEnum<DependencyType>(p.DependencyType)))
            .ToList() ?? [];
}

// ─── Request records ─────────────────────────────────────────────────────────

public sealed record CreateTaskRequest(
    Guid? ParentId,
    string Type,
    string? Vbs,
    string Name,
    string Priority,
    string Status,
    string? Notes,
    DateOnly? PlannedStartDate,
    DateOnly? PlannedEndDate,
    DateOnly? ActualStartDate,
    DateOnly? ActualEndDate,
    decimal? PlannedEffortHours,
    decimal? PercentComplete,
    Guid? AssigneeUserId,
    int SortOrder,
    List<TaskDependencyRequest>? Predecessors);

public sealed record UpdateTaskRequest(
    Guid? ParentId,
    string Type,
    string? Vbs,
    string Name,
    string Priority,
    string Status,
    string? Notes,
    DateOnly? PlannedStartDate,
    DateOnly? PlannedEndDate,
    DateOnly? ActualStartDate,
    DateOnly? ActualEndDate,
    decimal? PlannedEffortHours,
    decimal? PercentComplete,
    Guid? AssigneeUserId,
    int SortOrder,
    List<TaskDependencyRequest>? Predecessors);

public sealed record TaskDependencyRequest(Guid PredecessorId, string DependencyType);
