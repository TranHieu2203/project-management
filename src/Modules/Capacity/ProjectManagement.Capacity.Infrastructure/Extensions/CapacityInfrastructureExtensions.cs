using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectManagement.Capacity.Application.Common.Interfaces;
using ProjectManagement.Capacity.Infrastructure.Persistence;
using ProjectManagement.Capacity.Infrastructure.Workers;

namespace ProjectManagement.Capacity.Infrastructure.Extensions;

public static class CapacityInfrastructureExtensions
{
    public static IServiceCollection AddCapacityInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("Default") ??
            "Host=localhost;Port=5432;Database=project_management;Username=pm_app;Password=pm_app_password";

        services.AddDbContext<CapacityDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<ICapacityDbContext>(sp => sp.GetRequiredService<CapacityDbContext>());
        services.AddHostedService<ForecastWorker>();
        return services;
    }
}
