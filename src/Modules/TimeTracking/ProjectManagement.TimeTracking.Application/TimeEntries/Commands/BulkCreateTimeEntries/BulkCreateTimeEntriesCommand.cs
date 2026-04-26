using MediatR;
using ProjectManagement.TimeTracking.Application.DTOs;

namespace ProjectManagement.TimeTracking.Application.TimeEntries.Commands.BulkCreateTimeEntries;

public sealed record BulkTimesheetRowDto(
    Guid ResourceId,
    Guid ProjectId,
    Guid? TaskId,
    DateOnly Date,
    decimal Hours,
    string EntryType,
    string Role,
    string Level,
    string? Note
);

public sealed record BulkValidationError(
    int RowIndex,
    string ErrorType,
    string Message
);

public sealed record BulkCreateResult(
    bool Success,
    IReadOnlyList<TimeEntryDto> CreatedEntries,
    IReadOnlyList<BulkValidationError> Errors
);

public sealed record BulkCreateTimeEntriesCommand(
    IReadOnlyList<BulkTimesheetRowDto> Rows,
    string EnteredBy
) : IRequest<BulkCreateResult>;
