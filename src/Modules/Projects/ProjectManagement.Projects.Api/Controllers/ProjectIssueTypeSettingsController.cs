using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Projects.Application.IssueTypes.Services;
using ProjectManagement.Shared.Infrastructure.Services;

namespace ProjectManagement.Projects.Api.Controllers;

[Authorize]
[ApiController]
public sealed class ProjectIssueTypeSettingsController : ControllerBase
{
    private readonly IProjectIssueTypeSettingsService _service;
    private readonly ICurrentUserService _currentUser;

    public ProjectIssueTypeSettingsController(IProjectIssueTypeSettingsService service, ICurrentUserService currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    [HttpGet]
    [Route("api/v1/projects/{projectId:guid}/issue-type-settings")]
    public async Task<IActionResult> Get(Guid projectId, CancellationToken ct)
    {
        var items = await _service.GetAsync(projectId, _currentUser.UserId, ct);
        return Ok(items);
    }

    [HttpPut]
    [Route("api/v1/projects/{projectId:guid}/issue-type-settings/{typeId:guid}")]
    public async Task<IActionResult> SetEnabled(
        Guid projectId,
        Guid typeId,
        [FromBody] SetIssueTypeEnabledRequest body,
        CancellationToken ct)
    {
        var updated = await _service.SetEnabledAsync(projectId, typeId, body.IsEnabled, _currentUser.UserId, ct);
        return Ok(updated);
    }
}

public sealed record SetIssueTypeEnabledRequest(bool IsEnabled);

