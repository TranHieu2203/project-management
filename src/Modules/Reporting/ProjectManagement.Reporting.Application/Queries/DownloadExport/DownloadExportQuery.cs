using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Reporting.Application.Common.Interfaces;
using ProjectManagement.Reporting.Domain.Entities;
using ProjectManagement.Shared.Domain.Exceptions;

namespace ProjectManagement.Reporting.Application.Queries.DownloadExport;

public sealed record DownloadExportQuery(Guid CurrentUserId, Guid JobId) : IRequest<DownloadExportResult>;

public sealed record DownloadExportResult(byte[] Content, string ContentType, string FileName);

public sealed class DownloadExportHandler : IRequestHandler<DownloadExportQuery, DownloadExportResult>
{
    private readonly IReportingDbContext _db;

    public DownloadExportHandler(IReportingDbContext db) => _db = db;

    public async Task<DownloadExportResult> Handle(DownloadExportQuery query, CancellationToken ct)
    {
        var job = await _db.ExportJobs
            .FirstOrDefaultAsync(j => j.Id == query.JobId && j.TriggeredBy == query.CurrentUserId, ct);

        if (job is null)
            throw new NotFoundException("ExportJob", query.JobId);

        if (job.Status != ExportJobStatus.Ready)
            throw new InvalidOperationException($"Export job chưa sẵn sàng. Trạng thái hiện tại: {job.Status}.");

        var contentType = job.Format switch
        {
            "csv"  => "text/csv;charset=utf-8",
            "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "pdf"  => "application/pdf",
            _      => "application/octet-stream"
        };

        return new DownloadExportResult(job.FileContent!, contentType, job.FileName!);
    }
}
