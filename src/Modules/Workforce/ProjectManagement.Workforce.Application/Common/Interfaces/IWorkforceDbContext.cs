using Microsoft.EntityFrameworkCore;
using ProjectManagement.Workforce.Domain.Entities;

namespace ProjectManagement.Workforce.Application.Common.Interfaces;

public interface IWorkforceDbContext
{
    DbSet<Vendor> Vendors { get; }
    DbSet<Resource> Resources { get; }
    DbSet<MonthlyRate> MonthlyRates { get; }
    DbSet<AuditEvent> AuditEvents { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
