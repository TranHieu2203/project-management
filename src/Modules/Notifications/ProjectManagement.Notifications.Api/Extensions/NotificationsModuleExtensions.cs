using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectManagement.Notifications.Api.Controllers;
using ProjectManagement.Notifications.Application.Common.Interfaces;
using ProjectManagement.Notifications.Application.Queries.GetNotificationPreferences;
using ProjectManagement.Notifications.Infrastructure.Persistence;
using ProjectManagement.Notifications.Infrastructure.Services;
using ProjectManagement.Notifications.Infrastructure.Workers;

namespace ProjectManagement.Notifications.Api.Extensions;

public static class NotificationsModuleExtensions
{
    public static IServiceCollection AddNotificationsModule(
        this IServiceCollection services,
        IConfiguration configuration,
        IMvcBuilder mvc)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? configuration["ConnectionStrings:Default"]!;

        services.AddDbContext<NotificationsDbContext>(opts =>
            opts.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory_notifications")));
        services.AddScoped<INotificationsDbContext>(sp => sp.GetRequiredService<NotificationsDbContext>());

        services.Configure<SmtpSettings>(configuration.GetSection("Smtp"));
        services.AddTransient<IEmailService, EmailService>();

        services.AddHostedService<DigestWorker>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(GetNotificationPreferencesHandler).Assembly));

        mvc.AddApplicationPart(typeof(NotificationPreferencesController).Assembly);
        return services;
    }
}
