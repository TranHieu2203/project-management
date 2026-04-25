using Microsoft.AspNetCore.Mvc.Testing;

namespace ProjectManagement.Host.Tests;

public sealed class HealthEndpointTests
{
    [Fact]
    public async Task GetHealth_ReturnsOk()
    {
        await using var app = new WebApplicationFactory<Program>();
        using var client = app.CreateClient();

        using var response = await client.GetAsync("/health");

        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task GetApiV1Health_ReturnsOk()
    {
        await using var app = new WebApplicationFactory<Program>();
        using var client = app.CreateClient();

        using var response = await client.GetAsync("/api/v1/health");

        Assert.True(response.IsSuccessStatusCode);
    }
}
