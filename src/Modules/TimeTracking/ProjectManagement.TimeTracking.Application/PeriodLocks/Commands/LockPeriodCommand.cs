using MediatR;
using ProjectManagement.TimeTracking.Application.PeriodLocks.DTOs;

namespace ProjectManagement.TimeTracking.Application.PeriodLocks.Commands;

public sealed record LockPeriodCommand(Guid VendorId, int Year, int Month, string LockedBy) : IRequest<PeriodLockDto>;
