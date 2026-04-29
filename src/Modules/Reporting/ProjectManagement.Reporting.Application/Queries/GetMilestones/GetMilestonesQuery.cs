using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Projects.Application.Common.Interfaces;
using ProjectManagement.Projects.Domain.Enums;

namespace ProjectManagement.Reporting.Application.Queries.GetMilestones;

public sealed record MilestoneDto(
    Guid TaskId,
    string Name,
    Guid ProjectId,
    string ProjectName,
    DateOnly? DueDate,
    string Status);

public sealed record GetMilestonesQuery(
    Guid CurrentUserId,
    DateOnly? DateFrom = null,
    DateOnly? DateTo = null)
    : IRequest<IReadOnlyList<MilestoneDto>>;

public sealed class GetMilestonesHandler : IRequestHandler<GetMilestonesQuery, IReadOnlyList<MilestoneDto>>
{
    private readonly IProjectsDbContext _db;

    public GetMilestonesHandler(IProjectsDbContext db) => _db = db;

    public async Task<IReadOnlyList<MilestoneDto>> Handle(GetMilestonesQuery request, CancellationToken ct)
    {
        var memberProjectIds = await _db.ProjectMemberships
            .AsNoTracking()
            .Where(m => m.UserId == request.CurrentUserId)
            .Select(m => m.ProjectId)
            .Distinct()
            .ToListAsync(ct);

        if (memberProjectIds.Count == 0)
            return [];

        var projectNames = await _db.Projects
            .AsNoTracking()
            .Where(p => memberProjectIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Name })
            .ToDictionaryAsync(p => p.Id, p => p.Name, ct);

        var query = _db.ProjectTasks
            .AsNoTracking()
            .Where(t =>
                memberProjectIds.Contains(t.ProjectId) &&
                t.Type == TaskType.Milestone &&
                !t.IsDeleted);

        if (request.DateFrom.HasValue)
            query = query.Where(t => t.PlannedEndDate >= request.DateFrom.Value);
        if (request.DateTo.HasValue)
            query = query.Where(t => t.PlannedEndDate <= request.DateTo.Value);

        var tasks = await query
            .OrderBy(t => t.PlannedEndDate)
            .ToListAsync(ct);

        return tasks.Select(t => new MilestoneDto(
            t.Id,
            t.Name,
            t.ProjectId,
            projectNames.GetValueOrDefault(t.ProjectId, string.Empty),
            t.PlannedEndDate,
            t.Status.ToString()))
            .ToList();
    }
}
