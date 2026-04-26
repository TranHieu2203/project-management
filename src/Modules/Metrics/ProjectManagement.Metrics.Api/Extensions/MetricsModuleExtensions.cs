using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectManagement.Metrics.Api.Controllers;
using ProjectManagement.Metrics.Application.Common.Interfaces;
using ProjectManagement.Metrics.Application.Commands.RecordMetricEvent;
using ProjectManagement.Metrics.Infrastructure.Persistence;

namespace ProjectManagement.Metrics.Api.Extensions;

public static class MetricsModuleExtensions
{
    public static IServiceCollection AddMetricsModule(
        this IServiceCollection services,
        IConfiguration configuration,
        IMvcBuilder mvc)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? configuration["ConnectionStrings:Default"]!;

        services.AddDbContext<MetricsDbContext>(opts =>
            opts.UseNpgsql(connectionString));
        services.AddScoped<IMetricsDbContext>(sp => sp.GetRequiredService<MetricsDbContext>());

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(RecordMetricEventHandler).Assembly));

        mvc.AddApplicationPart(typeof(MetricsController).Assembly);
        return services;
    }
}
