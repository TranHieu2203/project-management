namespace ProjectManagement.Auth.Infrastructure.Seeding;

public interface IAuthSeeder
{
    Task SeedAsync(CancellationToken ct);
}

