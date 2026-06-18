using System.Net;
using System.Net.Http.Json;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Clothes;
using Segaris.Api.Modules.Clothes.Contracts;
using Segaris.Api.Modules.Configuration.Contracts;

namespace Segaris.Api.IntegrationTests.Clothes;

public sealed class ClothesCatalogEndpointTests
{
    [Theory]
    [InlineData("/api/clothes/categories")]
    [InlineData("/api/clothes/colors")]
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

        var response = await client.GetFromJsonAsync<ClothingCategoryResponse[]>(
            "/api/clothes/categories",
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal(ClothesCatalog.Categories.Count, response.Length);
        Assert.Equal(
            ClothesCatalog.Categories.Select(seed => seed.Name),
            response.Select(item => item.Name));
        Assert.Equal(
            Enumerable.Range(0, response.Length),
            response.Select(item => item.SortOrder));
        Assert.All(response, item => Assert.True(item.Id > 0));
    }

    [Fact]
    public async Task Colors_return_the_seeded_catalog_with_colour_values_in_sort_order()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();

        var response = await client.GetFromJsonAsync<ClothingColorResponse[]>(
            "/api/clothes/colors",
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal(ClothesCatalog.Colors.Count, response.Length);
        Assert.Equal(
            ClothesCatalog.Colors.Select(seed => seed.Name),
            response.Select(item => item.Name));
        Assert.Equal(
            ClothesCatalog.Colors.Select(seed => seed.ColorValue),
            response.Select(item => item.ColorValue));
    }

    [Theory]
    [InlineData("/api/clothes/categories")]
    [InlineData("/api/clothes/colors")]
    public async Task Management_routes_reject_normal_users(string route)
    {
        using var server = new CapexTestServer();
        await server.CreateUserAsync("member", "MemberPass123!");
        using var client = await server.CreateAuthenticatedClientAsync("member", "MemberPass123!");
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var response = await CapexApi.PostJsonAsync(
            client, route, new CatalogItemRequest("Anything", "#123456"), csrf);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_can_create_move_impact_and_delete_a_category()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var createdResponse = await CapexApi.PostJsonAsync(
            client, "/api/clothes/categories", new CatalogItemRequest("  Loungewear  "), csrf);
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var created = await createdResponse.Content.ReadFromJsonAsync<ClothingCategoryResponse>(CancellationToken.None);
        Assert.Equal("Loungewear", created!.Name);

        var beforeMove = await client.GetFromJsonAsync<ClothingCategoryResponse[]>("/api/clothes/categories", CancellationToken.None);
        Assert.Equal(created.Id, beforeMove![^1].Id);

        using var moved = await CapexApi.PostJsonAsync(
            client, $"/api/clothes/categories/{created.Id}/move", new CatalogMoveRequest("up"), csrf);
        Assert.Equal(HttpStatusCode.NoContent, moved.StatusCode);
        var afterMove = await client.GetFromJsonAsync<ClothingCategoryResponse[]>("/api/clothes/categories", CancellationToken.None);
        Assert.Equal(created.Id, afterMove![^2].Id);

        var impact = await client.GetFromJsonAsync<CatalogDeletionImpactResponse>(
            $"/api/clothes/categories/{created.Id}/deletion-impact", CancellationToken.None);
        Assert.False(impact!.IsReferenced);
        Assert.True(impact.CanDeleteDirectly);
        Assert.False(impact.CanClearReferences);

        using var deleted = await CapexApi.DeleteAsync(client, $"/api/clothes/categories/{created.Id}", csrf);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        var afterDelete = await client.GetFromJsonAsync<ClothingCategoryResponse[]>("/api/clothes/categories", CancellationToken.None);
        Assert.Equal(ClothesCatalog.Categories.Count, afterDelete!.Length);
    }

    [Fact]
    public async Task Admin_can_create_update_and_delete_a_colour_with_its_colour_value()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var createdResponse = await CapexApi.PostJsonAsync(
            client, "/api/clothes/colors", new CatalogItemRequest("  Teal  ", " #0d9488 "), csrf);
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var created = await createdResponse.Content.ReadFromJsonAsync<ClothingColorResponse>(CancellationToken.None);
        Assert.Equal("Teal", created!.Name);
        Assert.Equal("#0D9488", created.ColorValue);

        using var updatedResponse = await CapexApi.PutJsonAsync(
            client, $"/api/clothes/colors/{created.Id}", new CatalogItemRequest("Teal", "#14B8A6"), csrf);
        Assert.Equal(HttpStatusCode.OK, updatedResponse.StatusCode);
        var updated = await updatedResponse.Content.ReadFromJsonAsync<ClothingColorResponse>(CancellationToken.None);
        Assert.Equal("#14B8A6", updated!.ColorValue);

        using var deleted = await CapexApi.DeleteAsync(client, $"/api/clothes/colors/{created.Id}", csrf);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        var afterDelete = await client.GetFromJsonAsync<ClothingColorResponse[]>("/api/clothes/colors", CancellationToken.None);
        Assert.Equal(ClothesCatalog.Colors.Count, afterDelete!.Length);
    }

    [Fact]
    public async Task Colour_creation_rejects_an_invalid_colour_value()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var response = await CapexApi.PostJsonAsync(
            client, "/api/clothes/colors", new CatalogItemRequest("Almost", "#12345"), csrf);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        Assert.Equal("clothes.color.validation", problem!.Code);
    }

    [Fact]
    public async Task Duplicate_category_name_returns_conflict()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var duplicate = await CapexApi.PostJsonAsync(
            client, "/api/clothes/categories", new CatalogItemRequest(" tops "), csrf);

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var problem = await duplicate.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        Assert.Equal("clothes.category.duplicate_name", problem!.Code);
    }

    [Fact]
    public async Task Duplicate_colour_name_returns_conflict()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var duplicate = await CapexApi.PostJsonAsync(
            client, "/api/clothes/colors", new CatalogItemRequest(" black ", "#222222"), csrf);

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var problem = await duplicate.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        Assert.Equal("clothes.color.duplicate_name", problem!.Code);
    }

    private sealed record ProblemPayload(string? Code);
}
