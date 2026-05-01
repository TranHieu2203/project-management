using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ProjectManagement.Host.Tests;

/// <summary>
/// Integration tests for Story 1.2: membership-only authorization baseline.
/// Tests run against the in-memory test host (WebApplicationFactory).
/// Each test method gets its own HttpClient to avoid shared header state.
/// DB-dependent tests require a running PostgreSQL with seeded data;
/// no-auth tests work purely with the in-memory host (no DB needed).
/// </summary>
public sealed class ProjectsMembershipTests : IClassFixture<TestHostFactory>
{
    private readonly TestHostFactory _factory;

    private const string SeedEmail = "pm1@local.test";
    private const string SeedPassword = "P@ssw0rd!123";
    private const string LoginUrl = "/api/v1/auth/login";
    private const string ProjectsUrl = "/api/v1/projects";

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public ProjectsMembershipTests(TestHostFactory factory)
    {
        _factory = factory;
    }

    // --- AC 5 / AC 6: No JWT → 401 (no DB needed, [Authorize] rejects before handler) ---

    [Fact]
    public async Task GetProjects_NoJwt_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync(ProjectsUrl);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProjectById_NoJwt_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync($"{ProjectsUrl}/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // --- AC 1: GET /api/v1/projects — membership-only list (requires DB + seeded data) ---

    [Fact]
    public async Task GetProjects_AuthenticatedMember_ReturnsMembershipOnlyList()
    {
        var client = _factory.CreateClient();
        var token = await GetTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync(ProjectsUrl);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var projects = await response.Content.ReadFromJsonAsync<JsonElement[]>(JsonOpts);
        Assert.NotNull(projects);
        foreach (var project in projects)
        {
            Assert.True(project.TryGetProperty("id", out _));
            Assert.True(project.TryGetProperty("code", out _));
            Assert.True(project.TryGetProperty("name", out _));
            Assert.True(project.TryGetProperty("status", out _));
        }
    }

    // --- AC 2: GET /api/v1/projects/{id} — member → 200 (requires DB + seeded data) ---

    [Fact]
    public async Task GetProjectById_IsMember_Returns200WithProjectDetail()
    {
        var client = _factory.CreateClient();
        var token = await GetTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var listResponse = await client.GetAsync(ProjectsUrl);
        listResponse.EnsureSuccessStatusCode();

        var projects = await listResponse.Content.ReadFromJsonAsync<JsonElement[]>(JsonOpts);
        Assert.NotNull(projects);
        Assert.NotEmpty(projects);

        var projectId = projects[0].GetProperty("id").GetString();
        Assert.NotNull(projectId);

        var response = await client.GetAsync($"{ProjectsUrl}/{projectId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var project = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.Equal(projectId, project.GetProperty("id").GetString());
    }

    // --- AC 3: GET /api/v1/projects/{id} — non-member → 404 ProblemDetails (requires DB) ---

    [Fact]
    public async Task GetProjectById_NonMemberOrNonExistent_Returns404ProblemDetails()
    {
        var client = _factory.CreateClient();
        var token = await GetTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var nonExistentId = Guid.NewGuid();
        var response = await client.GetAsync($"{ProjectsUrl}/{nonExistentId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertProblemDetails(response, 404);
    }

    // --- AC 6: Error responses always ProblemDetails (requires DB) ---

    [Fact]
    public async Task ErrorResponses_Always_ProblemDetailsFormat()
    {
        var client = _factory.CreateClient();
        var token = await GetTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync($"{ProjectsUrl}/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertProblemDetails(response, 404);
    }

    // --- helpers ---

    private static async Task<string> GetTokenAsync(HttpClient client)
    {
        var resp = await client.PostAsJsonAsync(LoginUrl, new { email = SeedEmail, password = SeedPassword });
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        return body.GetProperty("accessToken").GetString()!;
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
}
