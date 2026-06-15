using System.Net;
using System.Net.Http.Json;
using Segaris.Api.Modules.Capex;
using Segaris.Api.Modules.Capex.Contracts;
using Segaris.Api.Modules.Capex.Domain;
using Segaris.Api.Modules.Configuration;
using Segaris.Shared.Authorization;

namespace Segaris.Api.IntegrationTests.Capex;

public sealed class CapexEntryDetailTests
{
    [Fact]
    public async Task Detail_returns_ordered_items_audit_data_and_an_empty_attachment_list()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var entryId = await CapexTestData.SeedEntryAsync(
            server.Services,
            founderId,
            title: "Detailed entry",
            supplierName: "Amazon",
            items: [new("First", 2m, 1.50m), new("Second", 1m, 0.99m)]);

        var entry = await client.GetFromJsonAsync<CapexEntryResponse>(
            $"/api/capex/entries/{entryId}",
            CancellationToken.None);

        Assert.NotNull(entry);
        Assert.Equal("Detailed entry", entry.Title);
        Assert.Equal("Amazon", entry.SupplierName);
        Assert.Equal(new[] { 0, 1 }, entry.Items.Select(item => item.Position).ToArray());
        Assert.Equal(new[] { 3.00m, 0.99m }, entry.Items.Select(item => item.LineAmount).ToArray());
        Assert.Equal(3.99m, entry.TotalAmount);
        Assert.Equal(CapexTestServer.AdminUserName, entry.CreatedByName);
        Assert.Empty(entry.Attachments);
    }

    [Fact]
    public async Task Missing_entry_returns_a_capex_not_found_problem()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();

        using var response = await client.GetAsync("/api/capex/entries/9999", CancellationToken.None);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("capex.entry.not_found", problem!.Code);
    }

    [Fact]
    public async Task Hidden_private_entry_returns_the_same_not_found_as_an_absent_one()
    {
        using var server = new CapexTestServer();
        using var admin = await server.CreateAuthenticatedClientAsync();
        var memberId = await server.CreateUserAsync("owner", "OwnerPass123!");
        var privateId = await CapexTestData.SeedEntryAsync(
            server.Services,
            memberId,
            title: "Owner private",
            visibility: RecordVisibility.Private);

        using var adminResponse = await admin.GetAsync($"/api/capex/entries/{privateId}", CancellationToken.None);
        var problem = await adminResponse.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        // Administrators receive no privacy bypass: the private entry is invisible.
        Assert.Equal(HttpStatusCode.NotFound, adminResponse.StatusCode);
        Assert.Equal("capex.entry.not_found", problem!.Code);
    }

    [Fact]
    public async Task Creator_can_open_their_own_private_entry()
    {
        using var server = new CapexTestServer();
        var memberId = await server.CreateUserAsync("creator", "CreatorPass123!");
        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, "creator", "CreatorPass123!");
        var privateId = await CapexTestData.SeedEntryAsync(
            server.Services,
            memberId,
            title: "My private",
            visibility: RecordVisibility.Private);

        var entry = await member.GetFromJsonAsync<CapexEntryResponse>(
            $"/api/capex/entries/{privateId}",
            CancellationToken.None);

        Assert.NotNull(entry);
        Assert.Equal("My private", entry.Title);
        Assert.Equal("Private", entry.Visibility);
    }

    private sealed record ProblemPayload(string? Code);
}
