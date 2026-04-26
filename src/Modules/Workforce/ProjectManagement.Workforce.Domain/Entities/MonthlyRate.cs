using ProjectManagement.Shared.Domain.Entities;

namespace ProjectManagement.Workforce.Domain.Entities;

public class MonthlyRate : AuditableEntity
{
    public Guid VendorId { get; private set; }
    public string Role { get; private set; } = string.Empty;
    public string Level { get; private set; } = string.Empty;
    public int Year { get; private set; }
    public int Month { get; private set; }
    public decimal MonthlyAmount { get; private set; }

    public Vendor? Vendor { get; private set; }

    public decimal HourlyRate => MonthlyAmount / 176m;

    public static MonthlyRate Create(
        Guid vendorId, string role, string level,
        int year, int month, decimal monthlyAmount,
        string createdBy)
        => new()
        {
            Id = Guid.NewGuid(),
            VendorId = vendorId,
            Role = role,
            Level = level,
            Year = year,
            Month = month,
            MonthlyAmount = monthlyAmount,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
}
