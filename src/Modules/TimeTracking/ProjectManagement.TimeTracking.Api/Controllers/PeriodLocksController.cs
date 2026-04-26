using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Shared.Infrastructure.Services;
using ProjectManagement.TimeTracking.Application.PeriodLocks.Commands;
using ProjectManagement.TimeTracking.Application.PeriodLocks.Queries;

namespace ProjectManagement.TimeTracking.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/period-locks")]
public sealed class PeriodLocksController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public PeriodLocksController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    /// <summary>
    /// List all locked periods for a vendor.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetLocks([FromQuery] Guid vendorId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPeriodLocksQuery(vendorId), ct);
        return Ok(result);
    }

    /// <summary>
    /// Get reconcile summary (estimated / pm-adjusted / confirmed) for a vendor + month.
    /// </summary>
    [HttpGet("reconcile")]
    public async Task<IActionResult> GetReconcile(
        [FromQuery] Guid vendorId,
        [FromQuery] int year,
        [FromQuery] int month,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPeriodReconcileQuery(vendorId, year, month), ct);
        return Ok(result);
    }

    /// <summary>
    /// Lock a period for a vendor (idempotent).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Lock([FromBody] LockPeriodRequest body, CancellationToken ct)
    {
        var cmd = new LockPeriodCommand(body.VendorId, body.Year, body.Month, _currentUser.UserId.ToString());
        var result = await _mediator.Send(cmd, ct);
        return Ok(result);
    }

    /// <summary>
    /// Unlock a period (admin operation).
    /// </summary>
    [HttpDelete("{vendorId:guid}/{year:int}/{month:int}")]
    public async Task<IActionResult> Unlock(Guid vendorId, int year, int month, CancellationToken ct)
    {
        await _mediator.Send(new UnlockPeriodCommand(vendorId, year, month), ct);
        return NoContent();
    }
}

public sealed record LockPeriodRequest(Guid VendorId, int Year, int Month);
