using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectManagement.TimeTracking.Domain.Entities;

namespace ProjectManagement.TimeTracking.Infrastructure.Persistence.Configurations;

public sealed class ImportJobErrorConfiguration : IEntityTypeConfiguration<ImportJobError>
{
    public void Configure(EntityTypeBuilder<ImportJobError> b)
    {
        b.ToTable("import_job_errors");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.ImportJobId).HasColumnName("import_job_id");
        b.Property(x => x.RowIndex).HasColumnName("row_index");
        b.Property(x => x.ColumnName).HasColumnName("column_name").HasMaxLength(100);
        b.Property(x => x.ErrorType).HasColumnName("error_type").HasMaxLength(20).IsRequired();
        b.Property(x => x.Message).HasColumnName("message").HasMaxLength(1000).IsRequired();

        b.HasIndex(x => x.ImportJobId).HasDatabaseName("ix_import_job_errors_job_id");
    }
}
