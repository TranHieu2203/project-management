using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectManagement.Workforce.Api.Controllers;
using ProjectManagement.Workforce.Infrastructure.Extensions;

namespace ProjectManagement.Workforce.Api.Extensions;

public static class WorkforceModuleExtensions
{
    public static IServiceCollection AddWorkforceModule(
        this IServiceCollection services,
        IConfiguration configuration,
        IMvcBuilder mvc)
    {
        services.AddWorkforceInfrastructure(configuration);
        mvc.AddApplicationPart(typeof(VendorsController).Assembly);
        return services;
    }
}
