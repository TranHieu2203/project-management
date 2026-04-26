using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Workforce.Application.Lookups.Queries.GetRoleLevelCatalog;

namespace ProjectManagement.Workforce.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/lookups")]
public sealed class LookupsController : ControllerBase
{
    private readonly IMediator _mediator;

    public LookupsController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Returns available resource roles catalog.
    /// </summary>
    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles(CancellationToken ct)
    {
        var catalog = await _mediator.Send(new GetRoleLevelCatalogQuery(), ct);
        return Ok(catalog.Roles);
    }

    /// <summary>
    /// Returns available resource levels catalog.
    /// </summary>
    [HttpGet("levels")]
    public async Task<IActionResult> GetLevels(CancellationToken ct)
    {
        var catalog = await _mediator.Send(new GetRoleLevelCatalogQuery(), ct);
        return Ok(catalog.Levels);
    }
}
