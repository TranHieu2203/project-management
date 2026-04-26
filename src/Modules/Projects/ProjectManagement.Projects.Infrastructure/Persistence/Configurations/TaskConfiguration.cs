using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectManagement.Projects.Domain.Entities;

namespace ProjectManagement.Projects.Infrastructure.Persistence.Configurations;

public sealed class TaskConfiguration : IEntityTypeConfiguration<ProjectTask>
{
    public void Configure(EntityTypeBuilder<ProjectTask> b)
    {
        b.ToTable("project_tasks");

        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.ProjectId).HasColumnName("project_id").IsRequired();
        b.Property(x => x.ParentId).HasColumnName("parent_id");
        b.Property(x => x.Type).HasColumnName("type")
            .HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.Vbs).HasColumnName("vbs").HasMaxLength(50).IsRequired(false);
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(500).IsRequired();
        b.Property(x => x.Priority).HasColumnName("priority")
            .HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.Status).HasColumnName("status")
            .HasConversion<string>().HasMaxLength(30).IsRequired();
        b.Property(x => x.Notes).HasColumnName("notes").HasMaxLength(4000);
        b.Property(x => x.PlannedStartDate).HasColumnName("planned_start_date");
        b.Property(x => x.PlannedEndDate).HasColumnName("planned_end_date");
        b.Property(x => x.ActualStartDate).HasColumnName("actual_start_date");
        b.Property(x => x.ActualEndDate).HasColumnName("actual_end_date");
        b.Property(x => x.PlannedEffortHours).HasColumnName("planned_effort_hours")
            .HasColumnType("numeric(8,2)");
        b.Property(x => x.PercentComplete).HasColumnName("percent_complete")
            .HasColumnType("numeric(5,2)");
        b.Property(x => x.AssigneeUserId).HasColumnName("assignee_user_id");
        b.Property(x => x.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);
        b.Property(x => x.Version).HasColumnName("version").HasDefaultValue(1);
        b.Property(x => x.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.CreatedBy).HasColumnName("created_by").HasMaxLength(450);
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        b.Property(x => x.UpdatedBy).HasColumnName("updated_by").HasMaxLength(450);

        b.HasIndex(x => x.ProjectId).HasDatabaseName("ix_project_tasks_project_id");
        b.HasIndex(x => x.ParentId).HasDatabaseName("ix_project_tasks_parent_id");
        b.HasIndex(x => new { x.ProjectId, x.SortOrder })
            .HasDatabaseName("ix_project_tasks_project_sort");

        // Soft-delete filter — query filter loại bỏ tasks đã xóa
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}
