using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Reporting.Application.Common.Interfaces;
using ProjectManagement.Shared.Domain.Exceptions;

namespace ProjectManagement.Reporting.Application.Queries.GetExportJob;

public sealed record GetExportJobQuery(Guid CurrentUserId, Guid JobId) : IRequest<ExportJobDto>;

public sealed record ExportJobDto(
    Guid JobId,
    string Status,
    string Format,
    string GroupBy,
    string? FileName,
    string? ErrorMessage,
    DateTime CreatedAt,
    DateTime? CompletedAt);

public sealed class GetExportJobHandler : IRequestHandler<GetExportJobQuery, ExportJobDto>
{
    private readonly IReportingDbContext _db;

    public GetExportJobHandler(IReportingDbContext db) => _db = db;

    public async Task<ExportJobDto> Handle(GetExportJobQuery query, CancellationToken ct)
    {
        var job = await _db.ExportJobs
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == query.JobId && j.TriggeredBy == query.CurrentUserId, ct);

        if (job is null)
            throw new NotFoundException("ExportJob", query.JobId);

        return new ExportJobDto(
            job.Id, job.Status, job.Format, job.GroupBy,
            job.FileName, job.ErrorMessage, job.CreatedAt, job.CompletedAt);
    }
}
