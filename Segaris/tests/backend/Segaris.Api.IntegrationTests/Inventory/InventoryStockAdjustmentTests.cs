using System.Net;
using System.Net.Http.Json;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Inventory.Contracts;
using Segaris.Shared.Authorization;

namespace Segaris.Api.IntegrationTests.Inventory;

public sealed class InventoryStockAdjustmentTests
{
    [Fact]
    public async Task Increase_adds_to_current_stock_and_returns_the_updated_item()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var itemId = await InventoryTestData.SeedItemAsync(server.Services, founderId, currentStock: 3m);

        using var response = await CapexApi.PostJsonAsync(
            client,
            $"/api/inventory/items/{itemId}/stock-adjustments",
            new InventoryStockAdjustmentRequest("Increase", 2.5m),
            csrf);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var item = await response.Content.ReadFromJsonAsync<InventoryItemResponse>(CancellationToken.None);
        Assert.Equal(5.5m, item!.CurrentStock);
        Assert.Equal(5.5m, await InventoryTestData.CurrentStockAsync(server.Services, itemId));
    }

    [Fact]
    public async Task Decrease_subtracts_from_current_stock()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var itemId = await InventoryTestData.SeedItemAsync(server.Services, founderId, currentStock: 10m);

        using var response = await CapexApi.PostJsonAsync(
            client,
            $"/api/inventory/items/{itemId}/stock-adjustments",
            new InventoryStockAdjustmentRequest("Decrease", 4m),
            csrf);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(6m, await InventoryTestData.CurrentStockAsync(server.Services, itemId));
    }

    [Fact]
    public async Task Decrease_below_zero_is_rejected_and_leaves_stock_unchanged()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var itemId = await InventoryTestData.SeedItemAsync(server.Services, founderId, currentStock: 1m);

        using var response = await CapexApi.PostJsonAsync(
            client,
            $"/api/inventory/items/{itemId}/stock-adjustments",
            new InventoryStockAdjustmentRequest("Decrease", 5m),
            csrf);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        Assert.Equal("inventory.stock.negative_result", problem!.Code);
        Assert.Equal(1m, await InventoryTestData.CurrentStockAsync(server.Services, itemId));
    }

    [Theory]
    [InlineData("Sideways", 1)]
    [InlineData("Increase", 0)]
    [InlineData("Increase", -2)]
    public async Task Invalid_direction_or_quantity_is_rejected(string direction, decimal quantity)
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var itemId = await InventoryTestData.SeedItemAsync(server.Services, founderId, currentStock: 5m);

        using var response = await CapexApi.PostJsonAsync(
            client,
            $"/api/inventory/items/{itemId}/stock-adjustments",
            new InventoryStockAdjustmentRequest(direction, quantity),
            csrf);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        Assert.Equal("inventory.item.validation", problem!.Code);
        Assert.Equal(5m, await InventoryTestData.CurrentStockAsync(server.Services, itemId));
    }

    [Fact]
    public async Task Adjusting_another_users_private_item_is_reported_as_not_found()
    {
        using var server = new CapexTestServer();
        using var admin = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(admin);
        var memberId = await server.CreateUserAsync("member", "MemberPass123!");
        var privateItemId = await InventoryTestData.SeedItemAsync(
            server.Services,
            memberId,
            currentStock: 5m,
            visibility: RecordVisibility.Private);

        using var response = await CapexApi.PostJsonAsync(
            admin,
            $"/api/inventory/items/{privateItemId}/stock-adjustments",
            new InventoryStockAdjustmentRequest("Increase", 1m),
            csrf);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        Assert.Equal("inventory.item.not_found", problem!.Code);
        Assert.Equal(5m, await InventoryTestData.CurrentStockAsync(server.Services, privateItemId));
    }

    [Fact]
    public async Task Adjustment_requires_an_antiforgery_token()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var itemId = await InventoryTestData.SeedItemAsync(server.Services, founderId, currentStock: 5m);

        using var response = await CapexApi.PostJsonAsync(
            client,
            $"/api/inventory/items/{itemId}/stock-adjustments",
            new InventoryStockAdjustmentRequest("Increase", 1m),
            csrf: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(5m, await InventoryTestData.CurrentStockAsync(server.Services, itemId));
    }

    private sealed record ProblemPayload(string? Code);
}
