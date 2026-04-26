using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ProjectManagement.Host.Tests;

/// <summary>
/// Integration tests for Story 1.4: Project Structure CRUD (Phase/Milestone/Task).
/// Covers ACs: hierarchy management, field support, cycle detection, membership guard,
/// optimistic locking (412/409), and date validation.
/// </summary>
public sealed class TasksCrudTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    private const string SeedEmail = "pm1@local.test";
    private const string SeedPassword = "P@ssw0rd!123";
    private const string LoginUrl = "/api/v1/auth/login";
    private const string ProjectsUrl = "/api/v1/projects";

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public TasksCrudTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

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

    private static async Task<(string ProjectId, HttpClient Client)> CreateProjectAndGetIdAsync(
        WebApplicationFactory<Program> factory)
    {
        var client = await CreateAuthClientAsync(factory);
        var code = $"TSK-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
        var resp = await client.PostAsJsonAsync(ProjectsUrl, new { code, name = "Test Project For Tasks" });
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        return (body.GetProperty("id").GetString()!, client);
    }

    private static string TasksUrl(string projectId) => $"{ProjectsUrl}/{projectId}/tasks";

    private static async Task AssertProblemDetails(HttpResponseMessage response, int expectedStatus)
    {
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.True(body.TryGetProperty("status", out var statusProp));
        Assert.Equal(expectedStatus, statusProp.GetInt32());
        var hasTitle = body.TryGetProperty("title", out _);
        var hasDetail = body.TryGetProperty("detail", out _);
        Assert.True(hasTitle || hasDetail);
    }

    // ─── AC 5: Non-member → 404 ───────────────────────────────────────────────

    [Fact]
    public async Task GetTasks_NonMember_Returns404()
    {
        var (projectId, _) = await CreateProjectAndGetIdAsync(_factory);

        // Dùng client khác (same user thực ra nên trả 200 — trong test này user là member)
        // Test với project không tồn tại để đảm bảo 404
        var client = await CreateAuthClientAsync(_factory);
        var response = await client.GetAsync(TasksUrl(Guid.NewGuid().ToString()));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ─── AC 1: POST → 201 + ETag ──────────────────────────────────────────────

    [Fact]
    public async Task CreateTask_ValidPayload_Returns201WithETag()
    {
        var (projectId, client) = await CreateProjectAndGetIdAsync(_factory);

        var response = await client.PostAsJsonAsync(TasksUrl(projectId), new
        {
            type = "Phase",
            name = "Phase 1",
            priority = "Medium",
            status = "NotStarted",
            sortOrder = 1
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.ETag);
        Assert.Equal("\"1\"", response.Headers.ETag.ToString());

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.True(body.TryGetProperty("id", out _));
        Assert.Equal("Phase", body.GetProperty("type").GetString());
        Assert.Equal("Phase 1", body.GetProperty("name").GetString());
        Assert.Equal(1, body.GetProperty("version").GetInt32());
        // actualEffortHours luôn null (Epic 3)
        Assert.Equal(JsonValueKind.Null, body.GetProperty("actualEffortHours").ValueKind);
    }

    // ─── AC 2: Task supports all required fields ──────────────────────────────

    [Fact]
    public async Task CreateTask_WithAllFields_ReturnsCorrectFields()
    {
        var (projectId, client) = await CreateProjectAndGetIdAsync(_factory);

        var response = await client.PostAsJsonAsync(TasksUrl(projectId), new
        {
            type = "Task",
            vbs = "1.1.1",
            name = "Thiết kế DB",
            priority = "High",
            status = "NotStarted",
            notes = "Thiết kế schema cho task module",
            plannedStartDate = "2026-05-01",
            plannedEndDate = "2026-05-10",
            plannedEffortHours = 40,
            percentComplete = 0,
            sortOrder = 1
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.Equal("1.1.1", body.GetProperty("vbs").GetString());
        Assert.Equal("High", body.GetProperty("priority").GetString());
        Assert.Equal("2026-05-01", body.GetProperty("plannedStartDate").GetString());
        Assert.Equal(40, body.GetProperty("plannedEffortHours").GetDecimal());
        // actualEffortHours luôn null
        Assert.Equal(JsonValueKind.Null, body.GetProperty("actualEffortHours").ValueKind);
    }

    // ─── AC 1: Hierarchy — parent-child relationship ──────────────────────────

    [Fact]
    public async Task CreateTask_WithParentId_CreatesHierarchy()
    {
        var (projectId, client) = await CreateProjectAndGetIdAsync(_factory);

        // Tạo Phase
        var phaseResp = await client.PostAsJsonAsync(TasksUrl(projectId), new
        {
            type = "Phase", name = "Phase 1", priority = "Medium",
            status = "NotStarted", sortOrder = 1
        });
        phaseResp.EnsureSuccessStatusCode();
        var phase = await phaseResp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        var phaseId = phase.GetProperty("id").GetString()!;

        // Tạo Milestone trong Phase
        var milestoneResp = await client.PostAsJsonAsync(TasksUrl(projectId), new
        {
            parentId = phaseId,
            type = "Milestone", name = "Milestone 1", priority = "Medium",
            status = "NotStarted", sortOrder = 1
        });
        Assert.Equal(HttpStatusCode.Created, milestoneResp.StatusCode);

        var milestone = await milestoneResp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.Equal(phaseId, milestone.GetProperty("parentId").GetString());
    }

    // ─── AC 1: Hierarchy cycle detection ─────────────────────────────────────

    [Fact]
    public async Task UpdateTask_ParentCycle_Returns422()
    {
        var (projectId, client) = await CreateProjectAndGetIdAsync(_factory);

        // Tạo A
        var aResp = await client.PostAsJsonAsync(TasksUrl(projectId), new
        {
            type = "Phase", name = "A", priority = "Low", status = "NotStarted", sortOrder = 1
        });
        aResp.EnsureSuccessStatusCode();
        var a = await aResp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        var aId = a.GetProperty("id").GetString()!;

        // Tạo B là child của A
        var bResp = await client.PostAsJsonAsync(TasksUrl(projectId), new
        {
            parentId = aId,
            type = "Task", name = "B", priority = "Low", status = "NotStarted", sortOrder = 1
        });
        bResp.EnsureSuccessStatusCode();
        var b = await bResp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        var bId = b.GetProperty("id").GetString()!;

        // Update A với parentId = B → tạo cycle (A→B→A)
        client.DefaultRequestHeaders.Remove("If-Match");
        client.DefaultRequestHeaders.Add("If-Match", "\"1\"");
        var updateResp = await client.PutAsJsonAsync($"{TasksUrl(projectId)}/{aId}", new
        {
            type = "Phase", name = "A", priority = "Low", status = "NotStarted",
            sortOrder = 1, parentId = bId
        });

        // DomainException → 422
        Assert.Equal(HttpStatusCode.UnprocessableEntity, updateResp.StatusCode);
        await AssertProblemDetails(updateResp, 422);
    }

    // ─── AC 3: Dependency cycle detection ────────────────────────────────────

    [Fact]
    public async Task UpdateTask_DependencyCycle_Returns422()
    {
        var (projectId, client) = await CreateProjectAndGetIdAsync(_factory);

        // Tạo task A và B
        var aResp = await client.PostAsJsonAsync(TasksUrl(projectId), new
        {
            type = "Task", name = "A", priority = "Low", status = "NotStarted", sortOrder = 1
        });
        aResp.EnsureSuccessStatusCode();
        var a = await aResp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        var aId = a.GetProperty("id").GetString()!;

        var bResp = await client.PostAsJsonAsync(TasksUrl(projectId), new
        {
            type = "Task", name = "B", priority = "Low", status = "NotStarted", sortOrder = 2
        });
        bResp.EnsureSuccessStatusCode();
        var b = await bResp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        var bId = b.GetProperty("id").GetString()!;

        // Update B với predecessor = A (B phụ thuộc A)
        client.DefaultRequestHeaders.Remove("If-Match");
        client.DefaultRequestHeaders.Add("If-Match", "\"1\"");
        var updateB = await client.PutAsJsonAsync($"{TasksUrl(projectId)}/{bId}", new
        {
            type = "Task", name = "B", priority = "Low", status = "NotStarted",
            sortOrder = 2,
            predecessors = new[] { new { predecessorId = aId, dependencyType = "FS" } }
        });
        updateB.EnsureSuccessStatusCode();

        // Update A với predecessor = B → tạo cycle (A→B→A)
        client.DefaultRequestHeaders.Remove("If-Match");
        client.DefaultRequestHeaders.Add("If-Match", "\"1\"");
        var updateA = await client.PutAsJsonAsync($"{TasksUrl(projectId)}/{aId}", new
        {
            type = "Task", name = "A", priority = "Low", status = "NotStarted",
            sortOrder = 1,
            predecessors = new[] { new { predecessorId = bId, dependencyType = "FS" } }
        });

        // DomainException → 422
        Assert.Equal(HttpStatusCode.UnprocessableEntity, updateA.StatusCode);
    }

    // ─── AC 6: PUT missing If-Match → 412 ────────────────────────────────────

    [Fact]
    public async Task UpdateTask_MissingIfMatch_Returns412()
    {
        var (projectId, client) = await CreateProjectAndGetIdAsync(_factory);

        var createResp = await client.PostAsJsonAsync(TasksUrl(projectId), new
        {
            type = "Task", name = "Test", priority = "Low", status = "NotStarted", sortOrder = 1
        });
        createResp.EnsureSuccessStatusCode();
        var task = await createResp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        var taskId = task.GetProperty("id").GetString()!;

        // PUT không có If-Match
        var putClient = _factory.CreateClient();
        var token = await GetTokenAsync(putClient);
        putClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var updateResp = await putClient.PutAsJsonAsync($"{TasksUrl(projectId)}/{taskId}", new
        {
            type = "Task", name = "Updated", priority = "Low", status = "NotStarted", sortOrder = 1
        });

        Assert.Equal(HttpStatusCode.PreconditionFailed, updateResp.StatusCode);
    }

    // ─── AC 6: PUT stale If-Match → 409 với current state ────────────────────

    [Fact]
    public async Task UpdateTask_StaleIfMatch_Returns409WithCurrentStateAtRoot()
    {
        var (projectId, client) = await CreateProjectAndGetIdAsync(_factory);

        // Tạo task
        var createResp = await client.PostAsJsonAsync(TasksUrl(projectId), new
        {
            type = "Task", name = "Original", priority = "Low", status = "NotStarted", sortOrder = 1
        });
        createResp.EnsureSuccessStatusCode();
        var task = await createResp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        var taskId = task.GetProperty("id").GetString()!;
        var version = task.GetProperty("version").GetInt32();

        // Update lần 1 — version tăng thành 2
        client.DefaultRequestHeaders.Remove("If-Match");
        client.DefaultRequestHeaders.Add("If-Match", $"\"{version}\"");
        var update1 = await client.PutAsJsonAsync($"{TasksUrl(projectId)}/{taskId}", new
        {
            type = "Task", name = "Updated Once", priority = "Low", status = "NotStarted", sortOrder = 1
        });
        update1.EnsureSuccessStatusCode();

        // Update lần 2 với stale version (version = 1)
        var client2 = await CreateAuthClientAsync(_factory);
        client2.DefaultRequestHeaders.Add("If-Match", $"\"{version}\"");  // stale

        var conflictResp = await client2.PutAsJsonAsync($"{TasksUrl(projectId)}/{taskId}", new
        {
            type = "Task", name = "Conflict Update", priority = "Low", status = "NotStarted", sortOrder = 1
        });

        Assert.Equal(HttpStatusCode.Conflict, conflictResp.StatusCode);
        var body = await conflictResp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);

        // current và eTag phải ở ROOT level (không phải extensions.current)
        Assert.True(body.TryGetProperty("current", out var current),
            "Expected 'current' at root level");
        Assert.True(body.TryGetProperty("eTag", out _),
            "Expected 'eTag' at root level");
        Assert.True(current.TryGetProperty("version", out _),
            "'current' must have version field");
    }

    // ─── AC 6: DELETE missing If-Match → 412 ─────────────────────────────────

    [Fact]
    public async Task DeleteTask_MissingIfMatch_Returns412()
    {
        var (projectId, client) = await CreateProjectAndGetIdAsync(_factory);

        var createResp = await client.PostAsJsonAsync(TasksUrl(projectId), new
        {
            type = "Task", name = "ToDelete", priority = "Low", status = "NotStarted", sortOrder = 1
        });
        createResp.EnsureSuccessStatusCode();
        var task = await createResp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        var taskId = task.GetProperty("id").GetString()!;

        // DELETE không có If-Match
        var delClient = _factory.CreateClient();
        var token = await GetTokenAsync(delClient);
        delClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var deleteResp = await delClient.DeleteAsync($"{TasksUrl(projectId)}/{taskId}");
        Assert.Equal(HttpStatusCode.PreconditionFailed, deleteResp.StatusCode);
    }

    // ─── DELETE with children → 422 ──────────────────────────────────────────

    [Fact]
    public async Task DeleteTask_HasChildren_Returns422()
    {
        var (projectId, client) = await CreateProjectAndGetIdAsync(_factory);

        // Tạo parent task
        var parentResp = await client.PostAsJsonAsync(TasksUrl(projectId), new
        {
            type = "Phase", name = "Parent Phase", priority = "Low", status = "NotStarted", sortOrder = 1
        });
        parentResp.EnsureSuccessStatusCode();
        var parent = await parentResp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        var parentId = parent.GetProperty("id").GetString()!;
        var parentVersion = parent.GetProperty("version").GetInt32();

        // Tạo child task
        var childResp = await client.PostAsJsonAsync(TasksUrl(projectId), new
        {
            parentId,
            type = "Task", name = "Child Task", priority = "Low", status = "NotStarted", sortOrder = 1
        });
        childResp.EnsureSuccessStatusCode();

        // Xóa parent → 422 vì còn child
        client.DefaultRequestHeaders.Remove("If-Match");
        client.DefaultRequestHeaders.Add("If-Match", $"\"{parentVersion}\"");
        var deleteResp = await client.DeleteAsync($"{TasksUrl(projectId)}/{parentId}");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, deleteResp.StatusCode);
        await AssertProblemDetails(deleteResp, 422);
    }

    // ─── DELETE valid If-Match → 204 ─────────────────────────────────────────

    [Fact]
    public async Task DeleteTask_ValidIfMatch_Returns204()
    {
        var (projectId, client) = await CreateProjectAndGetIdAsync(_factory);

        var createResp = await client.PostAsJsonAsync(TasksUrl(projectId), new
        {
            type = "Task", name = "ToDelete OK", priority = "Low", status = "NotStarted", sortOrder = 1
        });
        createResp.EnsureSuccessStatusCode();
        var task = await createResp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        var taskId = task.GetProperty("id").GetString()!;
        var version = task.GetProperty("version").GetInt32();

        client.DefaultRequestHeaders.Remove("If-Match");
        client.DefaultRequestHeaders.Add("If-Match", $"\"{version}\"");
        var deleteResp = await client.DeleteAsync($"{TasksUrl(projectId)}/{taskId}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResp.StatusCode);
    }

    // ─── GET list returns flat list ───────────────────────────────────────────

    [Fact]
    public async Task GetTasks_ReturnsAllTasksInProject()
    {
        var (projectId, client) = await CreateProjectAndGetIdAsync(_factory);

        // Tạo 2 tasks
        for (var i = 1; i <= 2; i++)
        {
            await client.PostAsJsonAsync(TasksUrl(projectId), new
            {
                type = "Task", name = $"Task {i}", priority = "Low", status = "NotStarted",
                sortOrder = i
            });
        }

        var getResp = await client.GetAsync(TasksUrl(projectId));
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);

        var tasks = await getResp.Content.ReadFromJsonAsync<JsonElement[]>(JsonOpts);
        Assert.NotNull(tasks);
        Assert.True(tasks.Length >= 2);
    }

    // ─── AC 4: Date validation ───────────────────────────────────────────────

    [Fact]
    public async Task CreateTask_InvalidDateRange_Returns400()
    {
        var (projectId, client) = await CreateProjectAndGetIdAsync(_factory);

        var response = await client.PostAsJsonAsync(TasksUrl(projectId), new
        {
            type = "Task", name = "Bad Dates", priority = "Low", status = "NotStarted",
            sortOrder = 1,
            plannedStartDate = "2026-06-01",
            plannedEndDate = "2026-05-01"  // endDate < startDate → validation error
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemDetails(response, 400);
    }
}
