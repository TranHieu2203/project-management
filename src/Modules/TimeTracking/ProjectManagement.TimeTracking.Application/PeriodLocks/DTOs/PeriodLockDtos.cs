namespace ProjectManagement.TimeTracking.Application.PeriodLocks.DTOs;

public sealed record PeriodLockDto(Guid Id, Guid VendorId, int Year, int Month, string LockedBy, DateTime LockedAt);

public sealed record PeriodReconcileDto(
    Guid VendorId,
    int Year,
    int Month,
    bool IsLocked,
    DateTime? LockedAt,
    decimal EstimatedHours,
    decimal PmAdjustedHours,
    decimal ConfirmedHours,
    decimal ConfirmedCost,
    int TotalEntries);
