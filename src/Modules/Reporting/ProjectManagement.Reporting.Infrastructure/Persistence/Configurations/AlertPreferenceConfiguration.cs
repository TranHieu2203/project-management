using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectManagement.Reporting.Domain.Entities;

namespace ProjectManagement.Reporting.Infrastructure.Persistence.Configurations;

public sealed class AlertPreferenceConfiguration : IEntityTypeConfiguration<AlertPreference>
{
    public void Configure(EntityTypeBuilder<AlertPreference> b)
    {
        b.ToTable("alert_preferences");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.UserId).HasColumnName("user_id");
        b.Property(x => x.AlertType).HasColumnName("alert_type").HasMaxLength(50).IsRequired();
        b.Property(x => x.Enabled).HasColumnName("enabled");
        b.Property(x => x.ThresholdDays).HasColumnName("threshold_days");

        b.HasIndex(x => new { x.UserId, x.AlertType })
            .HasDatabaseName("ix_alert_preferences_user_type")
            .IsUnique();
    }
}
