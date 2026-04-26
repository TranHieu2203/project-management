using Microsoft.EntityFrameworkCore;
using ProjectManagement.Metrics.Domain.Entities;

namespace ProjectManagement.Metrics.Application.Common.Interfaces;

public interface IMetricsDbContext
{
    DbSet<MetricEvent> MetricEvents { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
