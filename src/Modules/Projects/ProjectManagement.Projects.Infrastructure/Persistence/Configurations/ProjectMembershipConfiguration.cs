using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectManagement.Projects.Domain.Entities;

namespace ProjectManagement.Projects.Infrastructure.Persistence.Configurations;

public sealed class ProjectMembershipConfiguration : IEntityTypeConfiguration<ProjectMembership>
{
    public void Configure(EntityTypeBuilder<ProjectMembership> b)
    {
        b.ToTable("project_memberships");

        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.ProjectId).HasColumnName("project_id");
        b.Property(x => x.UserId).HasColumnName("user_id");
        b.Property(x => x.Role).HasColumnName("role").HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.JoinedAt).HasColumnName("joined_at");

        // 1 user = 1 membership record per project
        b.HasIndex(x => new { x.ProjectId, x.UserId })
         .IsUnique()
         .HasDatabaseName("uq_project_memberships_project_user");
    }
}
