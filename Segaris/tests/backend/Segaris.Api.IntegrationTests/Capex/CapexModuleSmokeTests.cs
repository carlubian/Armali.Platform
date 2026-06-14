using System.Net;

namespace Segaris.Api.IntegrationTests.Capex;

/// <summary>
/// Wave 0 smoke coverage: the full integration host boots with the new
/// Configuration, Capex, and Launcher modules registered (a duplicate module
/// name would throw during composition), and the shared entry request builder
/// produces the documented creation defaults.
/// </summary>
public sealed class CapexModuleSmokeTests
{
    [Fact]
    public async Task Host_starts_with_the_new_modules_registered()
    {
        using var server = new CapexTestServer();
        using var client = server.CreateClient();

        var token = await CapexTestServer.GetCsrfTokenAsync(client);

        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public async Task Admin_can_authenticate_against_the_shared_fixture()
    {
        using var server = new CapexTestServer();

        using var client = await server.CreateAuthenticatedClientAsync();
        using var session = await client.GetAsync("/api/session", CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, session.StatusCode);
    }

    [Fact]
    public void Default_entry_request_has_one_item_and_documented_defaults()
    {
        var request = CapexEntryRequestBuilder.Default().BuildCreate();

        Assert.Equal("Expense", request.MovementType);
        Assert.Equal("Planning", request.Status);
        Assert.Equal("Public", request.Visibility);
        var item = Assert.Single(request.Items);
        Assert.Equal(1m, item.Quantity);
        Assert.Equal(0m, item.UnitAmount);
    }
}
