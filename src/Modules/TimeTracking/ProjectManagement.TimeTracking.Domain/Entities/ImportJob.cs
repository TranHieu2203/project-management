using ProjectManagement.TimeTracking.Domain.Enums;

namespace ProjectManagement.TimeTracking.Domain.Entities;

public class ImportJob
{
    public Guid Id { get; private set; }
    public Guid VendorId { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string FileHash { get; private set; } = string.Empty;
    public string? RawContent { get; private set; }
    public ImportJobStatus Status { get; private set; }
    public int TotalRows { get; private set; }
    public int ErrorCount { get; private set; }
    public string EnteredBy { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    public static ImportJob Create(Guid vendorId, string fileName, string fileHash, string rawContent, string enteredBy)
        => new()
        {
            Id = Guid.NewGuid(),
            VendorId = vendorId,
            FileName = fileName,
            FileHash = fileHash,
            RawContent = rawContent,
            Status = ImportJobStatus.Pending,
            EnteredBy = enteredBy,
            CreatedAt = DateTime.UtcNow,
        };

    public void SetValidationResult(ImportJobStatus status, int totalRows, int errorCount)
    {
        Status = status;
        TotalRows = totalRows;
        ErrorCount = errorCount;
    }

    public void MarkApplying() => Status = ImportJobStatus.Applying;

    public void MarkCompleted()
    {
        Status = ImportJobStatus.Completed;
        RawContent = null;
        CompletedAt = DateTime.UtcNow;
    }

    public void MarkFailed()
    {
        Status = ImportJobStatus.Failed;
        CompletedAt = DateTime.UtcNow;
    }
}
