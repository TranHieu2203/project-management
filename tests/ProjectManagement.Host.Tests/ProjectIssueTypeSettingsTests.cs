using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectManagement.Projects.Domain.Enums;
using ProjectManagement.Projects.Infrastructure.Persistence;

namespace ProjectManagement.Host.Tests;

public sealed class ProjectIssueTypeSettingsTests : IClassFixture<TestHostFactory>
{
    private readonly TestHostFactory _factory;

    private const string SeedEmail1 = "pm1@local.test";
    private const string SeedEmail2 = "pm2@local.test";
    private const string SeedPassword = "P@ssw0rd!123";
    private const string LoginUrl = "/api/v1/auth/login";
    private const string ProjectsUrl = "/api/v1/projects";

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public ProjectIssueTypeSettingsTests(TestHostFactory factory) => _factory = factory;

    private static string IssueTypeSettingsUrl(string projectId) => $"{ProjectsUrl}/{projectId}/issue-type-settings";

    private static async Task<string> GetTokenAsync(HttpClient client, string email)
    {
        var resp = await client.PostAsJsonAsync(LoginUrl, new { email, password = SeedPassword });
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        return body.GetProperty("accessToken").GetString()!;
    }

    private static async Task<HttpClient> CreateAuthClientAsync(TestHostFactory factory, string email)
    {
        var client = factory.CreateClient();
        var token = await GetTokenAsync(client, email);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task<string> CreateProjectAndGetIdAsync(TestHostFactory factory, HttpClient client)
    {
        var code = $"ITS-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
        var resp = await client.PostAsJsonAsync(ProjectsUrl, new { code, name = "IssueTypeSettings Test Project" });
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
    public async Task GetSettings_Manager_DefaultsEnabledTrue()
    {
        var client = await CreateAuthClientAsync(_factory, SeedEmail1);
        var projectId = await CreateProjectAndGetIdAsync(_factory, client);

        var response = await client.GetAsync(IssueTypeSettingsUrl(projectId));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var items = await response.Content.ReadFromJsonAsync<JsonElement[]>(JsonOpts);
        Assert.NotNull(items);
        Assert.NotEmpty(items);

        foreach (var item in items)
        {
            Assert.True(item.TryGetProperty("isEnabled", out var enabledProp));
            Assert.True(enabledProp.GetBoolean());
        }
    }

    [Fact]
    public async Task PutSettings_Manager_CanDisable_AndPersists()
    {
        var client = await CreateAuthClientAsync(_factory, SeedEmail1);
        var projectId = await CreateProjectAndGetIdAsync(_factory, client);

        var list = await client.GetAsync(IssueTypeSettingsUrl(projectId));
        list.EnsureSuccessStatusCode();
        var items = await list.Content.ReadFromJsonAsync<JsonElement[]>(JsonOpts);
        Assert.NotNull(items);
        var typeId = items[0].GetProperty("id").GetString()!;

        var put = await client.PutAsJsonAsync($"{IssueTypeSettingsUrl(projectId)}/{typeId}", new { isEnabled = false });
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);

        var after = await client.GetAsync(IssueTypeSettingsUrl(projectId));
        after.EnsureSuccessStatusCode();
        var afterItems = await after.Content.ReadFromJsonAsync<JsonElement[]>(JsonOpts);
        Assert.NotNull(afterItems);

        var match = afterItems.First(x => x.GetProperty("id").GetString() == typeId);
        Assert.False(match.GetProperty("isEnabled").GetBoolean());
    }

    [Fact]
    public async Task PutSettings_MemberButNotManager_Returns403ProblemDetails()
    {
        var client = await CreateAuthClientAsync(_factory, SeedEmail1);
        var projectId = await CreateProjectAndGetIdAsync(_factory, client);

        // downgrade pm1 role to Member for this project (test-only)
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ProjectsDbContext>();
            var pm1UserId = await ResolveUserIdAsync(scope.ServiceProvider, SeedEmail1);
            var membership = await db.ProjectMemberships
                .FirstAsync(x => x.ProjectId == Guid.Parse(projectId) && x.UserId == pm1UserId);
            // Role has private setter; set via EF tracked entity property
            db.Entry(membership).Property(x => x.Role).CurrentValue = ProjectMemberRole.Member;
            await db.SaveChangesAsync();
        }

        var list = await client.GetAsync(IssueTypeSettingsUrl(projectId));
        // Member is not allowed to access settings → 403
        Assert.Equal(HttpStatusCode.Forbidden, list.StatusCode);
        await AssertProblemDetails(list, 403);
    }

    [Fact]
    public async Task GetSettings_NonMember_Returns404()
    {
        var manager = await CreateAuthClientAsync(_factory, SeedEmail1);
        var projectId = await CreateProjectAndGetIdAsync(_factory, manager);

        var nonMember = await CreateAuthClientAsync(_factory, SeedEmail2);
        var response = await nonMember.GetAsync(IssueTypeSettingsUrl(projectId));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertProblemDetails(response, 404);
    }

    private static async Task<Guid> ResolveUserIdAsync(IServiceProvider serviceProvider, string email)
    {
        var userManager = serviceProvider.GetRequiredService<
            Microsoft.AspNetCore.Identity.UserManager<ProjectManagement.Auth.Domain.Users.ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);
        Assert.NotNull(user);
        return user!.Id;
    }
}

