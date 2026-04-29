using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Reporting.Application.Commands.TriggerExport;
using ProjectManagement.Reporting.Application.Queries.DownloadExport;
using ProjectManagement.Reporting.Application.Queries.GetBudgetReport;
using ProjectManagement.Reporting.Application.Queries.GetCostBreakdown;
using ProjectManagement.Reporting.Application.Queries.GetCostSummary;
using ProjectManagement.Reporting.Application.Queries.GetExportJob;
using ProjectManagement.Reporting.Application.Queries.GetMilestones;
using ProjectManagement.Reporting.Application.Queries.GetResourceReport;
using ProjectManagement.Reporting.Infrastructure.Services;
using ProjectManagement.Shared.Infrastructure.Services;

namespace ProjectManagement.Reporting.Api.Controllers;

[ApiController]
[Route("api/v1/reports")]
[Authorize]
public class ReportingController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;
    private readonly PdfExportService _pdf;

    public ReportingController(IMediator mediator, ICurrentUserService currentUser, PdfExportService pdf)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _pdf = pdf;
    }

    /// <summary>
    /// Budget report: planned vs actual cost by project/vendor for a given month.
    /// </summary>
    [HttpGet("budget")]
    public async Task<IActionResult> GetBudgetReport(
        [FromQuery] string month,
        [FromQuery] Guid[]? projectIds,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new GetBudgetReportQuery(_currentUser.UserId, month, projectIds), ct);
        return Ok(result);
    }

    /// <summary>
    /// Export budget report as PDF for a given month.
    /// </summary>
    [HttpGet("budget/pdf")]
    public async Task<IActionResult> ExportBudgetPdf(
        [FromQuery] string month,
        [FromQuery] Guid[]? projectIds,
        CancellationToken ct)
    {
        var data = await _mediator.Send(
            new GetBudgetReportQuery(_currentUser.UserId, month, projectIds), ct);
        var bytes = _pdf.GenerateBudgetReport(data);
        return File(bytes, "application/pdf", $"budget-{month}.pdf");
    }

    /// <summary>
    /// Cost summary: planned (estimated) vs actual (official = VendorConfirmed + PmAdjusted).
    /// </summary>
    [HttpGet("cost")]
    public async Task<IActionResult> GetCostSummary(
        [FromQuery] DateOnly dateFrom,
        [FromQuery] DateOnly dateTo,
        [FromQuery] Guid? projectId,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new GetCostSummaryQuery(_currentUser.UserId, dateFrom, dateTo, projectId), ct);
        return Ok(result);
    }

    /// <summary>
    /// Cost breakdown by dimension: vendor, project, resource, or month.
    /// </summary>
    [HttpGet("cost/breakdown")]
    public async Task<IActionResult> GetCostBreakdown(
        [FromQuery] string groupBy,
        [FromQuery] string? month,
        [FromQuery] Guid? vendorId,
        [FromQuery] Guid? projectId,
        [FromQuery] Guid? resourceId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetCostBreakdownQuery(
                _currentUser.UserId, groupBy, month,
                vendorId, projectId, resourceId, page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>
    /// Trigger async export (CSV/XLSX). Returns 202 Accepted + jobId.
    /// </summary>
    [HttpPost("export-jobs")]
    public async Task<IActionResult> TriggerExport([FromBody] TriggerExportRequest req, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new TriggerExportCommand(
                _currentUser.UserId, req.Format, req.GroupBy,
                req.Month, req.VendorId, req.ProjectId, req.ResourceId), ct);
        return Accepted(new { result.JobId, result.Status });
    }

    /// <summary>
    /// Poll export job status.
    /// </summary>
    [HttpGet("export-jobs/{jobId:guid}")]
    public async Task<IActionResult> GetExportJob(Guid jobId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetExportJobQuery(_currentUser.UserId, jobId), ct);
        return Ok(result);
    }

    /// <summary>
    /// Download file when status = Ready.
    /// </summary>
    [HttpGet("export-jobs/{jobId:guid}/download")]
    public async Task<IActionResult> DownloadExport(Guid jobId, CancellationToken ct)
    {
        var result = await _mediator.Send(new DownloadExportQuery(_currentUser.UserId, jobId), ct);
        return File(result.Content, result.ContentType, result.FileName);
    }

    /// <summary>
    /// Resource utilization heatmap (person × week), scoped to user's project membership.
    /// </summary>
    [HttpGet("resources")]
    [ResponseCache(Duration = 300)]
    public async Task<IActionResult> GetResourceHeatmap(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken ct)
    {
        if (to < from)
            return BadRequest(new { detail = "to phải >= from." });
        var result = await _mediator.Send(new GetResourceReportQuery(_currentUser.UserId, from, to), ct);
        return Ok(result);
    }

    /// <summary>
    /// Cross-project milestone timeline, scoped to user's project membership.
    /// </summary>
    [HttpGet("milestones")]
    [ResponseCache(Duration = 300)]
    public async Task<IActionResult> GetMilestones(
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetMilestonesQuery(_currentUser.UserId, from, to), ct);
        return Ok(result);
    }
}

public sealed record TriggerExportRequest(
    string Format,
    string GroupBy,
    string? Month,
    Guid? VendorId,
    Guid? ProjectId,
    Guid? ResourceId);
