using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectManagement.Workforce.Domain.Entities;

namespace ProjectManagement.Workforce.Infrastructure.Persistence.Configurations;

public sealed class ResourceConfiguration : IEntityTypeConfiguration<Resource>
{
    public void Configure(EntityTypeBuilder<Resource> b)
    {
        b.ToTable("resources");

        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        b.Property(x => x.Email).HasColumnName("email").HasMaxLength(256).IsRequired(false);
        b.Property(x => x.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.VendorId).HasColumnName("vendor_id").IsRequired(false);
        b.Property(x => x.IsActive).HasColumnName("is_active");
        b.Property(x => x.Version).HasColumnName("version");
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.CreatedBy).HasColumnName("created_by").HasMaxLength(256);
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        b.Property(x => x.UpdatedBy).HasColumnName("updated_by").HasMaxLength(256);
        b.Property(x => x.IsDeleted).HasColumnName("is_deleted");

        b.HasIndex(x => x.Code).IsUnique().HasDatabaseName("uq_resources_code");

        b.HasOne(x => x.Vendor)
         .WithMany()
         .HasForeignKey(x => x.VendorId)
         .IsRequired(false)
         .OnDelete(DeleteBehavior.Restrict);
    }
}
