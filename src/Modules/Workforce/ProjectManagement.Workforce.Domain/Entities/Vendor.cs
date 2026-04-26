using ProjectManagement.Shared.Domain.Entities;

namespace ProjectManagement.Workforce.Domain.Entities;

public class Vendor : AuditableEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public int Version { get; private set; }

    public static Vendor Create(string code, string name, string? description, string createdBy)
        => new()
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            Description = description,
            IsActive = true,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };

    public void Update(string name, string? description, string updatedBy)
    {
        Name = name;
        Description = description;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
        Version++;
    }

    public void Inactivate(string updatedBy)
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
        Version++;
    }

    public void Reactivate(string updatedBy)
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
        Version++;
    }
}
