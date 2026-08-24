using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Blackwing.Api.IntegrationTests.Infrastructure;

namespace Blackwing.Api.IntegrationTests.Identity;

/// <summary>
/// The authentication surface as a client sees it: what a successful sign-in yields, what every
/// failure mode yields, and what stops a session from outliving the account behind it.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class AuthenticationTests(PostgresFixture postgres)
{
    /// <summary>Acceptance case 1.</summary>
    [Fact]
    public async Task Login_establishes_a_session_cookie_and_the_session_endpoint_returns_the_user_and_roles()
    {
        using var server = await IdentityTestServer.StartAsync(RequirePostgres());
        using var client = server.CreateClient();

        using var login = await IdentityTestServer.PostLoginAsync(
            client,
            IdentityTestServer.AdminUserName,
            IdentityTestServer.AdminPassword);
        var session = await client.GetFromJsonAsync<JsonElement>("/api/session", CancellationToken.None);

        Assert.Equal(HttpStatusCode.NoContent, login.StatusCode);

        var setCookie = string.Join(
            "; ",
            login.Headers.TryGetValues("Set-Cookie", out var values) ? values : []);
        Assert.Contains("blackwing.session", setCookie, StringComparison.Ordinal);
        Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", setCookie, StringComparison.OrdinalIgnoreCase);

        Assert.Equal(IdentityTestServer.AdminUserName, session.GetProperty("userName").GetString());
        Assert.Equal(IdentityTestServer.AdminUserName, session.GetProperty("displayName").GetString());
        Assert.True(session.GetProperty("userId").GetInt32() > 0);
        Assert.Equal(
            ["Admin"],
            session.GetProperty("roles")
                .EnumerateArray()
                .Select(role => role.GetString() ?? string.Empty)
                .ToArray());
    }

    /// <summary>
    /// Acceptance case 2. An unknown account, a wrong password and a deactivated account must be
    /// indistinguishable from outside: same status, and the same body once the per-request trace
    /// identifier is set aside.
    /// </summary>
    [Fact]
    public async Task Unknown_user_wrong_password_and_deactivated_account_answer_401_with_an_identical_body()
    {
        using var server = await IdentityTestServer.StartAsync(RequirePostgres());
        using var admin = server.CreateClient();
        await IdentityTestServer.LoginAsync(
            admin,
            IdentityTestServer.AdminUserName,
            IdentityTestServer.AdminPassword);

        var memberId = await IdentityTestServer.CreateUserAsync(admin, "member", "MemberPass123!", "User");
        using var deactivation = await IdentityTestServer.SendWithCsrfAsync(
            admin,
            HttpMethod.Post,
            $"/api/admin/users/{memberId}/deactivate");
        deactivation.EnsureSuccessStatusCode();

        using var unknownClient = server.CreateClient();
        using var wrongPasswordClient = server.CreateClient();
        using var deactivatedClient = server.CreateClient();

        using var unknown = await IdentityTestServer.PostLoginAsync(unknownClient, "ghost", "GhostPass123!");
        using var wrongPassword = await IdentityTestServer.PostLoginAsync(
            wrongPasswordClient,
            IdentityTestServer.AdminUserName,
            "WrongPassword123!");
        using var deactivated = await IdentityTestServer.PostLoginAsync(
            deactivatedClient,
            "member",
            "MemberPass123!");

        Assert.Equal(HttpStatusCode.Unauthorized, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, deactivated.StatusCode);

        var unknownBody = await NormalizeProblemAsync(unknown);
        Assert.Equal(unknownBody, await NormalizeProblemAsync(wrongPassword));
        Assert.Equal(unknownBody, await NormalizeProblemAsync(deactivated));
        Assert.Contains("authentication.required", unknownBody, StringComparison.Ordinal);

        // Neither the account that exists nor the one that does not may be echoed back.
        Assert.DoesNotContain("member", unknownBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ghost", unknownBody, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Acceptance case 3. Five failures lock the account, so the sixth attempt fails even when the
    /// password is the right one.
    /// </summary>
    [Fact]
    public async Task The_sixth_attempt_finds_the_account_locked_even_with_the_correct_password()
    {
        using var server = await IdentityTestServer.StartAsync(RequirePostgres());
        using var client = server.CreateClient();

        for (var attempt = 0; attempt < 5; attempt++)
        {
            using var failure = await IdentityTestServer.PostLoginAsync(
                client,
                IdentityTestServer.AdminUserName,
                "WrongPassword123!");
            Assert.Equal(HttpStatusCode.Unauthorized, failure.StatusCode);
        }

        using var locked = await IdentityTestServer.PostLoginAsync(
            client,
            IdentityTestServer.AdminUserName,
            IdentityTestServer.AdminPassword);

        Assert.Equal(HttpStatusCode.Unauthorized, locked.StatusCode);
    }

    /// <summary>Acceptance case 5.</summary>
    [Fact]
    public async Task A_mutation_without_the_antiforgery_header_is_rejected()
    {
        using var server = await IdentityTestServer.StartAsync(RequirePostgres());
        using var client = server.CreateClient();
        await IdentityTestServer.LoginAsync(
            client,
            IdentityTestServer.AdminUserName,
            IdentityTestServer.AdminPassword);

        using var withoutToken = await client.DeleteAsync("/api/session", CancellationToken.None);
        var problem = await withoutToken.Content.ReadFromJsonAsync<JsonElement>(CancellationToken.None);

        // With the token the very same call succeeds, so the rejection is the token's doing and
        // not some unrelated failure of the endpoint.
        using var withToken = await IdentityTestServer.SendWithCsrfAsync(
            client,
            HttpMethod.Delete,
            "/api/session");
        using var afterLogout = await client.GetAsync("/api/session", CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, withoutToken.StatusCode);
        Assert.Equal("request.invalid", problem.GetProperty("code").GetString());
        Assert.Equal(HttpStatusCode.NoContent, withToken.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);
    }

    /// <summary>
    /// Acceptance case 9. The security stamp validation interval is zero, so a deactivated
    /// account loses its live session on its next request rather than up to thirty minutes later.
    /// </summary>
    [Fact]
    public async Task Deactivating_an_account_invalidates_its_live_session_on_the_next_request()
    {
        using var server = await IdentityTestServer.StartAsync(RequirePostgres());
        using var admin = server.CreateClient();
        await IdentityTestServer.LoginAsync(
            admin,
            IdentityTestServer.AdminUserName,
            IdentityTestServer.AdminPassword);
        var memberId = await IdentityTestServer.CreateUserAsync(admin, "member", "MemberPass123!", "User");

        using var member = server.CreateClient();
        await IdentityTestServer.LoginAsync(member, "member", "MemberPass123!");
        using var beforeDeactivation = await member.GetAsync("/api/session", CancellationToken.None);

        using var deactivate = await IdentityTestServer.SendWithCsrfAsync(
            admin,
            HttpMethod.Post,
            $"/api/admin/users/{memberId}/deactivate");
        using var afterDeactivation = await member.GetAsync("/api/session", CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, beforeDeactivation.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, deactivate.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, afterDeactivation.StatusCode);
    }

    /// <summary>
    /// Reduces a problem response to a comparable string. The trace identifier is dropped because
    /// it is unique per request by design; everything else must match, or the shape of the answer
    /// would tell a caller which of the three failures it hit.
    /// </summary>
    private static async Task<string> NormalizeProblemAsync(HttpResponseMessage response)
    {
        var payload = await response.Content.ReadAsStringAsync(CancellationToken.None);
        using var document = JsonDocument.Parse(payload);

        var properties = document.RootElement
            .EnumerateObject()
            .Where(property => !string.Equals(property.Name, "traceId", StringComparison.Ordinal))
            .OrderBy(property => property.Name, StringComparer.Ordinal)
            .Select(property => $"{property.Name}={property.Value.GetRawText()}");

        return string.Join("|", properties);
    }

    private PostgresFixture RequirePostgres()
    {
        ArgumentNullException.ThrowIfNull(postgres);
        return postgres;
    }
}
