using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ProjectManagement.Host.Tests;

public sealed class AuthControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    private const string SeedEmail = "pm1@local.test";
    private const string SeedPassword = "P@ssw0rd!123";
    private const string LoginUrl = "/api/v1/auth/login";
    private const string MeUrl = "/api/v1/auth/me";
    private const string LogoutUrl = "/api/v1/auth/logout";

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public AuthControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    // --- Task 5.1: login happy path ---

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithToken()
    {
        var response = await _client.PostAsJsonAsync(LoginUrl, new { email = SeedEmail, password = SeedPassword });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.True(body.TryGetProperty("accessToken", out var tokenProp));
        Assert.False(string.IsNullOrWhiteSpace(tokenProp.GetString()));

        Assert.True(body.TryGetProperty("expiresInSeconds", out var expProp));
        Assert.Equal(480 * 60, expProp.GetInt32()); // 8h = 28800 s

        Assert.True(body.TryGetProperty("user", out var userProp));
        Assert.Equal(SeedEmail, userProp.GetProperty("email").GetString());
        Assert.False(string.IsNullOrWhiteSpace(userProp.GetProperty("id").GetString()));
    }

    // --- Task 5.2: wrong password → 401, same message ---

    [Fact]
    public async Task Login_WrongPassword_Returns401ProblemDetails()
    {
        var response = await _client.PostAsJsonAsync(LoginUrl, new { email = SeedEmail, password = "WrongPass!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await AssertUniformUnauthorizedMessage(response);
    }

    [Fact]
    public async Task Login_NonExistentEmail_Returns401SameMessage()
    {
        var response = await _client.PostAsJsonAsync(LoginUrl, new { email = "ghost@nowhere.test", password = SeedPassword });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var wrongPassResponse = await _client.PostAsJsonAsync(LoginUrl, new { email = SeedEmail, password = "Bad!" });
        await AssertUniformUnauthorizedMessage(response);
        await AssertUniformUnauthorizedMessage(wrongPassResponse);

        // Same message for both — no email enumeration
        var msg1 = await GetProblemTitle(response);
        var msg2 = await GetProblemTitle(wrongPassResponse);
        Assert.Equal(msg1, msg2);
    }

    // --- Task 5.3: GET /me ---

    [Fact]
    public async Task Me_ValidToken_Returns200WithUser()
    {
        var token = await GetTokenAsync();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync(MeUrl);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.Equal(SeedEmail, body.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Me_NoToken_Returns401()
    {
        using var anonClient = new HttpClient { BaseAddress = _client.BaseAddress };
        var response = await anonClient.GetAsync(MeUrl);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // --- Task 5.4: POST /logout ---

    [Fact]
    public async Task Logout_Returns204()
    {
        var response = await _client.PostAsJsonAsync(LogoutUrl, new { });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // --- helpers ---

    private async Task<string> GetTokenAsync()
    {
        using var loginClient = new HttpClient { BaseAddress = _client.BaseAddress };
        var resp = await loginClient.PostAsJsonAsync(LoginUrl, new { email = SeedEmail, password = SeedPassword });
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        return body.GetProperty("accessToken").GetString()!;
    }

    private static async Task AssertUniformUnauthorizedMessage(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.True(body.TryGetProperty("status", out var statusProp));
        Assert.Equal(401, statusProp.GetInt32());
        // Must have a title or detail — and NOT expose which field was wrong
        var hasTitle = body.TryGetProperty("title", out _);
        var hasDetail = body.TryGetProperty("detail", out _);
        Assert.True(hasTitle || hasDetail, "ProblemDetails must have title or detail");
    }

    private static async Task<string?> GetProblemTitle(HttpResponseMessage response)
    {
        var stream = await response.Content.ReadAsStreamAsync();
        stream.Position = 0;
        var body = await JsonDocument.ParseAsync(stream);
        body.RootElement.TryGetProperty("title", out var titleProp);
        return titleProp.GetString();
    }
}
