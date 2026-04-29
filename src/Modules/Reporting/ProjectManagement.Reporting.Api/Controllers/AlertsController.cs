using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Reporting.Application.Alerts.GetMyAlerts;
using ProjectManagement.Reporting.Application.Alerts.MarkAlertRead;
using ProjectManagement.Reporting.Application.Alerts.UpsertAlertPreference;
using ProjectManagement.Shared.Infrastructure.Services;

namespace ProjectManagement.Reporting.Api.Controllers;

[ApiController]
[Route("api/v1/alerts")]
[Authorize]
public class AlertsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public AlertsController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyAlerts(
        [FromQuery] bool unreadOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetMyAlertsQuery(_currentUser.UserId, unreadOnly, page, pageSize), ct);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkAlertRead(Guid id, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new MarkAlertReadCommand(id, _currentUser.UserId), ct);
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPut("preferences")]
    public async Task<IActionResult> UpsertPreference(
        [FromBody] UpsertAlertPreferenceRequest req,
        CancellationToken ct)
    {
        await _mediator.Send(
            new UpsertAlertPreferenceCommand(
                _currentUser.UserId,
                req.AlertType,
                req.Enabled,
                req.ThresholdDays), ct);
        return NoContent();
    }
}

public sealed record UpsertAlertPreferenceRequest(
    string AlertType,
    bool Enabled,
    int? ThresholdDays);
