using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectManagement.Auth.Domain.Users;
using ProjectManagement.Notifications.Application.Common.Interfaces;
using ProjectManagement.Reporting.Application.Common.Interfaces;

namespace ProjectManagement.Reporting.Infrastructure.Workers;

public class AlertDigestWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AlertDigestWorker> _logger;

    public AlertDigestWorker(IServiceScopeFactory scopeFactory, ILogger<AlertDigestWorker> logger)
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
        var db       = scope.ServiceProvider.GetRequiredService<IReportingDbContext>();
        var emailSvc = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var userMgr  = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var isoWeek = ISOWeek.GetWeekOfYear(DateTime.UtcNow);
        var year    = DateTime.UtcNow.Year;

        var usersWithOverloadPref = await db.AlertPreferences
            .AsNoTracking()
            .Where(p => p.AlertType == "overload" && p.Enabled)
            .Select(p => p.UserId)
            .Distinct()
            .ToListAsync(ct);

        if (usersWithOverloadPref.Count == 0) return;

        // Alerts from last week (Mon–Sun)
        var weekStart = DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek + 1).AddDays(-7);
        var weekEnd   = weekStart.AddDays(7);

        foreach (var userId in usersWithOverloadPref)
        {
            try
            {
                var alerts = await db.Alerts
                    .AsNoTracking()
                    .Where(a => a.UserId == userId
                             && a.Type == "overload"
                             && a.CreatedAt >= weekStart
                             && a.CreatedAt < weekEnd)
                    .OrderByDescending(a => a.CreatedAt)
                    .Take(50)
                    .Select(a => new { a.Title, a.CreatedAt })
                    .ToListAsync(ct);

                if (alerts.Count == 0) continue;

                var user = await userMgr.FindByIdAsync(userId.ToString());
                if (user is null || string.IsNullOrEmpty(user.Email)) continue;

                var rows = string.Join("", alerts.Select(a =>
                    $"<tr><td>{System.Net.WebUtility.HtmlEncode(a.Title)}</td><td>{a.CreatedAt:yyyy-MM-dd HH:mm}</td></tr>"));

                var html = $"""
                    <html><body>
                    <h2>Alert Digest — Tuần {isoWeek}/{year}</h2>
                    <h3>⚠️ Overload Alerts ({alerts.Count})</h3>
                    <table border='1' cellpadding='4'>
                    <tr><th>Alert</th><th>Thời gian</th></tr>
                    {rows}
                    </table>
                    <hr/><p style='font-size:11px;color:#999'>
                    Để tắt: <a href='/settings/notifications'>Cài đặt thông báo</a></p>
                    </body></html>
                    """;

                await emailSvc.SendAsync(user.Email,
                    $"[PM Tool] Alert Digest — Tuần {isoWeek}/{year}", html, ct);

                _logger.LogInformation(
                    "AlertDigestWorker: sent digest to {Email} (week {Week}/{Year})",
                    user.Email, isoWeek, year);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AlertDigestWorker: error for user {UserId}", userId);
            }
        }
    }
}
