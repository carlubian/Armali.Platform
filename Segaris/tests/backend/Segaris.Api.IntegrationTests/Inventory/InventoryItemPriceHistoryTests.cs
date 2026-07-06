using System.Net;
using System.Net.Http.Json;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Inventory.Contracts;
using Segaris.Shared.Authorization;

namespace Segaris.Api.IntegrationTests.Inventory;

public sealed class InventoryItemPriceHistoryTests
{
    [Fact]
    public async Task Price_history_requires_authentication()
    {
        using var server = new CapexTestServer();
        using var client = server.CreateClient();

        using var response = await client.GetAsync(
            "/api/inventory/items/1/price-history",
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Price_history_derives_unit_prices_and_hides_inaccessible_records()
    {
        using var server = new CapexTestServer();
        using var admin = await server.CreateAuthenticatedClientAsync();
        var adminId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var itemId = await InventoryTestData.SeedItemAsync(server.Services, adminId, name: "Olive oil");
        await InventoryTestData.SeedOrderAsync(
            server.Services,
            adminId,
            itemId,
            supplierName: "Amazon",
            orderDate: new DateOnly(2026, 6, 1),
            quantity: 2m,
            lineTotal: 9.98m);
        await InventoryTestData.SeedOrderAsync(
            server.Services,
            adminId,
            itemId,
            orderDate: new DateOnly(2026, 6, 2),
            quantity: 3m,
            lineTotal: 12m,
            visibility: RecordVisibility.Private);

        var history = await admin.GetFromJsonAsync<InventoryItemPriceHistoryResponse>(
            $"/api/inventory/items/{itemId}/price-history",
            CancellationToken.None);

        Assert.Equal("Olive oil", history!.ItemName);
        Assert.Equal(24, history.MinimumRecentOrderCount);
        Assert.Equal(2, history.ReturnedOrderCount);
        Assert.Equal([4m, 4.99m], history.Entries.Select(entry => entry.UnitPrice).ToArray());

        var memberId = await server.CreateUserAsync("inventory-price-reader", "PriceReader123!");
        var memberItemId = await InventoryTestData.SeedItemAsync(
            server.Services,
            memberId,
            name: "Private tea",
            visibility: RecordVisibility.Private);
        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, "inventory-price-reader", "PriceReader123!");

        using var hidden = await admin.GetAsync(
            $"/api/inventory/items/{memberItemId}/price-history",
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
    }

    [Fact]
    public async Task Price_history_returns_the_larger_of_twelve_months_or_latest_twenty_four_orders()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var adminId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var itemId = await InventoryTestData.SeedItemAsync(server.Services, adminId, name: "Coffee");

        for (var index = 0; index < 30; index++)
        {
            await InventoryTestData.SeedOrderAsync(
                server.Services,
                adminId,
                itemId,
                orderDate: new DateOnly(2026, 1, 1).AddDays(index),
                quantity: 1m,
                lineTotal: index + 1m);
        }

        var fullRecent = await client.GetFromJsonAsync<InventoryItemPriceHistoryResponse>(
            $"/api/inventory/items/{itemId}/price-history",
            CancellationToken.None);
        Assert.Equal(30, fullRecent!.ReturnedOrderCount);
        Assert.Equal(30, fullRecent.Entries.Count);

        var olderItemId = await InventoryTestData.SeedItemAsync(server.Services, adminId, name: "Rice");
        for (var index = 0; index < 30; index++)
        {
            await InventoryTestData.SeedOrderAsync(
                server.Services,
                adminId,
                olderItemId,
                orderDate: new DateOnly(2024, 1, 1).AddDays(index),
                quantity: 1m,
                lineTotal: index + 1m);
        }

        var latestOnly = await client.GetFromJsonAsync<InventoryItemPriceHistoryResponse>(
            $"/api/inventory/items/{olderItemId}/price-history",
            CancellationToken.None);
        Assert.Equal(24, latestOnly!.ReturnedOrderCount);
        Assert.Equal(24, latestOnly.Entries.Count);
        Assert.Equal(new DateOnly(2024, 1, 30), latestOnly.Entries[0].OrderDate);
        Assert.Equal(new DateOnly(2024, 1, 7), latestOnly.Entries[^1].OrderDate);
    }
}
