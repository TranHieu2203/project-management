using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ProjectManagement.Host.Tests;

/// <summary>
/// Integration tests for Story 8.1: Filter bar, GetTasksByProject filter params, GetMyTasks.
/// Covers: keyword, status, priority, nodeType, assignee/unassigned, overdue, date range,
/// ancestor inclusion, milestoneId subtree, and /api/v1/my-tasks endpoint.
/// </summary>
public sealed class TasksFilterTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    private const string SeedEmail    = "pm1@local.test";
    private const string SeedPassword = "P@ssw0rd!123";
    private const string LoginUrl     = "/api/v1/auth/login";
    private const string ProjectsUrl  = "/api/v1/projects";
    private const string MyTasksUrl   = "/api/v1/my-tasks";

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public TasksFilterTests(WebApplicationFactory<Program> factory) => _factory = factory;

    // ─── Helpers ────────────────────────────────────────────────────────────────

    private static async Task<string> GetTokenAsync(HttpClient client)
    {
        var resp = await client.PostAsJsonAsync(LoginUrl,
            new { email = SeedEmail, password = SeedPassword });
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        return body.GetProperty("accessToken").GetString()!;
    }

    private static async Task<HttpClient> CreateAuthClientAsync(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        var token  = await GetTokenAsync(client);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task<(string ProjectId, HttpClient Client)> CreateProjectAsync(
        WebApplicationFactory<Program> factory)
    {
        var client = await CreateAuthClientAsync(factory);
        var code   = $"FLT-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
        var resp   = await client.PostAsJsonAsync(ProjectsUrl,
            new { code, name = "Filter Test Project" });
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        return (body.GetProperty("id").GetString()!, client);
    }

    private static string TasksUrl(string projectId) => $"{ProjectsUrl}/{projectId}/tasks";

    private static async Task<string> CreateTaskAsync(
        HttpClient client, string projectId, object payload)
    {
        var resp = await client.PostAsJsonAsync(TasksUrl(projectId), payload);
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        return body.GetProperty("id").GetString()!;
    }

    private static async Task<JsonElement[]> GetFilteredTasksAsync(
        HttpClient client, string projectId, string query = "")
    {
        var url  = $"{TasksUrl(projectId)}{(query.Length > 0 ? "?" + query : "")}";
        var resp = await client.GetAsync(url);
        resp.EnsureSuccessStatusCode();
        var arr = await resp.Content.ReadFromJsonAsync<JsonElement[]>(JsonOpts);
        return arr ?? [];
    }

    // ─── No filter → returns all tasks ──────────────────────────────────────────

    [Fact]
    public async Task GetTasks_NoFilter_ReturnsAllTasks()
    {
        var (projectId, client) = await CreateProjectAsync(_factory);

        await CreateTaskAsync(client, projectId, new { type = "Task", name = "A", priority = "Low", status = "NotStarted", sortOrder = 1 });
        await CreateTaskAsync(client, projectId, new { type = "Task", name = "B", priority = "High", status = "InProgress", sortOrder = 2 });

        var tasks = await GetFilteredTasksAsync(client, projectId);
        Assert.True(tasks.Length >= 2);

        // No filter → IsFilterMatch should be null (not present or null)
        foreach (var t in tasks)
        {
            if (t.TryGetProperty("isFilterMatch", out var m))
                Assert.Equal(JsonValueKind.Null, m.ValueKind);
        }
    }

    // ─── Keyword filter ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetTasks_KeywordFilter_ReturnsOnlyMatchingTasks()
    {
        var (projectId, client) = await CreateProjectAsync(_factory);

        await CreateTaskAsync(client, projectId, new { type = "Task", name = "UniqueXYZ task", priority = "Low", status = "NotStarted", sortOrder = 1 });
        await CreateTaskAsync(client, projectId, new { type = "Task", name = "Another task",   priority = "Low", status = "NotStarted", sortOrder = 2 });

        var tasks = await GetFilteredTasksAsync(client, projectId, "q=UniqueXYZ");

        Assert.True(tasks.Length >= 1);
        Assert.All(tasks.Where(t => t.GetProperty("isFilterMatch").GetBoolean()),
            t => Assert.Contains("UniqueXYZ", t.GetProperty("name").GetString()!));
    }

    // ─── Status filter ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetTasks_StatusFilter_ReturnsMatchingStatus()
    {
        var (projectId, client) = await CreateProjectAsync(_factory);

        await CreateTaskAsync(client, projectId, new { type = "Task", name = "Task IP",  priority = "Low", status = "InProgress",  sortOrder = 1 });
        await CreateTaskAsync(client, projectId, new { type = "Task", name = "Task NS",  priority = "Low", status = "NotStarted",  sortOrder = 2 });

        var tasks = await GetFilteredTasksAsync(client, projectId, "status=InProgress");

        var matchingTasks = tasks.Where(t =>
            t.TryGetProperty("isFilterMatch", out var m) && m.ValueKind == JsonValueKind.True
        ).ToArray();

        Assert.All(matchingTasks, t =>
            Assert.Equal("InProgress", t.GetProperty("status").GetString()));
    }

    // ─── Priority filter ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetTasks_PriorityFilter_ReturnsOnlyHighPriority()
    {
        var (projectId, client) = await CreateProjectAsync(_factory);

        await CreateTaskAsync(client, projectId, new { type = "Task", name = "High P",   priority = "High",   status = "NotStarted", sortOrder = 1 });
        await CreateTaskAsync(client, projectId, new { type = "Task", name = "Low P",    priority = "Low",    status = "NotStarted", sortOrder = 2 });
        await CreateTaskAsync(client, projectId, new { type = "Task", name = "Medium P", priority = "Medium", status = "NotStarted", sortOrder = 3 });

        var tasks = await GetFilteredTasksAsync(client, projectId, "priority=High");

        var matched = tasks.Where(t =>
            t.TryGetProperty("isFilterMatch", out var m) && m.ValueKind == JsonValueKind.True
        ).ToArray();

        Assert.True(matched.Length >= 1);
        Assert.All(matched, t => Assert.Equal("High", t.GetProperty("priority").GetString()));
    }

    // ─── NodeType filter ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetTasks_NodeTypeFilter_ReturnsOnlyMatchingType()
    {
        var (projectId, client) = await CreateProjectAsync(_factory);

        var phaseId = await CreateTaskAsync(client, projectId,
            new { type = "Phase", name = "Phase 1", priority = "Low", status = "NotStarted", sortOrder = 1 });
        await CreateTaskAsync(client, projectId,
            new { type = "Milestone", name = "MS 1", parentId = phaseId, priority = "Low", status = "NotStarted", sortOrder = 1 });

        var tasks = await GetFilteredTasksAsync(client, projectId, "type=Milestone");

        var matched = tasks.Where(t =>
            t.TryGetProperty("isFilterMatch", out var m) && m.ValueKind == JsonValueKind.True
        ).ToArray();

        Assert.True(matched.Length >= 1);
        Assert.All(matched, t => Assert.Equal("Milestone", t.GetProperty("type").GetString()));
    }

    // ─── Unassigned filter ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetTasks_UnassignedFilter_ReturnsOnlyUnassignedTasks()
    {
        var (projectId, client) = await CreateProjectAsync(_factory);

        await CreateTaskAsync(client, projectId,
            new { type = "Task", name = "Assigned task",   priority = "Low", status = "NotStarted", sortOrder = 1 });
        await CreateTaskAsync(client, projectId,
            new { type = "Task", name = "Unassigned task", priority = "Low", status = "NotStarted", sortOrder = 2 });

        var tasks = await GetFilteredTasksAsync(client, projectId, "unassigned=true");

        var matched = tasks.Where(t =>
            t.TryGetProperty("isFilterMatch", out var m) && m.ValueKind == JsonValueKind.True
        ).ToArray();

        Assert.True(matched.Length >= 1);
        Assert.All(matched, t =>
            Assert.Equal(JsonValueKind.Null, t.GetProperty("assigneeUserId").ValueKind));
    }

    // ─── Ancestor inclusion ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetTasks_FilterWithAncestors_IncludesParentAsContext()
    {
        var (projectId, client) = await CreateProjectAsync(_factory);

        // Phase → Task chain
        var phaseId = await CreateTaskAsync(client, projectId,
            new { type = "Phase", name = "Parent Phase", priority = "Low", status = "NotStarted", sortOrder = 1 });
        await CreateTaskAsync(client, projectId,
            new { type = "Task", name = "ChildTaskUNIQUE", parentId = phaseId, priority = "High", status = "InProgress", sortOrder = 1 });

        var tasks = await GetFilteredTasksAsync(client, projectId, "q=ChildTaskUNIQUE");

        // Should contain the match (isFilterMatch=true) AND the parent phase (isFilterMatch=false)
        var matchedTask   = tasks.FirstOrDefault(t =>
            t.TryGetProperty("isFilterMatch", out var m) && m.ValueKind == JsonValueKind.True);
        var contextParent = tasks.FirstOrDefault(t =>
            t.TryGetProperty("isFilterMatch", out var m) && m.ValueKind == JsonValueKind.False);

        Assert.NotNull(matchedTask);
        Assert.NotNull(contextParent);
        Assert.Equal("ChildTaskUNIQUE", matchedTask.Value.GetProperty("name").GetString());
        Assert.Equal(phaseId, contextParent.Value.GetProperty("id").GetString());
    }

    // ─── includeAncestors=false ──────────────────────────────────────────────────

    [Fact]
    public async Task GetTasks_IncludeAncestorsFalse_ExcludesParentContext()
    {
        var (projectId, client) = await CreateProjectAsync(_factory);

        var phaseId = await CreateTaskAsync(client, projectId,
            new { type = "Phase", name = "Phase Ancestor", priority = "Low", status = "NotStarted", sortOrder = 1 });
        await CreateTaskAsync(client, projectId,
            new { type = "Task", name = "TaskNoAncestor", parentId = phaseId, priority = "High", status = "InProgress", sortOrder = 1 });

        var tasks = await GetFilteredTasksAsync(client, projectId, "q=TaskNoAncestor&includeAncestors=false");

        // Should NOT include parent — only the matching task
        Assert.All(tasks, t =>
        {
            var isMatch = t.GetProperty("isFilterMatch");
            Assert.NotEqual(JsonValueKind.False, isMatch.ValueKind);
        });
        Assert.True(tasks.Length >= 1);
    }

    // ─── Overdue filter ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetTasks_OverdueFilter_ReturnsOnlyOverdueTasks()
    {
        var (projectId, client) = await CreateProjectAsync(_factory);

        // Past date → overdue
        await CreateTaskAsync(client, projectId, new
        {
            type = "Task", name = "Overdue task", priority = "High",
            status = "InProgress", sortOrder = 1,
            plannedEndDate = "2020-01-01"
        });
        // Future date → not overdue
        await CreateTaskAsync(client, projectId, new
        {
            type = "Task", name = "Future task", priority = "Low",
            status = "NotStarted", sortOrder = 2,
            plannedEndDate = "2030-12-31"
        });

        var tasks = await GetFilteredTasksAsync(client, projectId, "overdue=true");

        var matched = tasks.Where(t =>
            t.TryGetProperty("isFilterMatch", out var m) && m.ValueKind == JsonValueKind.True
        ).ToArray();

        Assert.True(matched.Length >= 1);
        Assert.All(matched, t =>
        {
            var endDate = t.GetProperty("plannedEndDate").GetString();
            Assert.True(string.Compare(endDate, DateTime.UtcNow.ToString("yyyy-MM-dd"),
                StringComparison.Ordinal) < 0);
        });
    }

    // ─── Date range filter ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetTasks_DateRangeFilter_ReturnsTasksInRange()
    {
        var (projectId, client) = await CreateProjectAsync(_factory);

        await CreateTaskAsync(client, projectId, new
        {
            type = "Task", name = "InRange",  priority = "Low",
            status = "NotStarted", sortOrder = 1, plannedEndDate = "2026-06-15"
        });
        await CreateTaskAsync(client, projectId, new
        {
            type = "Task", name = "OutRange", priority = "Low",
            status = "NotStarted", sortOrder = 2, plannedEndDate = "2026-12-31"
        });

        var tasks = await GetFilteredTasksAsync(client, projectId, "dateFrom=2026-06-01&dateTo=2026-06-30");

        var matched = tasks.Where(t =>
            t.TryGetProperty("isFilterMatch", out var m) && m.ValueKind == JsonValueKind.True
        ).ToArray();

        Assert.True(matched.Length >= 1);
        Assert.All(matched, t =>
        {
            var d = t.GetProperty("plannedEndDate").GetString()!;
            Assert.True(string.Compare(d, "2026-06-01", StringComparison.Ordinal) >= 0);
            Assert.True(string.Compare(d, "2026-06-30", StringComparison.Ordinal) <= 0);
        });
    }

    // ─── GetMyTasks returns assigned tasks ───────────────────────────────────────

    [Fact]
    public async Task GetMyTasks_ReturnsTasksAssignedToCurrentUser()
    {
        var client = await CreateAuthClientAsync(_factory);

        var resp = await client.GetAsync(MyTasksUrl);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var tasks = await resp.Content.ReadFromJsonAsync<JsonElement[]>(JsonOpts);
        Assert.NotNull(tasks);

        // Verify response shape: each element has projectId, projectName, etc.
        if (tasks.Length > 0)
        {
            var first = tasks[0];
            Assert.True(first.TryGetProperty("id", out _));
            Assert.True(first.TryGetProperty("projectId", out _));
            Assert.True(first.TryGetProperty("projectName", out _));
            Assert.True(first.TryGetProperty("name", out _));
            Assert.True(first.TryGetProperty("status", out _));
        }
    }

    // ─── GetMyTasks keyword filter ───────────────────────────────────────────────

    [Fact]
    public async Task GetMyTasks_KeywordFilter_AppliesSearch()
    {
        var client = await CreateAuthClientAsync(_factory);

        var resp = await client.GetAsync($"{MyTasksUrl}?q=nonexistentxyz99");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var tasks = await resp.Content.ReadFromJsonAsync<JsonElement[]>(JsonOpts);
        Assert.NotNull(tasks);
        Assert.Empty(tasks);
    }

    // ─── GetMyTasks returns 401 without auth ─────────────────────────────────────

    [Fact]
    public async Task GetMyTasks_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();
        var resp   = await client.GetAsync(MyTasksUrl);
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }
}
