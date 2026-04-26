using Microsoft.EntityFrameworkCore;
using ProjectManagement.Reporting.Application.Common.Interfaces;
using ProjectManagement.Reporting.Domain.Entities;
using ProjectManagement.Reporting.Infrastructure.Persistence.Configurations;

namespace ProjectManagement.Reporting.Infrastructure.Persistence;

public sealed class ReportingDbContext : DbContext, IReportingDbContext
{
    public ReportingDbContext(DbContextOptions<ReportingDbContext> options) : base(options) { }

    public DbSet<ExportJob> ExportJobs => Set<ExportJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("reporting");
        modelBuilder.ApplyConfiguration(new ExportJobConfiguration());
    }
}
