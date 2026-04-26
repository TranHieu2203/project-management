using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.Domain.Exceptions;
using ProjectManagement.TimeTracking.Application.Common.Interfaces;
using ProjectManagement.TimeTracking.Application.DTOs;
using ProjectManagement.TimeTracking.Application.TimeEntries.Commands.CreateTimeEntry;

namespace ProjectManagement.TimeTracking.Application.TimeEntries.Queries.GetTimeEntryById;

public sealed class GetTimeEntryByIdHandler : IRequestHandler<GetTimeEntryByIdQuery, TimeEntryDto>
{
    private readonly ITimeTrackingDbContext _db;

    public GetTimeEntryByIdHandler(ITimeTrackingDbContext db) => _db = db;

    public async Task<TimeEntryDto> Handle(GetTimeEntryByIdQuery query, CancellationToken ct)
    {
        var entry = await _db.TimeEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == query.EntryId, ct)
            ?? throw new NotFoundException("TimeEntry không tồn tại.");

        return CreateTimeEntryHandler.ToDto(entry);
    }
}
