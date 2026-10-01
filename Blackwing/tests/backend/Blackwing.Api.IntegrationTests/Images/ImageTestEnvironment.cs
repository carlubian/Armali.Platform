using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Blackwing.Api.IntegrationTests.Identity;
using Blackwing.Api.IntegrationTests.Infrastructure;
using Blackwing.Api.Modules.Identity.Persistence;
using Blackwing.Api.Platform.Images;
using Blackwing.Persistence;
using Blackwing.Shared.Identity;
using Blackwing.Shared.Images;
using Microsoft.EntityFrameworkCore;

namespace Blackwing.Api.IntegrationTests.Images;

internal sealed record TestAccount(string UserName, int UserId, HttpClient Client);

/// <summary>
/// A running host on a database of its own, an administrator, and any number of ordinary
/// accounts that are already logged in. Everything the image tests have in common.
/// </summary>
internal sealed class ImageTestEnvironment : IDisposable
{
    private readonly Dictionary<string, TestAccount> _accounts = new(StringComparer.Ordinal);

    private ImageTestEnvironment(IdentityTestServer server, HttpClient admin)
    {
        Server = server;
        Admin = admin;
    }

    public IdentityTestServer Server { get; }

    /// <summary>Logged in as the bootstrap administrator.</summary>
    public HttpClient Admin { get; }

    public string ImagesRoot => Server.ImagesPath;

    public TestAccount this[string userName] => _accounts[userName];

    public static async Task<ImageTestEnvironment> StartAsync(PostgresFixture postgres, params string[] userNames)
    {
        ArgumentNullException.ThrowIfNull(postgres);

        var server = await IdentityTestServer.StartAsync(postgres);
        var admin = server.CreateClient();
        await IdentityTestServer.LoginAsync(admin, IdentityTestServer.AdminUserName, IdentityTestServer.AdminPassword);

        var environment = new ImageTestEnvironment(server, admin);
        foreach (var userName in userNames)
        {
            var password = PasswordFor(userName);
            var userId = await IdentityTestServer.CreateUserAsync(admin, userName, password, "User");

            var client = server.CreateClient();
            await IdentityTestServer.LoginAsync(client, userName, password);
            environment._accounts[userName] = new TestAccount(userName, userId, client);
        }

        return environment;
    }

    public void Dispose()
    {
        foreach (var account in _accounts.Values)
        {
            account.Client.Dispose();
        }

        Admin.Dispose();
        Server.Dispose();
    }

    /// <summary>
    /// Opens a database context bound to one account, the way the host binds one per request. It
    /// carries the very same model contributors as the host: the model of a context type is
    /// cached for the whole process, so a context built from a different set could poison the
    /// model the running application uses.
    /// </summary>
    public BlackwingDbContext OpenDatabase(TestAccount account) => OpenDatabase(account.UserId);

    public BlackwingDbContext OpenDatabase(int? userId)
    {
        var options = new DbContextOptionsBuilder<BlackwingDbContext>()
            .UseNpgsql(Server.ConnectionString)
            .Options;

        return new BlackwingDbContext(options, [new IdentityModelContributor()], new FixedUser(userId));
    }

    public string PathOf(int ownerUserId, string contentHash, ImageVariant variant) =>
        Path.Combine(ImagesRoot, FileSystemImageBlobStore.GetRelativePath(new UserId(ownerUserId), contentHash, variant));

    /// <summary>Everything stored on the volume, staging area excluded.</summary>
    public string[] StoredFiles()
    {
        if (!Directory.Exists(ImagesRoot))
        {
            return [];
        }

        var staging = Path.Combine(ImagesRoot, ".staging") + Path.DirectorySeparatorChar;
        return Directory.GetFiles(ImagesRoot, "*", SearchOption.AllDirectories)
            .Where(path => !path.StartsWith(staging, StringComparison.Ordinal))
            .ToArray();
    }

    public string[] StagingFiles()
    {
        var staging = Path.Combine(ImagesRoot, ".staging");
        return Directory.Exists(staging) ? Directory.GetFiles(staging) : [];
    }

    public static string Sha256(byte[] content) => Convert.ToHexStringLower(SHA256.HashData(content));

    public static Task<HttpResponseMessage> UploadAsync(
        HttpClient client,
        string fileName,
        byte[] content,
        string? contentType = null,
        bool includeAntiforgery = true,
        params (string Name, string Value)[] extraFields) =>
        UploadAsync(
            client,
            fileName,
            new MemoryStream(content),
            contentType ?? ContentTypeOf(fileName),
            includeAntiforgery,
            extraFields);

    public static async Task<HttpResponseMessage> UploadAsync(
        HttpClient client,
        string fileName,
        Stream content,
        string contentType,
        bool includeAntiforgery = true,
        params (string Name, string Value)[] extraFields)
    {
        ArgumentNullException.ThrowIfNull(client);

        using var form = new MultipartFormDataContent();
        foreach (var (name, value) in extraFields)
        {
            form.Add(new StringContent(value), name);
        }

        var file = new StreamContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(file, "file", fileName);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/images") { Content = form };
        if (includeAntiforgery)
        {
            request.Headers.Add("X-CSRF-TOKEN", await IdentityTestServer.GetCsrfTokenAsync(client));
        }

        return await client.SendAsync(request, CancellationToken.None);
    }

    /// <summary>Uploads a file that is expected to be accepted and returns the new identifier.</summary>
    public static async Task<int> UploadOkAsync(HttpClient client, string fileName, byte[] content)
    {
        using var response = await UploadAsync(client, fileName, content);
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken.None);
        return body.GetProperty("id").GetInt32();
    }

    /// <summary>The stable machine-readable code of a problem response.</summary>
    public static async Task<string?> ReadCodeAsync(HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken.None);
        return problem.TryGetProperty("code", out var code) ? code.GetString() : null;
    }

    private static string ContentTypeOf(string fileName) => Path.GetExtension(fileName).ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".webp" => "image/webp",
        ".gif" => "image/gif",
        _ => "application/octet-stream",
    };

    private static string PasswordFor(string userName) =>
        $"{char.ToUpperInvariant(userName[0])}{userName[1..]}Pass123456!";

    private sealed class FixedUser(int? userId) : ICurrentUser
    {
        public bool IsAuthenticated => userId is not null;

        public UserId? UserId => userId is { } id ? new UserId(id) : null;

        public bool IsInRole(PlatformRole role) => false;
    }
}
