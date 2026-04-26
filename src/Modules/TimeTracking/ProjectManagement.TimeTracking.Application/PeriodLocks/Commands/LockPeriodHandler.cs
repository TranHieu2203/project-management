using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.TimeTracking.Application.Common.Interfaces;
using ProjectManagement.TimeTracking.Application.PeriodLocks.DTOs;
using ProjectManagement.TimeTracking.Domain.Entities;

namespace ProjectManagement.TimeTracking.Application.PeriodLocks.Commands;

public sealed class LockPeriodHandler : IRequestHandler<LockPeriodCommand, PeriodLockDto>
{
    private readonly ITimeTrackingDbContext _db;
    public LockPeriodHandler(ITimeTrackingDbContext db) => _db = db;

    public async Task<PeriodLockDto> Handle(LockPeriodCommand cmd, CancellationToken ct)
    {
        var existing = await _db.PeriodLocks
            .FirstOrDefaultAsync(p => p.VendorId == cmd.VendorId && p.Year == cmd.Year && p.Month == cmd.Month, ct);

        if (existing != null)
            return ToDto(existing);

        var newLock = PeriodLock.Create(cmd.VendorId, cmd.Year, cmd.Month, cmd.LockedBy);
        _db.PeriodLocks.Add(newLock);
        await _db.SaveChangesAsync(ct);
        return ToDto(newLock);
    }

    internal static PeriodLockDto ToDto(PeriodLock p)
        => new(p.Id, p.VendorId, p.Year, p.Month, p.LockedBy, p.LockedAt);
}
