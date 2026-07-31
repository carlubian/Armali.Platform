using System.Net;
using System.Net.Http.Json;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Inventory.Contracts;
using Segaris.Api.Modules.Inventory.Domain;
using Segaris.Shared.Authorization;

namespace Segaris.Api.IntegrationTests.Inventory;

/// <summary>
/// Covers the computed replenishment shopping list: its inclusion rule, the
/// <c>Required</c> and <c>Optional</c> blocks, the guaranteed ordering, and the
/// standard Inventory privacy rules. Assertions are grouped per host because each
/// test server pays the API startup cost.
/// </summary>
public sealed class InventoryShoppingListTests
{
    [Fact]
    public async Task Shopping_list_requires_authentication()
    {
        using var server = new CapexTestServer();
        using var client = server.CreateClient();

        using var response = await client.GetAsync("/api/inventory/shopping-list", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Shopping_list_lists_only_active_items_that_track_a_threshold()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);

        Assert.Empty(await EntriesAsync(client));

        await InventoryTestData.SeedItemAsync(
            server.Services,
            founderId,
            name: "Candidate low",
            status: InventoryItemStatus.Candidate,
            currentStock: 0m,
            minimumStock: 5m);
        await InventoryTestData.SeedItemAsync(
            server.Services,
            founderId,
            name: "Deprecated low",
            status: InventoryItemStatus.Deprecated,
            currentStock: 0m,
            minimumStock: 5m);

        // A zero minimum means no replenishment threshold, whatever the current stock is.
        await InventoryTestData.SeedItemAsync(
            server.Services,
            founderId,
            name: "Untracked and empty",
            currentStock: 0m,
            minimumStock: 0m);
        await InventoryTestData.SeedItemAsync(
            server.Services,
            founderId,
            name: "Untracked and stocked",
            currentStock: 12m,
            minimumStock: 0m);
        await InventoryTestData.SeedItemAsync(
            server.Services,
            founderId,
            name: "Above the minimum",
            currentStock: 6m,
            minimumStock: 5m);

        Assert.Empty(await EntriesAsync(client));

        await InventoryTestData.SeedItemAsync(
            server.Services,
            founderId,
            name: "Olive oil",
            currentStock: 1m,
            minimumStock: 5m);

        var listed = Assert.Single(await EntriesAsync(client));
        Assert.Equal("Olive oil", listed.Name);
    }

    [Fact]
    public async Task Blocks_split_on_the_minimum_stock_boundary_and_carry_exact_quantities()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);

        var coffeeId = await InventoryTestData.SeedItemAsync(
            server.Services,
            founderId,
            name: "Coffee",
            currentStock: 0.25m,
            minimumStock: 2.75m,
            supplierNames: ["Leroy Merlin", "Amazon", "Carrefour"]);
        var riceId = await InventoryTestData.SeedItemAsync(
            server.Services,
            founderId,
            name: "Rice",
            currentStock: 3m,
            minimumStock: 3m,
            supplierNames: ["IKEA"]);

        var entries = await EntriesAsync(client);

        Assert.Equal(2, entries.Count);

        // Strictly below the minimum is Required, and the quantity is the exact difference.
        var coffee = entries[0];
        Assert.Equal(coffeeId, coffee.ItemId);
        Assert.Equal("Required", coffee.Block);
        Assert.Equal(2.5m, coffee.RequiredQuantity);
        Assert.Equal(["Amazon", "Carrefour", "Leroy Merlin"], coffee.Suppliers);

        // Exactly at the minimum is Optional, and carries no quantity at all.
        var rice = entries[1];
        Assert.Equal(riceId, rice.ItemId);
        Assert.Equal("Optional", rice.Block);
        Assert.Null(rice.RequiredQuantity);
        Assert.Equal(["IKEA"], rice.Suppliers);
    }

    [Fact]
    public async Task Entries_are_ordered_by_block_then_category_order_then_item_name()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);

        await InventoryTestData.SeedItemAsync(
            server.Services,
            founderId,
            name: "Paper",
            categoryName: "Office",
            currentStock: 0m,
            minimumStock: 1m);
        await InventoryTestData.SeedItemAsync(
            server.Services,
            founderId,
            name: "banana",
            categoryName: "Food",
            currentStock: 0m,
            minimumStock: 2m);
        await InventoryTestData.SeedItemAsync(
            server.Services,
            founderId,
            name: "Apple",
            categoryName: "Food",
            currentStock: 0m,
            minimumStock: 2m);
        await InventoryTestData.SeedItemAsync(
            server.Services,
            founderId,
            name: "Zucchini",
            categoryName: "Food",
            currentStock: 2m,
            minimumStock: 2m);

        var entries = await EntriesAsync(client);

        // The Required block comes first as a whole, categories follow the catalog sort
        // order, and items are alphabetical inside a category regardless of casing.
        Assert.Equal(
            ["Apple", "banana", "Paper", "Zucchini"],
            entries.Select(entry => entry.Name).ToArray());
        Assert.Equal(
            ["Required", "Required", "Required", "Optional"],
            entries.Select(entry => entry.Block).ToArray());
        Assert.Equal(
            ["Food", "Food", "Office", "Food"],
            entries.Select(entry => entry.CategoryName).ToArray());

        // Categories without listed items are absent, and the sort order is exposed.
        Assert.Equal(["Food", "Office"], entries.Select(entry => entry.CategoryName).Distinct().Order().ToArray());
        Assert.True(entries[0].CategorySortOrder < entries[2].CategorySortOrder);
    }

    [Fact]
    public async Task Shopping_list_hides_private_items_of_other_users_from_every_reader()
    {
        using var server = new CapexTestServer();
        using var admin = await server.CreateAuthenticatedClientAsync();
        var memberId = await server.CreateUserAsync("inventory-shopping-reader", "ShoppingReader123!");

        await InventoryTestData.SeedItemAsync(
            server.Services,
            memberId,
            name: "Private soap",
            currentStock: 0m,
            minimumStock: 2m,
            visibility: RecordVisibility.Private);
        await InventoryTestData.SeedItemAsync(
            server.Services,
            memberId,
            name: "Public soap",
            currentStock: 0m,
            minimumStock: 2m);

        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, "inventory-shopping-reader", "ShoppingReader123!");

        // The administrator receives no privacy bypass; the owning member sees both.
        Assert.Equal(["Public soap"], (await EntriesAsync(admin)).Select(entry => entry.Name).ToArray());
        Assert.Equal(
            ["Private soap", "Public soap"],
            (await EntriesAsync(member)).Select(entry => entry.Name).ToArray());
    }

    private static async Task<IReadOnlyList<InventoryShoppingListEntryResponse>> EntriesAsync(HttpClient client)
    {
        var response = await client.GetFromJsonAsync<InventoryShoppingListResponse>(
            "/api/inventory/shopping-list",
            CancellationToken.None);
        Assert.NotNull(response);
        return response.Entries;
    }
}
