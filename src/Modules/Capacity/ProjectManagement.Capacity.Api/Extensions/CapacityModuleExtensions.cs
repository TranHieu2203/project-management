using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectManagement.Capacity.Api.Controllers;
using ProjectManagement.Capacity.Application.Queries.GetResourceOverload;
using ProjectManagement.Capacity.Infrastructure.Extensions;

namespace ProjectManagement.Capacity.Api.Extensions;

public static class CapacityModuleExtensions
{
    public static IServiceCollection AddCapacityModule(
        this IServiceCollection services,
        IConfiguration configuration,
        IMvcBuilder mvc)
    {
        services.AddCapacityInfrastructure(configuration);

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(GetResourceOverloadHandler).Assembly));

        mvc.AddApplicationPart(typeof(CapacityController).Assembly);
        return services;
    }
}
