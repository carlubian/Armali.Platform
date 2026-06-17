using System.Net;
using System.Net.Http.Json;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Inventory.Contracts;
using Segaris.Shared.Authorization;

namespace Segaris.Api.IntegrationTests.Inventory;

public sealed class InventoryItemMutationTests
{
    [Fact]
    public async Task Create_persists_the_item_with_suppliers_and_defaults()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var supplierId = await InventoryTestData.SupplierIdAsync(server.Services, "Amazon");
        var request = (await DefaultBuilderAsync(server))
            .WithName("Olive oil")
            .WithStatus(null)
            .WithVisibility(null)
            .WithStock(3m, 1m)
            .WithSuppliers(supplierId)
            .BuildCreate();

        using var response = await CapexApi.PostJsonAsync(client, "/api/inventory/items", request, csrf);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<InventoryItemResponse>(CancellationToken.None);

        Assert.NotNull(created);
        Assert.Equal("Olive oil", created.Name);
        Assert.Equal("Candidate", created.Status);
        Assert.Equal("Public", created.Visibility);
        Assert.Equal(3m, created.CurrentStock);
        Assert.Equal(supplierId, Assert.Single(created.Suppliers).SupplierId);
        Assert.Empty(created.Attachments);
    }

    [Fact]
    public async Task Create_rejects_missing_suppliers_and_unknown_catalog_references()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var missingSupplier = await CapexApi.PostJsonAsync(
            client,
            "/api/inventory/items",
            (await DefaultBuilderAsync(server)).WithSuppliers().BuildCreate(),
            csrf);
        var supplierProblem = await missingSupplier.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        using var unknownCatalog = await CapexApi.PostJsonAsync(
            client,
            "/api/inventory/items",
            (await DefaultBuilderAsync(server)).WithCategory(999_999).BuildCreate(),
            csrf);
        var catalogProblem = await unknownCatalog.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, missingSupplier.StatusCode);
        Assert.Equal("inventory.item.supplier_required", supplierProblem!.Code);
        Assert.Equal(HttpStatusCode.BadRequest, unknownCatalog.StatusCode);
        Assert.Equal("inventory.catalog.unknown_reference", catalogProblem!.Code);
    }

    [Fact]
    public async Task Update_replaces_editable_fields_and_supplier_set()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var itemId = await InventoryTestData.SeedItemAsync(server.Services, founderId, name: "Original", currentStock: 1m);
        var supplierId = await InventoryTestData.SupplierIdAsync(server.Services, "Amazon");

        var update = (await DefaultBuilderAsync(server))
            .WithName("Updated")
            .WithStatus("Active")
            .WithNotes("Shelf A")
            .WithStock(8m, 2m)
            .WithSuppliers(supplierId)
            .BuildUpdate();

        using var response = await CapexApi.PutJsonAsync(client, $"/api/inventory/items/{itemId}", update, csrf);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<InventoryItemResponse>(CancellationToken.None);

        Assert.Equal("Updated", updated!.Name);
        Assert.Equal("Active", updated.Status);
        Assert.Equal("Shelf A", updated.Notes);
        Assert.Equal(8m, updated.CurrentStock);
        Assert.Equal(2m, updated.MinimumStock);
    }

    [Fact]
    public async Task Delete_removes_unreferenced_item_and_blocks_order_referenced_item()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var disposableId = await InventoryTestData.SeedItemAsync(server.Services, founderId, name: "Disposable");
        var referencedId = await InventoryTestData.SeedItemAsync(server.Services, founderId, name: "Referenced");
        await InventoryTestData.SeedOrderAsync(server.Services, founderId, referencedId);

        using var deleted = await CapexApi.DeleteAsync(client, $"/api/inventory/items/{disposableId}", csrf);
        using var blocked = await CapexApi.DeleteAsync(client, $"/api/inventory/items/{referencedId}", csrf);
        var problem = await blocked.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.False(await InventoryTestData.ItemExistsAsync(server.Services, disposableId));
        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
        Assert.Equal("inventory.item.referenced", problem!.Code);
    }

    [Fact]
    public async Task Create_requires_an_antiforgery_token()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();

        using var response = await CapexApi.PostJsonAsync(
            client,
            "/api/inventory/items",
            (await DefaultBuilderAsync(server)).BuildCreate(),
            csrf: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    internal static async Task<InventoryItemRequestBuilder> DefaultBuilderAsync(CapexTestServer server)
    {
        var categoryId = await InventoryTestData.CategoryIdAsync(server.Services, "Other");
        var locationId = await InventoryTestData.LocationIdAsync(server.Services, "Other");
        var supplierId = await InventoryTestData.SupplierIdAsync(server.Services, "Amazon");
        return InventoryItemRequestBuilder.Default()
            .WithCategory(categoryId)
            .WithLocation(locationId)
            .WithSuppliers(supplierId);
    }

    internal static async Task<int> CreateItemAsync(
        CapexTestServer server,
        HttpClient client,
        string csrf,
        Func<InventoryItemRequestBuilder, InventoryItemRequestBuilder> configure)
    {
        var builder = configure(await DefaultBuilderAsync(server));
        using var response = await CapexApi.PostJsonAsync(client, "/api/inventory/items", builder.BuildCreate(), csrf);
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<InventoryItemResponse>(CancellationToken.None);
        return created!.Id;
    }

    private sealed record ProblemPayload(string? Code);
}
