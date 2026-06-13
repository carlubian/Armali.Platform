using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Segaris.Api.IntegrationTests.Identity;

/// <summary>
/// Hosts the API against a file-backed SQLite database and a persistent Data Protection
/// key directory so authentication flows and cookie validity survive across requests
/// (and, when paths are shared, across host restarts).
/// </summary>
internal sealed class IdentityTestServer : IDisposable
{
    public const string AdminUserName = "founder";
    public const string AdminPassword = "FounderPass123!";

    private readonly WebApplicationFactory<Program> _factory;
    private readonly bool _deleteOnDispose;

    public IdentityTestServer(
        string? databasePath = null,
        string? keysPath = null,
        bool deleteOnDispose = true)
    {
        _deleteOnDispose = deleteOnDispose;
        DatabasePath = databasePath
            ?? Path.Combine(Path.GetTempPath(), $"segaris-identity-{Guid.NewGuid():N}.db");
        KeysPath = keysPath
            ?? Path.Combine(Path.GetTempPath(), $"segaris-keys-{Guid.NewGuid():N}");
        AttachmentsPath = Path.Combine(Path.GetTempPath(), $"segaris-identity-attachments-{Guid.NewGuid():N}");

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(Environments.Staging);
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

    public void Dispose()
    {
        _factory.Dispose();
        SqliteConnection.ClearAllPools();
        if (_deleteOnDispose)
        {
            TryDeleteFile(DatabasePath);
            TryDeleteDirectory(KeysPath);
            TryDeleteDirectory(AttachmentsPath);
        }
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

    internal static void TryDeleteFile(string path)
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

    internal static void TryDeleteDirectory(string path)
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
