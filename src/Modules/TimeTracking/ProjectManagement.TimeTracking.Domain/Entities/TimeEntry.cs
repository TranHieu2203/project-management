using ProjectManagement.Shared.Domain.Exceptions;

namespace ProjectManagement.TimeTracking.Domain.Entities;

public class TimeEntry
{
    public Guid Id { get; private set; }
    public Guid ResourceId { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid? TaskId { get; private set; }
    public DateOnly Date { get; private set; }
    public decimal Hours { get; private set; }
    public string EntryType { get; private set; } = string.Empty;
    public string? Note { get; private set; }
    public decimal RateAtTime { get; private set; }
    public decimal CostAtTime { get; private set; }
    public string EnteredBy { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    // Void fields — set by Void() only; original hours/rate unchanged
    public bool IsVoided { get; private set; }
    public string? VoidReason { get; private set; }
    public string? VoidedBy { get; private set; }
    public DateTime? VoidedAt { get; private set; }

    // Correction chain — points to the entry this corrects
    public Guid? SupersedesId { get; private set; }

    // Import pipeline tracking — set only when created via vendor import
    public Guid? ImportJobId { get; private set; }
    public string? RowFingerprint { get; private set; }

    public static TimeEntry Create(
        Guid resourceId,
        Guid projectId,
        Guid? taskId,
        DateOnly date,
        decimal hours,
        string entryType,
        string? note,
        decimal rateAtTime,
        string enteredBy,
        Guid? supersededEntryId = null,
        Guid? importJobId = null,
        string? rowFingerprint = null)
        => new()
        {
            Id = Guid.NewGuid(),
            ResourceId = resourceId,
            ProjectId = projectId,
            TaskId = taskId,
            Date = date,
            Hours = hours,
            EntryType = entryType,
            Note = note,
            RateAtTime = rateAtTime,
            CostAtTime = hours * rateAtTime,
            EnteredBy = enteredBy,
            CreatedAt = DateTime.UtcNow,
            IsVoided = false,
            SupersedesId = supersededEntryId,
            ImportJobId = importJobId,
            RowFingerprint = rowFingerprint,
        };

    public void Void(string reason, string voidedBy)
    {
        if (IsVoided)
            throw new DomainException("Entry đã bị void.");
        IsVoided = true;
        VoidReason = reason;
        VoidedBy = voidedBy;
        VoidedAt = DateTime.UtcNow;
    }
}
