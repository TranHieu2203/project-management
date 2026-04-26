namespace ProjectManagement.TimeTracking.Application.ImportJobs.DTOs;

public sealed record ImportJobDto(
    Guid Id,
    Guid VendorId,
    string FileName,
    string FileHash,
    string Status,
    int TotalRows,
    int ErrorCount,
    string EnteredBy,
    DateTime CreatedAt,
    DateTime? CompletedAt
);

public sealed record ImportJobErrorDto(
    Guid Id,
    Guid ImportJobId,
    int RowIndex,
    string? ColumnName,
    string ErrorType,
    string Message
);
