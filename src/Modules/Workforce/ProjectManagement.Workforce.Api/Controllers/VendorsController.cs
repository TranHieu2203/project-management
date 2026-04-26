using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Shared.Infrastructure.OptimisticLocking;
using ProjectManagement.Shared.Infrastructure.Services;
using ProjectManagement.Workforce.Application.Vendors.Commands.CreateVendor;
using ProjectManagement.Workforce.Application.Vendors.Commands.InactivateVendor;
using ProjectManagement.Workforce.Application.Vendors.Commands.UpdateVendor;
using ProjectManagement.Workforce.Application.Vendors.Queries.GetVendorById;
using ProjectManagement.Workforce.Application.Vendors.Queries.GetVendorList;

namespace ProjectManagement.Workforce.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/vendors")]
public sealed class VendorsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public VendorsController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Returns list of vendors. Optional filter: activeOnly=true.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetVendors([FromQuery] bool? activeOnly, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetVendorListQuery(activeOnly), ct);
        return Ok(result);
    }

    /// <summary>
    /// Returns vendor by ID. Returns 404 if not found.
    /// </summary>
    [HttpGet("{vendorId:guid}")]
    public async Task<IActionResult> GetVendor(Guid vendorId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetVendorByIdQuery(vendorId), ct);
        Response.Headers.ETag = ETagHelper.Generate(result.Version);
        return Ok(result);
    }

    /// <summary>
    /// Creates a new vendor. Returns 409 if code already exists.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateVendor([FromBody] CreateVendorRequest body, CancellationToken ct)
    {
        var cmd = new CreateVendorCommand(body.Code, body.Name, body.Description, _currentUser.UserId.ToString());
        var result = await _mediator.Send(cmd, ct);
        Response.Headers.ETag = ETagHelper.Generate(result.Version);
        return CreatedAtAction(nameof(GetVendor), new { vendorId = result.Id }, result);
    }

    /// <summary>
    /// Updates vendor name/description. Requires If-Match header (412 if missing, 409 if mismatch).
    /// </summary>
    [HttpPut("{vendorId:guid}")]
    public async Task<IActionResult> UpdateVendor(Guid vendorId, [FromBody] UpdateVendorRequest body, CancellationToken ct)
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

        var cmd = new UpdateVendorCommand(vendorId, body.Name, body.Description, (int)version, _currentUser.UserId.ToString());
        var result = await _mediator.Send(cmd, ct);
        Response.Headers.ETag = ETagHelper.Generate(result.Version);
        return Ok(result);
    }

    /// <summary>
    /// Inactivates a vendor (soft, data preserved). Requires If-Match header.
    /// Returns 204 on success.
    /// </summary>
    [HttpDelete("{vendorId:guid}")]
    public async Task<IActionResult> InactivateVendor(Guid vendorId, CancellationToken ct)
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

        var cmd = new InactivateVendorCommand(vendorId, (int)version, _currentUser.UserId.ToString());
        await _mediator.Send(cmd, ct);
        return NoContent();
    }
}

public sealed record CreateVendorRequest(string Code, string Name, string? Description);
public sealed record UpdateVendorRequest(string Name, string? Description);
