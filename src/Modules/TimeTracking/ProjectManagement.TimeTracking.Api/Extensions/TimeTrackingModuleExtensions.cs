using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectManagement.TimeTracking.Api.Controllers;
using ProjectManagement.TimeTracking.Infrastructure.Extensions;

namespace ProjectManagement.TimeTracking.Api.Extensions;

public static class TimeTrackingModuleExtensions
{
    public static IServiceCollection AddTimeTrackingModule(
        this IServiceCollection services,
        IConfiguration configuration,
        IMvcBuilder mvc)
    {
        services.AddTimeTrackingInfrastructure(configuration);
        mvc.AddApplicationPart(typeof(TimeEntriesController).Assembly);
        return services;
    }
}
