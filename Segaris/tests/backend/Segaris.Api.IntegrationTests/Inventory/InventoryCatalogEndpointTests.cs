using System.Net;
using System.Net.Http.Json;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Modules.Inventory;
using Segaris.Api.Modules.Inventory.Contracts;

namespace Segaris.Api.IntegrationTests.Inventory;

public sealed class InventoryCatalogEndpointTests
{
    [Theory]
    [InlineData("/api/inventory/categories")]
    [InlineData("/api/inventory/locations")]
    public async Task Catalogs_require_authentication(string route)
    {
        using var server = new CapexTestServer();
        using var client = server.CreateClient();

        using var response = await client.GetAsync(route, CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Categories_return_the_seeded_catalog_in_sort_order()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();

        var response = await client.GetFromJsonAsync<InventoryCategoryResponse[]>(
            "/api/inventory/categories",
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal(InventoryCatalog.Categories.Count, response.Length);
        Assert.Equal(
            InventoryCatalog.Categories.Select(seed => seed.Name),
            response.Select(item => item.Name));
        Assert.Equal(
            Enumerable.Range(0, response.Length),
            response.Select(item => item.SortOrder));
        Assert.All(response, item => Assert.True(item.Id > 0));
    }

    [Fact]
    public async Task Locations_return_the_seeded_catalog_in_sort_order()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();

        var response = await client.GetFromJsonAsync<InventoryLocationResponse[]>(
            "/api/inventory/locations",
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal(InventoryCatalog.Locations.Count, response.Length);
        Assert.Equal(
            InventoryCatalog.Locations.Select(seed => seed.Name),
            response.Select(item => item.Name));
    }

    [Theory]
    [InlineData("/api/inventory/categories")]
    [InlineData("/api/inventory/locations")]
    public async Task Management_routes_reject_normal_users(string route)
    {
        using var server = new CapexTestServer();
        await server.CreateUserAsync("member", "MemberPass123!");
        using var client = await server.CreateAuthenticatedClientAsync("member", "MemberPass123!");
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var response = await CapexApi.PostJsonAsync(
            client, route, new CatalogItemRequest("Anything"), csrf);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_can_create_move_and_delete_a_category()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var createdResponse = await CapexApi.PostJsonAsync(
            client, "/api/inventory/categories", new CatalogItemRequest("  Snacks  "), csrf);
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var created = await createdResponse.Content.ReadFromJsonAsync<InventoryCategoryResponse>(CancellationToken.None);
        Assert.Equal("Snacks", created!.Name);

        var beforeMove = await client.GetFromJsonAsync<InventoryCategoryResponse[]>("/api/inventory/categories", CancellationToken.None);
        Assert.Equal(created.Id, beforeMove![^1].Id);

        using var moved = await CapexApi.PostJsonAsync(
            client, $"/api/inventory/categories/{created.Id}/move", new CatalogMoveRequest("up"), csrf);
        Assert.Equal(HttpStatusCode.NoContent, moved.StatusCode);
        var afterMove = await client.GetFromJsonAsync<InventoryCategoryResponse[]>("/api/inventory/categories", CancellationToken.None);
        Assert.Equal(created.Id, afterMove![^2].Id);

        var impact = await client.GetFromJsonAsync<CatalogDeletionImpactResponse>(
            $"/api/inventory/categories/{created.Id}/deletion-impact", CancellationToken.None);
        Assert.False(impact!.IsReferenced);
        Assert.True(impact.CanDeleteDirectly);

        using var deleted = await CapexApi.DeleteAsync(client, $"/api/inventory/categories/{created.Id}", csrf);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        var afterDelete = await client.GetFromJsonAsync<InventoryCategoryResponse[]>("/api/inventory/categories", CancellationToken.None);
        Assert.Equal(InventoryCatalog.Categories.Count, afterDelete!.Length);
    }

    [Fact]
    public async Task Admin_can_create_and_delete_a_location()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var createdResponse = await CapexApi.PostJsonAsync(
            client, "/api/inventory/locations", new CatalogItemRequest("  Garage  "), csrf);
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var created = await createdResponse.Content.ReadFromJsonAsync<InventoryLocationResponse>(CancellationToken.None);
        Assert.Equal("Garage", created!.Name);

        using var deleted = await CapexApi.DeleteAsync(client, $"/api/inventory/locations/{created.Id}", csrf);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        var afterDelete = await client.GetFromJsonAsync<InventoryLocationResponse[]>("/api/inventory/locations", CancellationToken.None);
        Assert.Equal(InventoryCatalog.Locations.Count, afterDelete!.Length);
    }

    [Fact]
    public async Task Duplicate_category_name_returns_conflict()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var duplicate = await CapexApi.PostJsonAsync(
            client, "/api/inventory/categories", new CatalogItemRequest(" food "), csrf);

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var problem = await duplicate.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        Assert.Equal("inventory.category.duplicate_name", problem!.Code);
    }

    [Fact]
    public async Task Duplicate_location_name_returns_conflict()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var duplicate = await CapexApi.PostJsonAsync(
            client, "/api/inventory/locations", new CatalogItemRequest(" pantry "), csrf);

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var problem = await duplicate.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        Assert.Equal("inventory.location.duplicate_name", problem!.Code);
    }

    private sealed record ProblemPayload(string? Code);
}
