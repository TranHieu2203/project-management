using Microsoft.EntityFrameworkCore;
using ProjectManagement.Reporting.Domain.Entities;

namespace ProjectManagement.Reporting.Application.Common.Interfaces;

public interface IReportingDbContext
{
    DbSet<ExportJob> ExportJobs { get; }
    DbSet<Alert> Alerts { get; }
    DbSet<AlertPreference> AlertPreferences { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
