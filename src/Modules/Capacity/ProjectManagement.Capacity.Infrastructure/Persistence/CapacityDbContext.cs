using Microsoft.EntityFrameworkCore;
using ProjectManagement.Capacity.Application.Common.Interfaces;
using ProjectManagement.Capacity.Domain.Entities;
using ProjectManagement.Capacity.Infrastructure.Persistence.Configurations;

namespace ProjectManagement.Capacity.Infrastructure.Persistence;

public sealed class CapacityDbContext : DbContext, ICapacityDbContext
{
    public CapacityDbContext(DbContextOptions<CapacityDbContext> options) : base(options) { }

    public DbSet<CapacityOverride> CapacityOverrides => Set<CapacityOverride>();
    public DbSet<ForecastArtifact> ForecastArtifacts => Set<ForecastArtifact>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("capacity");
        modelBuilder.ApplyConfiguration(new CapacityOverrideConfiguration());
        modelBuilder.ApplyConfiguration(new ForecastArtifactConfiguration());
    }
}
