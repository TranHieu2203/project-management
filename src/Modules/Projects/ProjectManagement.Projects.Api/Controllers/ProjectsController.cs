using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Projects.Application.Commands.CreateProject;
using ProjectManagement.Projects.Application.Commands.DeleteProject;
using ProjectManagement.Projects.Application.Commands.UpdateProject;
using ProjectManagement.Projects.Application.Queries.GetProjectById;
using ProjectManagement.Projects.Application.Queries.GetProjectList;
using ProjectManagement.Projects.Application.Queries.GetProjectMembers;
using ProjectManagement.Shared.Infrastructure.OptimisticLocking;
using ProjectManagement.Shared.Infrastructure.Services;

namespace ProjectManagement.Projects.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/projects")]
public sealed class ProjectsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public ProjectsController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Returns only projects the current user is a member of (membership-only).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetProjects(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetProjectListQuery(_currentUser.UserId), ct);
        return Ok(result);
    }

    /// <summary>
    /// Returns project detail. Returns 404 for both non-existent and non-member projects
    /// to prevent existence leakage (membership-only policy).
    /// </summary>
    [HttpGet("{projectId:guid}")]
    public async Task<IActionResult> GetProject(Guid projectId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetProjectByIdQuery(projectId, _currentUser.UserId), ct);
        Response.Headers.ETag = ETagHelper.Generate(result.Version);
        return Ok(result);
    }

    /// <summary>
    /// Returns the list of members for a project (membership-only: caller must be a member).
    /// </summary>
    [HttpGet("{projectId:guid}/members")]
    public async Task<IActionResult> GetProjectMembers(Guid projectId, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new GetProjectMembersQuery(projectId, _currentUser.UserId), ct);
        return Ok(result);
    }

    /// <summary>
    /// Creates a new project. Creator is automatically added as Manager member.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateProject([FromBody] CreateProjectRequest body, CancellationToken ct)
    {
        var cmd = new CreateProjectCommand(body.Code, body.Name, body.Description, _currentUser.UserId);
        var result = await _mediator.Send(cmd, ct);
        Response.Headers.ETag = ETagHelper.Generate(result.Version);
        return CreatedAtAction(nameof(GetProject), new { projectId = result.Id }, result);
    }

    /// <summary>
    /// Updates an existing project. Requires If-Match header for optimistic locking.
    /// Returns 412 if header is missing, 409 if version mismatch.
    /// </summary>
    [HttpPut("{projectId:guid}")]
    public async Task<IActionResult> UpdateProject(Guid projectId, [FromBody] UpdateProjectRequest body, CancellationToken ct)
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

        var cmd = new UpdateProjectCommand(projectId, body.Name, body.Description, (int)version, _currentUser.UserId);
        var result = await _mediator.Send(cmd, ct);
        Response.Headers.ETag = ETagHelper.Generate(result.Version);
        return Ok(result);
    }

    /// <summary>
    /// Archives (soft-deletes) a project. Requires If-Match header for optimistic locking.
    /// Returns 412 if header is missing, 409 if version mismatch.
    /// </summary>
    [HttpDelete("{projectId:guid}")]
    public async Task<IActionResult> DeleteProject(Guid projectId, CancellationToken ct)
    {
        var version = ETagHelper.ParseIfMatch(Request.Headers.IfMatch);
        if (version is null)
            return StatusCode(StatusCodes.Status412PreconditionFailed,
                new ProblemDetails
                {
                    Status = 412,
                    Title = "Precondition Required",
                    Detail = "If-Match header là bắt buộc cho xóa."
                });

        var cmd = new DeleteProjectCommand(projectId, (int)version, _currentUser.UserId);
        await _mediator.Send(cmd, ct);
        return NoContent();
    }
}

public sealed record CreateProjectRequest(string Code, string Name, string? Description);
public sealed record UpdateProjectRequest(string Name, string? Description);
