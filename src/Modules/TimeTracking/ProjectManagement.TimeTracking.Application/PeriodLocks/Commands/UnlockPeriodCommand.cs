using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.Domain.Exceptions;
using ProjectManagement.TimeTracking.Application.Common.Interfaces;

namespace ProjectManagement.TimeTracking.Application.PeriodLocks.Commands;

public sealed record UnlockPeriodCommand(Guid VendorId, int Year, int Month) : IRequest<Unit>;

public sealed class UnlockPeriodHandler : IRequestHandler<UnlockPeriodCommand, Unit>
{
    private readonly ITimeTrackingDbContext _db;
    public UnlockPeriodHandler(ITimeTrackingDbContext db) => _db = db;

    public async Task<Unit> Handle(UnlockPeriodCommand cmd, CancellationToken ct)
    {
        var existing = await _db.PeriodLocks
            .FirstOrDefaultAsync(p => p.VendorId == cmd.VendorId && p.Year == cmd.Year && p.Month == cmd.Month, ct)
            ?? throw new NotFoundException($"PeriodLock {cmd.VendorId}/{cmd.Year}/{cmd.Month} không tồn tại.");

        _db.PeriodLocks.Remove(existing);
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
