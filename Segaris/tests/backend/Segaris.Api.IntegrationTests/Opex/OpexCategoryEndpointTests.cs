using System.Net;
using System.Net.Http.Json;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Modules.Opex;
using Segaris.Api.Modules.Opex.Contracts;

namespace Segaris.Api.IntegrationTests.Opex;

public sealed class OpexCategoryEndpointTests
{
    [Fact]
    public async Task Categories_require_authentication()
    {
        using var server = new CapexTestServer();
        using var client = server.CreateClient();

        using var response = await client.GetAsync("/api/opex/categories", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Categories_return_the_seeded_catalog_in_sort_order()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();

        var response = await client.GetFromJsonAsync<OpexCategoryResponse[]>(
            "/api/opex/categories",
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal(OpexCategoryCatalog.Categories.Count, response.Length);
        Assert.Equal(
            OpexCategoryCatalog.Categories.Select(seed => seed.Name),
            response.Select(item => item.Name));
        Assert.Equal(
            Enumerable.Range(0, response.Length),
            response.Select(item => item.SortOrder));
        Assert.All(response, item => Assert.True(item.Id > 0));
    }

    [Fact]
    public async Task Management_routes_reject_normal_users()
    {
        using var server = new CapexTestServer();
        await server.CreateUserAsync("member", "MemberPass123!");
        using var client = await server.CreateAuthenticatedClientAsync("member", "MemberPass123!");
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var response = await CapexApi.PostJsonAsync(
            client, "/api/opex/categories", new CatalogItemRequest("Pets"), csrf);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_can_create_move_and_delete_a_category()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var createdResponse = await CapexApi.PostJsonAsync(
            client, "/api/opex/categories", new CatalogItemRequest("  Pets  "), csrf);
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var created = await createdResponse.Content.ReadFromJsonAsync<OpexCategoryResponse>(CancellationToken.None);
        Assert.Equal("Pets", created!.Name);

        var beforeMove = await client.GetFromJsonAsync<OpexCategoryResponse[]>("/api/opex/categories", CancellationToken.None);
        Assert.Equal(created.Id, beforeMove![^1].Id);

        using var moved = await CapexApi.PostJsonAsync(
            client, $"/api/opex/categories/{created.Id}/move", new CatalogMoveRequest("up"), csrf);
        Assert.Equal(HttpStatusCode.NoContent, moved.StatusCode);
        var afterMove = await client.GetFromJsonAsync<OpexCategoryResponse[]>("/api/opex/categories", CancellationToken.None);
        Assert.Equal(created.Id, afterMove![^2].Id);

        var impact = await client.GetFromJsonAsync<CatalogDeletionImpactResponse>(
            $"/api/opex/categories/{created.Id}/deletion-impact", CancellationToken.None);
        Assert.False(impact!.IsReferenced);
        Assert.True(impact.CanDeleteDirectly);

        using var deleted = await CapexApi.DeleteAsync(client, $"/api/opex/categories/{created.Id}", csrf);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        var afterDelete = await client.GetFromJsonAsync<OpexCategoryResponse[]>("/api/opex/categories", CancellationToken.None);
        Assert.Equal(OpexCategoryCatalog.Categories.Count, afterDelete!.Length);
    }

    [Fact]
    public async Task Duplicate_category_name_returns_conflict()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var duplicate = await CapexApi.PostJsonAsync(
            client, "/api/opex/categories", new CatalogItemRequest(" housing "), csrf);

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var problem = await duplicate.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        Assert.Equal("opex.category.duplicate_name", problem!.Code);
    }

    private sealed record ProblemPayload(string? Code);
}
