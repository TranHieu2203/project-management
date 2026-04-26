using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.Domain.Exceptions;
using ProjectManagement.TimeTracking.Application.Common.Interfaces;
using ProjectManagement.TimeTracking.Application.ImportJobs.Commands.StartImportJob;
using ProjectManagement.TimeTracking.Application.ImportJobs.DTOs;
using ProjectManagement.TimeTracking.Domain.Entities;
using ProjectManagement.TimeTracking.Domain.Enums;

namespace ProjectManagement.TimeTracking.Application.ImportJobs.Commands.ApplyImportJob;

public sealed class ApplyImportJobHandler : IRequestHandler<ApplyImportJobCommand, ImportJobDto>
{
    private readonly ITimeTrackingDbContext _db;
    private readonly ITimeTrackingRateService _rateService;

    public ApplyImportJobHandler(ITimeTrackingDbContext db, ITimeTrackingRateService rateService)
    {
        _db = db;
        _rateService = rateService;
    }

    public async Task<ImportJobDto> Handle(ApplyImportJobCommand cmd, CancellationToken ct)
    {
        var job = await _db.ImportJobs.FindAsync([cmd.JobId], ct)
            ?? throw new NotFoundException($"ImportJob {cmd.JobId} không tồn tại.");

        if (job.Status != ImportJobStatus.ValidatedOk && job.Status != ImportJobStatus.ValidatedWithWarnings)
            throw new DomainException($"Job phải ở trạng thái ValidatedOk hoặc ValidatedWithWarnings để apply. Trạng thái hiện tại: {job.Status}.");

        if (job.RawContent == null)
            throw new DomainException("Job không có raw content để apply.");

        job.MarkApplying();
        await _db.SaveChangesAsync(ct);

        try
        {
            var rows = ParseValidRows(job.RawContent, cmd.ColumnMapping);

            // Enforce period lock — reject if any month in this import is already locked
            var distinctMonths = rows.Select(r => (r.Date.Year, r.Date.Month)).Distinct();
            foreach (var (year, month) in distinctMonths)
            {
                var locked = await _db.PeriodLocks.AsNoTracking()
                    .AnyAsync(p => p.Year == year && p.Month == month, ct);
                if (locked)
                    throw new DomainException($"Kỳ {year}/{month:D2} đã bị lock. Import job bị từ chối.");
            }

            // Load existing fingerprints for this job to skip duplicates
            var existingFingerprints = await _db.TimeEntries.AsNoTracking()
                .Where(e => e.ImportJobId == job.Id)
                .Select(e => e.RowFingerprint)
                .ToHashSetAsync(ct);

            foreach (var row in rows)
            {
                var fingerprint = ComputeFingerprint(job.Id, row);
                if (existingFingerprints.Contains(fingerprint)) continue;

                var hourlyRate = await _rateService.GetHourlyRateAsync(
                    row.ResourceId, row.Role, row.Level, row.Date, ct);

                var entry = TimeEntry.Create(
                    row.ResourceId, row.ProjectId, row.TaskId,
                    row.Date, row.Hours, nameof(TimeEntryStatus.VendorConfirmed),
                    row.Note, hourlyRate, cmd.EnteredBy,
                    importJobId: job.Id, rowFingerprint: fingerprint);

                _db.TimeEntries.Add(entry);
            }

            job.MarkCompleted();
            await _db.SaveChangesAsync(ct);
        }
        catch
        {
            job.MarkFailed();
            await _db.SaveChangesAsync(ct);
            throw;
        }

        return StartImportJobHandler.ToDto(job);
    }

    private static List<ParsedRow> ParseValidRows(string csvContent, CsvColumnMapping mapping)
    {
        var rows = new List<ParsedRow>();
        using var reader = new StringReader(csvContent);
        var headerLine = reader.ReadLine();
        if (headerLine == null) return rows;

        var headers = headerLine.Split(',').Select(h => h.Trim()).ToArray();
        var colIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < headers.Length; i++) colIndex[headers[i]] = i;

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cols = line.Split(',');

            string? GetCol(string? colName) =>
                colName != null && colIndex.TryGetValue(colName, out var idx) && idx < cols.Length
                    ? cols[idx].Trim() : null;

            if (!Guid.TryParse(GetCol(mapping.ResourceIdColumn), out var resourceId)) continue;
            if (!Guid.TryParse(GetCol(mapping.ProjectIdColumn), out var projectId)) continue;
            if (!DateOnly.TryParse(GetCol(mapping.DateColumn), out var date)) continue;
            if (!decimal.TryParse(GetCol(mapping.HoursColumn), out var hours) || hours <= 0) continue;

            var role = GetCol(mapping.RoleColumn);
            var level = GetCol(mapping.LevelColumn);
            if (string.IsNullOrWhiteSpace(role) || string.IsNullOrWhiteSpace(level)) continue;

            var note = GetCol(mapping.NoteColumn);
            var taskIdStr = GetCol(mapping.TaskIdColumn);
            Guid? taskId = Guid.TryParse(taskIdStr, out var tid) ? tid : null;

            rows.Add(new ParsedRow(resourceId, projectId, taskId, date, hours, role, level, note));
        }
        return rows;
    }

    private static string ComputeFingerprint(Guid jobId, ParsedRow row)
    {
        var input = $"{jobId}|{row.ResourceId}|{row.ProjectId}|{row.Date:yyyy-MM-dd}|{row.Hours}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant()[..32];
    }
}
