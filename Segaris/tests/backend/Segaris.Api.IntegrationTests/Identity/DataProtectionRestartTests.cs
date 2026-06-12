using System.Net;
using System.Net.Http.Json;

namespace Segaris.Api.IntegrationTests.Identity;

public sealed class DataProtectionRestartTests
{
    [Fact]
    public async Task Persisted_data_protection_keys_preserve_sessions_across_restarts()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"segaris-restart-{Guid.NewGuid():N}.db");
        var keysPath = Path.Combine(Path.GetTempPath(), $"segaris-restart-keys-{Guid.NewGuid():N}");

        try
        {
            string sessionCookie;
            using (var first = new IdentityTestServer(databasePath, keysPath, deleteOnDispose: false))
            using (var firstClient = first.CreateClient(handleCookies: false))
            {
                using var login = await firstClient.PostAsJsonAsync(
                    "/api/session",
                    new { userName = IdentityTestServer.AdminUserName, password = IdentityTestServer.AdminPassword },
                    CancellationToken.None);
                login.EnsureSuccessStatusCode();
                sessionCookie = ExtractSessionCookie(login);
            }

            using (var second = new IdentityTestServer(databasePath, keysPath, deleteOnDispose: false))
            using (var secondClient = second.CreateClient(handleCookies: false))
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, "/api/session");
                request.Headers.Add("Cookie", sessionCookie);
                using var response = await secondClient.SendAsync(request, CancellationToken.None);

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }
        finally
        {
            IdentityTestServer.TryDeleteFile(databasePath);
            IdentityTestServer.TryDeleteDirectory(keysPath);
        }
    }

    private static string ExtractSessionCookie(HttpResponseMessage response)
    {
        var cookies = response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values
            : [];
        var sessionCookie = cookies.FirstOrDefault(cookie =>
            cookie.StartsWith("segaris.session=", StringComparison.Ordinal));

        Assert.NotNull(sessionCookie);
        var end = sessionCookie!.IndexOf(';', StringComparison.Ordinal);
        return end < 0 ? sessionCookie : sessionCookie[..end];
    }
}
