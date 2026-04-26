using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ProjectManagement.Host.Tests;

/// <summary>
/// Integration tests for Story 1.3: Projects CRUD + Optimistic Locking.
/// Covers AC 1-12: POST/PUT/DELETE with ETag/If-Match, 412, 409, 201, 200, 204.
/// </summary>
public sealed class ProjectsCrudTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    private const string SeedEmail = "pm1@local.test";
    private const string SeedPassword = "P@ssw0rd!123";
    private const string LoginUrl = "/api/v1/auth/login";
    private const string ProjectsUrl = "/api/v1/projects";

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public ProjectsCrudTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    // ─── Helpers ───────────────────────────────────────────────────────────────

    private static async Task<string> GetTokenAsync(HttpClient client)
    {
        var resp = await client.PostAsJsonAsync(LoginUrl, new { email = SeedEmail, password = SeedPassword });
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        return body.GetProperty("accessToken").GetString()!;
    }

    private static async Task<HttpClient> CreateAuthClientAsync(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        var token = await GetTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task AssertProblemDetails(HttpResponseMessage response, int expectedStatus)
    {
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.True(body.TryGetProperty("status", out var statusProp));
        Assert.Equal(expectedStatus, statusProp.GetInt32());
        var hasTitle = body.TryGetProperty("title", out _);
        var hasDetail = body.TryGetProperty("detail", out _);
        Assert.True(hasTitle || hasDetail);
    }

    // ─── AC 11: No JWT → 401 ───────────────────────────────────────────────────

    [Fact]
    public async Task CreateProject_NoJwt_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync(ProjectsUrl, new { code = "TEST-01", name = "Test" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProject_NoJwt_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("If-Match", "\"1\"");
        var response = await client.PutAsJsonAsync($"{ProjectsUrl}/{Guid.NewGuid()}", new { name = "X" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteProject_NoJwt_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("If-Match", "\"1\"");
        var response = await client.DeleteAsync($"{ProjectsUrl}/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ─── AC 1: POST → 201 + ETag + creator is member ─────────────────────────

    [Fact]
    public async Task CreateProject_ValidPayload_Returns201WithETag()
    {
        var client = await CreateAuthClientAsync(_factory);
        var code = $"CRD-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

        var response = await client.PostAsJsonAsync(ProjectsUrl,
            new { code, name = "Test Project", description = "Optional desc" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.ETag);
        Assert.Equal("\"1\"", response.Headers.ETag.ToString());

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.True(body.TryGetProperty("id", out _));
        Assert.Equal(code, body.GetProperty("code").GetString());
        Assert.Equal("MembersOnly", body.GetProperty("visibility").GetString());
        Assert.Equal(1, body.GetProperty("version").GetInt32());
    }

    [Fact]
    public async Task CreateProject_CreatorIsAutoMember_CanFetchByIdAfterCreate()
    {
        var client = await CreateAuthClientAsync(_factory);
        var code = $"MBR-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

        var createResp = await client.PostAsJsonAsync(ProjectsUrl, new { code, name = "Member Test" });
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);

        var created = await createResp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        var projectId = created.GetProperty("id").GetString()!;

        // Creator phải thấy được project vừa tạo (membership-only)
        var getResp = await client.GetAsync($"{ProjectsUrl}/{projectId}");
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);
    }

    // ─── AC 10: POST duplicate code → 409 ───────────────────────────────────

    [Fact]
    public async Task CreateProject_DuplicateCode_Returns409ProblemDetails()
    {
        var client = await CreateAuthClientAsync(_factory);
        var code = $"DUP-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

        // First create
        var resp1 = await client.PostAsJsonAsync(ProjectsUrl, new { code, name = "First" });
        Assert.Equal(HttpStatusCode.Created, resp1.StatusCode);

        // Second create with same code → 409
        var resp2 = await client.PostAsJsonAsync(ProjectsUrl, new { code, name = "Second" });
        Assert.Equal(HttpStatusCode.Conflict, resp2.StatusCode);
        await AssertProblemDetails(resp2, 409);
    }

    // ─── AC 2: GET list has ETag fields ──────────────────────────────────────

    [Fact]
    public async Task GetProjects_AuthenticatedMember_HasVersionField()
    {
        var client = await CreateAuthClientAsync(_factory);
        var response = await client.GetAsync(ProjectsUrl);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var projects = await response.Content.ReadFromJsonAsync<JsonElement[]>(JsonOpts);
        Assert.NotNull(projects);
        foreach (var p in projects)
        {
            Assert.True(p.TryGetProperty("version", out _));
        }
    }

    // ─── AC 3: GET /{id} has ETag header ────────────────────────────────────

    [Fact]
    public async Task GetProjectById_IsMember_ReturnsETagHeader()
    {
        var client = await CreateAuthClientAsync(_factory);
        var code = $"EGT-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
        var createResp = await client.PostAsJsonAsync(ProjectsUrl, new { code, name = "ETag Test" });
        createResp.EnsureSuccessStatusCode();

        var created = await createResp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        var projectId = created.GetProperty("id").GetString()!;

        var getResp = await client.GetAsync($"{ProjectsUrl}/{projectId}");
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);
        Assert.NotNull(getResp.Headers.ETag);
    }

    // ─── AC 4: PUT valid If-Match → 200 + new ETag ──────────────────────────

    [Fact]
    public async Task UpdateProject_ValidIfMatch_Returns200WithNewETag()
    {
        var client = await CreateAuthClientAsync(_factory);
        var code = $"UPD-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
        var createResp = await client.PostAsJsonAsync(ProjectsUrl, new { code, name = "Original Name" });
        createResp.EnsureSuccessStatusCode();

        var created = await createResp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        var projectId = created.GetProperty("id").GetString()!;
        var version = created.GetProperty("version").GetInt32();

        client.DefaultRequestHeaders.Remove("If-Match");
        client.DefaultRequestHeaders.Add("If-Match", $"\"{version}\"");

        var updateResp = await client.PutAsJsonAsync($"{ProjectsUrl}/{projectId}",
            new { name = "Updated Name" });

        Assert.Equal(HttpStatusCode.OK, updateResp.StatusCode);
        Assert.NotNull(updateResp.Headers.ETag);

        var updated = await updateResp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.Equal("Updated Name", updated.GetProperty("name").GetString());
        Assert.Equal(version + 1, updated.GetProperty("version").GetInt32());
    }

    // ─── AC 5: PUT missing If-Match → 412 ────────────────────────────────────

    [Fact]
    public async Task UpdateProject_MissingIfMatch_Returns412()
    {
        var client = await CreateAuthClientAsync(_factory);
        var code = $"P12-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
        var createResp = await client.PostAsJsonAsync(ProjectsUrl, new { code, name = "For 412 Test" });
        createResp.EnsureSuccessStatusCode();

        var created = await createResp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        var projectId = created.GetProperty("id").GetString()!;

        // PUT without If-Match
        var updateClient = _factory.CreateClient();
        var token = await GetTokenAsync(updateClient);
        updateClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var updateResp = await updateClient.PutAsJsonAsync($"{ProjectsUrl}/{projectId}",
            new { name = "Should Fail" });
        Assert.Equal(HttpStatusCode.PreconditionFailed, updateResp.StatusCode);
    }

    // ─── AC 6: PUT stale If-Match → 409 + current state at root ──────────────

    [Fact]
    public async Task UpdateProject_StaleIfMatch_Returns409WithCurrentStateAtRoot()
    {
        var client = await CreateAuthClientAsync(_factory);
        var code = $"C09-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
        var createResp = await client.PostAsJsonAsync(ProjectsUrl, new { code, name = "Conflict Project" });
        createResp.EnsureSuccessStatusCode();

        var created = await createResp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        var projectId = created.GetProperty("id").GetString()!;
        var version = created.GetProperty("version").GetInt32();

        // Update once with correct version
        client.DefaultRequestHeaders.Remove("If-Match");
        client.DefaultRequestHeaders.Add("If-Match", $"\"{version}\"");
        var firstUpdate = await client.PutAsJsonAsync($"{ProjectsUrl}/{projectId}",
            new { name = "First Update" });
        firstUpdate.EnsureSuccessStatusCode();

        // Now use stale If-Match (version = original)
        var client2 = _factory.CreateClient();
        var token2 = await GetTokenAsync(client2);
        client2.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token2);
        client2.DefaultRequestHeaders.Add("If-Match", $"\"{version}\"");  // stale

        var conflictResp = await client2.PutAsJsonAsync($"{ProjectsUrl}/{projectId}",
            new { name = "Conflict Update" });

        Assert.Equal(HttpStatusCode.Conflict, conflictResp.StatusCode);
        var body = await conflictResp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);

        // AC 6: current và eTag phải ở ROOT level (không phải extensions.current)
        Assert.True(body.TryGetProperty("current", out var current), "Expected 'current' at root level");
        Assert.True(body.TryGetProperty("eTag", out _), "Expected 'eTag' at root level");
        Assert.True(current.TryGetProperty("version", out _), "'current' must have version field");
    }

    // ─── AC 7: DELETE valid If-Match → 204 ───────────────────────────────────

    [Fact]
    public async Task DeleteProject_ValidIfMatch_Returns204()
    {
        var client = await CreateAuthClientAsync(_factory);
        var code = $"DEL-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
        var createResp = await client.PostAsJsonAsync(ProjectsUrl, new { code, name = "To Delete" });
        createResp.EnsureSuccessStatusCode();

        var created = await createResp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        var projectId = created.GetProperty("id").GetString()!;
        var version = created.GetProperty("version").GetInt32();

        client.DefaultRequestHeaders.Remove("If-Match");
        client.DefaultRequestHeaders.Add("If-Match", $"\"{version}\"");
        var deleteResp = await client.DeleteAsync($"{ProjectsUrl}/{projectId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResp.StatusCode);
    }

    // ─── AC 8: DELETE missing If-Match → 412 ─────────────────────────────────

    [Fact]
    public async Task DeleteProject_MissingIfMatch_Returns412()
    {
        var client = await CreateAuthClientAsync(_factory);
        var code = $"D12-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
        var createResp = await client.PostAsJsonAsync(ProjectsUrl, new { code, name = "For Delete 412" });
        createResp.EnsureSuccessStatusCode();

        var created = await createResp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        var projectId = created.GetProperty("id").GetString()!;

        // DELETE without If-Match
        var delClient = _factory.CreateClient();
        var token = await GetTokenAsync(delClient);
        delClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var deleteResp = await delClient.DeleteAsync($"{ProjectsUrl}/{projectId}");
        Assert.Equal(HttpStatusCode.PreconditionFailed, deleteResp.StatusCode);
    }

    // ─── AC 9: DELETE stale If-Match → 409 ───────────────────────────────────

    [Fact]
    public async Task DeleteProject_StaleIfMatch_Returns409()
    {
        var client = await CreateAuthClientAsync(_factory);
        var code = $"D09-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
        var createResp = await client.PostAsJsonAsync(ProjectsUrl, new { code, name = "For Delete 409" });
        createResp.EnsureSuccessStatusCode();

        var created = await createResp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        var projectId = created.GetProperty("id").GetString()!;
        var version = created.GetProperty("version").GetInt32();

        // Update to bump version
        client.DefaultRequestHeaders.Add("If-Match", $"\"{version}\"");
        var updateResp = await client.PutAsJsonAsync($"{ProjectsUrl}/{projectId}",
            new { name = "Updated" });
        updateResp.EnsureSuccessStatusCode();

        // Now delete with stale version
        var delClient = _factory.CreateClient();
        var token = await GetTokenAsync(delClient);
        delClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        delClient.DefaultRequestHeaders.Add("If-Match", $"\"{version}\"");  // stale

        var deleteResp = await delClient.DeleteAsync($"{ProjectsUrl}/{projectId}");
        Assert.Equal(HttpStatusCode.Conflict, deleteResp.StatusCode);
    }

    // ─── Non-member PUT/DELETE → 404 ─────────────────────────────────────────

    [Fact]
    public async Task UpdateProject_NonExistentOrNonMember_Returns404()
    {
        var client = await CreateAuthClientAsync(_factory);
        client.DefaultRequestHeaders.Add("If-Match", "\"1\"");

        var response = await client.PutAsJsonAsync($"{ProjectsUrl}/{Guid.NewGuid()}",
            new { name = "NonExistent" });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteProject_NonExistentOrNonMember_Returns404()
    {
        var client = await CreateAuthClientAsync(_factory);
        client.DefaultRequestHeaders.Add("If-Match", "\"1\"");

        var response = await client.DeleteAsync($"{ProjectsUrl}/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ─── AC 12: All errors return ProblemDetails ──────────────────────────────

    [Fact]
    public async Task AllErrors_ReturnProblemDetailsFormat()
    {
        var client = await CreateAuthClientAsync(_factory);

        // 404
        var notFound = await client.GetAsync($"{ProjectsUrl}/{Guid.NewGuid()}");
        await AssertProblemDetails(notFound, 404);

        // 412
        var delClient = _factory.CreateClient();
        var token = await GetTokenAsync(delClient);
        delClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var precondFailed = await delClient.PutAsJsonAsync($"{ProjectsUrl}/{Guid.NewGuid()}",
            new { name = "Test" });
        Assert.Equal(HttpStatusCode.PreconditionFailed, precondFailed.StatusCode);
    }
}
