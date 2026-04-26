namespace ProjectManagement.Reporting.Domain.Entities;

public class ExportJob
{
    public Guid Id { get; private set; }
    public Guid TriggeredBy { get; private set; }
    public string Format { get; private set; } = string.Empty;
    public string GroupBy { get; private set; } = string.Empty;
    public string FilterParams { get; private set; } = string.Empty;
    public string Status { get; private set; } = string.Empty;
    public string? FileName { get; private set; }
    public byte[]? FileContent { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    public static ExportJob Create(Guid triggeredBy, string format, string groupBy, string filterParams)
        => new()
        {
            Id = Guid.NewGuid(),
            TriggeredBy = triggeredBy,
            Format = format,
            GroupBy = groupBy,
            FilterParams = filterParams,
            Status = ExportJobStatus.Queued,
            CreatedAt = DateTime.UtcNow,
        };

    public void MarkProcessing() => Status = ExportJobStatus.Processing;

    public void MarkReady(string fileName, byte[] content)
    {
        Status = ExportJobStatus.Ready;
        FileName = fileName;
        FileContent = content;
        CompletedAt = DateTime.UtcNow;
    }

    public void MarkFailed(string errorMessage)
    {
        Status = ExportJobStatus.Failed;
        ErrorMessage = errorMessage;
        CompletedAt = DateTime.UtcNow;
    }
}
