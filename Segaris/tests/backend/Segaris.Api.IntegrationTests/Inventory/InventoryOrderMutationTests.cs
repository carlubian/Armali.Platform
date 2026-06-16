using System.Net;
using System.Net.Http.Json;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Inventory.Contracts;
using Segaris.Api.Modules.Inventory.Domain;
using Segaris.Shared.Authorization;

namespace Segaris.Api.IntegrationTests.Inventory;

public sealed class InventoryOrderMutationTests
{
    [Fact]
    public async Task Create_persists_the_order_with_lines_and_defaults()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var itemId = await InventoryTestData.SeedItemAsync(server.Services, founderId);
        var request = (await DefaultBuilderAsync(server, itemId))
            .WithStatus(null)
            .WithVisibility(null)
            .WithDates(null, null)
            .BuildCreate();

        using var response = await CapexApi.PostJsonAsync(client, "/api/inventory/orders", request, csrf);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<InventoryOrderResponse>(CancellationToken.None);

        Assert.NotNull(created);
        Assert.Equal("Planning", created.Status);
        Assert.Equal("Public", created.Visibility);
        Assert.Equal(created.OrderDate!.Value.AddDays(7), created.ExpectedReceiptDate);
        Assert.Equal(itemId, Assert.Single(created.Lines).ItemId);
    }

    [Fact]
    public async Task Create_rejects_unknown_references_inaccessible_items_supplier_mismatch_and_public_private_mix()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var itemId = await InventoryTestData.SeedItemAsync(server.Services, founderId, supplierNames: ["Amazon"]);
        var privateItemId = await InventoryTestData.SeedItemAsync(server.Services, founderId, visibility: RecordVisibility.Private);
        var ikeaId = await InventoryTestData.SupplierIdAsync(server.Services, "IKEA");

        using var unknownCurrency = await CapexApi.PostJsonAsync(
            client,
            "/api/inventory/orders",
            (await DefaultBuilderAsync(server, itemId)).WithCurrency(999_999).BuildCreate(),
            csrf);
        using var unknownItem = await CapexApi.PostJsonAsync(
            client,
            "/api/inventory/orders",
            (await DefaultBuilderAsync(server, itemId)).WithLines(new InventoryOrderLineRequest(999_999, 1m, 1m)).BuildCreate(),
            csrf);
        using var wrongSupplier = await CapexApi.PostJsonAsync(
            client,
            "/api/inventory/orders",
            (await DefaultBuilderAsync(server, itemId)).WithSupplier(ikeaId).BuildCreate(),
            csrf);
        using var publicWithPrivateItem = await CapexApi.PostJsonAsync(
            client,
            "/api/inventory/orders",
            (await DefaultBuilderAsync(server, privateItemId)).BuildCreate(),
            csrf);

        Assert.Equal(HttpStatusCode.BadRequest, unknownCurrency.StatusCode);
        Assert.Equal("inventory.catalog.unknown_reference", (await unknownCurrency.Content.ReadFromJsonAsync<ProblemPayload>())!.Code);
        Assert.Equal(HttpStatusCode.NotFound, unknownItem.StatusCode);
        Assert.Equal("inventory.order.line.item_not_accessible", (await unknownItem.Content.ReadFromJsonAsync<ProblemPayload>())!.Code);
        Assert.Equal(HttpStatusCode.BadRequest, wrongSupplier.StatusCode);
        Assert.Equal("inventory.order.line.supplier_not_allowed", (await wrongSupplier.Content.ReadFromJsonAsync<ProblemPayload>())!.Code);
        Assert.Equal(HttpStatusCode.Forbidden, publicWithPrivateItem.StatusCode);
        Assert.Equal("inventory.order.visibility_forbidden", (await publicWithPrivateItem.Content.ReadFromJsonAsync<ProblemPayload>())!.Code);
    }

    [Fact]
    public async Task Update_replaces_fields_and_full_line_set()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var firstItem = await InventoryTestData.SeedItemAsync(server.Services, founderId, name: "First");
        var secondItem = await InventoryTestData.SeedItemAsync(server.Services, founderId, name: "Second");
        var orderId = await InventoryTestData.SeedOrderAsync(server.Services, founderId, firstItem);

        var update = (await DefaultBuilderAsync(server, secondItem))
            .WithStatus("Cancelled")
            .WithNotes("Updated")
            .WithLines(new InventoryOrderLineRequest(secondItem, 3m, 33m))
            .BuildUpdate();

        using var response = await CapexApi.PutJsonAsync(client, $"/api/inventory/orders/{orderId}", update, csrf);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<InventoryOrderResponse>(CancellationToken.None);

        Assert.Equal("Cancelled", updated!.Status);
        Assert.Equal("Updated", updated.Notes);
        var line = Assert.Single(updated.Lines);
        Assert.Equal(secondItem, line.ItemId);
        Assert.Equal(3m, line.Quantity);
        Assert.Equal(33m, line.LineTotal);
    }

    [Fact]
    public async Task Received_orders_allow_only_status_unlock_before_full_editing()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var itemId = await InventoryTestData.SeedItemAsync(server.Services, founderId);
        var orderId = await InventoryTestData.SeedOrderAsync(server.Services, founderId, itemId, status: InventoryOrderStatus.Received);

        using var blocked = await CapexApi.PutJsonAsync(
            client,
            $"/api/inventory/orders/{orderId}",
            (await DefaultBuilderAsync(server, itemId)).WithStatus("Received").WithNotes("Edit").BuildUpdate(),
            csrf);
        using var unlocked = await CapexApi.PutJsonAsync(
            client,
            $"/api/inventory/orders/{orderId}",
            (await DefaultBuilderAsync(server, itemId))
                .WithStatus("Active")
                .WithDates(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 8))
                .BuildUpdate(),
            csrf);
        using var edited = await CapexApi.PutJsonAsync(
            client,
            $"/api/inventory/orders/{orderId}",
            (await DefaultBuilderAsync(server, itemId)).WithStatus("Active").WithNotes("Now editable").BuildUpdate(),
            csrf);

        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
        Assert.Equal("inventory.order.received_locked", (await blocked.Content.ReadFromJsonAsync<ProblemPayload>())!.Code);
        Assert.Equal(HttpStatusCode.OK, unlocked.StatusCode);
        Assert.Equal(HttpStatusCode.OK, edited.StatusCode);
    }

    [Fact]
    public async Task Private_order_visibility_changes_are_creator_only_and_require_public_items()
    {
        using var server = new CapexTestServer();
        using var founder = await server.CreateAuthenticatedClientAsync();
        var founderCsrf = await CapexTestServer.GetCsrfTokenAsync(founder);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var privateItem = await InventoryTestData.SeedItemAsync(server.Services, founderId, visibility: RecordVisibility.Private);
        var publicItem = await InventoryTestData.SeedItemAsync(server.Services, founderId);
        var privateOrder = await InventoryTestData.SeedOrderAsync(server.Services, founderId, privateItem, visibility: RecordVisibility.Private);

        await server.CreateUserAsync("inventory-order-editor", "OrderEditor123!");
        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, "inventory-order-editor", "OrderEditor123!");
        var memberCsrf = await CapexTestServer.GetCsrfTokenAsync(member);

        using var hiddenFromMember = await CapexApi.PutJsonAsync(
            member,
            $"/api/inventory/orders/{privateOrder}",
            (await DefaultBuilderAsync(server, publicItem)).BuildUpdate(),
            memberCsrf);
        using var creatorCannotPromotePrivateItem = await CapexApi.PutJsonAsync(
            founder,
            $"/api/inventory/orders/{privateOrder}",
            (await DefaultBuilderAsync(server, privateItem)).WithVisibility("Public").BuildUpdate(),
            founderCsrf);

        Assert.Equal(HttpStatusCode.NotFound, hiddenFromMember.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, creatorCannotPromotePrivateItem.StatusCode);
    }

    [Fact]
    public async Task Delete_removes_an_order_without_touching_stock()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var itemId = await InventoryTestData.SeedItemAsync(server.Services, founderId, currentStock: 5m);
        var orderId = await InventoryTestData.SeedOrderAsync(server.Services, founderId, itemId, status: InventoryOrderStatus.Received, quantity: 2m);

        using var response = await CapexApi.DeleteAsync(client, $"/api/inventory/orders/{orderId}", csrf);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.False(await InventoryTestData.OrderExistsAsync(server.Services, orderId));
        Assert.Equal(5m, await InventoryTestData.CurrentStockAsync(server.Services, itemId));
    }

    internal static async Task<InventoryOrderRequestBuilder> DefaultBuilderAsync(CapexTestServer server, int itemId)
    {
        var supplierId = await InventoryTestData.SupplierIdAsync(server.Services, "Amazon");
        var currencyId = await InventoryTestData.CurrencyIdAsync(server.Services);
        return InventoryOrderRequestBuilder.Default()
            .WithSupplier(supplierId)
            .WithCurrency(currencyId)
            .WithLines(new InventoryOrderLineRequest(itemId, 1m, 10m));
    }

    internal static async Task<int> CreateOrderAsync(
        CapexTestServer server,
        HttpClient client,
        string csrf,
        int itemId,
        Func<InventoryOrderRequestBuilder, InventoryOrderRequestBuilder> configure)
    {
        var builder = configure(await DefaultBuilderAsync(server, itemId));
        using var response = await CapexApi.PostJsonAsync(client, "/api/inventory/orders", builder.BuildCreate(), csrf);
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<InventoryOrderResponse>(CancellationToken.None);
        return created!.Id;
    }

    private sealed record ProblemPayload(string? Code);
}
