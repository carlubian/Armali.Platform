using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Segaris.Api.IntegrationTests.Infrastructure;
using Segaris.Api.Modules.Identity;
using Segaris.Persistence;
using Segaris.Shared.Time;

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

    public CapexTestServer(string environment = "Staging", Action<IServiceCollection>? configureServices = null)
    {
        DatabasePath = Path.Combine(Path.GetTempPath(), $"segaris-capex-{Guid.NewGuid():N}.db");
        KeysPath = Path.Combine(Path.GetTempPath(), $"segaris-capex-keys-{Guid.NewGuid():N}");
        AttachmentsPath = Path.Combine(Path.GetTempPath(), $"segaris-capex-attachments-{Guid.NewGuid():N}");

        // Start from a migrated and seeded template so host startup skips the expensive
        // schema creation and seed inserts; the migrations are already applied and the
        // seeders are idempotent.
        SqliteTemplateDatabase.CopyTo(DatabasePath, AdminUserName, AdminPassword);

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
            builder.ConfigureTestServices(services => configureServices?.Invoke(services));
        });
    }

    public string DatabasePath { get; }

    public string KeysPath { get; }

    public string AttachmentsPath { get; }

    public IServiceProvider Services => _factory.Services;

    public async Task<int> GetUserIdAsync(string userName)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        return await database.Set<SegarisUser>()
            .Where(user => user.UserName == userName)
            .Select(user => user.Id)
            .SingleAsync();
    }

    public async Task<int> CreateUserAsync(string userName, string password)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<SegarisUser>>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var user = new SegarisUser
        {
            UserName = userName,
            DisplayName = userName,
            IsActive = true,
            CreatedAt = clock.UtcNow,
        };

        var created = await userManager.CreateAsync(user, password);
        if (!created.Succeeded)
        {
            throw new InvalidOperationException(
                $"Could not create test user '{userName}': {string.Join(", ", created.Errors.Select(error => error.Description))}");
        }

        var assigned = await userManager.AddToRoleAsync(user, "User");
        if (!assigned.Succeeded)
        {
            throw new InvalidOperationException(
                $"Could not assign the role to test user '{userName}': {string.Join(", ", assigned.Errors.Select(error => error.Description))}");
        }

        return user.Id;
    }

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
