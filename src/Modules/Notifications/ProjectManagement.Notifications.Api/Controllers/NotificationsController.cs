using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Notifications.Application.Commands.MarkAllNotificationsRead;
using ProjectManagement.Notifications.Application.Commands.MarkNotificationRead;
using ProjectManagement.Notifications.Application.Queries.GetMyNotifications;
using ProjectManagement.Shared.Infrastructure.Services;

namespace ProjectManagement.Notifications.Api.Controllers;

[ApiController]
[Route("api/v1/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public NotificationsController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] bool unreadOnly = false, CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetMyNotificationsQuery(_currentUser.UserId, unreadOnly), ct);
        return Ok(result);
    }

    // read-all MUST come before {id:guid}/read to avoid route conflict
    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        await _mediator.Send(new MarkAllNotificationsReadCommand(_currentUser.UserId), ct);
        return NoContent();
    }

    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
    {
        var found = await _mediator.Send(
            new MarkNotificationReadCommand(id, _currentUser.UserId), ct);
        return found ? NoContent() : NotFound();
    }
}
