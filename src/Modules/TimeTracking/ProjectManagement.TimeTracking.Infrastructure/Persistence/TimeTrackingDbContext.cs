using Microsoft.EntityFrameworkCore;
using ProjectManagement.TimeTracking.Application.Common.Interfaces;
using ProjectManagement.TimeTracking.Domain.Entities;
using ProjectManagement.TimeTracking.Infrastructure.Persistence.Configurations;

namespace ProjectManagement.TimeTracking.Infrastructure.Persistence;

public sealed class TimeTrackingDbContext : DbContext, ITimeTrackingDbContext
{
    public TimeTrackingDbContext(DbContextOptions<TimeTrackingDbContext> options) : base(options) { }

    public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();
    public DbSet<ImportJob> ImportJobs => Set<ImportJob>();
    public DbSet<ImportJobError> ImportJobErrors => Set<ImportJobError>();
    public DbSet<PeriodLock> PeriodLocks => Set<PeriodLock>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("time_tracking");
        modelBuilder.ApplyConfiguration(new TimeEntryConfiguration());
        modelBuilder.ApplyConfiguration(new ImportJobConfiguration());
        modelBuilder.ApplyConfiguration(new ImportJobErrorConfiguration());
        modelBuilder.ApplyConfiguration(new PeriodLockConfiguration());
    }
}
