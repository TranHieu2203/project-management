using Microsoft.EntityFrameworkCore;
using ProjectManagement.Projects.Domain.Entities;
using ProjectManagement.Projects.Domain.Enums;
using ProjectManagement.Projects.Infrastructure.Persistence;

namespace ProjectManagement.Projects.Infrastructure.Seeding;

/// <summary>
/// Seeds a demo project with tasks and dependencies for Epic 1 staging demo.
/// Program.cs resolves user IDs after auth seeder runs and passes them here.
/// </summary>
public sealed class ProjectsSeeder
{
    private readonly ProjectsDbContext _db;

    public ProjectsSeeder(ProjectsDbContext db) => _db = db;

    public async Task SeedAsync(Guid pm1UserId, Guid pm2UserId, CancellationToken ct)
    {
        if (pm1UserId == Guid.Empty)
            return;

        var alreadySeeded = await _db.Projects.AnyAsync(ct);
        if (alreadySeeded)
            return;

        // 1. Create demo project
        var project = Project.Create("DEMO-01", "Dự Án Demo Sprint 1",
            "Demo project cho Epic 1 stakeholder review", "system");
        _db.Projects.Add(project);
        await _db.SaveChangesAsync(ct);

        // 2. Memberships
        _db.ProjectMemberships.Add(
            ProjectMembership.Create(project.Id, pm1UserId, ProjectMemberRole.Manager));
        if (pm2UserId != Guid.Empty)
            _db.ProjectMemberships.Add(
                ProjectMembership.Create(project.Id, pm2UserId, ProjectMemberRole.Manager));
        await _db.SaveChangesAsync(ct);

        // 3. Phase 1 — Khởi động (tháng 5/2026)
        var phase1 = ProjectTask.Create(project.Id, null, TaskType.Phase,
            "1", "Khởi động",
            TaskPriority.High, ProjectTaskStatus.InProgress,
            null,
            new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 30),
            null, null, null, 30m, null, 1, "system");
        _db.ProjectTasks.Add(phase1);

        var t11 = ProjectTask.Create(project.Id, phase1.Id, TaskType.Task,
            "1.1", "Thu thập yêu cầu",
            TaskPriority.High, ProjectTaskStatus.Completed,
            null,
            new DateOnly(2026, 5, 5), new DateOnly(2026, 5, 16),
            null, null, 40m, 100m, null, 2, "system");
        _db.ProjectTasks.Add(t11);

        var t12 = ProjectTask.Create(project.Id, phase1.Id, TaskType.Task,
            "1.2", "Phân tích nghiệp vụ",
            TaskPriority.High, ProjectTaskStatus.InProgress,
            null,
            new DateOnly(2026, 5, 19), new DateOnly(2026, 5, 30),
            null, null, 40m, 60m, null, 3, "system");
        _db.ProjectTasks.Add(t12);

        var ms1 = ProjectTask.Create(project.Id, phase1.Id, TaskType.Milestone,
            "1.M", "Kick-off Approved",
            TaskPriority.Critical, ProjectTaskStatus.Completed,
            null,
            new DateOnly(2026, 5, 16), new DateOnly(2026, 5, 16),
            null, null, null, 100m, null, 4, "system");
        _db.ProjectTasks.Add(ms1);

        // 4. Phase 2 — Phát triển (tháng 6/2026)
        var phase2 = ProjectTask.Create(project.Id, null, TaskType.Phase,
            "2", "Phát triển",
            TaskPriority.High, ProjectTaskStatus.NotStarted,
            null,
            new DateOnly(2026, 6, 2), new DateOnly(2026, 6, 30),
            null, null, null, 0m, null, 5, "system");
        _db.ProjectTasks.Add(phase2);

        var t21 = ProjectTask.Create(project.Id, phase2.Id, TaskType.Task,
            "2.1", "Thiết kế database",
            TaskPriority.High, ProjectTaskStatus.NotStarted,
            null,
            new DateOnly(2026, 6, 2), new DateOnly(2026, 6, 13),
            null, null, 40m, 0m, null, 6, "system");
        _db.ProjectTasks.Add(t21);

        var t22 = ProjectTask.Create(project.Id, phase2.Id, TaskType.Task,
            "2.2", "Xây dựng API Backend",
            TaskPriority.High, ProjectTaskStatus.NotStarted,
            null,
            new DateOnly(2026, 6, 16), new DateOnly(2026, 6, 30),
            null, null, 80m, 0m, null, 7, "system");
        _db.ProjectTasks.Add(t22);

        var t23 = ProjectTask.Create(project.Id, phase2.Id, TaskType.Task,
            "2.3", "Xây dựng UI Frontend",
            TaskPriority.Medium, ProjectTaskStatus.NotStarted,
            null,
            new DateOnly(2026, 6, 16), new DateOnly(2026, 6, 30),
            null, null, 80m, 0m, null, 8, "system");
        _db.ProjectTasks.Add(t23);

        var ms2 = ProjectTask.Create(project.Id, phase2.Id, TaskType.Milestone,
            "2.M", "MVP Release v1.0",
            TaskPriority.Critical, ProjectTaskStatus.NotStarted,
            null,
            new DateOnly(2026, 6, 30), new DateOnly(2026, 6, 30),
            null, null, null, 0m, null, 9, "system");
        _db.ProjectTasks.Add(ms2);

        await _db.SaveChangesAsync(ct);

        // 5. Dependencies (FS: 1.2→1.1, 2.1→1.2, 2.2→2.1; SS: 2.3→2.2)
        _db.TaskDependencies.Add(TaskDependency.Create(t12.Id, t11.Id, DependencyType.FS));
        _db.TaskDependencies.Add(TaskDependency.Create(t21.Id, t12.Id, DependencyType.FS));
        _db.TaskDependencies.Add(TaskDependency.Create(t22.Id, t21.Id, DependencyType.FS));
        _db.TaskDependencies.Add(TaskDependency.Create(t23.Id, t22.Id, DependencyType.SS));
        await _db.SaveChangesAsync(ct);
    }
}
