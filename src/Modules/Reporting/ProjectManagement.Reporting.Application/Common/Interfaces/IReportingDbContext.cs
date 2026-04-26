using Microsoft.EntityFrameworkCore;
using ProjectManagement.Reporting.Domain.Entities;

namespace ProjectManagement.Reporting.Application.Common.Interfaces;

public interface IReportingDbContext
{
    DbSet<ExportJob> ExportJobs { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
