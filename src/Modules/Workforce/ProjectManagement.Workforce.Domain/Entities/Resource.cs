using ProjectManagement.Shared.Domain.Entities;
using ProjectManagement.Workforce.Domain.Enums;

namespace ProjectManagement.Workforce.Domain.Entities;

public class Resource : AuditableEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public ResourceType Type { get; private set; }
    public Guid? VendorId { get; private set; }
    public bool IsActive { get; private set; }
    public int Version { get; private set; }

    public Vendor? Vendor { get; private set; }

    public static Resource Create(
        string code, string name, string? email,
        ResourceType type, Guid? vendorId, string createdBy)
        => new()
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            Email = email,
            Type = type,
            VendorId = vendorId,
            IsActive = true,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };

    public void Update(string name, string? email, string updatedBy)
    {
        Name = name;
        Email = email;
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
