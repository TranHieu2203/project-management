using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Reporting.Application.Queries.GetMyTasksCrossProject;
using ProjectManagement.Reporting.Application.Queries.GetProjectsSummary;
using ProjectManagement.Reporting.Application.Queries.GetStatCards;
using ProjectManagement.Reporting.Application.Queries.GetUpcomingDeadlines;
using ProjectManagement.Shared.Infrastructure.Services;

namespace ProjectManagement.Reporting.Api.Controllers;

[ApiController]
[Route("api/v1/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public DashboardController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Portfolio summary: health cards for all projects the PM has membership in.
    /// </summary>
    [HttpGet("summary")]
    [ResponseCache(Duration = 60)]
    public async Task<IActionResult> GetSummary(
        [FromQuery] Guid[]? projectIds = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetProjectsSummaryQuery(_currentUser.UserId, projectIds?.ToList()), ct);
        return Ok(result);
    }

    /// <summary>
    /// Stat cards: overdue task count, at-risk project count, overloaded resource count.
    /// </summary>
    [HttpGet("stat-cards")]
    public async Task<IActionResult> GetStatCards(
        [FromQuery] Guid[]? projectIds = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetStatCardsQuery(_currentUser.UserId, projectIds?.ToList()), ct);
        return Ok(result);
    }

    /// <summary>
    /// Upcoming deadlines within the next N days (default 7), sorted by due date.
    /// </summary>
    [HttpGet("deadlines")]
    public async Task<IActionResult> GetDeadlines(
        [FromQuery] int daysAhead = 7,
        [FromQuery] Guid[]? projectIds = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetUpcomingDeadlinesQuery(_currentUser.UserId, daysAhead, projectIds?.ToList()), ct);
        return Ok(result);
    }

    /// <summary>
    /// My tasks across all projects the PM has membership in, with status + project filters and pagination.
    /// </summary>
    [HttpGet("my-tasks")]
    public async Task<IActionResult> GetMyTasks(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string[]? status = null,
        [FromQuery] Guid[]? projectIds = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetMyTasksCrossProjectQuery(
                _currentUser.UserId,
                page,
                pageSize,
                status?.ToList(),
                projectIds?.ToList()),
            ct);
        return Ok(result);
    }
}
