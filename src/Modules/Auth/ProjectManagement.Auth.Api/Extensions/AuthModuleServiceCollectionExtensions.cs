using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ProjectManagement.Auth.Api.Controllers;
using ProjectManagement.Auth.Infrastructure.Extensions;

namespace ProjectManagement.Auth.Api.Extensions;

public static class AuthModuleServiceCollectionExtensions
{
    public static IServiceCollection AddAuthModule(this IServiceCollection services, IConfiguration configuration, IMvcBuilder mvc)
    {
        services.AddAuthInfrastructure(configuration);

        mvc.AddApplicationPart(typeof(AuthController).Assembly);
        return services;
    }
}

