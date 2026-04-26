using System.Text;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Shared.Infrastructure.Services;
using ProjectManagement.TimeTracking.Application.ImportJobs.Commands.ApplyImportJob;
using ProjectManagement.TimeTracking.Application.ImportJobs.Commands.StartImportJob;
using ProjectManagement.TimeTracking.Application.ImportJobs.Queries;
using CsvColumnMapping = ProjectManagement.TimeTracking.Application.ImportJobs.Commands.StartImportJob.CsvColumnMapping;

namespace ProjectManagement.TimeTracking.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/import-jobs")]
public sealed class ImportJobsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public ImportJobsController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Upload vendor CSV and validate (dry-run). Returns jobId + status.
    /// Column mapping sent as form fields (one per CSV column name).
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> StartImport(
        [FromForm] IFormFile file,
        [FromForm] Guid vendorId,
        [FromForm] string resourceIdColumn,
        [FromForm] string projectIdColumn,
        [FromForm] string dateColumn,
        [FromForm] string hoursColumn,
        [FromForm] string roleColumn,
        [FromForm] string levelColumn,
        [FromForm] string? noteColumn,
        [FromForm] string? taskIdColumn,
        CancellationToken ct)
    {
        if (file.Length == 0)
            return BadRequest(new { detail = "File không được trống." });

        string rawContent;
        using (var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8))
            rawContent = await reader.ReadToEndAsync(ct);

        var mapping = new CsvColumnMapping(
            resourceIdColumn, projectIdColumn, dateColumn, hoursColumn,
            roleColumn, levelColumn, noteColumn, taskIdColumn);

        var cmd = new StartImportJobCommand(vendorId, file.FileName, rawContent, mapping,
            _currentUser.UserId.ToString());

        var result = await _mediator.Send(cmd, ct);
        return Ok(result);
    }

    /// <summary>
    /// Get import job status for polling.
    /// </summary>
    [HttpGet("{jobId:guid}")]
    public async Task<IActionResult> GetImportJob(Guid jobId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetImportJobQuery(jobId), ct);
        return Ok(result);
    }

    /// <summary>
    /// Apply a validated import job — creates VendorConfirmed entries. Idempotent.
    /// </summary>
    [HttpPost("{jobId:guid}/apply")]
    public async Task<IActionResult> ApplyImport(
        Guid jobId,
        [FromBody] ApplyImportRequest body,
        CancellationToken ct)
    {
        var mapping = new CsvColumnMapping(
            body.ResourceIdColumn, body.ProjectIdColumn, body.DateColumn, body.HoursColumn,
            body.RoleColumn, body.LevelColumn, body.NoteColumn, body.TaskIdColumn);

        var cmd = new ApplyImportJobCommand(jobId, _currentUser.UserId.ToString(), mapping);
        var result = await _mediator.Send(cmd, ct);
        return Ok(result);
    }

    /// <summary>
    /// Get error list as JSON for in-UI preview.
    /// </summary>
    [HttpGet("{jobId:guid}/errors")]
    public async Task<IActionResult> GetErrors(Guid jobId, CancellationToken ct)
    {
        var errors = await _mediator.Send(new GetImportJobErrorsQuery(jobId), ct);
        return Ok(errors);
    }

    /// <summary>
    /// Download error report as CSV for a job.
    /// </summary>
    [HttpGet("{jobId:guid}/errors/download")]
    public async Task<IActionResult> DownloadErrors(Guid jobId, CancellationToken ct)
    {
        var errors = await _mediator.Send(new GetImportJobErrorsQuery(jobId), ct);
        var sb = new StringBuilder();
        sb.AppendLine("row_index,column_name,error_type,message");
        foreach (var e in errors)
            sb.AppendLine($"{e.RowIndex},{e.ColumnName ?? ""},\"{e.ErrorType}\",\"{e.Message.Replace("\"", "\"\"")}\"");

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv", $"import-errors-{jobId}.csv");
    }
}

public sealed record ApplyImportRequest(
    string ResourceIdColumn,
    string ProjectIdColumn,
    string DateColumn,
    string HoursColumn,
    string RoleColumn,
    string LevelColumn,
    string? NoteColumn,
    string? TaskIdColumn);
