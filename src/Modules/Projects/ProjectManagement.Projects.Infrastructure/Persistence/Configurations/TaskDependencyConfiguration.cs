using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectManagement.Projects.Domain.Entities;

namespace ProjectManagement.Projects.Infrastructure.Persistence.Configurations;

public sealed class TaskDependencyConfiguration : IEntityTypeConfiguration<TaskDependency>
{
    public void Configure(EntityTypeBuilder<TaskDependency> b)
    {
        b.ToTable("task_dependencies");

        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.TaskId).HasColumnName("task_id").IsRequired();
        b.Property(x => x.PredecessorId).HasColumnName("predecessor_id").IsRequired();
        b.Property(x => x.DependencyType).HasColumnName("dependency_type")
            .HasConversion<string>().HasMaxLength(5).IsRequired();
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        // TaskDependency không có CreatedBy logic phức tạp — bỏ qua mapping phụ
        b.Ignore(x => x.CreatedBy);

        // Unique: không cho thêm cùng predecessor 2 lần cho cùng task
        b.HasIndex(x => new { x.TaskId, x.PredecessorId })
            .IsUnique()
            .HasDatabaseName("uq_task_dependencies_task_predecessor");

        b.HasIndex(x => x.TaskId).HasDatabaseName("ix_task_dependencies_task_id");
        b.HasIndex(x => x.PredecessorId).HasDatabaseName("ix_task_dependencies_predecessor_id");

        // FK: task_id → project_tasks.id (task là "successor")
        b.HasOne<ProjectTask>()
            .WithMany(t => t.Predecessors)
            .HasForeignKey(d => d.TaskId)
            .OnDelete(DeleteBehavior.Restrict);

        // FK: predecessor_id → project_tasks.id (predecessor là "predecessor")
        b.HasOne<ProjectTask>()
            .WithMany(t => t.Successors)
            .HasForeignKey(d => d.PredecessorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
