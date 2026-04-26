using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectManagement.Capacity.Domain.Entities;

namespace ProjectManagement.Capacity.Infrastructure.Persistence.Configurations;

public sealed class CapacityOverrideConfiguration : IEntityTypeConfiguration<CapacityOverride>
{
    public void Configure(EntityTypeBuilder<CapacityOverride> builder)
    {
        builder.ToTable("capacity_overrides");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ResourceId).HasColumnName("resource_id");
        builder.Property(x => x.DateFrom).HasColumnName("date_from");
        builder.Property(x => x.DateTo).HasColumnName("date_to");
        builder.Property(x => x.TrafficLight).HasColumnName("traffic_light").HasMaxLength(16);
        builder.Property(x => x.OverriddenBy).HasColumnName("overridden_by").HasMaxLength(256);
        builder.Property(x => x.OverriddenAt).HasColumnName("overridden_at");

        builder.HasIndex(x => x.ResourceId).HasDatabaseName("ix_capacity_overrides_resource_id");
    }
}
