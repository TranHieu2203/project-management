using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectManagement.Workforce.Application.Common.Interfaces;
using ProjectManagement.Workforce.Application.Vendors.Commands.CreateVendor;
using ProjectManagement.Workforce.Infrastructure.Persistence;

namespace ProjectManagement.Workforce.Infrastructure.Extensions;

public static class WorkforceInfrastructureExtensions
{
    public static IServiceCollection AddWorkforceInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("Default") ??
            "Host=localhost;Port=5432;Database=project_management;Username=pm_app;Password=pm_app_password";

        services.AddDbContext<WorkforceDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory_workforce")));

        services.AddScoped<IWorkforceDbContext>(sp => sp.GetRequiredService<WorkforceDbContext>());

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(CreateVendorHandler).Assembly));

        return services;
    }
}
