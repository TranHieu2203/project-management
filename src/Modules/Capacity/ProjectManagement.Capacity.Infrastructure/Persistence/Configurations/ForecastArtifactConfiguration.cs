using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectManagement.Capacity.Domain.Entities;

namespace ProjectManagement.Capacity.Infrastructure.Persistence.Configurations;

public sealed class ForecastArtifactConfiguration : IEntityTypeConfiguration<ForecastArtifact>
{
    public void Configure(EntityTypeBuilder<ForecastArtifact> builder)
    {
        builder.ToTable("forecast_artifacts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Version).HasColumnName("version");
        builder.Property(x => x.ComputedAt).HasColumnName("computed_at");
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(32);
        builder.Property(x => x.Payload).HasColumnName("payload");
        builder.Property(x => x.ErrorMessage).HasColumnName("error_message").HasMaxLength(1024);
        builder.HasIndex(x => x.Version).HasDatabaseName("ix_forecast_artifacts_version");
    }
}
