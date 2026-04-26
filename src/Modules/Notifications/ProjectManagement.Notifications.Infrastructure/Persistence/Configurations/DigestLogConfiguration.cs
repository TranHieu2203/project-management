using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectManagement.Notifications.Domain.Entities;

namespace ProjectManagement.Notifications.Infrastructure.Persistence.Configurations;

public class DigestLogConfiguration : IEntityTypeConfiguration<DigestLog>
{
    public void Configure(EntityTypeBuilder<DigestLog> b)
    {
        b.ToTable("digest_logs");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.UserId).HasColumnName("user_id");
        b.Property(x => x.DigestType).HasColumnName("digest_type").HasMaxLength(30).IsRequired();
        b.Property(x => x.IsoWeek).HasColumnName("iso_week");
        b.Property(x => x.Year).HasColumnName("year");
        b.Property(x => x.SentAt).HasColumnName("sent_at");

        b.HasIndex(x => new { x.UserId, x.DigestType, x.IsoWeek, x.Year })
            .HasDatabaseName("ix_digest_logs_user_type_week")
            .IsUnique();
    }
}
