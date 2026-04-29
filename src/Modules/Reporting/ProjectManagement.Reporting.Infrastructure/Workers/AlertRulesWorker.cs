using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectManagement.Projects.Application.Common.Interfaces;
using ProjectManagement.Projects.Domain.Enums;
using ProjectManagement.Reporting.Application.Common.Interfaces;
using ProjectManagement.Reporting.Domain.Entities;

namespace ProjectManagement.Reporting.Infrastructure.Workers;

public class AlertRulesWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AlertRulesWorker> _logger;

    public AlertRulesWorker(IServiceScopeFactory scopeFactory, ILogger<AlertRulesWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await EvaluateRulesAsync(stoppingToken);
        }
    }

    private async Task EvaluateRulesAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var projectsDb  = scope.ServiceProvider.GetRequiredService<IProjectsDbContext>();
            var reportingDb = scope.ServiceProvider.GetRequiredService<IReportingDbContext>();

            await CreateDeadlineAlertsAsync(projectsDb, reportingDb, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AlertRulesWorker: error during evaluation");
        }
    }

    private async Task CreateDeadlineAlertsAsync(
        IProjectsDbContext projectsDb,
        IReportingDbContext reportingDb,
        CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var thresholdDate = today.AddDays(2);

        var tasks = await projectsDb.ProjectTasks
            .AsNoTracking()
            .Where(t => !t.IsDeleted
                     && t.PlannedEndDate.HasValue
                     && t.PlannedEndDate.Value <= thresholdDate
                     && t.Status != ProjectTaskStatus.Completed
                     && t.Status != ProjectTaskStatus.Cancelled)
            .Select(t => new { t.Id, t.Name, t.ProjectId, t.PlannedEndDate })
            .ToListAsync(ct);

        if (tasks.Count == 0) return;

        var projectIds = tasks.Select(t => t.ProjectId).Distinct().ToList();

        var memberships = await projectsDb.ProjectMemberships
            .AsNoTracking()
            .Where(m => projectIds.Contains(m.ProjectId))
            .Select(m => new { m.ProjectId, m.UserId })
            .ToListAsync(ct);

        var taskIds = tasks.Select(t => t.Id).ToList();
        var todayDt = DateTime.UtcNow.Date;

        var existingToday = await reportingDb.Alerts
            .AsNoTracking()
            .Where(a => a.Type == "deadline"
                     && a.EntityType == "Task"
                     && taskIds.Contains(a.EntityId!.Value)
                     && a.CreatedAt >= todayDt)
            .Select(a => new { a.UserId, EntityId = a.EntityId!.Value })
            .ToListAsync(ct);

        var existingSet = existingToday
            .Select(x => (x.UserId, x.EntityId))
            .ToHashSet();

        var membersByProject = memberships.ToLookup(m => m.ProjectId, m => m.UserId);

        int created = 0;
        foreach (var task in tasks)
        {
            var hoursLeft = task.PlannedEndDate.HasValue
                ? (task.PlannedEndDate.Value.ToDateTime(TimeOnly.MinValue) - DateTime.UtcNow).TotalHours
                : 48;
            var nHours = Math.Max(0, (int)Math.Ceiling(hoursLeft));
            var title = $"{task.Name} — deadline trong {nHours}h";

            foreach (var userId in membersByProject[task.ProjectId])
            {
                if (existingSet.Contains((userId, task.Id))) continue;

                reportingDb.Alerts.Add(Alert.Create(
                    userId, "deadline", title,
                    projectId: task.ProjectId,
                    entityType: "Task",
                    entityId: task.Id));
                created++;
            }
        }

        if (created > 0)
        {
            await reportingDb.SaveChangesAsync(ct);
            _logger.LogInformation("AlertRulesWorker: created {Count} deadline alerts", created);
        }
    }
}
