using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectManagement.TimeTracking.Domain.Entities;

namespace ProjectManagement.TimeTracking.Infrastructure.Persistence.Configurations;

public sealed class TimeEntryConfiguration : IEntityTypeConfiguration<TimeEntry>
{
    public void Configure(EntityTypeBuilder<TimeEntry> b)
    {
        b.ToTable("time_entries");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.ResourceId).HasColumnName("resource_id");
        b.Property(x => x.ProjectId).HasColumnName("project_id");
        b.Property(x => x.TaskId).HasColumnName("task_id");
        b.Property(x => x.Date).HasColumnName("date");
        b.Property(x => x.Hours).HasColumnName("hours").HasPrecision(10, 2);
        b.Property(x => x.EntryType).HasColumnName("entry_type").HasMaxLength(30).IsRequired();
        b.Property(x => x.Note).HasColumnName("note").HasMaxLength(1000);
        b.Property(x => x.RateAtTime).HasColumnName("rate_at_time").HasPrecision(18, 4);
        b.Property(x => x.CostAtTime).HasColumnName("cost_at_time").HasPrecision(18, 4);
        b.Property(x => x.EnteredBy).HasColumnName("entered_by").HasMaxLength(256).IsRequired();
        b.Property(x => x.CreatedAt).HasColumnName("created_at");

        b.Property(x => x.IsVoided).HasColumnName("is_voided").HasDefaultValue(false).IsRequired();
        b.Property(x => x.VoidReason).HasColumnName("void_reason").HasMaxLength(500);
        b.Property(x => x.VoidedBy).HasColumnName("voided_by").HasMaxLength(256);
        b.Property(x => x.VoidedAt).HasColumnName("voided_at");
        b.Property(x => x.SupersedesId).HasColumnName("supersedes_id");
        b.Property(x => x.ImportJobId).HasColumnName("import_job_id");
        b.Property(x => x.RowFingerprint).HasColumnName("row_fingerprint").HasMaxLength(64);

        b.HasIndex(x => new { x.ResourceId, x.Date }).HasDatabaseName("ix_time_entries_resource_date");
        b.HasIndex(x => new { x.ProjectId, x.Date }).HasDatabaseName("ix_time_entries_project_date");
        b.HasIndex(x => x.Date).HasDatabaseName("ix_time_entries_date");
        b.HasIndex(x => x.SupersedesId).HasDatabaseName("ix_time_entries_supersedes_id");
        b.HasIndex(x => new { x.ImportJobId, x.RowFingerprint })
         .HasDatabaseName("ix_time_entries_import_job_fingerprint")
         .IsUnique()
         .HasFilter("import_job_id IS NOT NULL");
    }
}
