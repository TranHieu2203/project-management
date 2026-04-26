using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using ProjectManagement.Auth.Application.Options;
using ProjectManagement.Auth.Application.Tokens;
using ProjectManagement.Auth.Domain.Users;
using ProjectManagement.Auth.Infrastructure.Persistence;
using ProjectManagement.Auth.Infrastructure.Seeding;
using ProjectManagement.Auth.Infrastructure.Services;
using ProjectManagement.Auth.Infrastructure.Tokens;
using ProjectManagement.Shared.Infrastructure.Services;

namespace ProjectManagement.Auth.Infrastructure.Extensions;

public static class AuthInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddAuthInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AuthDbContext>(options =>
        {
            var connectionString =
                configuration.GetConnectionString("Default") ??
                configuration["ConnectionStrings:Default"] ??
                "Host=localhost;Port=5432;Database=project_management;Username=pm_app;Password=pm_app_password";

            options.UseNpgsql(connectionString);
        });

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = false;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AuthDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.Issuer), "Jwt:Issuer is required")
            .Validate(o => !string.IsNullOrWhiteSpace(o.Audience), "Jwt:Audience is required")
            .Validate(o => !string.IsNullOrWhiteSpace(o.SigningKey), "Jwt:SigningKey is required")
            .ValidateOnStart();

        services.AddSingleton<ITokenService, TokenService>();
        services.AddScoped<IAuthSeeder, AuthSeeder>();
        services.AddScoped<IUserLookupService, UserLookupService>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>();
                if (jwt is null)
                {
                    throw new InvalidOperationException("Jwt options not configured");
                }

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        services.AddAuthorization();
        return services;
    }
}

