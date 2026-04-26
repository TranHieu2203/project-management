using Microsoft.EntityFrameworkCore;
using ProjectManagement.Projects.Domain.Entities;

namespace ProjectManagement.Projects.Application.Common.Interfaces;

public interface IProjectsDbContext
{
    DbSet<Project> Projects { get; }
    DbSet<ProjectMembership> ProjectMemberships { get; }
    DbSet<ProjectTask> ProjectTasks { get; }
    DbSet<TaskDependency> TaskDependencies { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
