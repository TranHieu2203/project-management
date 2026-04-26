using Microsoft.AspNetCore.Identity;
using ProjectManagement.Auth.Domain.Users;

namespace ProjectManagement.Auth.Infrastructure.Seeding;

public sealed class AuthSeeder : IAuthSeeder
{
    private readonly UserManager<ApplicationUser> _userManager;

    public AuthSeeder(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task SeedAsync(CancellationToken ct)
    {
        await SeedUserAsync("pm1@local.test", "PM One", ct);
        await SeedUserAsync("pm2@local.test", "PM Two", ct);
    }

    private async Task SeedUserAsync(string email, string displayName, CancellationToken ct)
    {
        const string password = "P@ssw0rd!123";

        var existing = await _userManager.FindByEmailAsync(email);
        if (existing is not null)
            return;

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            DisplayName = displayName,
            EmailConfirmed = true,
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Seed user '{email}' create failed: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
    }
}
