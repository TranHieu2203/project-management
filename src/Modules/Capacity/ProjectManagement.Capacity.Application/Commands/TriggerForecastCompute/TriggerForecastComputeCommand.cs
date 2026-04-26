using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Capacity.Application.Common.Interfaces;
using ProjectManagement.Capacity.Domain.Entities;
using ProjectManagement.Projects.Application.Common.Interfaces;
using ProjectManagement.TimeTracking.Application.Common.Interfaces;

namespace ProjectManagement.Capacity.Application.Commands.TriggerForecastCompute;

public sealed record ForecastComputeResult(int Version, DateTime ComputedAt, string Status);

public sealed record TriggerForecastComputeCommand(Guid CurrentUserId)
    : IRequest<ForecastComputeResult>;

public sealed class TriggerForecastComputeHandler
    : IRequestHandler<TriggerForecastComputeCommand, ForecastComputeResult>
{
    private readonly ICapacityDbContext _capacityDb;
    private readonly IProjectsDbContext _projectsDb;
    private readonly ITimeTrackingDbContext _timeTrackingDb;

    public TriggerForecastComputeHandler(
        ICapacityDbContext capacityDb,
        IProjectsDbContext projectsDb,
        ITimeTrackingDbContext timeTrackingDb)
    {
        _capacityDb = capacityDb;
        _projectsDb = projectsDb;
        _timeTrackingDb = timeTrackingDb;
    }

    public async Task<ForecastComputeResult> Handle(
        TriggerForecastComputeCommand command, CancellationToken ct)
    {
        var nextVersion = (await _capacityDb.ForecastArtifacts
            .MaxAsync(a => (int?)a.Version, ct) ?? 0) + 1;

        var artifact = ForecastArtifact.Create(nextVersion);
        _capacityDb.ForecastArtifacts.Add(artifact);

        try
        {
            var payload = await ComputePayloadAsync(command.CurrentUserId, ct);
            var json = JsonSerializer.Serialize(payload);
            artifact.MarkSucceeded(json);
        }
        catch (Exception ex)
        {
            artifact.MarkFailed(ex.Message);
        }

        await _capacityDb.SaveChangesAsync(ct);
        return new ForecastComputeResult(artifact.Version, artifact.ComputedAt, artifact.Status);
    }

    private async Task<ForecastPayload> ComputePayloadAsync(Guid userId, CancellationToken ct)
    {
        // Guid.Empty = background system job → include all projects
        var projectIds = userId == Guid.Empty
            ? await _projectsDb.Projects
                .Where(p => !p.IsDeleted)
                .Select(p => p.Id)
                .ToListAsync(ct)
            : await _projectsDb.ProjectMemberships
                .Where(m => m.UserId == userId)
                .Select(m => m.ProjectId)
                .Distinct()
                .ToListAsync(ct);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var lookbackFrom = today.AddDays(-28);

        var entries = projectIds.Count == 0
            ? new List<EntryData>()
            : await _timeTrackingDb.TimeEntries.AsNoTracking()
                .Where(e => projectIds.Contains(e.ProjectId)
                         && !e.IsVoided
                         && e.Date >= lookbackFrom
                         && e.Date <= today)
                .Select(e => new EntryData(e.ResourceId, e.Date, e.Hours))
                .ToListAsync(ct);

        var nextMonday = GetNextMonday(today);
        var forecastWeeks = Enumerable.Range(0, 4)
            .Select(i => nextMonday.AddDays(i * 7))
            .ToList();

        var resources = entries
            .GroupBy(e => e.ResourceId)
            .Select(g =>
            {
                var weeklyHours = g
                    .GroupBy(e => GetMonday(e.Date))
                    .Select(w => w.Sum(e => e.Hours))
                    .ToList();

                var avgWeekly = weeklyHours.Count > 0 ? weeklyHours.Average() : 0m;

                var cells = forecastWeeks.Select(week =>
                {
                    const decimal available = 40m;
                    var pct = available > 0 ? Math.Round(avgWeekly / available * 100, 1) : 0m;
                    var light = pct switch
                    {
                        >= 105m => "Red",
                        >= 95m  => "Orange",
                        >= 80m  => "Yellow",
                        _       => "Green",
                    };
                    return new ForecastWeekCell(week.ToString("yyyy-MM-dd"), avgWeekly, available, pct, light);
                }).ToList();

                return new ForecastResourceRow(g.Key, cells);
            })
            .OrderByDescending(r => r.Cells.Count(c => c.TrafficLight is "Red" or "Orange"))
            .ToList();

        return new ForecastPayload(
            forecastWeeks.Select(w => w.ToString("yyyy-MM-dd")).ToList(),
            resources);
    }

    private static DateOnly GetNextMonday(DateOnly today)
    {
        var daysUntilMonday = ((int)DayOfWeek.Monday - (int)today.DayOfWeek + 7) % 7;
        return today.AddDays(daysUntilMonday == 0 ? 7 : daysUntilMonday);
    }

    private static DateOnly GetMonday(DateOnly date)
    {
        var diff = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.AddDays(-diff);
    }

    private sealed record EntryData(Guid ResourceId, DateOnly Date, decimal Hours);
}

public sealed record ForecastWeekCell(
    string WeekStart,
    decimal ForecastedHours,
    decimal AvailableHours,
    decimal ForecastedUtilizationPct,
    string TrafficLight);

public sealed record ForecastResourceRow(
    Guid ResourceId,
    List<ForecastWeekCell> Cells);

public sealed record ForecastPayload(
    List<string> Weeks,
    List<ForecastResourceRow> Resources);
