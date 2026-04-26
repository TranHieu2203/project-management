using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Shared.Infrastructure.OptimisticLocking;
using ProjectManagement.Shared.Infrastructure.Services;
using ProjectManagement.Workforce.Application.Resources.Commands.CreateResource;
using ProjectManagement.Workforce.Application.Resources.Commands.InactivateResource;
using ProjectManagement.Workforce.Application.Resources.Commands.UpdateResource;
using ProjectManagement.Workforce.Application.Resources.Queries.GetResourceById;
using ProjectManagement.Workforce.Application.Resources.Queries.GetResourceList;

namespace ProjectManagement.Workforce.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/resources")]
public sealed class ResourcesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public ResourcesController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Returns list of resources. Optional filters: type, vendorId, activeOnly.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetResources(
        [FromQuery] string? type,
        [FromQuery] Guid? vendorId,
        [FromQuery] bool? activeOnly,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new GetResourceListQuery(type, vendorId, activeOnly), ct);
        return Ok(result);
    }

    /// <summary>
    /// Returns resource by ID. Returns 404 if not found.
    /// </summary>
    [HttpGet("{resourceId:guid}")]
    public async Task<IActionResult> GetResource(Guid resourceId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetResourceByIdQuery(resourceId), ct);
        Response.Headers.ETag = ETagHelper.Generate(result.Version);
        return Ok(result);
    }

    /// <summary>
    /// Creates a new resource. Returns 409 if code already exists.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateResource([FromBody] CreateResourceRequest body, CancellationToken ct)
    {
        var cmd = new CreateResourceCommand(body.Code, body.Name, body.Email, body.Type, body.VendorId, _currentUser.UserId.ToString());
        var result = await _mediator.Send(cmd, ct);
        Response.Headers.ETag = ETagHelper.Generate(result.Version);
        return CreatedAtAction(nameof(GetResource), new { resourceId = result.Id }, result);
    }

    /// <summary>
    /// Updates resource name/email. Code, Type, VendorId are immutable. Requires If-Match header.
    /// </summary>
    [HttpPut("{resourceId:guid}")]
    public async Task<IActionResult> UpdateResource(Guid resourceId, [FromBody] UpdateResourceRequest body, CancellationToken ct)
    {
        var version = ETagHelper.ParseIfMatch(Request.Headers.IfMatch);
        if (version is null)
            return StatusCode(StatusCodes.Status412PreconditionFailed,
                new ProblemDetails
                {
                    Status = 412,
                    Title = "Precondition Required",
                    Detail = "If-Match header là bắt buộc cho cập nhật."
                });

        var cmd = new UpdateResourceCommand(resourceId, body.Name, body.Email, (int)version, _currentUser.UserId.ToString());
        var result = await _mediator.Send(cmd, ct);
        Response.Headers.ETag = ETagHelper.Generate(result.Version);
        return Ok(result);
    }

    /// <summary>
    /// Inactivates a resource (soft, data preserved). Requires If-Match header. Returns 204.
    /// </summary>
    [HttpDelete("{resourceId:guid}")]
    public async Task<IActionResult> InactivateResource(Guid resourceId, CancellationToken ct)
    {
        var version = ETagHelper.ParseIfMatch(Request.Headers.IfMatch);
        if (version is null)
            return StatusCode(StatusCodes.Status412PreconditionFailed,
                new ProblemDetails
                {
                    Status = 412,
                    Title = "Precondition Required",
                    Detail = "If-Match header là bắt buộc cho inactivate."
                });

        var cmd = new InactivateResourceCommand(resourceId, (int)version, _currentUser.UserId.ToString());
        await _mediator.Send(cmd, ct);
        return NoContent();
    }
}

public sealed record CreateResourceRequest(string Code, string Name, string? Email, string Type, Guid? VendorId);
public sealed record UpdateResourceRequest(string Name, string? Email);
