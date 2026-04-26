using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Metrics.Application.Commands.RecordMetricEvent;
using ProjectManagement.Metrics.Application.Queries.GetMetricSummary;
using ProjectManagement.Shared.Infrastructure.Services;

namespace ProjectManagement.Metrics.Api.Controllers;

[ApiController]
[Route("api/v1/metrics")]
[Authorize]
public class MetricsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public MetricsController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    /// <summary>POST /api/v1/metrics/events — record a behavioural metric event</summary>
    [HttpPost("events")]
    public async Task<IActionResult> RecordEvent(
        [FromBody] RecordMetricEventRequest req, CancellationToken ct)
    {
        await _mediator.Send(new RecordMetricEventCommand(
            req.EventType,
            _currentUser.UserId,
            req.ContextJson,
            req.CorrelationId), ct);
        return NoContent();
    }

    /// <summary>GET /api/v1/metrics/summary — aggregate metric counts</summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? eventType,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new GetMetricSummaryQuery(from, to, eventType), ct);
        return Ok(result);
    }
}

public sealed record RecordMetricEventRequest(
    string EventType,
    string? ContextJson,
    string? CorrelationId);
