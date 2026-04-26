using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Shared.Infrastructure.Services;
using ProjectManagement.Workforce.Application.Rates.Commands.CreateRate;
using ProjectManagement.Workforce.Application.Rates.Commands.DeleteRate;
using ProjectManagement.Workforce.Application.Rates.Queries.GetRateById;
using ProjectManagement.Workforce.Application.Rates.Queries.GetRateList;

namespace ProjectManagement.Workforce.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/rates")]
public sealed class RatesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public RatesController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Returns list of monthly rates. Optional filters: vendorId, year, month.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetRates(
        [FromQuery] Guid? vendorId,
        [FromQuery] int? year,
        [FromQuery] int? month,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new GetRateListQuery(vendorId, year, month), ct);
        return Ok(result);
    }

    /// <summary>
    /// Returns rate by ID. Returns 404 if not found.
    /// </summary>
    [HttpGet("{rateId:guid}")]
    public async Task<IActionResult> GetRate(Guid rateId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetRateByIdQuery(rateId), ct);
        return Ok(result);
    }

    /// <summary>
    /// Creates a new monthly rate. Returns 409 if rate for same vendor/role/level/month already exists.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateRate([FromBody] CreateRateRequest body, CancellationToken ct)
    {
        var cmd = new CreateRateCommand(
            body.VendorId, body.Role, body.Level,
            body.Year, body.Month, body.MonthlyAmount,
            _currentUser.UserId.ToString());
        var result = await _mediator.Send(cmd, ct);
        return CreatedAtAction(nameof(GetRate), new { rateId = result.Id }, result);
    }

    /// <summary>
    /// Deletes a rate (hard delete). Returns 204 on success.
    /// </summary>
    [HttpDelete("{rateId:guid}")]
    public async Task<IActionResult> DeleteRate(Guid rateId, CancellationToken ct)
    {
        await _mediator.Send(new DeleteRateCommand(rateId, _currentUser.UserId.ToString()), ct);
        return NoContent();
    }
}

public sealed record CreateRateRequest(
    Guid VendorId,
    string Role,
    string Level,
    int Year,
    int Month,
    decimal MonthlyAmount);
