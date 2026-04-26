using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Notifications.Application.Commands.UpdateNotificationPreference;
using ProjectManagement.Notifications.Application.Queries.GetNotificationPreferences;
using ProjectManagement.Shared.Infrastructure.Services;

namespace ProjectManagement.Notifications.Api.Controllers;

[ApiController]
[Route("api/v1/notification-preferences")]
[Authorize]
public class NotificationPreferencesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public NotificationPreferencesController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetPreferences(CancellationToken ct)
    {
        var result = await _mediator.Send(
            new GetNotificationPreferencesQuery(_currentUser.UserId), ct);
        return Ok(result);
    }

    [HttpPatch("{type}")]
    public async Task<IActionResult> UpdatePreference(string type, [FromBody] UpdatePreferenceRequest req, CancellationToken ct)
    {
        await _mediator.Send(
            new UpdateNotificationPreferenceCommand(_currentUser.UserId, type, req.IsEnabled), ct);
        return NoContent();
    }
}

public sealed record UpdatePreferenceRequest(bool IsEnabled);
