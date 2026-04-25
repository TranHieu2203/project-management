using System.Text.Json;
using Npgsql;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Auth.Api.Extensions;
using ProjectManagement.Auth.Infrastructure.Persistence;
using ProjectManagement.Auth.Infrastructure.Seeding;
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

    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    await db.Database.MigrateAsync();

    var seeder = scope.ServiceProvider.GetRequiredService<IAuthSeeder>();
    await seeder.SeedAsync(CancellationToken.None);
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
