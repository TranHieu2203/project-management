using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectManagement.Projects.Application.Commands.CreateProject;
using ProjectManagement.Projects.Application.Common.Interfaces;
using ProjectManagement.Projects.Application.IssueTypes.Services;
using ProjectManagement.Projects.Application.Queries.GetProjectList;
using ProjectManagement.Projects.Infrastructure.Persistence;
using ProjectManagement.Projects.Infrastructure.Seeding;
using ProjectManagement.Projects.Infrastructure.Services;
using ProjectManagement.Shared.Infrastructure.MediatR;

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
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory_projects"))
            // CI/test hosts may start before all migration artifacts are regenerated.
            // We treat pending model changes as non-fatal at runtime; migrations remain source of truth.
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));

        services.AddScoped<IProjectsDbContext>(sp => sp.GetRequiredService<ProjectsDbContext>());
        services.AddScoped<IMembershipChecker, MembershipChecker>();
        services.AddScoped<IProjectAdminChecker, ProjectAdminChecker>();
        services.AddScoped<IIssueTypesService, IssueTypesService>();
        services.AddScoped<IProjectIssueTypeSettingsService, ProjectIssueTypeSettingsService>();
        services.AddScoped<ProjectsSeeder>();

        services.AddValidatorsFromAssemblyContaining<CreateProjectCommandValidator>();
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(GetProjectListHandler).Assembly));

        return services;
    }
}
