namespace ProjectManagement.Workforce.Application.DTOs;

public sealed record ResourceDto(
    Guid Id,
    string Code,
    string Name,
    string? Email,
    string Type,
    Guid? VendorId,
    string? VendorName,
    bool IsActive,
    int Version,
    DateTime CreatedAt,
    string CreatedBy,
    DateTime? UpdatedAt,
    string? UpdatedBy
);
