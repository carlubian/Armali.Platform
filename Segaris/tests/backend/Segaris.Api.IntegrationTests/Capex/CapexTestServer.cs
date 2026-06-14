using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace Segaris.Api.IntegrationTests.Capex;

/// <summary>
/// Shared host fixture for the Configuration, Capex, and Launcher API
/// integration tests. It hosts the API against a file-backed SQLite database
/// with persistent Data Protection keys and an attachments directory so the
/// authentication, antiforgery, and attachment flows the later Waves exercise
/// behave as in production. Wave 0 only establishes the fixture; the endpoints
/// it targets are added in Waves 1, 3, and 4.
/// </summary>
internal sealed class CapexTestServer : IDisposable
{
    public const string AdminUserName = "founder";
    public const string AdminPassword = "FounderPass123!";

    private readonly WebApplicationFactory<Program> _factory;

    public CapexTestServer(string environment = "Staging")
    {
        DatabasePath = Path.Combine(Path.GetTempPath(), $"segaris-capex-{Guid.NewGuid():N}.db");
        KeysPath = Path.Combine(Path.GetTempPath(), $"segaris-capex-keys-{Guid.NewGuid():N}");
        AttachmentsPath = Path.Combine(Path.GetTempPath(), $"segaris-capex-attachments-{Guid.NewGuid():N}");

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.Sources.Clear();
                configuration.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal)
                {
                    ["Segaris:Database:Provider"] = "Sqlite",
                    ["ConnectionStrings:Segaris"] = $"Data Source={DatabasePath}",
                    ["Segaris:Storage:DataProtectionKeysPath"] = KeysPath,
                    ["Segaris:Storage:AttachmentsPath"] = AttachmentsPath,
                    ["Segaris:Identity:Bootstrap:UserName"] = AdminUserName,
                    ["Segaris:Identity:Bootstrap:Password"] = AdminPassword,
                });
            });
        });
    }

    public string DatabasePath { get; }

    public string KeysPath { get; }

    public string AttachmentsPath { get; }

    public HttpClient CreateClient(bool handleCookies = true) =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = handleCookies });

    public async Task<HttpClient> CreateAuthenticatedClientAsync(
        string? userName = null,
        string? password = null)
    {
        var client = CreateClient();
        await LoginAsync(client, userName ?? AdminUserName, password ?? AdminPassword);
        return client;
    }

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
        _factory.Dispose();
        SqliteConnection.ClearAllPools();
        TryDeleteFile(DatabasePath);
        TryDeleteDirectory(KeysPath);
        TryDeleteDirectory(AttachmentsPath);
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
