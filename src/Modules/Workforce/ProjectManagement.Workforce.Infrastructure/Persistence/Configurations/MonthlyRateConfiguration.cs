using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectManagement.Workforce.Domain.Entities;

namespace ProjectManagement.Workforce.Infrastructure.Persistence.Configurations;

public sealed class MonthlyRateConfiguration : IEntityTypeConfiguration<MonthlyRate>
{
    public void Configure(EntityTypeBuilder<MonthlyRate> b)
    {
        b.ToTable("monthly_rates");

        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.VendorId).HasColumnName("vendor_id");
        b.Property(x => x.Role).HasColumnName("role").HasMaxLength(50).IsRequired();
        b.Property(x => x.Level).HasColumnName("level").HasMaxLength(50).IsRequired();
        b.Property(x => x.Year).HasColumnName("year");
        b.Property(x => x.Month).HasColumnName("month");
        b.Property(x => x.MonthlyAmount).HasColumnName("monthly_amount").HasColumnType("decimal(18,2)");
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.CreatedBy).HasColumnName("created_by").HasMaxLength(256);
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        b.Property(x => x.UpdatedBy).HasColumnName("updated_by").HasMaxLength(256);
        b.Property(x => x.IsDeleted).HasColumnName("is_deleted");

        b.Ignore(x => x.HourlyRate);

        b.HasIndex(x => new { x.VendorId, x.Role, x.Level, x.Year, x.Month })
         .IsUnique()
         .HasDatabaseName("uq_monthly_rates_key");

        b.HasOne(x => x.Vendor)
         .WithMany()
         .HasForeignKey(x => x.VendorId)
         .IsRequired()
         .OnDelete(DeleteBehavior.Restrict);
    }
}
