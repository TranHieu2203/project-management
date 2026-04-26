using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectManagement.Reporting.Domain.Entities;

namespace ProjectManagement.Reporting.Infrastructure.Persistence.Configurations;

public sealed class ExportJobConfiguration : IEntityTypeConfiguration<ExportJob>
{
    public void Configure(EntityTypeBuilder<ExportJob> b)
    {
        b.ToTable("export_jobs");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.TriggeredBy).HasColumnName("triggered_by");
        b.Property(x => x.Format).HasColumnName("format").HasMaxLength(10).IsRequired();
        b.Property(x => x.GroupBy).HasColumnName("group_by").HasMaxLength(20).IsRequired();
        b.Property(x => x.FilterParams).HasColumnName("filter_params").HasColumnType("text").IsRequired();
        b.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
        b.Property(x => x.FileName).HasColumnName("file_name").HasMaxLength(500);
        b.Property(x => x.FileContent).HasColumnName("file_content").HasColumnType("bytea");
        b.Property(x => x.ErrorMessage).HasColumnName("error_message").HasMaxLength(2000);
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.CompletedAt).HasColumnName("completed_at");

        b.HasIndex(x => x.TriggeredBy).HasDatabaseName("ix_export_jobs_triggered_by");
        b.HasIndex(x => x.Status).HasDatabaseName("ix_export_jobs_status");
    }
}
