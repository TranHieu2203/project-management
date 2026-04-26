using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectManagement.TimeTracking.Domain.Entities;

namespace ProjectManagement.TimeTracking.Infrastructure.Persistence.Configurations;

public sealed class ImportJobConfiguration : IEntityTypeConfiguration<ImportJob>
{
    public void Configure(EntityTypeBuilder<ImportJob> b)
    {
        b.ToTable("import_jobs");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.VendorId).HasColumnName("vendor_id");
        b.Property(x => x.FileName).HasColumnName("file_name").HasMaxLength(500).IsRequired();
        b.Property(x => x.FileHash).HasColumnName("file_hash").HasMaxLength(64).IsRequired();
        b.Property(x => x.RawContent).HasColumnName("raw_content");
        b.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30).IsRequired();
        b.Property(x => x.TotalRows).HasColumnName("total_rows");
        b.Property(x => x.ErrorCount).HasColumnName("error_count");
        b.Property(x => x.EnteredBy).HasColumnName("entered_by").HasMaxLength(256).IsRequired();
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.CompletedAt).HasColumnName("completed_at");

        b.HasIndex(x => x.FileHash).HasDatabaseName("ix_import_jobs_file_hash");
        b.HasIndex(x => x.VendorId).HasDatabaseName("ix_import_jobs_vendor_id");
    }
}
