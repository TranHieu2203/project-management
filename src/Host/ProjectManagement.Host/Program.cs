using System.Text.Json;
using Npgsql;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Auth.Api.Extensions;
using ProjectManagement.Auth.Infrastructure.Persistence;
using ProjectManagement.Auth.Infrastructure.Seeding;
using ProjectManagement.Projects.Api.Extensions;
using ProjectManagement.Projects.Infrastructure.Persistence;
using ProjectManagement.Projects.Infrastructure.Seeding;
using ProjectManagement.Workforce.Api.Extensions;
using ProjectManagement.Workforce.Infrastructure.Persistence;
using ProjectManagement.TimeTracking.Api.Extensions;
using ProjectManagement.TimeTracking.Infrastructure.Persistence;
using ProjectManagement.Capacity.Api.Extensions;
using ProjectManagement.Capacity.Infrastructure.Persistence;
using ProjectManagement.Reporting.Api.Extensions;
using ProjectManagement.Reporting.Infrastructure.Persistence;
using ProjectManagement.Notifications.Api.Extensions;
using ProjectManagement.Notifications.Infrastructure.Persistence;
using ProjectManagement.Metrics.Api.Extensions;
using ProjectManagement.Metrics.Infrastructure.Persistence;
using ProjectManagement.Shared.Infrastructure.Middleware;
using ProjectManagement.Shared.Infrastructure.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console();
});

var mvc = builder.Services.AddControllers();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);

builder.Services.AddOpenApi();

builder.Services.AddAuthModule(builder.Configuration, mvc);
builder.Services.AddProjectsModule(builder.Configuration, mvc);
builder.Services.AddWorkforceModule(builder.Configuration, mvc);
builder.Services.AddTimeTrackingModule(builder.Configuration, mvc);
builder.Services.AddCapacityModule(builder.Configuration, mvc);
builder.Services.AddReportingModule(builder.Configuration, mvc);
builder.Services.AddNotificationsModule(builder.Configuration, mvc);
builder.Services.AddMetricsModule(builder.Configuration, mvc);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ICacheService, MemoryCacheService>();

builder.Services.AddSingleton<NpgsqlDataSource>(_ =>
{
    var connectionString =
        builder.Configuration.GetConnectionString("Default") ??
        builder.Configuration["ConnectionStrings:Default"] ??
        "Host=localhost;Port=5432;Database=project_management;Username=pm_app;Password=pm_app_password";

    return NpgsqlDataSource.Create(connectionString);
});

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<GlobalExceptionMiddleware>();

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

var autoMigrate = builder.Configuration.GetValue<bool>("Host:AutoMigrate");
if (autoMigrate)
{
    await using var scope = app.Services.CreateAsyncScope();

    var authDb = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    await authDb.Database.MigrateAsync();

    var seeder = scope.ServiceProvider.GetRequiredService<IAuthSeeder>();
    await seeder.SeedAsync(CancellationToken.None);

    var projectsDb = scope.ServiceProvider.GetRequiredService<ProjectsDbContext>();
    await projectsDb.Database.MigrateAsync();

    var workforceDb = scope.ServiceProvider.GetRequiredService<WorkforceDbContext>();
    await workforceDb.Database.MigrateAsync();

    var timeTrackingDb = scope.ServiceProvider.GetRequiredService<TimeTrackingDbContext>();
    await timeTrackingDb.Database.MigrateAsync();

    var capacityDb = scope.ServiceProvider.GetRequiredService<CapacityDbContext>();
    await capacityDb.Database.MigrateAsync();

    var reportingDb = scope.ServiceProvider.GetRequiredService<ReportingDbContext>();
    await reportingDb.Database.MigrateAsync();

    var notificationsDb = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
    await notificationsDb.Database.MigrateAsync();

    var metricsDb = scope.ServiceProvider.GetRequiredService<MetricsDbContext>();
    await metricsDb.Database.MigrateAsync();

    // Resolve seed user IDs after auth seeder runs so Projects.Infrastructure stays auth-independent
    var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ProjectManagement.Auth.Domain.Users.ApplicationUser>>();
    var pm1User = await userManager.FindByEmailAsync("pm1@local.test");
    var pm2User = await userManager.FindByEmailAsync("pm2@local.test");
    var projectsSeeder = scope.ServiceProvider.GetRequiredService<ProjectsSeeder>();
    await projectsSeeder.SeedAsync(
        pm1User?.Id ?? Guid.Empty,
        pm2User?.Id ?? Guid.Empty,
        CancellationToken.None);
}

app.MapGet("/health", async (NpgsqlDataSource dataSource, CancellationToken ct) =>
{
    var dbOk = false;
    string? dbError = null;

    try
    {
        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand("select 1", conn);
        _ = await cmd.ExecuteScalarAsync(ct);
        dbOk = true;
    }
    catch (Exception ex)
    {
        dbError = ex.Message;
        Log.Warning(ex, "Healthcheck DB connection failed");
    }

    return Results.Ok(new
    {
        status = "ok",
        service = "project-management-api",
        environment = app.Environment.EnvironmentName,
        db = new
        {
            ok = dbOk,
            error = dbError
        }
    });
});

app.MapGet("/api/v1/health", async (NpgsqlDataSource dataSource, CancellationToken ct) =>
{
    var dbOk = false;
    string? dbError = null;

    try
    {
        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand("select 1", conn);
        _ = await cmd.ExecuteScalarAsync(ct);
        dbOk = true;
    }
    catch (Exception ex)
    {
        dbError = ex.Message;
        Log.Warning(ex, "Healthcheck DB connection failed");
    }

    return Results.Ok(new
    {
        status = "ok",
        service = "project-management-api",
        environment = app.Environment.EnvironmentName,
        db = new
        {
            ok = dbOk,
            error = dbError
        }
    });
});

app.Run();
