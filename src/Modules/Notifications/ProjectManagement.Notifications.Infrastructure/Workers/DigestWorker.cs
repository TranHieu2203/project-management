using System.Globalization;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectManagement.Auth.Domain.Users;
using ProjectManagement.Capacity.Application.Queries.GetCrossProjectOverload;
using ProjectManagement.Notifications.Application.Common.Interfaces;
using ProjectManagement.Notifications.Domain.Entities;
using ProjectManagement.Notifications.Domain.Enums;
using ProjectManagement.Projects.Application.Common.Interfaces;
using ProjectManagement.Projects.Domain.Enums;

namespace ProjectManagement.Notifications.Infrastructure.Workers;

public class DigestWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DigestWorker> _logger;

    public DigestWorker(IServiceScopeFactory scopeFactory, ILogger<DigestWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var now = DateTime.UtcNow;
            if (now.DayOfWeek == DayOfWeek.Monday && now.Hour == 7)
            {
                await SendDigestsAsync(stoppingToken);
            }
        }
    }

    private async Task SendDigestsAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db         = scope.ServiceProvider.GetRequiredService<INotificationsDbContext>();
        var emailSvc   = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var userMgr    = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var projectsDb = scope.ServiceProvider.GetRequiredService<IProjectsDbContext>();
        var mediator   = scope.ServiceProvider.GetRequiredService<IMediator>();

        var today   = DateOnly.FromDateTime(DateTime.UtcNow);
        var isoWeek = ISOWeek.GetWeekOfYear(DateTime.UtcNow);
        var year    = DateTime.UtcNow.Year;

        var allUserIds = await projectsDb.ProjectMemberships
            .Select(m => m.UserId).Distinct().ToListAsync(ct);

        foreach (var userId in allUserIds)
        {
            try
            {
                await SendDigestForUserAsync(
                    userId, today, isoWeek, year, mediator, db, emailSvc, userMgr, projectsDb, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DigestWorker: error sending digest for user {UserId}", userId);
            }
        }
    }

    private async Task SendDigestForUserAsync(
        Guid userId, DateOnly today, int isoWeek, int year,
        IMediator mediator, INotificationsDbContext db,
        IEmailService emailSvc, UserManager<ApplicationUser> userMgr,
        IProjectsDbContext projectsDb, CancellationToken ct)
    {
        var user = await userMgr.FindByIdAsync(userId.ToString());
        if (user is null || string.IsNullOrEmpty(user.Email)) return;

        var prefs = await db.NotificationPreferences
            .Where(p => p.UserId == userId)
            .ToDictionaryAsync(p => p.Type, p => p.IsEnabled, ct);

        bool overloadEnabled = prefs.GetValueOrDefault(NotificationType.Overload, true);
        bool overdueEnabled  = prefs.GetValueOrDefault(NotificationType.Overdue, true);

        if (!overloadEnabled && !overdueEnabled) return;

        var alreadySent = await db.DigestLogs.AnyAsync(
            l => l.UserId == userId && l.DigestType == "weekly"
              && l.IsoWeek == isoWeek && l.Year == year, ct);
        if (alreadySent) return;

        var sections = new List<string>();

        if (overloadEnabled)
        {
            var weekStart = today.AddDays(-(int)today.DayOfWeek + 1);
            var weekEnd   = weekStart.AddDays(6);
            var overload  = await mediator.Send(
                new GetCrossProjectOverloadQuery(userId, weekStart, weekEnd), ct);
            var overloaded = overload.Resources.Where(r => r.HasOverload).ToList();
            if (overloaded.Count > 0)
            {
                var rows = string.Join("", overloaded.Select(r =>
                    $"<tr><td>{r.ResourceId}</td><td style='color:red'>{r.TotalHours:F1}h</td><td>{r.OverloadedDays} ngày</td></tr>"));
                sections.Add($"""
                    <h3>⚠️ Nhân sự quá tải ({overloaded.Count})</h3>
                    <table border='1' cellpadding='4'>
                    <tr><th>ResourceId</th><th>Tổng giờ</th><th>Ngày quá tải</th></tr>
                    {rows}
                    </table>
                    """);
            }
        }

        if (overdueEnabled)
        {
            var lookahead = today.AddDays(7);
            var memberProjectIds = await projectsDb.ProjectMemberships
                .Where(m => m.UserId == userId)
                .Select(m => m.ProjectId).Distinct().ToListAsync(ct);

            var overdueTasks = await projectsDb.ProjectTasks
                .Where(t => memberProjectIds.Contains(t.ProjectId)
                         && !t.IsDeleted
                         && t.PlannedEndDate.HasValue
                         && t.PlannedEndDate.Value <= lookahead
                         && t.Status != ProjectTaskStatus.Completed
                         && t.Status != ProjectTaskStatus.Cancelled)
                .Select(t => new { t.Name, t.PlannedEndDate, t.Status, t.ProjectId })
                .OrderBy(t => t.PlannedEndDate)
                .Take(20)
                .ToListAsync(ct);

            if (overdueTasks.Count > 0)
            {
                var rows = string.Join("", overdueTasks.Select(t =>
                {
                    var isLate = t.PlannedEndDate < today;
                    var color  = isLate ? "color:red" : "color:orange";
                    return $"<tr><td style='{color}'>{t.Name}</td><td>{t.PlannedEndDate:yyyy-MM-dd}</td><td>{t.Status}</td></tr>";
                }));
                sections.Add($"""
                    <h3>📋 Task sắp trễ / đã trễ ({overdueTasks.Count})</h3>
                    <table border='1' cellpadding='4'>
                    <tr><th>Task</th><th>Deadline</th><th>Trạng thái</th></tr>
                    {rows}
                    </table>
                    """);
            }
        }

        if (sections.Count == 0) return;

        var html = $"""
            <html><body>
            <h2>Weekly Digest — {DateTime.UtcNow:yyyy-MM-dd}</h2>
            {string.Join("<hr/>", sections)}
            <hr/><p style='font-size:11px;color:#999'>
            Để tắt thông báo: <a href='/settings/notifications'>Cài đặt thông báo</a></p>
            </body></html>
            """;

        await emailSvc.SendAsync(user.Email, $"[PM Tool] Weekly Digest {DateTime.UtcNow:yyyy-MM-dd}", html, ct);

        db.DigestLogs.Add(DigestLog.Create(userId, "weekly", isoWeek, year));
        await db.SaveChangesAsync(ct);
        _logger.LogInformation("DigestWorker: sent digest to {Email} (week {Week}/{Year})", user.Email, isoWeek, year);
    }
}
