using ProjectManagement.Projects.Domain.Enums;
using ProjectManagement.Shared.Domain.Entities;

namespace ProjectManagement.Projects.Domain.Entities;

public class Project : AuditableEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public ProjectStatus Status { get; private set; }
    public string Visibility { get; private set; } = "MembersOnly";
    public int Version { get; private set; }

    public ICollection<ProjectMembership> Members { get; private set; } = new List<ProjectMembership>();

    public static Project Create(string code, string name, string? description, string createdBy)
        => new()
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            Description = description,
            Status = ProjectStatus.Planning,
            Visibility = "MembersOnly",
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

    public void Archive(string updatedBy)
    {
        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
        Version++;
    }
}
