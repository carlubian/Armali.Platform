using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Segaris.Api.IntegrationTests.Jobs;

public sealed class BackupEndpointTests
{
    [Fact]
    public async Task Starting_a_backup_requires_the_administrator_role()
    {
        using var server = new JobTestServer();
        using var admin = server.CreateClient();
        await JobTestServer.LoginAsync(admin, JobTestServer.AdminUserName, JobTestServer.AdminPassword);
        await CreateUserAsync(admin, "backup-member", "BackupMemberPass123!");

        using var member = server.CreateClient();
        await JobTestServer.LoginAsync(member, "backup-member", "BackupMemberPass123!");

        using var response = await PostAsync(member, "/api/backup-jobs", body: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Starting_a_backup_on_a_non_postgres_provider_is_unprocessable()
    {
        using var server = new JobTestServer();
        using var admin = server.CreateClient();
        await JobTestServer.LoginAsync(admin, JobTestServer.AdminUserName, JobTestServer.AdminPassword);

        using var response = await PostAsync(admin, "/api/backup-jobs", body: null);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("request.unprocessable", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Backup_endpoints_require_authentication()
    {
        using var server = new JobTestServer();
        using var client = server.CreateClient();

        using var response = await client.GetAsync("/api/backup-jobs/1", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Querying_a_missing_backup_job_returns_not_found()
    {
        using var server = new JobTestServer();
        using var admin = server.CreateClient();
        await JobTestServer.LoginAsync(admin, JobTestServer.AdminUserName, JobTestServer.AdminPassword);

        using var response = await admin.GetAsync("/api/backup-jobs/999999", CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task CreateUserAsync(HttpClient admin, string userName, string password)
    {
        using var response = await PostAsync(
            admin,
            "/api/admin/users",
            new { userName, password, role = "User" });
        response.EnsureSuccessStatusCode();
    }

    private static async Task<HttpResponseMessage> PostAsync(HttpClient client, string url, object? body)
    {
        var csrf = await JobTestServer.GetCsrfTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("X-CSRF-TOKEN", csrf);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await client.SendAsync(request, CancellationToken.None);
    }
}
