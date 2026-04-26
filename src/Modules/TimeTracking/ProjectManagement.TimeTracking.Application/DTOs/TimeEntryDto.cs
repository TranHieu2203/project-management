namespace ProjectManagement.TimeTracking.Application.DTOs;

public sealed record TimeEntryDto(
    Guid Id,
    Guid ResourceId,
    Guid ProjectId,
    Guid? TaskId,
    DateOnly Date,
    decimal Hours,
    string EntryType,
    string? Note,
    decimal RateAtTime,
    decimal CostAtTime,
    string EnteredBy,
    DateTime CreatedAt,
    bool IsVoided,
    string? VoidReason,
    string? VoidedBy,
    DateTime? VoidedAt,
    Guid? SupersedesId
);
