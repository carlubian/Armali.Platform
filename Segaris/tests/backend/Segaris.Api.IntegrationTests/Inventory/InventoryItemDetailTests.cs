using System.Net;
using System.Net.Http.Json;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Inventory.Contracts;
using Segaris.Api.Modules.Inventory.Domain;
using Segaris.Shared.Authorization;

namespace Segaris.Api.IntegrationTests.Inventory;

public sealed class InventoryItemDetailTests
{
    [Fact]
    public async Task Detail_returns_the_item_with_ordered_suppliers_and_no_attachments()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var itemId = await InventoryTestData.SeedItemAsync(
            server.Services,
            founderId,
            name: "Detergent",
            status: InventoryItemStatus.Active,
            notes: "Concentrated",
            categoryName: "Cleaning",
            locationName: "Pantry",
            currentStock: 4m,
            minimumStock: 2m,
            supplierNames: ["IKEA", "Amazon"]);

        var item = await client.GetFromJsonAsync<InventoryItemResponse>(
            $"/api/inventory/items/{itemId}",
            CancellationToken.None);

        Assert.NotNull(item);
        Assert.Equal("Detergent", item.Name);
        Assert.Equal("Active", item.Status);
        Assert.Equal("Concentrated", item.Notes);
        Assert.Equal("Cleaning", item.CategoryName);
        Assert.Equal("Pantry", item.LocationName);
        Assert.Equal(4m, item.CurrentStock);
        Assert.Equal(2m, item.MinimumStock);
        Assert.Equal("Public", item.Visibility);
        Assert.Equal(new[] { "Amazon", "IKEA" }, item.Suppliers.Select(supplier => supplier.SupplierName).ToArray());
        Assert.Empty(item.Attachments);
        Assert.Equal(founderId, item.CreatedById);
    }

    [Fact]
    public async Task Detail_returns_not_found_for_a_missing_item()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();

        using var response = await client.GetAsync("/api/inventory/items/999999", CancellationToken.None);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("inventory.item.not_found", problem!.Code);
    }

    [Fact]
    public async Task Another_users_private_item_is_reported_as_not_found()
    {
        using var server = new CapexTestServer();
        using var admin = await server.CreateAuthenticatedClientAsync();
        var memberId = await server.CreateUserAsync("member", "MemberPass123!");
        var privateItemId = await InventoryTestData.SeedItemAsync(
            server.Services,
            memberId,
            name: "Member private",
            visibility: RecordVisibility.Private);

        // No privacy bypass for the administrator: the private item is hidden as a
        // not-found rather than disclosed.
        using var adminResponse = await admin.GetAsync($"/api/inventory/items/{privateItemId}", CancellationToken.None);
        Assert.Equal(HttpStatusCode.NotFound, adminResponse.StatusCode);

        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, "member", "MemberPass123!");
        var owned = await member.GetFromJsonAsync<InventoryItemResponse>(
            $"/api/inventory/items/{privateItemId}",
            CancellationToken.None);
        Assert.NotNull(owned);
        Assert.Equal("Member private", owned.Name);
    }

    private sealed record ProblemPayload(string? Code);
}
