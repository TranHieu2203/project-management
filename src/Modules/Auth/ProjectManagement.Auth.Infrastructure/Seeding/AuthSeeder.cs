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
        const string seedEmail = "pm1@local.test";
        const string seedPassword = "P@ssw0rd!123";

        var existing = await _userManager.FindByEmailAsync(seedEmail);
        if (existing is not null)
        {
            return;
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = seedEmail,
            UserName = seedEmail,
            DisplayName = "PM One",
            EmailConfirmed = true
        };

        var createResult = await _userManager.CreateAsync(user, seedPassword);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException($"Seed user create failed: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
        }
    }
}

