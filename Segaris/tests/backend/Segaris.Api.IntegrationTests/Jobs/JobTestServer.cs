using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Segaris.Api.IntegrationTests.Jobs;

/// <summary>
/// Hosts the API in the Testing environment against a file-backed SQLite database so the job
/// probe endpoints are mapped and the background worker can claim and run jobs across requests.
/// </summary>
internal sealed class JobTestServer : IDisposable
{
    public const string AdminUserName = "job-admin";
    public const string AdminPassword = "JobAdminPass123!";

    private readonly WebApplicationFactory<Program> factory;
    private readonly bool deleteOnDispose;

    public JobTestServer(string? databasePath = null, bool deleteOnDispose = true)
    {
        this.deleteOnDispose = deleteOnDispose;
        DatabasePath = databasePath
            ?? Path.Combine(Path.GetTempPath(), $"segaris-jobs-{Guid.NewGuid():N}.db");
        KeysPath = Path.Combine(Path.GetTempPath(), $"segaris-jobs-keys-{Guid.NewGuid():N}");
        AttachmentsPath = Path.Combine(Path.GetTempPath(), $"segaris-jobs-attachments-{Guid.NewGuid():N}");
        BackupsPath = Path.Combine(Path.GetTempPath(), $"segaris-jobs-backups-{Guid.NewGuid():N}");

        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.Sources.Clear();
                configuration.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal)
                {
                    ["Segaris:Database:Provider"] = "Sqlite",
                    ["ConnectionStrings:Segaris"] = $"Data Source={DatabasePath}",
                    ["Segaris:Storage:DataProtectionKeysPath"] = KeysPath,
                    ["Segaris:Storage:AttachmentsPath"] = AttachmentsPath,
                    ["Segaris:Storage:BackupsPath"] = BackupsPath,
                    ["Segaris:Identity:Bootstrap:UserName"] = AdminUserName,
                    ["Segaris:Identity:Bootstrap:Password"] = AdminPassword,
                });
            });
        });
    }

    public string DatabasePath { get; }

    public string KeysPath { get; }

    public string AttachmentsPath { get; }

    public string BackupsPath { get; }

    public HttpClient CreateClient() =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

    public static async Task LoginAsync(HttpClient client, string userName, string password)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/session",
            new { userName, password },
            CancellationToken.None);
        response.EnsureSuccessStatusCode();
    }

    public static async Task<string> GetCsrfTokenAsync(HttpClient client)
    {
        var token = await client.GetFromJsonAsync<JsonElement>(
            "/api/session/antiforgery",
            CancellationToken.None);
        return token.GetProperty("csrfToken").GetString()!;
    }

    public void Dispose()
    {
        factory.Dispose();
        SqliteConnection.ClearAllPools();
        if (deleteOnDispose)
        {
            TryDeleteFile(DatabasePath);
        }

        TryDeleteDirectory(KeysPath);
        TryDeleteDirectory(AttachmentsPath);
        TryDeleteDirectory(BackupsPath);
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
