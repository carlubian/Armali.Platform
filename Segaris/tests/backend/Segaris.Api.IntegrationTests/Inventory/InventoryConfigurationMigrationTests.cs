using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Modules.Configuration.Persistence;
using Segaris.Persistence;
using Segaris.Shared.Authorization;

namespace Segaris.Api.IntegrationTests.Inventory;

public sealed class InventoryConfigurationMigrationTests
{
    [Fact]
    public async Task Supplier_replacement_migrates_orders_and_item_eligibility_for_public_and_private_records()
    {
        using var server = new CapexTestServer();
        var adminId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var memberId = await server.CreateUserAsync("inventory-private-supplier", "MemberPass123!");

        var publicItemId = await InventoryTestData.SeedItemAsync(
            server.Services, adminId, name: "Public olive oil", supplierNames: ["Amazon"]);
        var privateItemId = await InventoryTestData.SeedItemAsync(
            server.Services, memberId, name: "Private olive oil", supplierNames: ["Amazon"], visibility: RecordVisibility.Private);
        var publicOrderId = await InventoryTestData.SeedOrderAsync(
            server.Services, adminId, publicItemId, supplierName: "Amazon");
        var privateOrderId = await InventoryTestData.SeedOrderAsync(
            server.Services, memberId, privateItemId, supplierName: "Amazon", visibility: RecordVisibility.Private);

        var sourceId = await InventoryTestData.SupplierIdAsync(server.Services, "Amazon");
        var replacementId = await InventoryTestData.SupplierIdAsync(server.Services, "IKEA");
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var response = await CapexApi.PostJsonAsync(
            client,
            $"/api/configuration/suppliers/{sourceId}/replace-and-delete",
            new CatalogReplacementRequest(replacementId, ClearReferences: false, ExchangeRate: null),
            csrf);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var publicOrder = await InventoryTestData.OrderReferencesAsync(server.Services, publicOrderId);
        var privateOrder = await InventoryTestData.OrderReferencesAsync(server.Services, privateOrderId);
        Assert.Equal(replacementId, publicOrder.SupplierId);
        Assert.Equal(replacementId, privateOrder.SupplierId);
        Assert.Equal(adminId, publicOrder.UpdatedBy);
        Assert.Equal(adminId, privateOrder.UpdatedBy);
        Assert.Equal([replacementId], await InventoryTestData.ItemSupplierIdsAsync(server.Services, publicItemId));
        Assert.Equal([replacementId], await InventoryTestData.ItemSupplierIdsAsync(server.Services, privateItemId));

        await using var scope = server.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        Assert.False(await database.Set<SegarisSupplier>().AnyAsync(supplier => supplier.Id == sourceId));
    }

    [Fact]
    public async Task Supplier_replacement_deduplicates_eligibility_when_the_target_is_already_allowed()
    {
        using var server = new CapexTestServer();
        var adminId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var itemId = await InventoryTestData.SeedItemAsync(
            server.Services, adminId, name: "Dual supplier item", supplierNames: ["Amazon", "IKEA"]);

        var sourceId = await InventoryTestData.SupplierIdAsync(server.Services, "Amazon");
        var replacementId = await InventoryTestData.SupplierIdAsync(server.Services, "IKEA");
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var response = await CapexApi.PostJsonAsync(
            client,
            $"/api/configuration/suppliers/{sourceId}/replace-and-delete",
            new CatalogReplacementRequest(replacementId, ClearReferences: false, ExchangeRate: null),
            csrf);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        // The item collapses to the single target row rather than gaining a duplicate.
        Assert.Equal([replacementId], await InventoryTestData.ItemSupplierIdsAsync(server.Services, itemId));
    }

    [Fact]
    public async Task Clearing_a_supplier_referenced_by_inventory_is_rejected_and_rolls_everything_back()
    {
        using var server = new CapexTestServer();
        var adminId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var itemId = await InventoryTestData.SeedItemAsync(
            server.Services, adminId, name: "Clear-guard item", supplierNames: ["Amazon"]);
        var orderId = await InventoryTestData.SeedOrderAsync(
            server.Services, adminId, itemId, supplierName: "Amazon");

        var sourceId = await InventoryTestData.SupplierIdAsync(server.Services, "Amazon");
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var response = await CapexApi.PostJsonAsync(
            client,
            $"/api/configuration/suppliers/{sourceId}/replace-and-delete",
            new CatalogReplacementRequest(ReplacementId: null, ClearReferences: true, ExchangeRate: null),
            csrf);

        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("configuration.catalog.replacement_required", problem!.Code);

        // The rejected clearing leaves the supplier, its order, and its eligibility intact.
        var order = await InventoryTestData.OrderReferencesAsync(server.Services, orderId);
        Assert.Equal(sourceId, order.SupplierId);
        Assert.Equal([sourceId], await InventoryTestData.ItemSupplierIdsAsync(server.Services, itemId));
        await using var scope = server.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        Assert.True(await database.Set<SegarisSupplier>().AnyAsync(supplier => supplier.Id == sourceId));
    }

    [Fact]
    public async Task Clearing_an_unreferenced_supplier_still_succeeds()
    {
        using var server = new CapexTestServer();
        // No Inventory items or orders reference the supplier, so clearing is allowed.
        var sourceId = await InventoryTestData.SupplierIdAsync(server.Services, "Leroy Merlin");
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var response = await CapexApi.PostJsonAsync(
            client,
            $"/api/configuration/suppliers/{sourceId}/replace-and-delete",
            new CatalogReplacementRequest(ReplacementId: null, ClearReferences: true, ExchangeRate: null),
            csrf);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await using var scope = server.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        Assert.False(await database.Set<SegarisSupplier>().AnyAsync(supplier => supplier.Id == sourceId));
    }

    [Fact]
    public async Task Currency_conversion_recalculates_order_line_totals_for_public_and_private_orders()
    {
        using var server = new CapexTestServer();
        var adminId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var memberId = await server.CreateUserAsync("inventory-private-currency", "MemberPass123!");

        var publicItemId = await InventoryTestData.SeedItemAsync(server.Services, adminId, name: "Public flour");
        var privateItemId = await InventoryTestData.SeedItemAsync(
            server.Services, memberId, name: "Private flour", visibility: RecordVisibility.Private);
        var publicOrderId = await InventoryTestData.SeedOrderAsync(
            server.Services, adminId, publicItemId, quantity: 1m, lineTotal: 10.00m);
        var privateOrderId = await InventoryTestData.SeedOrderAsync(
            server.Services, memberId, privateItemId, visibility: RecordVisibility.Private, quantity: 1m, lineTotal: 5.55m);

        var sourceId = await InventoryTestData.CurrencyIdAsync(server.Services, "EUR");
        var targetId = await InventoryTestData.CurrencyIdAsync(server.Services, "USD");
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var response = await CapexApi.PostJsonAsync(
            client,
            $"/api/configuration/currencies/{sourceId}/replace-and-delete",
            new CatalogReplacementRequest(targetId, ClearReferences: false, ExchangeRate: 1.20m),
            csrf);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var publicOrder = await InventoryTestData.OrderReferencesAsync(server.Services, publicOrderId);
        var privateOrder = await InventoryTestData.OrderReferencesAsync(server.Services, privateOrderId);
        Assert.Equal(targetId, publicOrder.CurrencyId);
        Assert.Equal(targetId, privateOrder.CurrencyId);
        Assert.Equal(adminId, publicOrder.UpdatedBy);
        Assert.Equal(adminId, privateOrder.UpdatedBy);
        // 10.00 * 1.20 = 12.00; 5.55 * 1.20 = 6.66 (rounded to two places).
        Assert.Equal([12.00m], await InventoryTestData.OrderLineTotalsAsync(server.Services, publicOrderId));
        Assert.Equal([6.66m], await InventoryTestData.OrderLineTotalsAsync(server.Services, privateOrderId));

        await using var scope = server.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        Assert.False(await database.Set<SegarisCurrency>().AnyAsync(currency => currency.Id == sourceId));
    }

    [Fact]
    public async Task Inventory_supplier_impact_reports_referenced_state_without_disclosing_private_records()
    {
        using var server = new CapexTestServer();
        var memberId = await server.CreateUserAsync("inventory-supplier-impact", "MemberPass123!");
        await InventoryTestData.SeedItemAsync(
            server.Services, memberId, name: "Private impact item", supplierNames: ["Amazon"], visibility: RecordVisibility.Private);
        var sourceId = await InventoryTestData.SupplierIdAsync(server.Services, "Amazon");
        using var client = await server.CreateAuthenticatedClientAsync();

        var impact = await client.GetFromJsonAsync<CatalogDeletionImpactResponse>(
            $"/api/configuration/suppliers/{sourceId}/deletion-impact",
            CancellationToken.None);

        Assert.True(impact!.IsReferenced);
        Assert.False(impact.CanDeleteDirectly);
    }

    private sealed record ProblemPayload(string? Code);
}
