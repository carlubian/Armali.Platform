using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Blackwing.Api.IntegrationTests.Identity;
using Blackwing.Api.IntegrationTests.Infrastructure;

namespace Blackwing.Api.IntegrationTests.Ownership;

/// <summary>
/// The privacy perimeter, demonstrated end to end over the ownership canary with real accounts
/// and real HTTP. Everything else in the milestone rests on this being true, so it is asserted
/// from outside the process rather than by inspecting the query filter.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class OwnershipIsolationTests(PostgresFixture postgres)
{
    /// <summary>Acceptance case 7.</summary>
    [Fact]
    public async Task Two_accounts_never_see_each_others_probes_and_a_foreign_identifier_answers_404()
    {
        using var server = await IdentityTestServer.StartAsync(RequirePostgres());
        using var admin = server.CreateClient();
        await IdentityTestServer.LoginAsync(
            admin,
            IdentityTestServer.AdminUserName,
            IdentityTestServer.AdminPassword);
        await IdentityTestServer.CreateUserAsync(admin, "alice", "AlicePass1234!", "User");
        await IdentityTestServer.CreateUserAsync(admin, "bob", "BobPass123456!", "User");

        using var alice = server.CreateClient();
        await IdentityTestServer.LoginAsync(alice, "alice", "AlicePass1234!");
        using var bob = server.CreateClient();
        await IdentityTestServer.LoginAsync(bob, "bob", "BobPass123456!");

        var aliceProbeId = await CreateProbeAsync(alice, "alice-probe");
        var bobProbeId = await CreateProbeAsync(bob, "bob-probe");

        var aliceLabels = await ListLabelsAsync(alice);
        var bobLabels = await ListLabelsAsync(bob);

        using var foreignRead = await alice.GetAsync(
            $"/api/platform/ownership/{bobProbeId}",
            CancellationToken.None);
        using var foreignDelete = await IdentityTestServer.SendWithCsrfAsync(
            alice,
            HttpMethod.Delete,
            $"/api/platform/ownership/{bobProbeId}");
        var bobLabelsAfterAttempt = await ListLabelsAsync(bob);

        Assert.Equal(["alice-probe"], aliceLabels);
        Assert.Equal(["bob-probe"], bobLabels);
        Assert.NotEqual(aliceProbeId, bobProbeId);

        // 404, never 403: a 403 would confirm that the identifier exists, which is itself a leak.
        Assert.Equal(HttpStatusCode.NotFound, foreignRead.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, foreignDelete.StatusCode);
        Assert.Equal(["bob-probe"], bobLabelsAfterAttempt);
    }

    /// <summary>
    /// Acceptance case 8. Administration is a surface over accounts, not over content: the role
    /// that can reset anyone's password still cannot read a single row that belongs to them.
    /// </summary>
    [Fact]
    public async Task An_administrator_neither_lists_nor_retrieves_another_accounts_probe()
    {
        using var server = await IdentityTestServer.StartAsync(RequirePostgres());
        using var admin = server.CreateClient();
        await IdentityTestServer.LoginAsync(
            admin,
            IdentityTestServer.AdminUserName,
            IdentityTestServer.AdminPassword);
        await IdentityTestServer.CreateUserAsync(admin, "alice", "AlicePass1234!", "User");

        using var alice = server.CreateClient();
        await IdentityTestServer.LoginAsync(alice, "alice", "AlicePass1234!");
        var aliceProbeId = await CreateProbeAsync(alice, "alice-probe");

        var adminLabels = await ListLabelsAsync(admin);
        using var adminRead = await admin.GetAsync(
            $"/api/platform/ownership/{aliceProbeId}",
            CancellationToken.None);
        using var adminDelete = await IdentityTestServer.SendWithCsrfAsync(
            admin,
            HttpMethod.Delete,
            $"/api/platform/ownership/{aliceProbeId}");

        Assert.Empty(adminLabels);
        Assert.Equal(HttpStatusCode.NotFound, adminRead.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, adminDelete.StatusCode);
        Assert.Equal(["alice-probe"], await ListLabelsAsync(alice));
    }

    /// <summary>
    /// The create contract has no owner field, so impersonation cannot even be expressed. An
    /// owner smuggled into the body is ignored and the row still belongs to its creator.
    /// </summary>
    [Fact]
    public async Task An_owner_supplied_in_the_request_body_is_ignored()
    {
        using var server = await IdentityTestServer.StartAsync(RequirePostgres());
        using var admin = server.CreateClient();
        await IdentityTestServer.LoginAsync(
            admin,
            IdentityTestServer.AdminUserName,
            IdentityTestServer.AdminPassword);
        await IdentityTestServer.CreateUserAsync(admin, "alice", "AlicePass1234!", "User");
        var bobId = await IdentityTestServer.CreateUserAsync(admin, "bob", "BobPass123456!", "User");

        using var alice = server.CreateClient();
        await IdentityTestServer.LoginAsync(alice, "alice", "AlicePass1234!");
        using var bob = server.CreateClient();
        await IdentityTestServer.LoginAsync(bob, "bob", "BobPass123456!");

        using var created = await IdentityTestServer.SendWithCsrfAsync(
            alice,
            HttpMethod.Post,
            "/api/platform/ownership",
            new { label = "smuggled", ownerUserId = bobId });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(["smuggled"], await ListLabelsAsync(alice));
        Assert.Empty(await ListLabelsAsync(bob));
    }

    private static async Task<int> CreateProbeAsync(HttpClient client, string label)
    {
        using var response = await IdentityTestServer.SendWithCsrfAsync(
            client,
            HttpMethod.Post,
            "/api/platform/ownership",
            new { label });
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken.None);
        return body.GetProperty("id").GetInt32();
    }

    private static async Task<string[]> ListLabelsAsync(HttpClient client)
    {
        var probes = await client.GetFromJsonAsync<JsonElement>(
            "/api/platform/ownership",
            CancellationToken.None);

        return probes.EnumerateArray()
            .Select(probe => probe.GetProperty("label").GetString() ?? string.Empty)
            .ToArray();
    }

    private PostgresFixture RequirePostgres()
    {
        ArgumentNullException.ThrowIfNull(postgres);
        return postgres;
    }
}
