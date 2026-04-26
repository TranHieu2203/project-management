using Microsoft.EntityFrameworkCore;
using ProjectManagement.Workforce.Application.Common.Interfaces;
using ProjectManagement.Workforce.Domain.Entities;
using ProjectManagement.Workforce.Infrastructure.Persistence.Configurations;

namespace ProjectManagement.Workforce.Infrastructure.Persistence;

public sealed class WorkforceDbContext : DbContext, IWorkforceDbContext
{
    public WorkforceDbContext(DbContextOptions<WorkforceDbContext> options) : base(options) { }

    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<Resource> Resources => Set<Resource>();
    public DbSet<MonthlyRate> MonthlyRates => Set<MonthlyRate>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("workforce");
        modelBuilder.ApplyConfiguration(new VendorConfiguration());
        modelBuilder.ApplyConfiguration(new ResourceConfiguration());
        modelBuilder.ApplyConfiguration(new MonthlyRateConfiguration());
        modelBuilder.ApplyConfiguration(new AuditEventConfiguration());
    }
}
