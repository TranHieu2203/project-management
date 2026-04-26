using Microsoft.EntityFrameworkCore;
using ProjectManagement.Capacity.Domain.Entities;

namespace ProjectManagement.Capacity.Application.Common.Interfaces;

public interface ICapacityDbContext
{
    DbSet<CapacityOverride> CapacityOverrides { get; }
    DbSet<ForecastArtifact> ForecastArtifacts { get; }
    Task<int> SaveChangesAsync(CancellationToken ct);
}
