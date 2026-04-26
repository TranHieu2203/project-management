using Microsoft.EntityFrameworkCore;
using ProjectManagement.Metrics.Application.Common.Interfaces;
using ProjectManagement.Metrics.Domain.Entities;

namespace ProjectManagement.Metrics.Infrastructure.Persistence;

public class MetricsDbContext : DbContext, IMetricsDbContext
{
    public MetricsDbContext(DbContextOptions<MetricsDbContext> options) : base(options) { }

    public DbSet<MetricEvent> MetricEvents => Set<MetricEvent>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.HasDefaultSchema("metrics");
        mb.ApplyConfigurationsFromAssembly(typeof(MetricsDbContext).Assembly);
    }
}
