using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ProjectManagement.Host.Tests;

public sealed class TestHostFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Debug);
        });

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Host:AutoMigrate"] = "true",
                ["Host__AutoMigrate"] = "true",
                // docker-compose exposes DB on localhost:5433 (container:5432)
                ["ConnectionStrings:Default"] =
                    "Host=localhost;Port=5433;Database=project_management;Username=pm_app;Password=pm_app_password",
                ["ConnectionStrings__Default"] =
                    "Host=localhost;Port=5433;Database=project_management;Username=pm_app;Password=pm_app_password",
                ["Serilog:MinimumLevel:Default"] = "Debug",
                ["Serilog:MinimumLevel:Override:Microsoft"] = "Information",
                ["Serilog:MinimumLevel:Override:Microsoft.EntityFrameworkCore.Database.Command"] = "Information"
            });
        });
    }
}

