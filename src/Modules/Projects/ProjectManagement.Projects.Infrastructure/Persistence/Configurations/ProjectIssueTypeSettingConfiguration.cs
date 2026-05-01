using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectManagement.Projects.Domain.Entities;

namespace ProjectManagement.Projects.Infrastructure.Persistence.Configurations;

public sealed class ProjectIssueTypeSettingConfiguration : IEntityTypeConfiguration<ProjectIssueTypeSetting>
{
    public void Configure(EntityTypeBuilder<ProjectIssueTypeSetting> b)
    {
        b.ToTable("project_issue_type_settings");

        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");

        b.Property(x => x.ProjectId).HasColumnName("project_id");
        b.Property(x => x.IssueTypeId).HasColumnName("issue_type_id");
        b.Property(x => x.IsEnabled).HasColumnName("is_enabled").HasDefaultValue(true);

        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.CreatedBy).HasColumnName("created_by").HasMaxLength(450);
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        b.Property(x => x.UpdatedBy).HasColumnName("updated_by").HasMaxLength(450);
        b.Property(x => x.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);

        b.HasIndex(x => new { x.ProjectId, x.IssueTypeId })
            .IsUnique()
            .HasDatabaseName("uq_project_issue_type_settings_project_type");

        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

