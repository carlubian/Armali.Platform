using System.Net;
using System.Net.Http.Json;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Inventory.Contracts;
using Segaris.Api.Modules.Inventory.Domain;
using Segaris.Shared.Authorization;

namespace Segaris.Api.IntegrationTests.Inventory;

public sealed class InventoryOrderReceiveTests
{
    [Fact]
    public async Task Receive_active_order_increases_stock_sets_received_and_updates_metadata()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var itemId = await InventoryTestData.SeedItemAsync(server.Services, founderId, currentStock: 5.50m);
        var orderId = await InventoryTestData.SeedOrderAsync(
            server.Services,
            founderId,
            itemId,
            status: InventoryOrderStatus.Active,
            quantity: 2.25m);
        var beforeItem = await InventoryTestData.ItemModificationAsync(server.Services, itemId);
        var beforeOrder = await InventoryTestData.OrderStateAsync(server.Services, orderId);

        using var response = await CapexApi.PostJsonAsync(client, $"/api/inventory/orders/{orderId}/receive", new { }, csrf);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var received = await response.Content.ReadFromJsonAsync<InventoryOrderResponse>(CancellationToken.None);

        Assert.Equal("Received", received!.Status);
        Assert.Equal(7.75m, await InventoryTestData.CurrentStockAsync(server.Services, itemId));
        var afterItem = await InventoryTestData.ItemModificationAsync(server.Services, itemId);
        var afterOrder = await InventoryTestData.OrderStateAsync(server.Services, orderId);
        Assert.Equal(InventoryOrderStatus.Received, afterOrder.Status);
        Assert.Equal(founderId, afterItem.UpdatedBy);
        Assert.Equal(founderId, afterOrder.UpdatedBy);
        Assert.True(afterItem.UpdatedAt > beforeItem.UpdatedAt);
        Assert.True(afterOrder.UpdatedAt > beforeOrder.UpdatedAt);
    }

    [Theory]
    [InlineData("Planning")]
    [InlineData("Received")]
    [InlineData("Cancelled")]
    public async Task Receive_rejects_non_active_orders_without_touching_stock(string statusName)
    {
        var status = Enum.Parse<InventoryOrderStatus>(statusName);
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var itemId = await InventoryTestData.SeedItemAsync(server.Services, founderId, currentStock: 4m);
        var orderId = await InventoryTestData.SeedOrderAsync(
            server.Services,
            founderId,
            itemId,
            status: status,
            quantity: 3m);

        using var response = await CapexApi.PostJsonAsync(client, $"/api/inventory/orders/{orderId}/receive", new { }, csrf);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("inventory.order.not_active", (await response.Content.ReadFromJsonAsync<ProblemPayload>())!.Code);
        Assert.Equal(4m, await InventoryTestData.CurrentStockAsync(server.Services, itemId));
        Assert.Equal(status, (await InventoryTestData.OrderStateAsync(server.Services, orderId)).Status);
    }

    [Fact]
    public async Task Receive_hides_inaccessible_private_orders()
    {
        using var server = new CapexTestServer();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var itemId = await InventoryTestData.SeedItemAsync(server.Services, founderId, currentStock: 1m);
        var orderId = await InventoryTestData.SeedOrderAsync(
            server.Services,
            founderId,
            itemId,
            status: InventoryOrderStatus.Active,
            visibility: RecordVisibility.Private);

        await server.CreateUserAsync("inventory-receiver", "InventoryReceiver123!");
        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, "inventory-receiver", "InventoryReceiver123!");
        var memberCsrf = await CapexTestServer.GetCsrfTokenAsync(member);

        using var response = await CapexApi.PostJsonAsync(member, $"/api/inventory/orders/{orderId}/receive", new { }, memberCsrf);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(1m, await InventoryTestData.CurrentStockAsync(server.Services, itemId));
    }

    [Fact]
    public async Task Manual_status_update_to_received_does_not_move_stock()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var itemId = await InventoryTestData.SeedItemAsync(server.Services, founderId, currentStock: 2m);
        var orderId = await InventoryTestData.SeedOrderAsync(
            server.Services,
            founderId,
            itemId,
            status: InventoryOrderStatus.Active,
            quantity: 5m);

        using var response = await CapexApi.PutJsonAsync(
            client,
            $"/api/inventory/orders/{orderId}",
            (await InventoryOrderMutationTests.DefaultBuilderAsync(server, itemId)).WithStatus("Received").BuildUpdate(),
            csrf);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2m, await InventoryTestData.CurrentStockAsync(server.Services, itemId));
        Assert.Equal(InventoryOrderStatus.Received, (await InventoryTestData.OrderStateAsync(server.Services, orderId)).Status);
    }

    private sealed record ProblemPayload(string? Code);
}
