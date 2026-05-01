using ProjectManagement.Shared.Domain.Entities;

namespace ProjectManagement.Projects.Domain.Entities;

public sealed class ProjectIssueTypeSetting : AuditableEntity
{
    public Guid ProjectId { get; private set; }
    public Guid IssueTypeId { get; private set; }
    public bool IsEnabled { get; private set; }

    public static ProjectIssueTypeSetting Create(Guid projectId, Guid issueTypeId, bool isEnabled, string createdBy)
        => new()
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            IssueTypeId = issueTypeId,
            IsEnabled = isEnabled,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy,
        };

    public void SetEnabled(bool isEnabled, string updatedBy)
    {
        IsEnabled = isEnabled;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }
}

