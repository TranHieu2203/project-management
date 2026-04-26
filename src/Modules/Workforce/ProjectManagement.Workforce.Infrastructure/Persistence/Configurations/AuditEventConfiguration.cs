using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectManagement.Workforce.Domain.Entities;

namespace ProjectManagement.Workforce.Infrastructure.Persistence.Configurations;

public sealed class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> b)
    {
        b.ToTable("audit_events");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.EntityType).HasColumnName("entity_type").HasMaxLength(50).IsRequired();
        b.Property(x => x.EntityId).HasColumnName("entity_id");
        b.Property(x => x.Action).HasColumnName("action").HasMaxLength(50).IsRequired();
        b.Property(x => x.Actor).HasColumnName("actor").HasMaxLength(256).IsRequired();
        b.Property(x => x.Summary).HasColumnName("summary").HasMaxLength(1000);
        b.Property(x => x.CreatedAt).HasColumnName("created_at");

        b.HasIndex(x => new { x.EntityType, x.EntityId }).HasDatabaseName("ix_audit_events_entity");
        b.HasIndex(x => x.CreatedAt).HasDatabaseName("ix_audit_events_created_at");
    }
}
