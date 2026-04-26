namespace ProjectManagement.TimeTracking.Domain.Entities;

public class ImportJobError
{
    public Guid Id { get; private set; }
    public Guid ImportJobId { get; private set; }
    public int RowIndex { get; private set; }
    public string? ColumnName { get; private set; }
    public string ErrorType { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;

    public static ImportJobError Create(Guid jobId, int rowIndex, string? columnName, string errorType, string message)
        => new()
        {
            Id = Guid.NewGuid(),
            ImportJobId = jobId,
            RowIndex = rowIndex,
            ColumnName = columnName,
            ErrorType = errorType,
            Message = message,
        };
}
