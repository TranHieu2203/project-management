using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Workforce.Application.Audit.Queries.GetAuditList;

namespace ProjectManagement.Workforce.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/audit")]
public sealed class AuditController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuditController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Returns audit events. Optional filters: entityType, entityId, pageSize (max 200).
    /// Read-only — no POST/PUT/DELETE endpoints.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAuditEvents(
        [FromQuery] string? entityType,
        [FromQuery] Guid? entityId,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetAuditListQuery(entityType, entityId, pageSize), ct);
        return Ok(result);
    }
}
