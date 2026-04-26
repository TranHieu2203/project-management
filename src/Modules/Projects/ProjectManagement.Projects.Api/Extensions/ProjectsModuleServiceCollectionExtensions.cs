using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectManagement.Projects.Api.Controllers;
using ProjectManagement.Projects.Infrastructure.Extensions;

namespace ProjectManagement.Projects.Api.Extensions;

public static class ProjectsModuleServiceCollectionExtensions
{
    public static IServiceCollection AddProjectsModule(
        this IServiceCollection services,
        IConfiguration configuration,
        IMvcBuilder mvc)
    {
        services.AddProjectsInfrastructure(configuration);

        mvc.AddApplicationPart(typeof(ProjectsController).Assembly);

        return services;
    }
}
