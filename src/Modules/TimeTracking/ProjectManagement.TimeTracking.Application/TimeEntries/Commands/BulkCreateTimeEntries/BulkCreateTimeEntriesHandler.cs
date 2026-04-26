using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.Domain.Exceptions;
using ProjectManagement.TimeTracking.Application.Common.Interfaces;
using ProjectManagement.TimeTracking.Application.DTOs;
using ProjectManagement.TimeTracking.Application.TimeEntries.Commands.CreateTimeEntry;
using ProjectManagement.TimeTracking.Domain.Entities;
using ProjectManagement.TimeTracking.Domain.Enums;

namespace ProjectManagement.TimeTracking.Application.TimeEntries.Commands.BulkCreateTimeEntries;

public sealed class BulkCreateTimeEntriesHandler : IRequestHandler<BulkCreateTimeEntriesCommand, BulkCreateResult>
{
    private readonly ITimeTrackingDbContext _db;
    private readonly ITimeTrackingRateService _rateService;

    public BulkCreateTimeEntriesHandler(ITimeTrackingDbContext db, ITimeTrackingRateService rateService)
    {
        _db = db;
        _rateService = rateService;
    }

    public async Task<BulkCreateResult> Handle(BulkCreateTimeEntriesCommand cmd, CancellationToken ct)
    {
        if (cmd.Rows.Count == 0)
            return new BulkCreateResult(true, [], []);

        var errors = new List<BulkValidationError>();

        // Validate EntryType for every row upfront
        for (int i = 0; i < cmd.Rows.Count; i++)
        {
            var row = cmd.Rows[i];
            if (!Enum.TryParse<TimeEntryStatus>(row.EntryType, out var status) ||
                status == TimeEntryStatus.VendorConfirmed)
            {
                errors.Add(new BulkValidationError(i, "hard",
                    $"Row {i}: EntryType '{row.EntryType}' không hợp lệ. Chấp nhận: Estimated, PmAdjusted."));
            }
            if (row.Hours <= 0 || row.Hours > 24)
            {
                errors.Add(new BulkValidationError(i, "hard",
                    $"Row {i}: Hours phải trong khoảng (0, 24]."));
            }
        }

        // Hard: 16h/day cap per (resourceId, date)
        var dayTotals = cmd.Rows
            .GroupBy(r => (r.ResourceId, r.Date))
            .ToDictionary(g => g.Key, g => g.Sum(r => r.Hours));

        for (int i = 0; i < cmd.Rows.Count; i++)
        {
            var row = cmd.Rows[i];
            var key = (row.ResourceId, row.Date);
            if (dayTotals[key] > 16m)
            {
                errors.Add(new BulkValidationError(i, "hard",
                    $"Row {i}: Tổng giờ ngày {row.Date:yyyy-MM-dd} cho resource vượt 16h (= {dayTotals[key]}h)."));
            }
        }

        if (errors.Any(e => e.ErrorType == "hard"))
            return new BulkCreateResult(false, [], errors);

        // Soft warning: PmAdjusted >20% deviation vs existing Estimated, without note
        for (int i = 0; i < cmd.Rows.Count; i++)
        {
            var row = cmd.Rows[i];
            if (row.EntryType != "PmAdjusted" || !string.IsNullOrWhiteSpace(row.Note))
                continue;

            var estimatedHours = await _db.TimeEntries.AsNoTracking()
                .Where(e => e.ResourceId == row.ResourceId
                    && e.ProjectId == row.ProjectId
                    && e.Date == row.Date
                    && e.EntryType == "Estimated"
                    && !e.IsVoided)
                .SumAsync(e => (decimal?)e.Hours, ct) ?? 0m;

            if (estimatedHours > 0 && Math.Abs(row.Hours - estimatedHours) / estimatedHours > 0.20m)
            {
                errors.Add(new BulkValidationError(i, "warning",
                    $"Row {i}: PmAdjusted {row.Hours}h lệch >20% so với Estimated {estimatedHours}h. Cần thêm Note."));
            }
        }

        if (errors.Any())
            return new BulkCreateResult(false, [], errors);

        // All valid — create entries in one transaction
        var created = new List<TimeEntryDto>();
        foreach (var row in cmd.Rows)
        {
            var hourlyRate = await _rateService.GetHourlyRateAsync(
                row.ResourceId, row.Role, row.Level, row.Date, ct);

            var entry = TimeEntry.Create(
                row.ResourceId, row.ProjectId, row.TaskId,
                row.Date, row.Hours, row.EntryType,
                row.Note, hourlyRate, cmd.EnteredBy);

            _db.TimeEntries.Add(entry);
            created.Add(CreateTimeEntryHandler.ToDto(entry));
        }
        await _db.SaveChangesAsync(ct);

        return new BulkCreateResult(true, created, []);
    }
}
