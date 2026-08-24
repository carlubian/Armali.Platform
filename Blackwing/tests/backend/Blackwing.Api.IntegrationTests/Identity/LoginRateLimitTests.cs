using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Blackwing.Api.IntegrationTests.Infrastructure;

namespace Blackwing.Api.IntegrationTests.Identity;

/// <summary>
/// The login limiter and the forwarded headers it depends on, exercised together.
/// </summary>
/// <remarks>
/// Testing the quota without two distinct client addresses would pass just as happily against a
/// limiter that put every caller in the same partition, which is exactly the failure mode this
/// phase set out to avoid: behind the Caddy ingress the connection address is always Caddy's, so
/// one client could otherwise exhaust the quota of the entire household.
/// </remarks>
[Collection(PostgresCollection.Name)]
public sealed class LoginRateLimitTests(PostgresFixture postgres)
{
    private const string FirstClientAddress = "203.0.113.10";
    private const string SecondClientAddress = "203.0.113.11";

    /// <summary>Acceptance case 4.</summary>
    [Fact]
    public async Task The_eleventh_login_from_one_address_is_throttled_while_another_address_keeps_its_quota()
    {
        using var server = await IdentityTestServer.StartAsync(RequirePostgres());
        using var first = server.CreateClient();
        using var second = server.CreateClient();

        first.DefaultRequestHeaders.Add("X-Forwarded-For", FirstClientAddress);
        second.DefaultRequestHeaders.Add("X-Forwarded-For", SecondClientAddress);

        // The account does not exist, so these attempts are refused by the endpoint without ever
        // touching the lockout counter of a real account.
        for (var attempt = 1; attempt <= 10; attempt++)
        {
            using var allowed = await IdentityTestServer.PostLoginAsync(first, "ghost", "GhostPass123!");
            Assert.Equal(HttpStatusCode.Unauthorized, allowed.StatusCode);
        }

        using var throttled = await IdentityTestServer.PostLoginAsync(first, "ghost", "GhostPass123!");
        var problem = await throttled.Content.ReadFromJsonAsync<JsonElement>(CancellationToken.None);

        // The second address has spent none of its own permits.
        using var otherAddress = await IdentityTestServer.PostLoginAsync(second, "ghost", "GhostPass123!");

        Assert.Equal(HttpStatusCode.TooManyRequests, throttled.StatusCode);

        // A refusal answers in problem+json rather than with an empty body the client cannot tell
        // apart from a network failure.
        Assert.Equal("application/problem+json", throttled.Content.Headers.ContentType?.MediaType);
        Assert.Equal("request.rate_limited", problem.GetProperty("code").GetString());
        Assert.Equal(HttpStatusCode.Unauthorized, otherAddress.StatusCode);
    }

    private PostgresFixture RequirePostgres()
    {
        ArgumentNullException.ThrowIfNull(postgres);
        return postgres;
    }
}
