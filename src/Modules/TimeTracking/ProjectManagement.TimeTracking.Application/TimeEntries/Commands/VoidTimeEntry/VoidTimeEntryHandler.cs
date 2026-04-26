using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.Domain.Exceptions;
using ProjectManagement.TimeTracking.Application.Common.Interfaces;
using ProjectManagement.TimeTracking.Application.DTOs;
using ProjectManagement.TimeTracking.Application.TimeEntries.Commands.CreateTimeEntry;

namespace ProjectManagement.TimeTracking.Application.TimeEntries.Commands.VoidTimeEntry;

public sealed class VoidTimeEntryHandler : IRequestHandler<VoidTimeEntryCommand, TimeEntryDto>
{
    private readonly ITimeTrackingDbContext _db;

    public VoidTimeEntryHandler(ITimeTrackingDbContext db) => _db = db;

    public async Task<TimeEntryDto> Handle(VoidTimeEntryCommand cmd, CancellationToken ct)
    {
        var entry = await _db.TimeEntries.FindAsync([cmd.EntryId], ct)
            ?? throw new NotFoundException($"TimeEntry {cmd.EntryId} không tồn tại.");

        if (entry.EntryType == "VendorConfirmed")
        {
            var locked = await _db.PeriodLocks.AsNoTracking()
                .AnyAsync(p => p.Year == entry.Date.Year && p.Month == entry.Date.Month, ct);
            if (locked)
                throw new DomainException($"Kỳ {entry.Date.Year}/{entry.Date.Month:D2} đã bị lock. Chỉ được điều chỉnh qua correction.");
        }

        entry.Void(cmd.Reason, cmd.VoidedBy);
        await _db.SaveChangesAsync(ct);

        return CreateTimeEntryHandler.ToDto(entry);
    }
}
