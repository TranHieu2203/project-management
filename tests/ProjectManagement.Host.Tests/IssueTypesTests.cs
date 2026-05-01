using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectManagement.Projects.Infrastructure.Persistence;

namespace ProjectManagement.Host.Tests;

public sealed class IssueTypesTests : IClassFixture<TestHostFactory>
{
    private readonly TestHostFactory _factory;

    private const string SeedEmail = "pm1@local.test";
    private const string SeedPassword = "P@ssw0rd!123";
    private const string LoginUrl = "/api/v1/auth/login";
    private const string ProjectsUrl = "/api/v1/projects";
    private const string IssueTypesUrl = "/api/v1/issue-types";

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public IssueTypesTests(TestHostFactory factory) => _factory = factory;

    private static async Task<string> GetTokenAsync(HttpClient client)
    {
        var resp = await client.PostAsJsonAsync(LoginUrl, new { email = SeedEmail, password = SeedPassword });
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        return body.GetProperty("accessToken").GetString()!;
    }

    private static async Task<HttpClient> CreateAuthClientAsync(TestHostFactory factory)
    {
        var client = factory.CreateClient();
        var token = await GetTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static string ProjectIssueTypesUrl(string projectId) => $"{ProjectsUrl}/{projectId}/issue-types";

    private static async Task<string> CreateProjectAndGetIdAsync(TestHostFactory factory, HttpClient client)
    {
        var code = $"IT-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
        var resp = await client.PostAsJsonAsync(ProjectsUrl, new { code, name = "IssueTypes Test Project" });
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        return body.GetProperty("id").GetString()!;
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

    [Fact]
    public async Task GetBuiltInIssueTypes_NoJwt_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync(IssueTypesUrl);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetBuiltInIssueTypes_Returns5()
    {
        var client = await CreateAuthClientAsync(_factory);
        var response = await client.GetAsync(IssueTypesUrl);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var items = await response.Content.ReadFromJsonAsync<JsonElement[]>(JsonOpts);
        Assert.NotNull(items);
        Assert.Equal(5, items.Length);
    }

    [Fact]
    public async Task CreateCustomIssueType_ForProject_Returns201()
    {
        var client = await CreateAuthClientAsync(_factory);
        var projectId = await CreateProjectAndGetIdAsync(_factory, client);

        var response = await client.PostAsJsonAsync(ProjectIssueTypesUrl(projectId), new
        {
            name = "Risk",
            iconKey = "risk",
            color = "#123ABC"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.Equal("Risk", created.GetProperty("name").GetString());
        Assert.Equal("#123ABC", created.GetProperty("color").GetString());
        Assert.Equal(projectId, created.GetProperty("projectId").GetString());
    }

    [Fact]
    public async Task DeleteCustomIssueType_InUse_Returns409ProblemDetails()
    {
        var client = await CreateAuthClientAsync(_factory);
        var projectId = await CreateProjectAndGetIdAsync(_factory, client);

        // Create custom issue type
        var createType = await client.PostAsJsonAsync(ProjectIssueTypesUrl(projectId), new
        {
            name = "Risk2",
            iconKey = "risk",
            color = "#654321"
        });
        createType.EnsureSuccessStatusCode();
        var createdType = await createType.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        var typeId = createdType.GetProperty("id").GetString()!;

        // Create a task (issue) via existing endpoint
        var createTask = await client.PostAsJsonAsync($"{ProjectsUrl}/{projectId}/tasks", new
        {
            type = "Task",
            name = "Issue for IssueType",
            priority = "Low",
            status = "NotStarted",
            sortOrder = 1
        });
        createTask.EnsureSuccessStatusCode();
        var createdTask = await createTask.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        var taskId = createdTask.GetProperty("id").GetString()!;

        // Mark the issue as using the custom issue type by updating DB directly (test-only)
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ProjectsDbContext>();
            var entityId = Guid.Parse(taskId);
            var entity = await db.Issues.FirstAsync(x => x.Id == entityId);
            db.Entry(entity).Property<Guid?>("IssueTypeId").CurrentValue = Guid.Parse(typeId);
            await db.SaveChangesAsync();
        }

        // Attempt delete → 409
        var deleteResp = await client.DeleteAsync($"{ProjectIssueTypesUrl(projectId)}/{typeId}");
        Assert.Equal(HttpStatusCode.Conflict, deleteResp.StatusCode);
        await AssertProblemDetails(deleteResp, 409);
    }
}

