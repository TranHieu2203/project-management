using System.Text.Json;
using System.Threading.Channels;
using MediatR;
using ProjectManagement.Reporting.Application.Common.Interfaces;
using ProjectManagement.Reporting.Domain.Entities;

namespace ProjectManagement.Reporting.Application.Commands.TriggerExport;

public sealed record TriggerExportCommand(
    Guid CurrentUserId,
    string Format,
    string GroupBy,
    string? Month,
    Guid? VendorId,
    Guid? ProjectId,
    Guid? ResourceId
) : IRequest<TriggerExportResult>;

public sealed record TriggerExportResult(Guid JobId, string Status);

public sealed class TriggerExportHandler : IRequestHandler<TriggerExportCommand, TriggerExportResult>
{
    private readonly IReportingDbContext _db;
    private readonly ChannelWriter<Guid> _channel;

    public TriggerExportHandler(IReportingDbContext db, ChannelWriter<Guid> channel)
    {
        _db = db;
        _channel = channel;
    }

    public async Task<TriggerExportResult> Handle(TriggerExportCommand command, CancellationToken ct)
    {
        var validFormats = new[] { "csv", "xlsx", "pdf" };
        if (!validFormats.Contains(command.Format.ToLowerInvariant()))
            throw new ArgumentException($"Format '{command.Format}' không hợp lệ. Chấp nhận: csv, xlsx.");

        var validGroupBy = new[] { "vendor", "project", "resource", "month" };
        if (!validGroupBy.Contains(command.GroupBy.ToLowerInvariant()))
            throw new ArgumentException($"GroupBy '{command.GroupBy}' không hợp lệ. Chấp nhận: vendor, project, resource, month.");

        var filterParams = JsonSerializer.Serialize(new
        {
            month = command.Month,
            vendorId = command.VendorId,
            projectId = command.ProjectId,
            resourceId = command.ResourceId,
        });

        var job = ExportJob.Create(
            command.CurrentUserId,
            command.Format.ToLowerInvariant(),
            command.GroupBy.ToLowerInvariant(),
            filterParams);

        _db.ExportJobs.Add(job);
        await _db.SaveChangesAsync(ct);

        await _channel.WriteAsync(job.Id, ct);

        return new TriggerExportResult(job.Id, job.Status);
    }
}
