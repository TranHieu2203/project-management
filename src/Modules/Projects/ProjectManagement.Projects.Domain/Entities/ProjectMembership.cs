using ProjectManagement.Projects.Domain.Enums;

namespace ProjectManagement.Projects.Domain.Entities;

public class ProjectMembership
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid UserId { get; private set; }
    public ProjectMemberRole Role { get; private set; }
    public DateTime JoinedAt { get; private set; }

    public Project Project { get; private set; } = null!;

    public static ProjectMembership Create(Guid projectId, Guid userId, ProjectMemberRole role)
        => new()
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            UserId = userId,
            Role = role,
            JoinedAt = DateTime.UtcNow
        };
}
