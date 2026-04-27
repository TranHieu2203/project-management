using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Projects.Application.Tasks.Queries.GetMyTasks;
using ProjectManagement.Shared.Infrastructure.Services;

namespace ProjectManagement.Projects.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/my-tasks")]
public sealed class MyTasksController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public MyTasksController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Trả tất cả tasks được giao cho current user (chưa hoàn thành / chưa hủy),
    /// across all projects.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetMyTasks(
        [FromQuery] bool overdue = false,
        [FromQuery] string? q = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetMyTasksQuery(_currentUser.UserId, OverdueOnly: overdue, Keyword: q), ct);
        return Ok(result);
    }
}
