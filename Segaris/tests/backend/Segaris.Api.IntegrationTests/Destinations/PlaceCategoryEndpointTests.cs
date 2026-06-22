using System.Net;
using System.Net.Http.Json;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Modules.Destinations;
using Segaris.Api.Modules.Destinations.Contracts;

namespace Segaris.Api.IntegrationTests.Destinations;

public sealed class PlaceCategoryEndpointTests
{
    [Fact]
    public async Task Place_categories_require_authentication()
    {
        using var server = new CapexTestServer();
        using var client = server.CreateClient();

        using var response = await client.GetAsync("/api/destinations/place-categories", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Place_categories_return_the_seeded_catalogue_in_sort_order()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();

        var response = await client.GetFromJsonAsync<PlaceCategoryResponse[]>(
            "/api/destinations/place-categories",
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal(DestinationsCatalog.PlaceCategories.Count, response.Length);
        Assert.Equal(
            DestinationsCatalog.PlaceCategories.Select(seed => seed.Name),
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
            client, "/api/destinations/place-categories", new PlaceCategoryRequest("Anything"), csrf);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_can_create_update_move_and_delete_a_place_category()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var createResponse = await CapexApi.PostJsonAsync(
            client, "/api/destinations/place-categories", new PlaceCategoryRequest("Beach"), csrf);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<PlaceCategoryResponse>();
        Assert.NotNull(created);
        Assert.Equal("Beach", created.Name);

        using var updateResponse = await CapexApi.PutJsonAsync(
            client, $"/api/destinations/place-categories/{created.Id}", new PlaceCategoryRequest("Cove"), csrf);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<PlaceCategoryResponse>();
        Assert.NotNull(updated);
        Assert.Equal("Cove", updated.Name);

        using var moveResponse = await CapexApi.PostJsonAsync(
            client, $"/api/destinations/place-categories/{created.Id}/move", new CatalogMoveRequest("up"), csrf);
        Assert.Equal(HttpStatusCode.NoContent, moveResponse.StatusCode);

        using var deleteResponse = await CapexApi.DeleteAsync(
            client, $"/api/destinations/place-categories/{created.Id}", csrf);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var categories = await client.GetFromJsonAsync<PlaceCategoryResponse[]>(
            "/api/destinations/place-categories", CancellationToken.None);
        Assert.NotNull(categories);
        Assert.DoesNotContain(categories, item => item.Id == created.Id);
    }

    [Fact]
    public async Task Cannot_delete_the_last_place_category()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        var all = await client.GetFromJsonAsync<PlaceCategoryResponse[]>(
            "/api/destinations/place-categories", CancellationToken.None);
        Assert.NotNull(all);
        Assert.True(all.Length > 1, "Expected more than one seeded place category.");

        var remaining = all.ToList();
        while (remaining.Count > 1)
        {
            var source = remaining[0];
            var target = remaining[1];
            using var replaceResponse = await CapexApi.PostJsonAsync(
                client,
                $"/api/destinations/place-categories/{source.Id}/replace-and-delete",
                new CatalogReplacementRequest(ReplacementId: target.Id, ClearReferences: false, ExchangeRate: null),
                csrf);
            Assert.Equal(HttpStatusCode.NoContent, replaceResponse.StatusCode);
            remaining.RemoveAt(0);
        }

        var last = remaining[0];
        using var deleteLastResponse = await CapexApi.DeleteAsync(
            client, $"/api/destinations/place-categories/{last.Id}", csrf);
        Assert.Equal(HttpStatusCode.Conflict, deleteLastResponse.StatusCode);
    }

    [Fact]
    public async Task Create_place_category_rejects_duplicate_name_case_insensitively()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        // The first seeded place category is "Hotel" – try to create it again.
        using var response = await CapexApi.PostJsonAsync(
            client, "/api/destinations/place-categories", new PlaceCategoryRequest("hotel"), csrf);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Place_category_deletion_impact_returns_not_found_for_unknown_id()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();

        using var response = await client.GetAsync(
            "/api/destinations/place-categories/99999/deletion-impact",
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
