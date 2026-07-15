using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Segaris.Api.Modules.Identity.ApiKeys;
using Segaris.Persistence;

namespace Segaris.Api.IntegrationTests.Identity;

/// <summary>
/// Wave 1 coverage for user-bound API keys: issuing, verification, and every path
/// that must stop a key working. The cookie scheme must be observably unchanged.
/// </summary>
public sealed class ApiKeyTests
{
    [Fact]
    public async Task Creating_a_key_returns_the_complete_token_exactly_once()
    {
        using var server = new IdentityTestServer();
        using var client = server.CreateClient();
        await IdentityTestServer.LoginAsync(client, IdentityTestServer.AdminUserName, IdentityTestServer.AdminPassword);

        var created = await CreateKeyAsync(client, "Household agent");
        var token = created.GetProperty("token").GetString()!;
        var list = await client.GetFromJsonAsync<JsonElement>(
            "/api/session/profile/api-keys",
            CancellationToken.None);

        Assert.StartsWith("segaris_", token, StringComparison.Ordinal);
        Assert.Equal(3, token.Split('_').Length);
        Assert.Equal("Household agent", created.GetProperty("key").GetProperty("name").GetString());

        // The listing describes keys but can never return a usable token again.
        var listed = Assert.Single(list.EnumerateArray());
        Assert.False(listed.TryGetProperty("token", out _));
        Assert.False(listed.TryGetProperty("secretHash", out _));
        Assert.DoesNotContain(token, list.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_database_never_stores_the_usable_token()
    {
        using var server = new IdentityTestServer();
        using var client = server.CreateClient();
        await IdentityTestServer.LoginAsync(client, IdentityTestServer.AdminUserName, IdentityTestServer.AdminPassword);

        var created = await CreateKeyAsync(client, "Agent");
        var token = created.GetProperty("token").GetString()!;
        var secret = token.Split('_')[2];

        await using var scope = server.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        var record = await database.Set<SegarisApiKey>().SingleAsync(CancellationToken.None);

        Assert.NotEqual(secret, record.SecretHash);
        Assert.DoesNotContain(secret, record.SecretHash, StringComparison.Ordinal);
        Assert.Equal(ApiKeyToken.Hash(secret), record.SecretHash);
    }

    [Fact]
    public async Task A_key_authenticates_as_its_user_without_a_cookie()
    {
        using var server = new IdentityTestServer();
        using var cookieClient = server.CreateClient();
        await IdentityTestServer.LoginAsync(cookieClient, IdentityTestServer.AdminUserName, IdentityTestServer.AdminPassword);
        var token = (await CreateKeyAsync(cookieClient, "Agent")).GetProperty("token").GetString()!;

        // A fresh client with no cookie jar: the key is the only credential present.
        using var agent = server.CreateClient(handleCookies: false);
        using var response = await SendWithKeyAsync(agent, HttpMethod.Get, "/api/session", token);
        var session = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(IdentityTestServer.AdminUserName, session.GetProperty("userName").GetString());
        Assert.Contains(
            session.GetProperty("roles").EnumerateArray().Select(role => role.GetString()),
            role => role == "Admin");
    }

    [Fact]
    public async Task Unknown_malformed_and_wrong_secret_tokens_are_all_rejected_identically()
    {
        using var server = new IdentityTestServer();
        using var client = server.CreateClient();
        await IdentityTestServer.LoginAsync(client, IdentityTestServer.AdminUserName, IdentityTestServer.AdminPassword);
        var token = (await CreateKeyAsync(client, "Agent")).GetProperty("token").GetString()!;
        var keyId = token.Split('_')[1];

        var otherSecret = new string('a', 64);

        using var agent = server.CreateClient(handleCookies: false);
        using var malformed = await SendWithKeyAsync(agent, HttpMethod.Get, "/api/session", "not-a-segaris-token");
        using var unknown = await SendWithKeyAsync(
            agent,
            HttpMethod.Get,
            "/api/session",
            $"segaris_{new string('b', 24)}_{otherSecret}");

        // A real key identifier with the wrong secret: the lookup succeeds and only
        // the verifier rejects it.
        using var wrongSecret = await SendWithKeyAsync(
            agent,
            HttpMethod.Get,
            "/api/session",
            $"segaris_{keyId}_{otherSecret}");

        Assert.Equal(HttpStatusCode.Unauthorized, malformed.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, wrongSecret.StatusCode);
    }

    [Fact]
    public async Task An_expired_key_stops_authenticating()
    {
        using var server = new IdentityTestServer();
        using var client = server.CreateClient();
        await IdentityTestServer.LoginAsync(client, IdentityTestServer.AdminUserName, IdentityTestServer.AdminPassword);

        var token = (await CreateKeyAsync(
            client,
            "Short lived",
            DateTimeOffset.UtcNow.AddDays(1))).GetProperty("token").GetString()!;

        using var agent = server.CreateClient(handleCookies: false);
        using var beforeExpiry = await SendWithKeyAsync(agent, HttpMethod.Get, "/api/session", token);

        // Creation refuses a past expiration, so the passage of time is simulated
        // by moving the stored expiry rather than by waiting for it.
        await MutateKeyAsync(server, key => key.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1));
        using var afterExpiry = await SendWithKeyAsync(agent, HttpMethod.Get, "/api/session", token);

        Assert.Equal(HttpStatusCode.OK, beforeExpiry.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, afterExpiry.StatusCode);
    }

    [Fact]
    public async Task Creating_a_key_rejects_an_expiration_in_the_past()
    {
        using var server = new IdentityTestServer();
        using var client = server.CreateClient();
        await IdentityTestServer.LoginAsync(client, IdentityTestServer.AdminUserName, IdentityTestServer.AdminPassword);

        using var response = await SendWithCsrfAsync(
            client,
            HttpMethod.Post,
            "/api/session/profile/api-keys",
            new { name = "Stale", expiresAt = DateTimeOffset.UtcNow.AddDays(-1) });
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(problem.GetProperty("errors").TryGetProperty("expiresAt", out _));
    }

    [Fact]
    public async Task A_key_with_no_expiration_keeps_working_and_stays_revocable()
    {
        using var server = new IdentityTestServer();
        using var client = server.CreateClient();
        await IdentityTestServer.LoginAsync(client, IdentityTestServer.AdminUserName, IdentityTestServer.AdminPassword);

        var created = await CreateKeyAsync(client, "Perpetual agent");
        var token = created.GetProperty("token").GetString()!;
        var id = created.GetProperty("key").GetProperty("id").GetInt32();

        using var agent = server.CreateClient(handleCookies: false);
        using var beforeRevocation = await SendWithKeyAsync(agent, HttpMethod.Get, "/api/session", token);
        using var revoke = await SendWithCsrfAsync(
            client,
            HttpMethod.Delete,
            $"/api/session/profile/api-keys/{id}");
        using var afterRevocation = await SendWithKeyAsync(agent, HttpMethod.Get, "/api/session", token);

        Assert.True(created.GetProperty("key").GetProperty("expiresAt").ValueKind is JsonValueKind.Null);
        Assert.Equal(HttpStatusCode.OK, beforeRevocation.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, revoke.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, afterRevocation.StatusCode);
    }

    [Fact]
    public async Task Deactivating_the_user_stops_their_keys()
    {
        using var server = new IdentityTestServer();
        using var admin = server.CreateClient();
        await IdentityTestServer.LoginAsync(admin, IdentityTestServer.AdminUserName, IdentityTestServer.AdminPassword);
        var memberId = await CreateUserAsync(admin, "member", "MemberPass123!");

        using var member = server.CreateClient();
        await IdentityTestServer.LoginAsync(member, "member", "MemberPass123!");
        var token = (await CreateKeyAsync(member, "Member agent")).GetProperty("token").GetString()!;

        using var agent = server.CreateClient(handleCookies: false);
        using var beforeDeactivation = await SendWithKeyAsync(agent, HttpMethod.Get, "/api/session", token);
        using var deactivate = await SendWithCsrfAsync(
            admin,
            HttpMethod.Post,
            $"/api/admin/users/{memberId}/deactivate");
        using var afterDeactivation = await SendWithKeyAsync(agent, HttpMethod.Get, "/api/session", token);

        Assert.Equal(HttpStatusCode.OK, beforeDeactivation.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, deactivate.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, afterDeactivation.StatusCode);
    }

    [Fact]
    public async Task An_administrative_password_reset_stops_the_users_keys()
    {
        using var server = new IdentityTestServer();
        using var admin = server.CreateClient();
        await IdentityTestServer.LoginAsync(admin, IdentityTestServer.AdminUserName, IdentityTestServer.AdminPassword);
        var memberId = await CreateUserAsync(admin, "member", "MemberPass123!");

        using var member = server.CreateClient();
        await IdentityTestServer.LoginAsync(member, "member", "MemberPass123!");
        var token = (await CreateKeyAsync(member, "Member agent")).GetProperty("token").GetString()!;

        using var agent = server.CreateClient(handleCookies: false);
        using var beforeRecovery = await SendWithKeyAsync(agent, HttpMethod.Get, "/api/session", token);
        using var recovery = await SendWithCsrfAsync(
            admin,
            HttpMethod.Post,
            $"/api/admin/users/{memberId}/password",
            new { newPassword = "RecoveredPass123!" });
        using var afterRecovery = await SendWithKeyAsync(agent, HttpMethod.Get, "/api/session", token);

        // A security change reaches keys through the same stamp that invalidates sessions.
        Assert.Equal(HttpStatusCode.OK, beforeRecovery.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, recovery.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, afterRecovery.StatusCode);
    }

    [Fact]
    public async Task Key_authenticated_writes_need_no_antiforgery_token()
    {
        using var server = new IdentityTestServer();
        using var client = server.CreateClient();
        await IdentityTestServer.LoginAsync(client, IdentityTestServer.AdminUserName, IdentityTestServer.AdminPassword);
        var token = (await CreateKeyAsync(client, "Agent")).GetProperty("token").GetString()!;

        using var agent = server.CreateClient(handleCookies: false);
        using var request = new HttpRequestMessage(HttpMethod.Put, "/api/session/profile")
        {
            Content = JsonContent.Create(new { displayName = "Renamed By Agent", language = "en-GB" }),
        };
        request.Headers.Add("Authorization", $"Bearer {token}");
        using var response = await agent.SendAsync(request, CancellationToken.None);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken.None);

        // No X-CSRF-TOKEN header and no antiforgery cookie: a header credential is
        // not ambient, so there is no cross-site request to forge.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Renamed By Agent", body.GetProperty("displayName").GetString());
    }

    [Fact]
    public async Task Cookie_authenticated_writes_still_require_an_antiforgery_token()
    {
        using var server = new IdentityTestServer();
        using var client = server.CreateClient();
        await IdentityTestServer.LoginAsync(client, IdentityTestServer.AdminUserName, IdentityTestServer.AdminPassword);

        // Issuing a key must not weaken the cookie scheme for the same user.
        await CreateKeyAsync(client, "Agent");
        using var response = await client.PutAsJsonAsync(
            "/api/session/profile",
            new { displayName = "Renamed By Browser", language = "en-GB" },
            CancellationToken.None);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("request.invalid", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Key_management_requires_authentication_and_is_scoped_to_the_owner()
    {
        using var server = new IdentityTestServer();
        using var admin = server.CreateClient();
        await IdentityTestServer.LoginAsync(admin, IdentityTestServer.AdminUserName, IdentityTestServer.AdminPassword);
        await CreateUserAsync(admin, "member", "MemberPass123!");
        var adminKeyId = (await CreateKeyAsync(admin, "Admin agent"))
            .GetProperty("key").GetProperty("id").GetInt32();

        using var member = server.CreateClient();
        await IdentityTestServer.LoginAsync(member, "member", "MemberPass123!");
        var memberKeys = await member.GetFromJsonAsync<JsonElement>(
            "/api/session/profile/api-keys",
            CancellationToken.None);
        using var crossRevoke = await SendWithCsrfAsync(
            member,
            HttpMethod.Delete,
            $"/api/session/profile/api-keys/{adminKeyId}");

        using var anonymous = server.CreateClient(handleCookies: false);
        using var unauthenticated = await anonymous.GetAsync(
            "/api/session/profile/api-keys",
            CancellationToken.None);

        // A member sees only their own keys and cannot revoke someone else's.
        Assert.Empty(memberKeys.EnumerateArray());
        Assert.Equal(HttpStatusCode.NotFound, crossRevoke.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unauthenticated.StatusCode);
    }

    [Fact]
    public async Task Using_a_key_records_its_last_use()
    {
        using var server = new IdentityTestServer();
        using var client = server.CreateClient();
        await IdentityTestServer.LoginAsync(client, IdentityTestServer.AdminUserName, IdentityTestServer.AdminPassword);
        var created = await CreateKeyAsync(client, "Agent");
        var token = created.GetProperty("token").GetString()!;

        using var agent = server.CreateClient(handleCookies: false);
        using var used = await SendWithKeyAsync(agent, HttpMethod.Get, "/api/session", token);
        var list = await client.GetFromJsonAsync<JsonElement>(
            "/api/session/profile/api-keys",
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, used.StatusCode);
        Assert.True(created.GetProperty("key").GetProperty("lastUsedAt").ValueKind is JsonValueKind.Null);
        Assert.True(
            list.EnumerateArray().Single().GetProperty("lastUsedAt").ValueKind is not JsonValueKind.Null,
            "A key that authenticated a request must record a last-use time.");
    }

    [Fact]
    public async Task Creating_a_key_requires_an_antiforgery_token()
    {
        using var server = new IdentityTestServer();
        using var client = server.CreateClient();
        await IdentityTestServer.LoginAsync(client, IdentityTestServer.AdminUserName, IdentityTestServer.AdminPassword);

        using var response = await client.PostAsJsonAsync(
            "/api/session/profile/api-keys",
            new { name = "Forged" },
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Creating_a_key_rejects_a_blank_name()
    {
        using var server = new IdentityTestServer();
        using var client = server.CreateClient();
        await IdentityTestServer.LoginAsync(client, IdentityTestServer.AdminUserName, IdentityTestServer.AdminPassword);

        using var response = await SendWithCsrfAsync(
            client,
            HttpMethod.Post,
            "/api/session/profile/api-keys",
            new { name = "   " });
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(problem.GetProperty("errors").TryGetProperty("name", out _));
    }

    [Fact]
    public async Task Active_keys_are_capped_and_revoking_one_frees_a_slot()
    {
        using var server = new IdentityTestServer();
        using var client = server.CreateClient();
        await IdentityTestServer.LoginAsync(client, IdentityTestServer.AdminUserName, IdentityTestServer.AdminPassword);

        var firstId = (await CreateKeyAsync(client, "Agent 1")).GetProperty("key").GetProperty("id").GetInt32();
        for (var index = 2; index <= ApiKeyPolicy.MaximumActiveKeysPerUser; index++)
        {
            await CreateKeyAsync(client, $"Agent {index}");
        }

        using var overCap = await SendWithCsrfAsync(
            client,
            HttpMethod.Post,
            "/api/session/profile/api-keys",
            new { name = "One too many" });
        var problem = await overCap.Content.ReadFromJsonAsync<JsonElement>(CancellationToken.None);

        // A revoked key no longer occupies a slot.
        using var revoke = await SendWithCsrfAsync(
            client,
            HttpMethod.Delete,
            $"/api/session/profile/api-keys/{firstId}");
        using var afterRevocation = await SendWithCsrfAsync(
            client,
            HttpMethod.Post,
            "/api/session/profile/api-keys",
            new { name = "Replacement" });

        Assert.Equal(HttpStatusCode.Conflict, overCap.StatusCode);
        Assert.Equal("resource.conflict", problem.GetProperty("code").GetString());
        Assert.Equal(HttpStatusCode.NoContent, revoke.StatusCode);
        Assert.Equal(HttpStatusCode.Created, afterRevocation.StatusCode);
    }

    internal static async Task<JsonElement> CreateKeyAsync(
        HttpClient client,
        string name,
        DateTimeOffset? expiresAt = null)
    {
        using var response = await SendWithCsrfAsync(
            client,
            HttpMethod.Post,
            "/api/session/profile/api-keys",
            new { name, expiresAt });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken.None);
    }

    internal static async Task<HttpResponseMessage> SendWithKeyAsync(
        HttpClient client,
        HttpMethod method,
        string url,
        string token)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Add("Authorization", $"Bearer {token}");
        return await client.SendAsync(request, CancellationToken.None);
    }

    private static async Task MutateKeyAsync(IdentityTestServer server, Action<SegarisApiKey> mutate)
    {
        await using var scope = server.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        var key = await database.Set<SegarisApiKey>().SingleAsync(CancellationToken.None);
        mutate(key);
        await database.SaveChangesAsync(CancellationToken.None);
    }

    private static async Task<int> CreateUserAsync(HttpClient admin, string userName, string password)
    {
        using var response = await SendWithCsrfAsync(
            admin,
            HttpMethod.Post,
            "/api/admin/users",
            new { userName, password, role = "User" });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken.None);
        return body.GetProperty("id").GetInt32();
    }

    private static async Task<HttpResponseMessage> SendWithCsrfAsync(
        HttpClient client,
        HttpMethod method,
        string url,
        object? body = null)
    {
        var csrf = await IdentityTestServer.GetCsrfTokenAsync(client);
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Add("X-CSRF-TOKEN", csrf);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await client.SendAsync(request, CancellationToken.None);
    }
}
