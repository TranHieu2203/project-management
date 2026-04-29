using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ProjectManagement.Reporting.Domain.Entities;
using ProjectManagement.Reporting.Infrastructure.Persistence;

namespace ProjectManagement.Host.Tests;

/// <summary>
/// Integration tests for Story 10-2: Alert Center endpoints.
/// Covers: 401 unauthenticated guard, per-user isolation, ownership check for PATCH /read.
/// </summary>
public sealed class AlertsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    private const string SeedEmail1    = "pm1@local.test";
    private const string SeedPassword1 = "P@ssw0rd!123";
    private const string SeedEmail2    = "pm2@local.test";
    private const string SeedPassword2 = "P@ssw0rd!123";
    private const string LoginUrl      = "/api/v1/auth/login";
    private const string MeUrl         = "/api/v1/auth/me";
    private const string AlertsUrl     = "/api/v1/alerts";

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public AlertsTests(WebApplicationFactory<Program> factory) => _factory = factory;

    // ─── Helpers ────────────────────────────────────────────────────────────────

    private static async Task<string> GetTokenAsync(HttpClient client, string email, string password)
    {
        var resp = await client.PostAsJsonAsync(LoginUrl,
            new { email, password });
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        return body.GetProperty("accessToken").GetString()!;
    }

    private static async Task<(HttpClient Client, Guid UserId)> CreateAuthClientAsync(
        WebApplicationFactory<Program> factory, string email, string password)
    {
        var client = factory.CreateClient();
        var token  = await GetTokenAsync(client, email, password);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var meResp = await client.GetAsync(MeUrl);
        meResp.EnsureSuccessStatusCode();
        var meBody = await meResp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        var userId = Guid.Parse(meBody.GetProperty("id").GetString()!);

        return (client, userId);
    }

    private async Task<Guid> SeedAlertAsync(Guid userId, string type = "DeadlineApproaching")
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ReportingDbContext>();

        var alert = Alert.Create(
            userId,
            type,
            $"Test alert for {type}",
            description: "Seeded by integration test");
        db.Alerts.Add(alert);
        await db.SaveChangesAsync();
        return alert.Id;
    }

    // ─── GET /api/v1/alerts ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetAlerts_Returns401_WhenNotAuthenticated()
    {
        var client = _factory.CreateClient();
        var resp   = await client.GetAsync(AlertsUrl);
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task GetAlerts_ReturnsOwnAlertsOnly()
    {
        var (client1, userId1) = await CreateAuthClientAsync(_factory, SeedEmail1, SeedPassword1);
        var (client2, userId2) = await CreateAuthClientAsync(_factory, SeedEmail2, SeedPassword2);

        // Seed one alert per user
        var alertId1 = await SeedAlertAsync(userId1, "DeadlineApproaching");
        var alertId2 = await SeedAlertAsync(userId2, "OverdueTask");

        // pm1 should see own alert but not pm2's
        var resp1 = await client1.GetAsync(AlertsUrl);
        resp1.EnsureSuccessStatusCode();
        var body1 = await resp1.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        var items1 = body1.GetProperty("items");

        bool found1InOwn = false, found2InOthers = false;
        foreach (var item in items1.EnumerateArray())
        {
            var id = Guid.Parse(item.GetProperty("id").GetString()!);
            if (id == alertId1) found1InOwn = true;
            if (id == alertId2) found2InOthers = true;
        }

        Assert.True(found1InOwn, "pm1 should see own alert");
        Assert.False(found2InOthers, "pm1 should NOT see pm2's alert");
    }

    [Fact]
    public async Task GetAlerts_Returns200_WithCorrectShape()
    {
        var (client, userId) = await CreateAuthClientAsync(_factory, SeedEmail1, SeedPassword1);
        await SeedAlertAsync(userId);

        var resp = await client.GetAsync(AlertsUrl);
        resp.EnsureSuccessStatusCode();

        var body = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.True(body.TryGetProperty("items", out _), "Response must have 'items'");
        Assert.True(body.TryGetProperty("totalCount", out _), "Response must have 'totalCount'");
    }

    // ─── PATCH /api/v1/alerts/{id}/read ─────────────────────────────────────────

    [Fact]
    public async Task MarkAlertRead_Returns204_WhenOwnAlert()
    {
        var (client, userId) = await CreateAuthClientAsync(_factory, SeedEmail1, SeedPassword1);
        var alertId = await SeedAlertAsync(userId);

        var resp = await client.PatchAsync($"{AlertsUrl}/{alertId}/read", null);
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
    }

    [Fact]
    public async Task MarkAlertRead_Returns403_WhenOtherUsersAlert()
    {
        var (_, userId1)    = await CreateAuthClientAsync(_factory, SeedEmail1, SeedPassword1);
        var (client2, _)    = await CreateAuthClientAsync(_factory, SeedEmail2, SeedPassword2);

        // Alert belongs to pm1 but pm2 tries to mark it
        var alertId = await SeedAlertAsync(userId1);

        var resp = await client2.PatchAsync($"{AlertsUrl}/{alertId}/read", null);
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task MarkAlertRead_Returns404_WhenAlertNotFound()
    {
        var (client, _) = await CreateAuthClientAsync(_factory, SeedEmail1, SeedPassword1);
        var nonExistentId = Guid.NewGuid();

        var resp = await client.PatchAsync($"{AlertsUrl}/{nonExistentId}/read", null);
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }
}
