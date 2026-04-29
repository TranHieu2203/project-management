using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Projects.Application.Common.Interfaces;
using ProjectManagement.Projects.Application.DTOs;
using ProjectManagement.Projects.Domain.Enums;

namespace ProjectManagement.Projects.Application.Tasks.Queries.GetMyTasks;

public sealed class GetMyTasksHandler : IRequestHandler<GetMyTasksQuery, List<MyTaskDto>>
{
    private readonly IProjectsDbContext _db;

    public GetMyTasksHandler(IProjectsDbContext db) => _db = db;

    public async Task<List<MyTaskDto>> Handle(GetMyTasksQuery query, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Join tasks with projects to get project name in one query
        // Note: use enum values directly — EF Core cannot translate .ToString() to SQL
        var q = _db.ProjectTasks
            .Where(t => t.AssigneeUserId == query.CurrentUserId
                     && t.Status != ProjectTaskStatus.Cancelled)
            .Join(_db.Projects,
                  t => t.ProjectId,
                  p => p.Id,
                  (t, p) => new { Task = t, p.Name, p.Code });

        if (query.OverdueOnly)
            q = q.Where(x => x.Task.PlannedEndDate < today);

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.Trim();
            q = q.Where(x => x.Task.Name.Contains(kw)
                           || (x.Task.Vbs != null && x.Task.Vbs.Contains(kw)));
        }

        var rows = await q
            .OrderBy(x => x.Task.PlannedEndDate == null)
            .ThenBy(x => x.Task.PlannedEndDate)
            .ThenBy(x => x.Task.Priority)
            .ToListAsync(ct);

        return rows.Select(x => new MyTaskDto(
            x.Task.Id, x.Task.ProjectId, x.Name, x.Code,
            x.Task.ParentId, x.Task.Type.ToString(),
            x.Task.Vbs, x.Task.Name,
            x.Task.Priority.ToString(), x.Task.Status.ToString(),
            x.Task.PlannedEndDate, x.Task.PercentComplete,
            x.Task.AssigneeUserId, x.Task.SortOrder, x.Task.Version))
            .ToList();
    }
}
