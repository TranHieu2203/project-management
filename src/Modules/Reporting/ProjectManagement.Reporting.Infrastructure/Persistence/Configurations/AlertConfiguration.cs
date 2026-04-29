using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectManagement.Reporting.Domain.Entities;

namespace ProjectManagement.Reporting.Infrastructure.Persistence.Configurations;

public sealed class AlertConfiguration : IEntityTypeConfiguration<Alert>
{
    public void Configure(EntityTypeBuilder<Alert> b)
    {
        b.ToTable("alerts");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.ProjectId).HasColumnName("project_id");
        b.Property(x => x.UserId).HasColumnName("user_id");
        b.Property(x => x.Type).HasColumnName("type").HasMaxLength(50).IsRequired();
        b.Property(x => x.EntityType).HasColumnName("entity_type").HasMaxLength(50);
        b.Property(x => x.EntityId).HasColumnName("entity_id");
        b.Property(x => x.Title).HasColumnName("title").HasMaxLength(500).IsRequired();
        b.Property(x => x.Description).HasColumnName("description").HasColumnType("text");
        b.Property(x => x.IsRead).HasColumnName("is_read");
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.ReadAt).HasColumnName("read_at");

        b.HasIndex(x => new { x.UserId, x.IsRead, x.CreatedAt })
            .HasDatabaseName("ix_alerts_user_read")
            .IsDescending(false, false, true);
    }
}
