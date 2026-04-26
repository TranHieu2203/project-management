using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectManagement.TimeTracking.Application.Common.Interfaces;
using ProjectManagement.TimeTracking.Application.TimeEntries.Commands.CreateTimeEntry;
using ProjectManagement.TimeTracking.Infrastructure.Persistence;
using ProjectManagement.TimeTracking.Infrastructure.Services;

namespace ProjectManagement.TimeTracking.Infrastructure.Extensions;

public static class TimeTrackingInfrastructureExtensions
{
    public static IServiceCollection AddTimeTrackingInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("Default") ??
            "Host=localhost;Port=5432;Database=project_management;Username=pm_app;Password=pm_app_password";

        services.AddDbContext<TimeTrackingDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<ITimeTrackingDbContext>(sp => sp.GetRequiredService<TimeTrackingDbContext>());
        services.AddScoped<ITimeTrackingRateService, TimeTrackingRateService>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(CreateTimeEntryHandler).Assembly));

        return services;
    }
}
