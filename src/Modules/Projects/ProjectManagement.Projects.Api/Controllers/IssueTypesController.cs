using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Projects.Application.IssueTypes.Services;
using ProjectManagement.Shared.Infrastructure.Services;

namespace ProjectManagement.Projects.Api.Controllers;

[Authorize]
[ApiController]
public sealed class IssueTypesController : ControllerBase
{
    private readonly IIssueTypesService _service;
    private readonly ICurrentUserService _currentUser;

    public IssueTypesController(IIssueTypesService service, ICurrentUserService currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    /// <summary>
    /// List system-wide built-in issue types.
    /// </summary>
    [HttpGet]
    [Route("api/v1/issue-types")]
    public async Task<IActionResult> GetBuiltIn(CancellationToken ct)
    {
        var result = await _service.GetBuiltInAsync(ct);
        return Ok(result);
    }

    /// <summary>
    /// List built-in + project custom issue types (membership-only).
    /// </summary>
    [HttpGet]
    [Route("api/v1/projects/{projectId:guid}/issue-types")]
    public async Task<IActionResult> GetByProject(Guid projectId, CancellationToken ct)
    {
        var result = await _service.GetByProjectAsync(projectId, _currentUser.UserId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Create a custom issue type for a project (membership-only).
    /// </summary>
    [HttpPost]
    [Route("api/v1/projects/{projectId:guid}/issue-types")]
    public async Task<IActionResult> Create(Guid projectId, [FromBody] UpsertIssueTypeRequest body, CancellationToken ct)
    {
        var created = await _service.CreateAsync(
            projectId,
            body.Name,
            body.IconKey,
            body.Color,
            _currentUser.UserId,
            ct);

        return CreatedAtAction(nameof(GetByProject), new { projectId }, created);
    }

    /// <summary>
    /// Backward-compatible route from epics AC: POST /api/v1/issue-types with projectId in body.
    /// </summary>
    [HttpPost]
    [Route("api/v1/issue-types")]
    public async Task<IActionResult> CreateCompat([FromBody] CreateIssueTypeCompatRequest body, CancellationToken ct)
    {
        if (body.ProjectId is null)
        {
            return BadRequest(new ProblemDetails
            {
                Status = 400,
                Title = "ValidationError",
                Detail = "projectId là bắt buộc khi tạo custom issue type."
            });
        }

        var created = await _service.CreateAsync(
            body.ProjectId.Value,
            body.Name,
            body.IconKey,
            body.Color,
            _currentUser.UserId,
            ct);

        return CreatedAtAction(nameof(GetByProject), new { projectId = body.ProjectId.Value }, created);
    }

    [HttpPut]
    [Route("api/v1/projects/{projectId:guid}/issue-types/{typeId:guid}")]
    public async Task<IActionResult> Update(
        Guid projectId,
        Guid typeId,
        [FromBody] UpsertIssueTypeRequest body,
        CancellationToken ct)
    {
        var updated = await _service.UpdateAsync(
            projectId,
            typeId,
            body.Name,
            body.IconKey,
            body.Color,
            _currentUser.UserId,
            ct);
        return Ok(updated);
    }

    [HttpDelete]
    [Route("api/v1/projects/{projectId:guid}/issue-types/{typeId:guid}")]
    public async Task<IActionResult> Delete(Guid projectId, Guid typeId, CancellationToken ct)
    {
        await _service.DeleteAsync(projectId, typeId, _currentUser.UserId, ct);
        return NoContent();
    }
}

public sealed record UpsertIssueTypeRequest(string Name, string? IconKey, string Color);

public sealed record CreateIssueTypeCompatRequest(
    string Name,
    string? IconKey,
    string Color,
    Guid? ProjectId);

