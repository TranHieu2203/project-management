using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ProjectManagement.Host.Tests;

/// <summary>
/// Integration tests for Story 9.2: Stat Cards and Upcoming Deadlines endpoints.
/// Covers: 401 unauthenticated guard, correct shape of responses, overdue count logic,
/// upcoming deadlines filtering, and empty-membership path.
/// </summary>
public sealed class DashboardTests : IClassFixture<TestHostFactory>
{
    private readonly TestHostFactory _factory;

    private const string SeedEmail    = "pm1@local.test";
    private const string SeedPassword = "P@ssw0rd!123";
    private const string LoginUrl     = "/api/v1/auth/login";
    private const string ProjectsUrl  = "/api/v1/projects";
    private const string StatCardsUrl = "/api/v1/dashboard/stat-cards";
    private const string DeadlinesUrl = "/api/v1/dashboard/deadlines";

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public DashboardTests(TestHostFactory factory) => _factory = factory;

    // ─── Helpers ────────────────────────────────────────────────────────────────

    private static async Task<string> GetTokenAsync(HttpClient client)
    {
        var resp = await client.PostAsJsonAsync(LoginUrl,
            new { email = SeedEmail, password = SeedPassword });
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        return body.GetProperty("accessToken").GetString()!;
    }

    private static async Task<HttpClient> CreateAuthClientAsync(TestHostFactory factory)
    {
        var client = factory.CreateClient();
        var token  = await GetTokenAsync(client);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task<(string ProjectId, HttpClient Client)> CreateProjectAsync(
        TestHostFactory factory)
    {
        var client = await CreateAuthClientAsync(factory);
        var code   = $"DSH-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
        var resp   = await client.PostAsJsonAsync(ProjectsUrl,
            new { code, name = "Dashboard Test Project" });
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        return (body.GetProperty("id").GetString()!, client);
    }

    private static string TasksUrl(string projectId) =>
        $"{ProjectsUrl}/{projectId}/tasks";

    private static async Task<string> CreateTaskAsync(
        HttpClient client,
        string projectId,
        string name,
        string plannedEndDate,
        string status = "NotStarted",
        string type = "Task")
    {
        var resp = await client.PostAsJsonAsync(
            TasksUrl(projectId),
            new
            {
                name,
                type,
                priority    = "Medium",
                status,
                plannedEndDate
            });
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        return body.GetProperty("id").GetString()!;
    }

    // ─── Stat Cards ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task StatCards_Returns401_WhenNotAuthenticated()
    {
        var client = _factory.CreateClient();
        var resp   = await client.GetAsync(StatCardsUrl);
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task StatCards_Returns200_WithCorrectShape()
    {
        var client = await CreateAuthClientAsync(_factory);
        var resp   = await client.GetAsync(StatCardsUrl);
        resp.EnsureSuccessStatusCode();

        var body = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.True(body.TryGetProperty("overdueTaskCount", out _),
            "Response must have overdueTaskCount");
        Assert.True(body.TryGetProperty("atRiskProjectCount", out _),
            "Response must have atRiskProjectCount");
        Assert.True(body.TryGetProperty("overloadedResourceCount", out _),
            "Response must have overloadedResourceCount");
    }

    [Fact]
    public async Task StatCards_OverloadedResourceCount_IsAlwaysZero()
    {
        var client = await CreateAuthClientAsync(_factory);
        var resp   = await client.GetAsync(StatCardsUrl);
        resp.EnsureSuccessStatusCode();

        var body  = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        var count = body.GetProperty("overloadedResourceCount").GetInt32();
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task StatCards_OverdueCount_IncludesTasksPastDue()
    {
        var (projectId, client) = await CreateProjectAsync(_factory);
        var pastDate = DateTime.UtcNow.AddDays(-5).ToString("yyyy-MM-dd");

        // Create 2 overdue tasks
        await CreateTaskAsync(client, projectId, "Overdue task A", pastDate);
        await CreateTaskAsync(client, projectId, "Overdue task B", pastDate);

        var resp   = await client.GetAsync(StatCardsUrl);
        resp.EnsureSuccessStatusCode();

        var body  = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        var count = body.GetProperty("overdueTaskCount").GetInt32();
        Assert.True(count >= 2, $"Expected at least 2 overdue tasks, got {count}");
    }

    [Fact]
    public async Task StatCards_OverdueCount_ExcludesCompletedTasks()
    {
        var (projectId, client) = await CreateProjectAsync(_factory);
        var pastDate = DateTime.UtcNow.AddDays(-3).ToString("yyyy-MM-dd");

        // Get baseline
        var baseResp  = await client.GetAsync(StatCardsUrl);
        var baseBody  = await baseResp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        var baseCount = baseBody.GetProperty("overdueTaskCount").GetInt32();

        // Completed task should NOT count as overdue
        await CreateTaskAsync(client, projectId, "Completed past task", pastDate, status: "Completed");

        var resp  = await client.GetAsync(StatCardsUrl);
        var body  = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        var count = body.GetProperty("overdueTaskCount").GetInt32();
        Assert.Equal(baseCount, count);
    }

    // ─── Deadlines ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Deadlines_Returns401_WhenNotAuthenticated()
    {
        var client = _factory.CreateClient();
        var resp   = await client.GetAsync(DeadlinesUrl);
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Deadlines_Returns200_WithArrayShape()
    {
        var client = await CreateAuthClientAsync(_factory);
        var resp   = await client.GetAsync(DeadlinesUrl);
        resp.EnsureSuccessStatusCode();

        var body = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.Equal(JsonValueKind.Array, body.ValueKind);
    }

    [Fact]
    public async Task Deadlines_IncludesTasksDueWithin7Days()
    {
        var (projectId, client) = await CreateProjectAsync(_factory);
        var soon = DateTime.UtcNow.AddDays(3).ToString("yyyy-MM-dd");

        await CreateTaskAsync(client, projectId, "Soon task", soon);

        var resp = await client.GetAsync(DeadlinesUrl);
        resp.EnsureSuccessStatusCode();

        var body = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.Equal(JsonValueKind.Array, body.ValueKind);

        bool found = false;
        foreach (var item in body.EnumerateArray())
        {
            if (item.TryGetProperty("name", out var nameProp) &&
                nameProp.GetString() == "Soon task")
            {
                found = true;
                // Verify required fields present
                Assert.True(item.TryGetProperty("taskId", out _));
                Assert.True(item.TryGetProperty("projectId", out _));
                Assert.True(item.TryGetProperty("projectName", out _));
                Assert.True(item.TryGetProperty("entityType", out _));
                Assert.True(item.TryGetProperty("dueDate", out _));
                Assert.True(item.TryGetProperty("daysRemaining", out _));
                break;
            }
        }
        Assert.True(found, "Expected to find 'Soon task' in deadlines response");
    }

    [Fact]
    public async Task Deadlines_ExcludesTasksBeyond7Days()
    {
        var (projectId, client) = await CreateProjectAsync(_factory);
        var farFuture = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd");

        await CreateTaskAsync(client, projectId, "Far future task", farFuture);

        var resp = await client.GetAsync(DeadlinesUrl);
        resp.EnsureSuccessStatusCode();

        var body = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        foreach (var item in body.EnumerateArray())
        {
            if (item.TryGetProperty("name", out var nameProp) &&
                nameProp.GetString() == "Far future task")
            {
                Assert.Fail("Far future task (30 days out) should not appear in 7-day deadlines");
            }
        }
    }

    [Fact]
    public async Task Deadlines_ExcludesCompletedTasks()
    {
        var (projectId, client) = await CreateProjectAsync(_factory);
        var soon = DateTime.UtcNow.AddDays(2).ToString("yyyy-MM-dd");

        await CreateTaskAsync(client, projectId, "Completed soon task", soon, status: "Completed");

        var resp = await client.GetAsync(DeadlinesUrl);
        resp.EnsureSuccessStatusCode();

        var body = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        foreach (var item in body.EnumerateArray())
        {
            if (item.TryGetProperty("name", out var nameProp) &&
                nameProp.GetString() == "Completed soon task")
            {
                Assert.Fail("Completed task should not appear in deadlines");
            }
        }
    }

    [Fact]
    public async Task Deadlines_ReturnsAtMost7Items()
    {
        var (projectId, client) = await CreateProjectAsync(_factory);
        var soon = DateTime.UtcNow.AddDays(4).ToString("yyyy-MM-dd");

        for (int i = 1; i <= 10; i++)
            await CreateTaskAsync(client, projectId, $"Deadline task {i}", soon);

        var resp = await client.GetAsync(DeadlinesUrl);
        resp.EnsureSuccessStatusCode();

        var body  = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        var count = body.GetArrayLength();
        Assert.True(count <= 7, $"Expected at most 7 deadlines, got {count}");
    }

    [Fact]
    public async Task Deadlines_DaysAheadParam_FiltersCorrectly()
    {
        var (projectId, client) = await CreateProjectAsync(_factory);
        // Task due in 10 days should be excluded with daysAhead=7 but included with daysAhead=14
        var date10 = DateTime.UtcNow.AddDays(10).ToString("yyyy-MM-dd");

        await CreateTaskAsync(client, projectId, "10-day task", date10);

        // With daysAhead=7: should NOT include
        var resp7 = await client.GetAsync($"{DeadlinesUrl}?daysAhead=7&projectIds={projectId}");
        resp7.EnsureSuccessStatusCode();
        var body7 = await resp7.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        bool in7 = false;
        foreach (var item in body7.EnumerateArray())
        {
            if (item.TryGetProperty("name", out var n) && n.GetString() == "10-day task")
                in7 = true;
        }

        // With daysAhead=14: should include
        var resp14 = await client.GetAsync($"{DeadlinesUrl}?daysAhead=14&projectIds={projectId}");
        resp14.EnsureSuccessStatusCode();
        var body14 = await resp14.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        bool in14 = false;
        foreach (var item in body14.EnumerateArray())
        {
            if (item.TryGetProperty("name", out var n) && n.GetString() == "10-day task")
                in14 = true;
        }

        Assert.False(in7, "10-day task should not appear with daysAhead=7");
        Assert.True(in14, "10-day task should appear with daysAhead=14");
    }
}
