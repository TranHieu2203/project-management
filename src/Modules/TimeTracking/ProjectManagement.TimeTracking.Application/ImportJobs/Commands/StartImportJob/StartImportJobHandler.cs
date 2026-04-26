using System.Security.Cryptography;
using System.Text;
using MediatR;
using ProjectManagement.TimeTracking.Application.Common.Interfaces;
using ProjectManagement.TimeTracking.Application.ImportJobs.DTOs;
using ProjectManagement.TimeTracking.Domain.Entities;
using ProjectManagement.TimeTracking.Domain.Enums;

namespace ProjectManagement.TimeTracking.Application.ImportJobs.Commands.StartImportJob;

public sealed class StartImportJobHandler : IRequestHandler<StartImportJobCommand, ImportJobDto>
{
    private readonly ITimeTrackingDbContext _db;

    public StartImportJobHandler(ITimeTrackingDbContext db) => _db = db;

    public async Task<ImportJobDto> Handle(StartImportJobCommand cmd, CancellationToken ct)
    {
        var fileHash = ComputeSha256(cmd.RawCsvContent);

        var job = ImportJob.Create(cmd.VendorId, cmd.FileName, fileHash, cmd.RawCsvContent, cmd.EnteredBy);
        _db.ImportJobs.Add(job);
        await _db.SaveChangesAsync(ct);

        var (rows, errors) = ParseAndValidate(job.Id, cmd.RawCsvContent, cmd.ColumnMapping);

        foreach (var err in errors)
            _db.ImportJobErrors.Add(err);

        var hasBlocking = errors.Any(e => e.ErrorType == "blocking");
        var hasWarning = errors.Any(e => e.ErrorType == "warning");

        var status = hasBlocking
            ? ImportJobStatus.ValidatedWithErrors
            : hasWarning
                ? ImportJobStatus.ValidatedWithWarnings
                : ImportJobStatus.ValidatedOk;

        job.SetValidationResult(status, rows.Count, errors.Count);
        await _db.SaveChangesAsync(ct);

        return ToDto(job);
    }

    private static (List<ParsedRow> rows, List<ImportJobError> errors) ParseAndValidate(
        Guid jobId, string csvContent, CsvColumnMapping mapping)
    {
        var rows = new List<ParsedRow>();
        var errors = new List<ImportJobError>();

        using var reader = new StringReader(csvContent);
        var headerLine = reader.ReadLine();
        if (headerLine == null) return (rows, errors);

        var headers = headerLine.Split(',').Select(h => h.Trim()).ToArray();
        var colIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < headers.Length; i++) colIndex[headers[i]] = i;

        int rowIdx = 1;
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) { rowIdx++; continue; }
            var cols = line.Split(',');

            string? GetCol(string? colName) =>
                colName != null && colIndex.TryGetValue(colName, out var idx) && idx < cols.Length
                    ? cols[idx].Trim() : null;

            var resourceIdStr = GetCol(mapping.ResourceIdColumn);
            var projectIdStr = GetCol(mapping.ProjectIdColumn);
            var dateStr = GetCol(mapping.DateColumn);
            var hoursStr = GetCol(mapping.HoursColumn);
            var role = GetCol(mapping.RoleColumn);
            var level = GetCol(mapping.LevelColumn);
            var note = GetCol(mapping.NoteColumn);
            var taskIdStr = GetCol(mapping.TaskIdColumn);

            bool rowValid = true;

            if (!Guid.TryParse(resourceIdStr, out var resourceId))
            {
                errors.Add(ImportJobError.Create(jobId, rowIdx, mapping.ResourceIdColumn, "blocking",
                    $"Row {rowIdx}: ResourceId '{resourceIdStr}' không hợp lệ."));
                rowValid = false;
            }
            if (!Guid.TryParse(projectIdStr, out var projectId))
            {
                errors.Add(ImportJobError.Create(jobId, rowIdx, mapping.ProjectIdColumn, "blocking",
                    $"Row {rowIdx}: ProjectId '{projectIdStr}' không hợp lệ."));
                rowValid = false;
            }
            if (!DateOnly.TryParse(dateStr, out var date))
            {
                errors.Add(ImportJobError.Create(jobId, rowIdx, mapping.DateColumn, "blocking",
                    $"Row {rowIdx}: Date '{dateStr}' không hợp lệ."));
                rowValid = false;
            }
            if (!decimal.TryParse(hoursStr, out var hours) || hours <= 0 || hours > 16)
            {
                errors.Add(ImportJobError.Create(jobId, rowIdx, mapping.HoursColumn, "blocking",
                    $"Row {rowIdx}: Hours '{hoursStr}' phải là số trong khoảng (0, 16]."));
                rowValid = false;
            }
            if (string.IsNullOrWhiteSpace(role))
            {
                errors.Add(ImportJobError.Create(jobId, rowIdx, mapping.RoleColumn, "blocking",
                    $"Row {rowIdx}: Role không được để trống."));
                rowValid = false;
            }
            if (string.IsNullOrWhiteSpace(level))
            {
                errors.Add(ImportJobError.Create(jobId, rowIdx, mapping.LevelColumn, "blocking",
                    $"Row {rowIdx}: Level không được để trống."));
                rowValid = false;
            }

            if (rowValid)
            {
                Guid.TryParse(taskIdStr, out var taskId);
                rows.Add(new ParsedRow(resourceId, projectId,
                    string.IsNullOrWhiteSpace(taskIdStr) ? null : taskId,
                    date, hours, role!, level!, note));
            }

            rowIdx++;
        }

        return (rows, errors);
    }

    internal static string ComputeSha256(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes).ToLowerInvariant()[..32];
    }

    internal static ImportJobDto ToDto(ImportJob j) => new(
        j.Id, j.VendorId, j.FileName, j.FileHash,
        j.Status.ToString(), j.TotalRows, j.ErrorCount,
        j.EnteredBy, j.CreatedAt, j.CompletedAt);
}

internal sealed record ParsedRow(
    Guid ResourceId, Guid ProjectId, Guid? TaskId,
    DateOnly Date, decimal Hours, string Role, string Level, string? Note);
