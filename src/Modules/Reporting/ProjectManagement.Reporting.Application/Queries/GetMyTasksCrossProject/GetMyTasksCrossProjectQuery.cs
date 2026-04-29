using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Projects.Application.Common.Interfaces;
using ProjectManagement.Projects.Domain.Enums;
using ProjectManagement.Reporting.Application.Common;

namespace ProjectManagement.Reporting.Application.Queries.GetMyTasksCrossProject;

public sealed record MyTaskDto(
    Guid Id,
    Guid ProjectId,
    string ProjectName,
    string? ProjectCode,
    string? Vbs,
    string Name,
    string Status,
    string Priority,
    string? PlannedEndDate,
    int? PercentComplete);

public sealed record GetMyTasksCrossProjectQuery(
    Guid CurrentUserId,
    int Page = 1,
    int PageSize = 20,
    IReadOnlyList<string>? StatusFilter = null,
    IReadOnlyList<Guid>? ProjectIds = null
) : IRequest<PagedResult<MyTaskDto>>;

public sealed class GetMyTasksCrossProjectHandler
    : IRequestHandler<GetMyTasksCrossProjectQuery, PagedResult<MyTaskDto>>
{
    private readonly IProjectsDbContext _db;

    public GetMyTasksCrossProjectHandler(IProjectsDbContext db) => _db = db;

    public async Task<PagedResult<MyTaskDto>> Handle(
        GetMyTasksCrossProjectQuery request, CancellationToken ct)
    {
        // 1. Membership-scoped project IDs
        var memberProjectIds = await _db.ProjectMemberships
            .AsNoTracking()
            .Where(m => m.UserId == request.CurrentUserId)
            .Select(m => m.ProjectId)
            .Distinct()
            .ToListAsync(ct);

        // 2. Intersect with requested project filter
        var targetProjectIds = (request.ProjectIds?.Count > 0)
            ? memberProjectIds.Intersect(request.ProjectIds).ToList()
            : memberProjectIds;

        if (targetProjectIds.Count == 0)
            return new PagedResult<MyTaskDto>([], 0, request.Page, request.PageSize);

        // 3. Parse status filter strings → enum values
        var statusEnums = new List<ProjectTaskStatus>();
        if (request.StatusFilter?.Count > 0)
        {
            foreach (var s in request.StatusFilter)
            {
                if (Enum.TryParse<ProjectTaskStatus>(s, ignoreCase: true, out var parsed))
                    statusEnums.Add(parsed);
            }
        }

        // 4. Base query: tasks assigned to current user within scoped projects
        var query = _db.ProjectTasks
            .AsNoTracking()
            .Where(t =>
                t.AssigneeUserId == request.CurrentUserId &&
                targetProjectIds.Contains(t.ProjectId) &&
                t.Type == TaskType.Task &&
                !t.IsDeleted);

        if (statusEnums.Count > 0)
            query = query.Where(t => statusEnums.Contains(t.Status));

        // 5. Count before pagination
        var totalCount = await query.CountAsync(ct);

        if (totalCount == 0)
            return new PagedResult<MyTaskDto>([], 0, request.Page, request.PageSize);

        // 6. Load project names for display
        var projects = await _db.Projects
            .AsNoTracking()
            .Where(p => targetProjectIds.Contains(p.Id) && !p.IsDeleted)
            .Select(p => new { p.Id, p.Name, p.Code })
            .ToListAsync(ct);

        var projectMap = projects.ToDictionary(p => p.Id, p => (p.Name, p.Code));

        // 7. Sort: null end dates last, then by end date asc, then name
        var tasks = await query
            .OrderBy(t => t.PlannedEndDate == null)
            .ThenBy(t => t.PlannedEndDate)
            .ThenBy(t => t.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => new
            {
                t.Id,
                t.ProjectId,
                t.Vbs,
                t.Name,
                t.Status,
                t.Priority,
                t.PlannedEndDate,
                t.PercentComplete,
            })
            .ToListAsync(ct);

        var items = tasks.Select(t =>
        {
            var (projName, projCode) = projectMap.TryGetValue(t.ProjectId, out var p)
                ? p : ("Unknown", null);
            return new MyTaskDto(
                t.Id,
                t.ProjectId,
                projName,
                string.IsNullOrEmpty(projCode) ? null : projCode,
                string.IsNullOrEmpty(t.Vbs) ? null : t.Vbs,
                t.Name,
                t.Status.ToString(),
                t.Priority.ToString(),
                t.PlannedEndDate?.ToString("yyyy-MM-dd"),
                t.PercentComplete.HasValue ? (int?)Convert.ToInt32(t.PercentComplete.Value) : null
            );
        }).ToList();

        return new PagedResult<MyTaskDto>(items, totalCount, request.Page, request.PageSize);
    }
}
