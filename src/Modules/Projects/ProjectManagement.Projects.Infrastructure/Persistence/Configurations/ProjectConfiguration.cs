using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectManagement.Projects.Domain.Entities;
using ProjectManagement.Projects.Domain.Enums;

namespace ProjectManagement.Projects.Infrastructure.Persistence.Configurations;

public sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> b)
    {
        b.ToTable("projects");

        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.Code).HasColumnName("code").HasMaxLength(20).IsRequired();
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        b.Property(x => x.Description).HasColumnName("description").HasMaxLength(1000).IsRequired(false);
        b.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Visibility).HasColumnName("visibility").HasMaxLength(20);
        b.Property(x => x.Version).HasColumnName("version");
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.CreatedBy).HasColumnName("created_by").HasMaxLength(256);
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        b.Property(x => x.UpdatedBy).HasColumnName("updated_by").HasMaxLength(256);
        b.Property(x => x.IsDeleted).HasColumnName("is_deleted");

        b.HasIndex(x => x.Code).IsUnique().HasDatabaseName("uq_projects_code");

        b.HasMany(x => x.Members)
         .WithOne(m => m.Project)
         .HasForeignKey(m => m.ProjectId);
    }
}
