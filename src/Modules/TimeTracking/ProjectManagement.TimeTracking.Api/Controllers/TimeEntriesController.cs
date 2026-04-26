using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Shared.Infrastructure.Services;
using ProjectManagement.TimeTracking.Application.TimeEntries.Commands.BulkCreateTimeEntries;
using ProjectManagement.TimeTracking.Application.TimeEntries.Commands.CreateTimeEntry;
using ProjectManagement.TimeTracking.Application.TimeEntries.Commands.VoidTimeEntry;
using ProjectManagement.TimeTracking.Application.TimeEntries.Queries.GetTimeEntryById;
using ProjectManagement.TimeTracking.Application.TimeEntries.Queries.GetTimeEntryList;

namespace ProjectManagement.TimeTracking.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/time-entries")]
public sealed class TimeEntriesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public TimeEntriesController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Returns a paged list of time entries. Filters: dateFrom, dateTo, resourceId, projectId, entryType.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetTimeEntries(
        [FromQuery] DateOnly? dateFrom,
        [FromQuery] DateOnly? dateTo,
        [FromQuery] Guid? resourceId,
        [FromQuery] Guid? projectId,
        [FromQuery] string? entryType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetTimeEntryListQuery(dateFrom, dateTo, resourceId, projectId, entryType, page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>
    /// Creates a new time entry (append-only). Returns 201 with the created entry.
    /// EntryType: "Estimated" or "PmAdjusted" only — "VendorConfirmed" is set by import pipeline.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateTimeEntry(
        [FromBody] CreateTimeEntryRequest body,
        CancellationToken ct)
    {
        var cmd = new CreateTimeEntryCommand(
            body.ResourceId,
            body.ProjectId,
            body.TaskId,
            body.Date,
            body.Hours,
            body.EntryType,
            body.Role,
            body.Level,
            body.Note,
            _currentUser.UserId.ToString(),
            body.SupersedesEntryId);

        var result = await _mediator.Send(cmd, ct);
        return CreatedAtAction(nameof(GetTimeEntry), new { entryId = result.Id }, result);
    }

    /// <summary>
    /// Returns a time entry by ID. Returns 404 if not found.
    /// </summary>
    [HttpGet("{entryId:guid}")]
    public async Task<IActionResult> GetTimeEntry(Guid entryId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetTimeEntryByIdQuery(entryId), ct);
        return Ok(result);
    }

    /// <summary>
    /// Soft-voids a time entry. Sets IsVoided=true with reason; original data unchanged.
    /// Returns 409 if already voided, 404 if not found.
    /// </summary>
    [HttpPost("{entryId:guid}/void")]
    public async Task<IActionResult> VoidTimeEntry(
        Guid entryId,
        [FromBody] VoidTimeEntryRequest body,
        CancellationToken ct)
    {
        var cmd = new VoidTimeEntryCommand(entryId, body.Reason, _currentUser.UserId.ToString());
        var result = await _mediator.Send(cmd, ct);
        return Ok(result);
    }

    /// <summary>
    /// Bulk-creates time entries from a timesheet grid submission.
    /// Hard validates: 16h/day cap per resource. Soft validates: PmAdjusted >20% deviation requires note.
    /// All-or-nothing: returns 400 with error list if any validation fails; no entries created.
    /// </summary>
    [HttpPost("bulk")]
    public async Task<IActionResult> BulkCreateTimeEntries(
        [FromBody] BulkTimesheetRequest body,
        CancellationToken ct)
    {
        var rows = body.Rows.Select(r => new BulkTimesheetRowDto(
            r.ResourceId, r.ProjectId, r.TaskId,
            r.Date, r.Hours, r.EntryType,
            r.Role, r.Level, r.Note)).ToList().AsReadOnly();

        var cmd = new BulkCreateTimeEntriesCommand(rows, _currentUser.UserId.ToString());
        var result = await _mediator.Send(cmd, ct);

        if (!result.Success)
            return BadRequest(new { errors = result.Errors });

        return Ok(result.CreatedEntries);
    }

    // NO PUT/PATCH/DELETE — TimeEntry is immutable
}

public sealed record CreateTimeEntryRequest(
    Guid ResourceId,
    Guid ProjectId,
    Guid? TaskId,
    DateOnly Date,
    decimal Hours,
    string EntryType,
    string Role,
    string Level,
    string? Note,
    Guid? SupersedesEntryId = null);

public sealed record VoidTimeEntryRequest(string Reason);

public sealed record BulkTimesheetRequest(IReadOnlyList<BulkTimesheetRowRequest> Rows);
public sealed record BulkTimesheetRowRequest(
    Guid ResourceId,
    Guid ProjectId,
    Guid? TaskId,
    DateOnly Date,
    decimal Hours,
    string EntryType,
    string Role,
    string Level,
    string? Note);
