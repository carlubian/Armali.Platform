using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace Segaris.Api.IntegrationTests.Jobs;

public sealed class JobRecoveryTests
{
    [Fact]
    public async Task A_job_left_running_by_a_previous_process_is_recovered_as_interrupted()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"segaris-jobs-recovery-{Guid.NewGuid():N}.db");
        int jobId;

        // First process: run a real job, then simulate a process that died mid-execution by
        // forcing the completed record back into the Running state with raw SQL. The database
        // file is preserved so the second process can recover it.
        using (var first = new JobTestServer(databasePath, deleteOnDispose: false))
        {
            using var client = first.CreateClient();
            await JobTestServer.LoginAsync(client, JobTestServer.AdminUserName, JobTestServer.AdminPassword);
            jobId = await RunProbeToCompletionAsync(client);

            await using var connection = new SqliteConnection($"Data Source={databasePath}");
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText =
                "UPDATE platform_background_jobs " +
                "SET State = 'Running', ActiveExclusivityKey = 'probe', CompletedAt = NULL " +
                "WHERE Id = $id";
            command.Parameters.AddWithValue("$id", jobId);
            await command.ExecuteNonQueryAsync();
        }

        SqliteConnection.ClearAllPools();

        // Second process: startup recovery must mark the stale running job as interrupted.
        using var second = new JobTestServer(databasePath);
        using var recoveredClient = second.CreateClient();
        await JobTestServer.LoginAsync(recoveredClient, JobTestServer.AdminUserName, JobTestServer.AdminPassword);

        // Startup recovery runs on the worker's background thread, so poll until it completes.
        var status = await PollUntilStateAsync(recoveredClient, jobId, "Interrupted");

        Assert.Equal("Interrupted", status.GetProperty("state").GetString());
        Assert.Equal("interrupted", status.GetProperty("failureCode").GetString());

        try
        {
            File.Delete(databasePath);
        }
        catch (IOException)
        {
        }
    }

    private static async Task<JsonElement> PollUntilStateAsync(HttpClient client, int id, string expected)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(20);
        while (DateTime.UtcNow < deadline)
        {
            var status = await client.GetFromJsonAsync<JsonElement>(
                $"/api/platform/jobs/{id}",
                CancellationToken.None);
            if (status.GetProperty("state").GetString() == expected)
            {
                return status;
            }

            await Task.Delay(100);
        }

        throw new TimeoutException($"Job {id} did not reach state {expected}.");
    }

    private static async Task<int> RunProbeToCompletionAsync(HttpClient client)
    {
        var csrf = await JobTestServer.GetCsrfTokenAsync(client);
        using var start = new HttpRequestMessage(HttpMethod.Post, "/api/platform/jobs");
        start.Headers.Add("X-CSRF-TOKEN", csrf);
        start.Content = JsonContent.Create(new { mode = "succeed" });
        using var startResponse = await client.SendAsync(start, CancellationToken.None);
        startResponse.EnsureSuccessStatusCode();
        var started = await startResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = started.GetProperty("id").GetInt32();

        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(20);
        while (DateTime.UtcNow < deadline)
        {
            var status = await client.GetFromJsonAsync<JsonElement>(
                $"/api/platform/jobs/{id}",
                CancellationToken.None);
            if (status.GetProperty("state").GetString() == "Succeeded")
            {
                return id;
            }

            await Task.Delay(100);
        }

        throw new TimeoutException($"Probe job {id} did not complete.");
    }
}
