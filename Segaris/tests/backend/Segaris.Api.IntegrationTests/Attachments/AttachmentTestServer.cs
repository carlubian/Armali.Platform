using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Segaris.Api.IntegrationTests.Infrastructure;

namespace Segaris.Api.IntegrationTests.Attachments;

internal sealed class AttachmentTestServer : IDisposable
{
    public const string AdminUserName = "attachment-admin";
    public const string AdminPassword = "AttachmentPass123!";

    private readonly WebApplicationFactory<Program> _factory;

    public AttachmentTestServer(string? attachmentsPath = null)
    {
        DatabasePath = Path.Combine(Path.GetTempPath(), $"segaris-attachments-{Guid.NewGuid():N}.db");
        KeysPath = Path.Combine(Path.GetTempPath(), $"segaris-attachment-keys-{Guid.NewGuid():N}");
        AttachmentsPath = attachmentsPath
            ?? Path.Combine(Path.GetTempPath(), $"segaris-attachment-files-{Guid.NewGuid():N}");

        // Start from a migrated and seeded template so host startup skips the expensive
        // schema creation and seed inserts.
        SqliteTemplateDatabase.CopyTo(DatabasePath, AdminUserName, AdminPassword);

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
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

    public async Task LoginAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/session",
            new { userName = AdminUserName, password = AdminPassword },
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
