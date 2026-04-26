using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Capacity.Application.Commands.LogCapacityOverride;
using ProjectManagement.Capacity.Application.Commands.TriggerForecastCompute;
using ProjectManagement.Capacity.Application.Queries.GetCapacityHeatmap;
using ProjectManagement.Capacity.Application.Queries.GetForecastDelta;
using ProjectManagement.Capacity.Application.Queries.GetLatestForecast;
using ProjectManagement.Capacity.Application.Queries.GetCapacityUtilization;
using ProjectManagement.Capacity.Application.Queries.GetCrossProjectOverload;
using ProjectManagement.Capacity.Application.Queries.GetResourceOverload;
using ProjectManagement.Shared.Infrastructure.Services;

namespace ProjectManagement.Capacity.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/capacity")]
public sealed class CapacityController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public CapacityController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Compute overload for a resource: OL-01 (&gt;8h/day) and OL-02 (&gt;40h/week).
    /// </summary>
    [HttpGet("overload")]
    public async Task<IActionResult> GetOverload(
        [FromQuery] Guid resourceId,
        [FromQuery] DateOnly dateFrom,
        [FromQuery] DateOnly dateTo,
        CancellationToken ct)
    {
        if (dateTo < dateFrom)
            return BadRequest(new { detail = "dateTo phải >= dateFrom." });

        var result = await _mediator.Send(new GetResourceOverloadQuery(resourceId, dateFrom, dateTo), ct);
        return Ok(result);
    }

    /// <summary>
    /// Get predictive capacity utilization % and traffic-light status.
    /// </summary>
    [HttpGet("utilization")]
    public async Task<IActionResult> GetUtilization(
        [FromQuery] Guid resourceId,
        [FromQuery] DateOnly dateFrom,
        [FromQuery] DateOnly dateTo,
        CancellationToken ct)
    {
        if (dateTo < dateFrom)
            return BadRequest(new { detail = "dateTo phải >= dateFrom." });

        var result = await _mediator.Send(new GetCapacityUtilizationQuery(resourceId, dateFrom, dateTo), ct);
        return Ok(result);
    }

    /// <summary>
    /// Log when a PM overrides a capacity warning — data for threshold tuning.
    /// </summary>
    [HttpPost("overrides")]
    public async Task<IActionResult> LogOverride([FromBody] LogOverrideRequest req, CancellationToken ct)
    {
        var userName = User.Identity?.Name ?? "unknown";
        await _mediator.Send(new LogCapacityOverrideCommand(
            req.ResourceId, req.DateFrom, req.DateTo, req.TrafficLight, userName), ct);
        return NoContent();
    }

    /// <summary>
    /// Cross-project overload aggregation — scoped to projects where current user is a member.
    /// Non-member projects are invisible; no existence leakage possible.
    /// </summary>
    [HttpGet("cross-project")]
    public async Task<IActionResult> GetCrossProjectOverload(
        [FromQuery] DateOnly dateFrom,
        [FromQuery] DateOnly dateTo,
        CancellationToken ct)
    {
        if (dateTo < dateFrom)
            return BadRequest(new { detail = "dateTo phải >= dateFrom." });

        var result = await _mediator.Send(
            new GetCrossProjectOverloadQuery(_currentUser.UserId, dateFrom, dateTo), ct);
        return Ok(result);
    }

    /// <summary>
    /// Capacity heatmap: person × week utilization, scoped to user's project membership.
    /// Non-member projects are invisible; no existence leakage possible.
    /// </summary>
    [HttpGet("heatmap")]
    public async Task<IActionResult> GetHeatmap(
        [FromQuery] DateOnly dateFrom,
        [FromQuery] DateOnly dateTo,
        CancellationToken ct)
    {
        if (dateTo < dateFrom)
            return BadRequest(new { detail = "dateTo phải >= dateFrom." });

        var result = await _mediator.Send(
            new GetCapacityHeatmapQuery(_currentUser.UserId, dateFrom, dateTo), ct);
        return Ok(result);
    }

    /// <summary>
    /// Trigger 4-week rolling capacity forecast precompute. Scoped to current user's membership.
    /// </summary>
    [HttpPost("forecast/compute")]
    public async Task<IActionResult> ComputeForecast(CancellationToken ct)
    {
        var result = await _mediator.Send(
            new TriggerForecastComputeCommand(_currentUser.UserId), ct);
        return Ok(result);
    }

    /// <summary>
    /// Get latest succeeded 4-week capacity forecast.
    /// </summary>
    [HttpGet("forecast")]
    public async Task<IActionResult> GetForecast(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetLatestForecastQuery(), ct);
        return Ok(result);
    }

    /// <summary>
    /// Forecast delta: top changes between the two most recent succeeded forecasts.
    /// </summary>
    [HttpGet("forecast/delta")]
    public async Task<IActionResult> GetForecastDelta(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetForecastDeltaQuery(), ct);
        return Ok(result);
    }
}

public sealed record LogOverrideRequest(Guid ResourceId, DateOnly DateFrom, DateOnly DateTo, string TrafficLight);
