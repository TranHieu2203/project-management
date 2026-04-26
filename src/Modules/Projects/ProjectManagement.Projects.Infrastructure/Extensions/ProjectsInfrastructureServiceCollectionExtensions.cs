using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectManagement.Projects.Application.Common.Interfaces;
using ProjectManagement.Projects.Application.Queries.GetProjectList;
using ProjectManagement.Projects.Infrastructure.Persistence;
using ProjectManagement.Projects.Infrastructure.Seeding;
using ProjectManagement.Projects.Infrastructure.Services;

namespace ProjectManagement.Projects.Infrastructure.Extensions;

public static class ProjectsInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddProjectsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("Default") ??
            "Host=localhost;Port=5432;Database=project_management;Username=pm_app;Password=pm_app_password";

        services.AddDbContext<ProjectsDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IProjectsDbContext>(sp => sp.GetRequiredService<ProjectsDbContext>());
        services.AddScoped<IMembershipChecker, MembershipChecker>();
        services.AddScoped<ProjectsSeeder>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(GetProjectListHandler).Assembly));

        return services;
    }
}
