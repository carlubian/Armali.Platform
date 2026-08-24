using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Blackwing.Api.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Blackwing.Api.IntegrationTests.Identity;

/// <summary>
/// Hosts the API against a database of its own inside the shared PostgreSQL container, with a
/// persistent Data Protection key directory and the bootstrap administrator configured, so
/// authentication flows survive across requests and, when the paths are shared, across host
/// restarts.
/// </summary>
internal sealed class IdentityTestServer : IDisposable
{
    public const string AdminUserName = "founder";
    public const string AdminPassword = "FounderPass123!";

    private readonly IdentityWebApplicationFactory _factory;
    private readonly bool _deleteOnDispose;

    private IdentityTestServer(string connectionString, string? keysPath, bool deleteOnDispose)
    {
        _deleteOnDispose = deleteOnDispose;
        ConnectionString = connectionString;
        KeysPath = keysPath ?? Path.Combine(Path.GetTempPath(), $"blackwing-keys-{Guid.NewGuid():N}");
        ImagesPath = Path.Combine(Path.GetTempPath(), $"blackwing-images-{Guid.NewGuid():N}");

        _factory = new IdentityWebApplicationFactory(new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["ConnectionStrings:Blackwing"] = ConnectionString,
            ["Blackwing:Storage:ImagesPath"] = ImagesPath,
            ["Blackwing:Storage:DataProtectionKeysPath"] = KeysPath,
            ["Blackwing:Identity:Bootstrap:UserName"] = AdminUserName,
            ["Blackwing:Identity:Bootstrap:Password"] = AdminPassword,
            ["Blackwing:Observability:Seq:Enabled"] = "false",
            ["Logging:LogLevel:Default"] = "Warning",
        });
    }

    public string ConnectionString { get; }

    public string KeysPath { get; }

    public string ImagesPath { get; }

    /// <summary>
    /// Starts a host on a brand new database. Migrations and the identity seed run at startup, so
    /// the returned server already has its bootstrap administrator and both roles.
    /// </summary>
    public static async Task<IdentityTestServer> StartAsync(
        PostgresFixture postgres,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(postgres);

        var connectionString = await postgres.CreateDatabaseAsync(cancellationToken);
        return new IdentityTestServer(connectionString, keysPath: null, deleteOnDispose: true);
    }

    /// <summary>
    /// Starts a host on an existing database and key directory. This is what lets a restart be
    /// simulated: the second host is a different process image over the same durable state.
    /// </summary>
    public static IdentityTestServer Restart(string connectionString, string keysPath) =>
        new(connectionString, keysPath, deleteOnDispose: false);

    public HttpClient CreateClient(bool handleCookies = true) =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = handleCookies });

    public void Dispose()
    {
        _factory.Dispose();
        TryDeleteDirectory(ImagesPath);

        if (_deleteOnDispose)
        {
            TryDeleteDirectory(KeysPath);
        }
    }

    public static async Task LoginAsync(HttpClient client, string userName, string password)
    {
        using var response = await PostLoginAsync(client, userName, password);
        response.EnsureSuccessStatusCode();
    }

    public static Task<HttpResponseMessage> PostLoginAsync(
        HttpClient client,
        string? userName,
        string? password)
    {
        ArgumentNullException.ThrowIfNull(client);

        return client.PostAsJsonAsync(
            "/api/session",
            new { userName, password },
            CancellationToken.None);
    }

    public static async Task<string> GetCsrfTokenAsync(HttpClient client)
    {
        ArgumentNullException.ThrowIfNull(client);

        var token = await client.GetFromJsonAsync<JsonElement>(
            "/api/session/antiforgery",
            CancellationToken.None);
        return token.GetProperty("csrfToken").GetString()!;
    }

    /// <summary>
    /// Sends a state-changing request carrying a valid antiforgery token. The token is fetched
    /// after authentication on purpose: a token issued to the anonymous identity is not valid for
    /// the authenticated one.
    /// </summary>
    public static async Task<HttpResponseMessage> SendWithCsrfAsync(
        HttpClient client,
        HttpMethod method,
        string url,
        object? body = null)
    {
        var csrf = await GetCsrfTokenAsync(client);
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Add("X-CSRF-TOKEN", csrf);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await client.SendAsync(request, CancellationToken.None);
    }

    public static async Task<int> CreateUserAsync(
        HttpClient admin,
        string userName,
        string password,
        string role)
    {
        using var response = await SendWithCsrfAsync(
            admin,
            HttpMethod.Post,
            "/api/admin/users",
            new { userName, password, role });
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken.None);
        return body.GetProperty("id").GetInt32();
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

    private sealed class IdentityWebApplicationFactory(IDictionary<string, string?> settings)
        : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.Sources.Clear();
                configuration.AddInMemoryCollection(settings);
            });

            builder.ConfigureServices(services =>
                services.AddSingleton<IStartupFilter, LoopbackRemoteAddressStartupFilter>());
        }
    }

    /// <summary>
    /// Guarantees every in-process request a loopback connection address before the application's
    /// own pipeline runs.
    /// </summary>
    /// <remarks>
    /// The forwarded-headers middleware only honours <c>X-Forwarded-For</c> when the connection
    /// itself arrives from a known proxy, and loopback is on that list precisely so an in-process
    /// host can exercise the per-IP partitioning of the login rate limiter. The test host does
    /// assign a loopback address of its own accord today, but nothing in its contract promises to
    /// keep doing so, and a request without one would quietly lose the header and leave the
    /// partitioning test failing for a reason that has nothing to do with the limiter. Setting it
    /// here costs nothing and removes the doubt.
    /// </remarks>
    private sealed class LoopbackRemoteAddressStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            ArgumentNullException.ThrowIfNull(next);

            return builder =>
            {
                builder.Use(async (context, request) =>
                {
                    context.Connection.RemoteIpAddress ??= IPAddress.Loopback;
                    await request(context);
                });

                next(builder);
            };
        }
    }
}
