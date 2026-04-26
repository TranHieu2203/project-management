namespace ProjectManagement.Workforce.Application.DTOs;

public sealed record VendorDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    bool IsActive,
    int Version,
    DateTime CreatedAt,
    string CreatedBy,
    DateTime? UpdatedAt,
    string? UpdatedBy
);
