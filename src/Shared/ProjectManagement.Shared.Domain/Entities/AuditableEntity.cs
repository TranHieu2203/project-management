namespace ProjectManagement.Shared.Domain.Entities;

public abstract class AuditableEntity : BaseEntity
{
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
}
