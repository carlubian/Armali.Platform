using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Segaris.Api.IntegrationTests.Jobs;

public sealed class JobLifecycleTests
{
    [Fact]
    public async Task A_successful_job_runs_to_completion_with_progress_and_result()
    {
        using var server = new JobTestServer();
        using var client = server.CreateClient();
        await JobTestServer.LoginAsync(client, JobTestServer.AdminUserName, JobTestServer.AdminPassword);

        var id = await StartProbeAsync(client, "succeed");
        var terminal = await PollUntilTerminalAsync(client, id);

        Assert.Equal("Succeeded", terminal.GetProperty("state").GetString());
        Assert.Equal(100, terminal.GetProperty("progress").GetInt32());
        Assert.Equal("probe-result", terminal.GetProperty("resultReference").GetString());
        Assert.Equal("probe_succeeded", terminal.GetProperty("resultCode").GetString());
        Assert.True(terminal.GetProperty("failureCode").ValueKind == JsonValueKind.Null);
    }

    [Fact]
    public async Task A_failing_job_records_a_safe_failure_code()
    {
        using var server = new JobTestServer();
        using var client = server.CreateClient();
        await JobTestServer.LoginAsync(client, JobTestServer.AdminUserName, JobTestServer.AdminPassword);

        var id = await StartProbeAsync(client, "fail");
        var terminal = await PollUntilTerminalAsync(client, id);

        Assert.Equal("Failed", terminal.GetProperty("state").GetString());
        Assert.Equal("probe_failed", terminal.GetProperty("failureCode").GetString());
    }

    [Fact]
    public async Task A_running_job_stops_at_a_cancellation_boundary()
    {
        using var server = new JobTestServer();
        using var client = server.CreateClient();
        await JobTestServer.LoginAsync(client, JobTestServer.AdminUserName, JobTestServer.AdminPassword);

        var id = await StartProbeAsync(client, "block");
        await WaitForStateAsync(client, id, "Running");

        using var cancel = await PostAsync(client, $"/api/platform/jobs/{id}/cancel", body: null);
        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);

        var terminal = await PollUntilTerminalAsync(client, id);
        Assert.Equal("Cancelled", terminal.GetProperty("state").GetString());
        Assert.True(terminal.GetProperty("cancellationRequested").GetBoolean());
    }

    [Fact]
    public async Task An_exclusive_job_type_rejects_a_second_active_job()
    {
        using var server = new JobTestServer();
        using var client = server.CreateClient();
        await JobTestServer.LoginAsync(client, JobTestServer.AdminUserName, JobTestServer.AdminPassword);

        var first = await StartProbeAsync(client, "block");
        await WaitForStateAsync(client, id: first, "Running");

        using var conflict = await PostAsync(client, "/api/platform/jobs", new { mode = "block" });
        var problem = await conflict.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.Equal(
            first.ToString(System.Globalization.CultureInfo.InvariantCulture),
            problem.GetProperty("errors").GetProperty("activeJobId")[0].GetString());

        // Release the blocking job so the host can shut down cleanly.
        using var cancel = await PostAsync(client, $"/api/platform/jobs/{first}/cancel", body: null);
        cancel.EnsureSuccessStatusCode();
        await PollUntilTerminalAsync(client, first);
    }

    [Fact]
    public async Task Starting_a_job_without_an_antiforgery_token_is_rejected()
    {
        using var server = new JobTestServer();
        using var client = server.CreateClient();
        await JobTestServer.LoginAsync(client, JobTestServer.AdminUserName, JobTestServer.AdminPassword);

        using var response = await client.PostAsJsonAsync(
            "/api/platform/jobs",
            new { mode = "succeed" },
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Job_endpoints_require_authentication()
    {
        using var server = new JobTestServer();
        using var client = server.CreateClient();

        using var response = await client.GetAsync("/api/platform/jobs/1", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static async Task<int> StartProbeAsync(HttpClient client, string mode)
    {
        using var response = await PostAsync(client, "/api/platform/jobs", new { mode });
        response.EnsureSuccessStatusCode();
        var status = await response.Content.ReadFromJsonAsync<JsonElement>();
        return status.GetProperty("id").GetInt32();
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

    private static async Task<JsonElement> PollUntilTerminalAsync(HttpClient client, int id)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(20);
        while (DateTime.UtcNow < deadline)
        {
            var status = await client.GetFromJsonAsync<JsonElement>(
                $"/api/platform/jobs/{id}",
                CancellationToken.None);
            var state = status.GetProperty("state").GetString();
            if (state is "Succeeded" or "Failed" or "Cancelled" or "Interrupted")
            {
                return status;
            }

            await Task.Delay(100);
        }

        throw new TimeoutException($"Job {id} did not reach a terminal state.");
    }

    private static async Task WaitForStateAsync(HttpClient client, int id, string expected)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(20);
        while (DateTime.UtcNow < deadline)
        {
            var status = await client.GetFromJsonAsync<JsonElement>(
                $"/api/platform/jobs/{id}",
                CancellationToken.None);
            if (status.GetProperty("state").GetString() == expected)
            {
                return;
            }

            await Task.Delay(100);
        }

        throw new TimeoutException($"Job {id} did not reach state {expected}.");
    }
}
