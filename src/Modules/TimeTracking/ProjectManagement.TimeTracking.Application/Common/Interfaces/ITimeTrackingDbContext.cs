using Microsoft.EntityFrameworkCore;
using ProjectManagement.TimeTracking.Domain.Entities;

namespace ProjectManagement.TimeTracking.Application.Common.Interfaces;

public interface ITimeTrackingDbContext
{
    DbSet<TimeEntry> TimeEntries { get; }
    DbSet<ImportJob> ImportJobs { get; }
    DbSet<ImportJobError> ImportJobErrors { get; }
    DbSet<PeriodLock> PeriodLocks { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
