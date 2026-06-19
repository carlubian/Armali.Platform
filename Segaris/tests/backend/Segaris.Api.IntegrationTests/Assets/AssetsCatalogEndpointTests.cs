using System.Net;
using System.Net.Http.Json;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Assets;
using Segaris.Api.Modules.Assets.Contracts;
using Segaris.Api.Modules.Configuration.Contracts;

namespace Segaris.Api.IntegrationTests.Assets;

public sealed class AssetsCatalogEndpointTests
{
    [Theory]
    [InlineData("/api/assets/categories")]
    [InlineData("/api/assets/locations")]
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

        var response = await client.GetFromJsonAsync<AssetCategoryResponse[]>(
            "/api/assets/categories",
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal(AssetCatalog.Categories.Count, response.Length);
        Assert.Equal(
            AssetCatalog.Categories.Select(seed => seed.Name),
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

        var response = await client.GetFromJsonAsync<AssetLocationResponse[]>(
            "/api/assets/locations",
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal(AssetCatalog.Locations.Count, response.Length);
        Assert.Equal(
            AssetCatalog.Locations.Select(seed => seed.Name),
            response.Select(item => item.Name));
    }

    [Theory]
    [InlineData("/api/assets/categories")]
    [InlineData("/api/assets/locations")]
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
            client, "/api/assets/categories", new CatalogItemRequest("  Gadgets  "), csrf);
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var created = await createdResponse.Content.ReadFromJsonAsync<AssetCategoryResponse>(CancellationToken.None);
        Assert.Equal("Gadgets", created!.Name);

        var beforeMove = await client.GetFromJsonAsync<AssetCategoryResponse[]>("/api/assets/categories", CancellationToken.None);
        Assert.Equal(created.Id, beforeMove![^1].Id);

        using var moved = await CapexApi.PostJsonAsync(
            client, $"/api/assets/categories/{created.Id}/move", new CatalogMoveRequest("up"), csrf);
        Assert.Equal(HttpStatusCode.NoContent, moved.StatusCode);
        var afterMove = await client.GetFromJsonAsync<AssetCategoryResponse[]>("/api/assets/categories", CancellationToken.None);
        Assert.Equal(created.Id, afterMove![^2].Id);

        var impact = await client.GetFromJsonAsync<CatalogDeletionImpactResponse>(
            $"/api/assets/categories/{created.Id}/deletion-impact", CancellationToken.None);
        Assert.False(impact!.IsReferenced);
        Assert.True(impact.CanDeleteDirectly);

        using var deleted = await CapexApi.DeleteAsync(client, $"/api/assets/categories/{created.Id}", csrf);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        var afterDelete = await client.GetFromJsonAsync<AssetCategoryResponse[]>("/api/assets/categories", CancellationToken.None);
        Assert.Equal(AssetCatalog.Categories.Count, afterDelete!.Length);
    }

    [Fact]
    public async Task Admin_can_create_and_delete_a_location()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var createdResponse = await CapexApi.PostJsonAsync(
            client, "/api/assets/locations", new CatalogItemRequest("  Attic  "), csrf);
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var created = await createdResponse.Content.ReadFromJsonAsync<AssetLocationResponse>(CancellationToken.None);
        Assert.Equal("Attic", created!.Name);

        using var deleted = await CapexApi.DeleteAsync(client, $"/api/assets/locations/{created.Id}", csrf);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        var afterDelete = await client.GetFromJsonAsync<AssetLocationResponse[]>("/api/assets/locations", CancellationToken.None);
        Assert.Equal(AssetCatalog.Locations.Count, afterDelete!.Length);
    }

    [Fact]
    public async Task Duplicate_category_name_returns_conflict()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var duplicate = await CapexApi.PostJsonAsync(
            client, "/api/assets/categories", new CatalogItemRequest(" furniture "), csrf);

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var problem = await duplicate.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        Assert.Equal("assets.category.duplicate_name", problem!.Code);
    }

    [Fact]
    public async Task Duplicate_location_name_returns_conflict()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var duplicate = await CapexApi.PostJsonAsync(
            client, "/api/assets/locations", new CatalogItemRequest(" kitchen "), csrf);

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var problem = await duplicate.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        Assert.Equal("assets.location.duplicate_name", problem!.Code);
    }

    private sealed record ProblemPayload(string? Code);
}
