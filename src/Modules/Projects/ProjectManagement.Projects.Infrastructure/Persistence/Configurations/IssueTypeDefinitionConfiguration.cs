using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectManagement.Projects.Domain.Entities;

namespace ProjectManagement.Projects.Infrastructure.Persistence.Configurations;

public sealed class IssueTypeDefinitionConfiguration : IEntityTypeConfiguration<IssueTypeDefinition>
{
    public void Configure(EntityTypeBuilder<IssueTypeDefinition> b)
    {
        b.ToTable("issue_type_definitions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        b.Property(x => x.IconKey).HasColumnName("icon_key").HasMaxLength(50).IsRequired();
        b.Property(x => x.Color).HasColumnName("color").HasMaxLength(7).IsRequired();
        b.Property(x => x.IsBuiltIn).HasColumnName("is_built_in").HasDefaultValue(false);
        b.Property(x => x.IsDeletable).HasColumnName("is_deletable").HasDefaultValue(true);
        b.Property(x => x.ProjectId).HasColumnName("project_id").IsRequired(false);
        b.Property(x => x.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.CreatedBy).HasColumnName("created_by").HasMaxLength(450);
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        b.Property(x => x.UpdatedBy).HasColumnName("updated_by").HasMaxLength(450);
        b.Property(x => x.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}
