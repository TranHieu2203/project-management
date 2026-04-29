using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectManagement.Reporting.Api.Controllers;
using ProjectManagement.Reporting.Application.Common.Interfaces;
using ProjectManagement.Reporting.Application.Queries.GetCostSummary;
using ProjectManagement.Reporting.Infrastructure.Persistence;
using ProjectManagement.Reporting.Infrastructure.Services;
using ProjectManagement.Reporting.Infrastructure.Workers;
using QuestPDF.Infrastructure;

namespace ProjectManagement.Reporting.Api.Extensions;

public static class ReportingModuleExtensions
{
    public static IServiceCollection AddReportingModule(
        this IServiceCollection services,
        IConfiguration configuration,
        IMvcBuilder mvc)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var connectionString = configuration.GetConnectionString("Default")
            ?? configuration["ConnectionStrings:Default"]!;

        services.AddDbContext<ReportingDbContext>(opts =>
            opts.UseNpgsql(connectionString));
        services.AddScoped<IReportingDbContext>(sp => sp.GetRequiredService<ReportingDbContext>());

        services.AddSingleton(Channel.CreateBounded<Guid>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
        }));
        services.AddSingleton(sp => sp.GetRequiredService<Channel<Guid>>().Writer);
        services.AddSingleton(sp => sp.GetRequiredService<Channel<Guid>>().Reader);

        services.AddHostedService<ExportWorker>();
        services.AddHostedService<AlertRulesWorker>();
        services.AddHostedService<AlertDigestWorker>();

        services.AddTransient<CsvExportService>();
        services.AddTransient<XlsxExportService>();
        services.AddTransient<PdfExportService>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(GetCostSummaryHandler).Assembly));

        mvc.AddApplicationPart(typeof(ReportingController).Assembly);
        return services;
    }
}
