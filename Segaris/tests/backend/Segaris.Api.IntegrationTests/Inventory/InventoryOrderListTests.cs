using System.Net;
using System.Net.Http.Json;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Inventory.Contracts;
using Segaris.Api.Modules.Inventory.Domain;
using Segaris.Shared.Api;
using Segaris.Shared.Authorization;

namespace Segaris.Api.IntegrationTests.Inventory;

public sealed class InventoryOrderListTests
{
    [Fact]
    public async Task Orders_require_authentication()
    {
        using var server = new CapexTestServer();
        using var client = server.CreateClient();

        using var response = await client.GetAsync("/api/inventory/orders", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Orders_paginate_and_default_to_order_date_then_id_descending()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var itemId = await InventoryTestData.SeedItemAsync(server.Services, founderId);
        var oldest = await InventoryTestData.SeedOrderAsync(server.Services, founderId, itemId, orderDate: new DateOnly(2026, 1, 1));
        var newest = await InventoryTestData.SeedOrderAsync(server.Services, founderId, itemId, orderDate: new DateOnly(2026, 2, 1));
        var sameNewestLaterId = await InventoryTestData.SeedOrderAsync(server.Services, founderId, itemId, orderDate: new DateOnly(2026, 2, 1));

        var firstPage = await GetPageAsync(client, "/api/inventory/orders?page=1&pageSize=2");
        var secondPage = await GetPageAsync(client, "/api/inventory/orders?page=2&pageSize=2");

        Assert.Equal(3, firstPage.TotalCount);
        Assert.Equal(new[] { sameNewestLaterId, newest }, firstPage.Items.Select(order => order.Id).ToArray());
        Assert.Equal(oldest, Assert.Single(secondPage.Items).Id);
    }

    [Theory]
    [InlineData("/api/inventory/orders?page=0", "page")]
    [InlineData("/api/inventory/orders?pageSize=0", "pageSize")]
    [InlineData("/api/inventory/orders?status=Nope", "status")]
    [InlineData("/api/inventory/orders?visibility=Nope", "visibility")]
    [InlineData("/api/inventory/orders?sort=unknown", "sort")]
    [InlineData("/api/inventory/orders?sortDirection=sideways", "sortDirection")]
    public async Task Orders_reject_invalid_query_values(string route, string field)
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();

        using var response = await client.GetAsync(route, CancellationToken.None);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(problem!.Errors!.ContainsKey(field));
    }

    [Fact]
    public async Task Search_matches_notes_and_line_item_names_without_duplicating_orders()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var widget = await InventoryTestData.SeedItemAsync(server.Services, founderId, name: "Widget pack");
        var plain = await InventoryTestData.SeedItemAsync(server.Services, founderId, name: "Plain pack");
        await InventoryTestData.SeedOrderAsync(server.Services, founderId, widget);
        await InventoryTestData.SeedOrderAsync(server.Services, founderId, plain, notes: "Contains a widget note");

        var page = await GetPageAsync(client, "/api/inventory/orders?search=WIDGET");

        Assert.Equal(2, page.TotalCount);
        Assert.Equal(2, page.Items.Count);
    }

    [Fact]
    public async Task Supplier_status_currency_visibility_and_creator_filters_are_exact()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var memberId = await server.CreateUserAsync("inventory-order-author", "OrderAuthor123!");
        var amazonItem = await InventoryTestData.SeedItemAsync(server.Services, founderId, name: "Amazon item", supplierNames: ["Amazon"]);
        var ikeaItem = await InventoryTestData.SeedItemAsync(server.Services, memberId, name: "IKEA item", supplierNames: ["IKEA"]);
        await InventoryTestData.SeedOrderAsync(server.Services, founderId, amazonItem, supplierName: "Amazon", status: InventoryOrderStatus.Active);
        await InventoryTestData.SeedOrderAsync(server.Services, memberId, ikeaItem, supplierName: "IKEA", status: InventoryOrderStatus.Cancelled, visibility: RecordVisibility.Private);

        var amazonId = await InventoryTestData.SupplierIdAsync(server.Services, "Amazon");
        var currencyId = await InventoryTestData.CurrencyIdAsync(server.Services);

        Assert.Equal("Active", Assert.Single((await GetPageAsync(client, $"/api/inventory/orders?supplier={amazonId}")).Items).Status);
        Assert.Equal("Active", Assert.Single((await GetPageAsync(client, "/api/inventory/orders?status=Active")).Items).Status);
        Assert.Single((await GetPageAsync(client, $"/api/inventory/orders?currency={currencyId}")).Items);
        Assert.Single((await GetPageAsync(client, "/api/inventory/orders?visibility=Public")).Items);
        Assert.Empty((await GetPageAsync(client, $"/api/inventory/orders?creator={memberId}")).Items);
    }

    [Fact]
    public async Task Detail_projects_lines_and_hides_inaccessible_private_orders()
    {
        using var server = new CapexTestServer();
        using var founder = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var itemId = await InventoryTestData.SeedItemAsync(server.Services, founderId, name: "Detail item");
        var orderId = await InventoryTestData.SeedOrderAsync(server.Services, founderId, itemId, notes: "Details");

        var detail = await founder.GetFromJsonAsync<InventoryOrderResponse>(
            $"/api/inventory/orders/{orderId}",
            CancellationToken.None);
        Assert.Equal("Details", detail!.Notes);
        Assert.Equal("Detail item", Assert.Single(detail.Lines).ItemName);

        var privateOrderId = await InventoryTestData.SeedOrderAsync(
            server.Services,
            founderId,
            itemId,
            visibility: RecordVisibility.Private);
        await server.CreateUserAsync("inventory-order-reader", "OrderReader123!");
        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, "inventory-order-reader", "OrderReader123!");

        using var hidden = await member.GetAsync($"/api/inventory/orders/{privateOrderId}", CancellationToken.None);
        var problem = await hidden.Content.ReadFromJsonAsync<ProblemCodePayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
        Assert.Equal("inventory.order.not_found", problem!.Code);
    }

    private static async Task<PaginatedResponse<InventoryOrderSummaryResponse>> GetPageAsync(
        HttpClient client,
        string route)
    {
        var page = await client.GetFromJsonAsync<PaginatedResponse<InventoryOrderSummaryResponse>>(
            route,
            CancellationToken.None);
        Assert.NotNull(page);
        return page;
    }

    private sealed record ProblemPayload(IReadOnlyDictionary<string, string[]>? Errors);

    private sealed record ProblemCodePayload(string? Code);
}
