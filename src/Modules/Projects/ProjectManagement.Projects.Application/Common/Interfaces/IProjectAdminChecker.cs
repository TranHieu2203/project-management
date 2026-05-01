namespace ProjectManagement.Projects.Application.Common.Interfaces;

public interface IProjectAdminChecker
{
    Task EnsureProjectAdminAsync(Guid projectId, Guid userId, CancellationToken ct = default);
}

