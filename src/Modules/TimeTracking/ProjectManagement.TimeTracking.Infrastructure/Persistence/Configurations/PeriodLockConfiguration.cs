using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectManagement.TimeTracking.Domain.Entities;

namespace ProjectManagement.TimeTracking.Infrastructure.Persistence.Configurations;

public sealed class PeriodLockConfiguration : IEntityTypeConfiguration<PeriodLock>
{
    public void Configure(EntityTypeBuilder<PeriodLock> b)
    {
        b.ToTable("period_locks");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.VendorId).HasColumnName("vendor_id").IsRequired();
        b.Property(x => x.Year).HasColumnName("year").IsRequired();
        b.Property(x => x.Month).HasColumnName("month").IsRequired();
        b.Property(x => x.LockedBy).HasColumnName("locked_by").HasMaxLength(256).IsRequired();
        b.Property(x => x.LockedAt).HasColumnName("locked_at").IsRequired();

        b.HasIndex(x => new { x.VendorId, x.Year, x.Month })
         .HasDatabaseName("ix_period_locks_vendor_year_month")
         .IsUnique();
    }
}
