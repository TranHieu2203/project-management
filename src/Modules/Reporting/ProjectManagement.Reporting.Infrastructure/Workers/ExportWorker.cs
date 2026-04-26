using System.Text.Json;
using System.Threading.Channels;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectManagement.Reporting.Application.Queries.GetCostBreakdown;
using ProjectManagement.Reporting.Domain.Entities;
using ProjectManagement.Reporting.Infrastructure.Persistence;
using ProjectManagement.Reporting.Infrastructure.Services;
// CsvExportService, XlsxExportService, PdfExportService all in same namespace

namespace ProjectManagement.Reporting.Infrastructure.Workers;

public class ExportWorker : BackgroundService
{
    private readonly ChannelReader<Guid> _reader;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExportWorker> _logger;

    public ExportWorker(ChannelReader<Guid> reader, IServiceScopeFactory scopeFactory, ILogger<ExportWorker> logger)
    {
        _reader = reader;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RecoverPendingJobsAsync(stoppingToken);

        await foreach (var jobId in _reader.ReadAllAsync(stoppingToken))
        {
            await ProcessJobAsync(jobId, stoppingToken);
        }
    }

    private async Task RecoverPendingJobsAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ReportingDbContext>();
        var writer = scope.ServiceProvider.GetRequiredService<ChannelWriter<Guid>>();

        var queuedIds = await db.ExportJobs
            .Where(j => j.Status == ExportJobStatus.Queued)
            .Select(j => j.Id)
            .ToListAsync(ct);

        foreach (var id in queuedIds)
            await writer.WriteAsync(id, ct);

        if (queuedIds.Count > 0)
            _logger.LogInformation("ExportWorker: recovered {Count} Queued job(s) on startup", queuedIds.Count);
    }

    private async Task ProcessJobAsync(Guid jobId, CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ReportingDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var csvSvc  = scope.ServiceProvider.GetRequiredService<CsvExportService>();
        var xlsxSvc = scope.ServiceProvider.GetRequiredService<XlsxExportService>();
        var pdfSvc  = scope.ServiceProvider.GetRequiredService<PdfExportService>();

        var job = await db.ExportJobs.FindAsync([jobId], ct);
        if (job is null)
        {
            _logger.LogWarning("ExportWorker: job {JobId} not found in DB, skipping", jobId);
            return;
        }

        try
        {
            job.MarkProcessing();
            await db.SaveChangesAsync(ct);

            var filter = JsonSerializer.Deserialize<ExportFilterParams>(job.FilterParams,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

            var breakdown = await mediator.Send(new GetCostBreakdownQuery(
                job.TriggeredBy,
                job.GroupBy,
                filter.Month,
                filter.VendorId,
                filter.ProjectId,
                filter.ResourceId,
                Page: 1,
                PageSize: 10000), ct);

            byte[] content = job.Format switch
            {
                "csv"  => csvSvc.Generate(breakdown),
                "xlsx" => xlsxSvc.Generate(breakdown),
                "pdf"  => pdfSvc.Generate(breakdown, $"Báo cáo Chi phí — {job.GroupBy}"),
                _      => throw new InvalidOperationException($"Format không hỗ trợ: {job.Format}")
            };

            var ts  = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var ext = job.Format switch { "xlsx" => "xlsx", "pdf" => "pdf", _ => "csv" };
            var fileName = $"cost-breakdown_{job.GroupBy}_{ts}.{ext}";

            job.MarkReady(fileName, content);
            await db.SaveChangesAsync(ct);

            _logger.LogInformation("ExportWorker: job {JobId} completed — {FileName}", jobId, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ExportWorker: job {JobId} failed", jobId);
            job.MarkFailed(ex.Message);
            await db.SaveChangesAsync(ct);
        }
    }
}

internal sealed record ExportFilterParams(
    string? Month,
    Guid? VendorId,
    Guid? ProjectId,
    Guid? ResourceId);
