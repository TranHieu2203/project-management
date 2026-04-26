namespace ProjectManagement.TimeTracking.Domain.Entities;

public class PeriodLock
{
    public Guid Id { get; private set; }
    public Guid VendorId { get; private set; }
    public int Year { get; private set; }
    public int Month { get; private set; }
    public string LockedBy { get; private set; } = string.Empty;
    public DateTime LockedAt { get; private set; }

    public static PeriodLock Create(Guid vendorId, int year, int month, string lockedBy)
        => new()
        {
            Id = Guid.NewGuid(),
            VendorId = vendorId,
            Year = year,
            Month = month,
            LockedBy = lockedBy,
            LockedAt = DateTime.UtcNow,
        };
}
