using System.Net;
using Blackwing.Api.IntegrationTests.Infrastructure;

namespace Blackwing.Api.IntegrationTests.Identity;

/// <summary>
/// Without a persisted key ring the Data Protection keys live in the container's ephemeral
/// filesystem, and every restart of the backend silently signs every household member out. The
/// symptom looks like a session bug, so it is pinned here.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class DataProtectionRestartTests(PostgresFixture postgres)
{
    /// <summary>Acceptance case 10.</summary>
    [Fact]
    public async Task Persisted_data_protection_keys_keep_the_session_cookie_valid_across_a_restart()
    {
        ArgumentNullException.ThrowIfNull(postgres);

        var connectionString = await postgres.CreateDatabaseAsync(CancellationToken.None);
        var keysPath = Path.Combine(Path.GetTempPath(), $"blackwing-restart-keys-{Guid.NewGuid():N}");

        try
        {
            string sessionCookie;

            // Cookies are handled by hand here: the cookie has to outlive the client that got it.
            using (var before = IdentityTestServer.Restart(connectionString, keysPath))
            using (var beforeClient = before.CreateClient(handleCookies: false))
            {
                using var login = await IdentityTestServer.PostLoginAsync(
                    beforeClient,
                    IdentityTestServer.AdminUserName,
                    IdentityTestServer.AdminPassword);
                login.EnsureSuccessStatusCode();
                sessionCookie = ExtractSessionCookie(login);
            }

            // A second host over the same database and the same key directory is what a restarted
            // container is: new process, same durable state.
            using (var after = IdentityTestServer.Restart(connectionString, keysPath))
            using (var afterClient = after.CreateClient(handleCookies: false))
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, "/api/session");
                request.Headers.Add("Cookie", sessionCookie);
                using var response = await afterClient.SendAsync(request, CancellationToken.None);

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }
        finally
        {
            IdentityTestServer.TryDeleteDirectory(keysPath);
        }
    }

    private static string ExtractSessionCookie(HttpResponseMessage response)
    {
        var cookies = response.Headers.TryGetValues("Set-Cookie", out var values) ? values : [];
        var sessionCookie = cookies.FirstOrDefault(cookie =>
            cookie.StartsWith("blackwing.session=", StringComparison.Ordinal));

        Assert.NotNull(sessionCookie);
        var end = sessionCookie!.IndexOf(';', StringComparison.Ordinal);
        return end < 0 ? sessionCookie : sessionCookie[..end];
    }
}
