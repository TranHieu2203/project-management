using MediatR;
using ProjectManagement.Shared.Domain.Exceptions;
using ProjectManagement.TimeTracking.Application.Common.Interfaces;
using ProjectManagement.TimeTracking.Application.ImportJobs.Commands.StartImportJob;
using ProjectManagement.TimeTracking.Application.ImportJobs.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ProjectManagement.TimeTracking.Application.ImportJobs.Queries;

public sealed record GetImportJobQuery(Guid JobId) : IRequest<ImportJobDto>;

public sealed class GetImportJobHandler : IRequestHandler<GetImportJobQuery, ImportJobDto>
{
    private readonly ITimeTrackingDbContext _db;
    public GetImportJobHandler(ITimeTrackingDbContext db) => _db = db;

    public async Task<ImportJobDto> Handle(GetImportJobQuery query, CancellationToken ct)
    {
        var job = await _db.ImportJobs.AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == query.JobId, ct)
            ?? throw new NotFoundException($"ImportJob {query.JobId} không tồn tại.");
        return StartImportJobHandler.ToDto(job);
    }
}

public sealed record GetImportJobErrorsQuery(Guid JobId) : IRequest<IReadOnlyList<ImportJobErrorDto>>;

public sealed class GetImportJobErrorsHandler : IRequestHandler<GetImportJobErrorsQuery, IReadOnlyList<ImportJobErrorDto>>
{
    private readonly ITimeTrackingDbContext _db;
    public GetImportJobErrorsHandler(ITimeTrackingDbContext db) => _db = db;

    public async Task<IReadOnlyList<ImportJobErrorDto>> Handle(GetImportJobErrorsQuery query, CancellationToken ct)
    {
        return await _db.ImportJobErrors.AsNoTracking()
            .Where(e => e.ImportJobId == query.JobId)
            .OrderBy(e => e.RowIndex)
            .Select(e => new ImportJobErrorDto(e.Id, e.ImportJobId, e.RowIndex, e.ColumnName, e.ErrorType, e.Message))
            .ToListAsync(ct);
    }
}
