namespace ProjectManagement.Workforce.Application.DTOs;

public sealed record MonthlyRateDto(
    Guid Id,
    Guid VendorId,
    string? VendorName,
    string Role,
    string Level,
    int Year,
    int Month,
    decimal MonthlyAmount,
    decimal HourlyRate,
    DateTime CreatedAt,
    string CreatedBy
);
