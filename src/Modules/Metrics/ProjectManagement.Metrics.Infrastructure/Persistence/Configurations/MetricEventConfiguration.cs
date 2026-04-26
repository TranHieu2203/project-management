using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectManagement.Metrics.Domain.Entities;

namespace ProjectManagement.Metrics.Infrastructure.Persistence.Configurations;

public class MetricEventConfiguration : IEntityTypeConfiguration<MetricEvent>
{
    public void Configure(EntityTypeBuilder<MetricEvent> b)
    {
        b.ToTable("metric_events");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.EventType).HasColumnName("event_type").HasMaxLength(50).IsRequired();
        b.Property(x => x.ActorId).HasColumnName("actor_id").IsRequired();
        b.Property(x => x.ContextJson).HasColumnName("context_json");
        b.Property(x => x.CorrelationId).HasColumnName("correlation_id").HasMaxLength(100);
        b.Property(x => x.OccurredAt).HasColumnName("occurred_at").IsRequired();

        b.HasIndex(x => new { x.EventType, x.OccurredAt })
            .HasDatabaseName("ix_metric_events_type_occurred");
    }
}
