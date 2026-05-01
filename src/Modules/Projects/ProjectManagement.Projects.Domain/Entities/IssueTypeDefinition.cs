using ProjectManagement.Shared.Domain.Entities;

namespace ProjectManagement.Projects.Domain.Entities;

public class IssueTypeDefinition : AuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string IconKey { get; private set; } = string.Empty;
    public string Color { get; private set; } = string.Empty;
    public bool IsBuiltIn { get; private set; }
    public bool IsDeletable { get; private set; }
    public Guid? ProjectId { get; private set; }
    public int SortOrder { get; private set; }

    public static IssueTypeDefinition CreateBuiltIn(
        Guid id, string name, string iconKey, string color, int sortOrder) => new()
    {
        Id = id,
        Name = name,
        IconKey = iconKey,
        Color = color,
        IsBuiltIn = true,
        IsDeletable = false,
        ProjectId = null,
        SortOrder = sortOrder,
        CreatedAt = DateTime.UtcNow,
        CreatedBy = "system",
    };

    public static IssueTypeDefinition CreateCustom(
        Guid projectId,
        string name,
        string iconKey,
        string color,
        int sortOrder,
        string createdBy) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        IconKey = iconKey,
        Color = color,
        IsBuiltIn = false,
        IsDeletable = true,
        ProjectId = projectId,
        SortOrder = sortOrder,
        CreatedAt = DateTime.UtcNow,
        CreatedBy = createdBy,
    };

    public void Update(string name, string iconKey, string color, int sortOrder, string updatedBy)
    {
        Name = name;
        IconKey = iconKey;
        Color = color;
        SortOrder = sortOrder;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void SoftDelete(string updatedBy)
    {
        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }
}
