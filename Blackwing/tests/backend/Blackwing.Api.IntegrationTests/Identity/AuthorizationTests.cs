using System.Net;
using Blackwing.Api.IntegrationTests.Infrastructure;

namespace Blackwing.Api.IntegrationTests.Identity;

/// <summary>
/// The two answers an unauthorised caller may get, and the line between them: 401 means "not
/// authenticated", 403 means "authenticated but not allowed". Cookie authentication defaults to
/// redirecting instead, which is why the module overrides both events.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class AuthorizationTests(PostgresFixture postgres)
{
    /// <summary>Acceptance case 6, first half.</summary>
    [Theory]
    [InlineData("/api/session")]
    [InlineData("/api/admin/users")]
    [InlineData("/api/platform/ownership")]
    public async Task An_anonymous_caller_gets_401_and_never_a_redirect(string url)
    {
        using var server = await IdentityTestServer.StartAsync(RequirePostgres());
        using var client = server.CreateClient();

        using var response = await client.GetAsync(url, CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(response.Headers.Location);
    }

    /// <summary>Acceptance case 6, second half.</summary>
    [Fact]
    public async Task An_authenticated_user_without_the_admin_role_gets_403_from_the_admin_surface()
    {
        using var server = await IdentityTestServer.StartAsync(RequirePostgres());
        using var admin = server.CreateClient();
        await IdentityTestServer.LoginAsync(
            admin,
            IdentityTestServer.AdminUserName,
            IdentityTestServer.AdminPassword);
        await IdentityTestServer.CreateUserAsync(admin, "member", "MemberPass123!", "User");

        using var member = server.CreateClient();
        await IdentityTestServer.LoginAsync(member, "member", "MemberPass123!");

        using var forbidden = await member.GetAsync("/api/admin/users", CancellationToken.None);
        using var allowed = await admin.GetAsync("/api/admin/users", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
    }

    private PostgresFixture RequirePostgres()
    {
        ArgumentNullException.ThrowIfNull(postgres);
        return postgres;
    }
}
