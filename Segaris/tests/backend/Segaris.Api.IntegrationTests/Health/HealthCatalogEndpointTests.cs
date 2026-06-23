using System.Net;
using System.Net.Http.Json;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Modules.Health;

namespace Segaris.Api.IntegrationTests.Health;

public sealed class HealthCatalogEndpointTests
{
    private sealed record CategoryRow(int Id, string Name, int SortOrder);

    public static TheoryData<string, string[]> Catalogs() => new()
    {
        { "disease-categories", HealthCatalog.DiseaseCategories.Select(seed => seed.Name).ToArray() },
        { "medicine-categories", HealthCatalog.MedicineCategories.Select(seed => seed.Name).ToArray() },
    };

    [Theory]
    [MemberData(nameof(Catalogs))]
    public async Task Categories_require_authentication(string segment, string[] _)
    {
        using var server = new CapexTestServer();
        using var client = server.CreateClient();

        using var response = await client.GetAsync($"/api/health/{segment}", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(Catalogs))]
    public async Task Categories_return_the_seeded_catalog_in_sort_order(string segment, string[] expectedNames)
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();

        var response = await client.GetFromJsonAsync<CategoryRow[]>(
            $"/api/health/{segment}", CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal(expectedNames, response.Select(item => item.Name));
        Assert.Equal(Enumerable.Range(0, response.Length), response.Select(item => item.SortOrder));
        Assert.All(response, item => Assert.True(item.Id > 0));
    }

    [Theory]
    [MemberData(nameof(Catalogs))]
    public async Task Management_routes_reject_normal_users(string segment, string[] _)
    {
        using var server = new CapexTestServer();
        await server.CreateUserAsync("member", "MemberPass123!");
        using var client = await server.CreateAuthenticatedClientAsync("member", "MemberPass123!");
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var response = await CapexApi.PostJsonAsync(
            client, $"/api/health/{segment}", new CatalogItemRequest("Anything"), csrf);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(Catalogs))]
    public async Task Admin_can_create_update_move_and_delete_a_category(string segment, string[] _)
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var route = $"/api/health/{segment}";

        using var createResponse = await CapexApi.PostJsonAsync(
            client, route, new CatalogItemRequest("Temporary"), csrf);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<CategoryRow>();
        Assert.NotNull(created);
        Assert.Equal("Temporary", created.Name);

        using var updateResponse = await CapexApi.PutJsonAsync(
            client, $"{route}/{created.Id}", new CatalogItemRequest("Renamed"), csrf);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<CategoryRow>();
        Assert.NotNull(updated);
        Assert.Equal("Renamed", updated.Name);

        using var moveResponse = await CapexApi.PostJsonAsync(
            client, $"{route}/{created.Id}/move", new CatalogMoveRequest("up"), csrf);
        Assert.Equal(HttpStatusCode.NoContent, moveResponse.StatusCode);

        using var deleteResponse = await CapexApi.DeleteAsync(client, $"{route}/{created.Id}", csrf);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var categories = await client.GetFromJsonAsync<CategoryRow[]>(route, CancellationToken.None);
        Assert.NotNull(categories);
        Assert.DoesNotContain(categories, item => item.Id == created.Id);
    }

    [Theory]
    [MemberData(nameof(Catalogs))]
    public async Task Create_category_rejects_duplicate_name_case_insensitively(string segment, string[] expectedNames)
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var response = await CapexApi.PostJsonAsync(
            client, $"/api/health/{segment}", new CatalogItemRequest(expectedNames[0].ToUpperInvariant()), csrf);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(Catalogs))]
    public async Task Deletion_impact_reports_replacement_candidates(string segment, string[] _)
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();

        var all = await client.GetFromJsonAsync<CategoryRow[]>($"/api/health/{segment}", CancellationToken.None);
        Assert.NotNull(all);
        Assert.True(all.Length > 1);

        var impact = await client.GetFromJsonAsync<CatalogDeletionImpactResponse>(
            $"/api/health/{segment}/{all[0].Id}/deletion-impact", CancellationToken.None);

        Assert.NotNull(impact);
        Assert.False(impact.IsReferenced);
        Assert.True(impact.CanDeleteDirectly);
        Assert.True(impact.HasReplacementCandidates);
        Assert.False(impact.CanClearReferences);
        Assert.False(impact.RequiresExchangeRate);
    }

    [Theory]
    [MemberData(nameof(Catalogs))]
    public async Task Cannot_delete_the_last_category(string segment, string[] _)
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var route = $"/api/health/{segment}";

        var all = await client.GetFromJsonAsync<CategoryRow[]>(route, CancellationToken.None);
        Assert.NotNull(all);
        Assert.True(all.Length > 1, "Expected more than one seeded category.");

        var remaining = all.ToList();
        while (remaining.Count > 1)
        {
            var source = remaining[0];
            var target = remaining[1];
            using var replaceResponse = await CapexApi.PostJsonAsync(
                client,
                $"{route}/{source.Id}/replace-and-delete",
                new CatalogReplacementRequest(ReplacementId: target.Id, ClearReferences: false, ExchangeRate: null),
                csrf);
            Assert.Equal(HttpStatusCode.NoContent, replaceResponse.StatusCode);
            remaining.RemoveAt(0);
        }

        using var deleteLastResponse = await CapexApi.DeleteAsync(client, $"{route}/{remaining[0].Id}", csrf);
        Assert.Equal(HttpStatusCode.Conflict, deleteLastResponse.StatusCode);
    }

    [Theory]
    [MemberData(nameof(Catalogs))]
    public async Task Deletion_impact_returns_not_found_for_unknown_id(string segment, string[] _)
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();

        using var response = await client.GetAsync(
            $"/api/health/{segment}/99999/deletion-impact", CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
