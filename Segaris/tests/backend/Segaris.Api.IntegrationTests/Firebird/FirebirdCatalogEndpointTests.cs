using System.Net;
using System.Net.Http.Json;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Modules.Firebird.Contracts;
using Segaris.Api.Modules.Firebird.Domain;

namespace Segaris.Api.IntegrationTests.Firebird;

public sealed class FirebirdCatalogEndpointTests
{
    [Fact]
    public async Task Catalogues_require_authentication()
    {
        using var server = new CapexTestServer();
        using var client = server.CreateClient();

        using var response = await client.GetAsync("/api/people/categories", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Catalogues_return_seeded_values_in_sort_order()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();

        var categories = await client.GetFromJsonAsync<PersonCategoryResponse[]>("/api/people/categories", CancellationToken.None);
        var platforms = await client.GetFromJsonAsync<UsernamePlatformResponse[]>("/api/people/platforms", CancellationToken.None);

        Assert.NotNull(categories);
        Assert.Equal(FirebirdDefaults.InitialCategories, categories.Select(item => item.Name));
        Assert.Equal(Enumerable.Range(0, categories.Length), categories.Select(item => item.SortOrder));
        Assert.All(categories, item => Assert.True(item.Id > 0));

        Assert.NotNull(platforms);
        Assert.Equal(FirebirdDefaults.InitialUsernamePlatforms, platforms.Select(item => item.Name));
        Assert.Equal(Enumerable.Range(0, platforms.Length), platforms.Select(item => item.SortOrder));
        Assert.All(platforms, item => Assert.True(item.Id > 0));
    }

    [Fact]
    public async Task Management_routes_reject_normal_users()
    {
        using var server = new CapexTestServer();
        await server.CreateUserAsync("member", "MemberPass123!");
        using var client = await server.CreateAuthenticatedClientAsync("member", "MemberPass123!");
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var response = await CapexApi.PostJsonAsync(
            client, "/api/people/categories", new PersonCategoryRequest("Anything"), csrf);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_can_create_move_and_delete_a_category()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var createdResponse = await CapexApi.PostJsonAsync(
            client, "/api/people/categories", new PersonCategoryRequest("  Neighbor  "), csrf);
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var created = await createdResponse.Content.ReadFromJsonAsync<PersonCategoryResponse>(CancellationToken.None);
        Assert.Equal("Neighbor", created!.Name);

        var beforeMove = await client.GetFromJsonAsync<PersonCategoryResponse[]>("/api/people/categories", CancellationToken.None);
        Assert.Equal(created.Id, beforeMove![^1].Id);

        using var moved = await CapexApi.PostJsonAsync(
            client, $"/api/people/categories/{created.Id}/move", new CatalogMoveRequest("up"), csrf);
        Assert.Equal(HttpStatusCode.NoContent, moved.StatusCode);
        var afterMove = await client.GetFromJsonAsync<PersonCategoryResponse[]>("/api/people/categories", CancellationToken.None);
        Assert.Equal(created.Id, afterMove![^2].Id);

        var impact = await client.GetFromJsonAsync<CatalogDeletionImpactResponse>(
            $"/api/people/categories/{created.Id}/deletion-impact", CancellationToken.None);
        Assert.False(impact!.IsReferenced);
        Assert.True(impact.CanDeleteDirectly);

        using var deleted = await CapexApi.DeleteAsync(client, $"/api/people/categories/{created.Id}", csrf);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
    }

    [Fact]
    public async Task Admin_can_create_move_and_delete_a_platform()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var createdResponse = await CapexApi.PostJsonAsync(
            client, "/api/people/platforms", new UsernamePlatformRequest("  Mastodon  "), csrf);
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var created = await createdResponse.Content.ReadFromJsonAsync<UsernamePlatformResponse>(CancellationToken.None);
        Assert.Equal("Mastodon", created!.Name);

        using var moved = await CapexApi.PostJsonAsync(
            client, $"/api/people/platforms/{created.Id}/move", new CatalogMoveRequest("up"), csrf);
        Assert.Equal(HttpStatusCode.NoContent, moved.StatusCode);

        var impact = await client.GetFromJsonAsync<CatalogDeletionImpactResponse>(
            $"/api/people/platforms/{created.Id}/deletion-impact", CancellationToken.None);
        Assert.False(impact!.IsReferenced);
        Assert.True(impact.CanDeleteDirectly);

        using var deleted = await CapexApi.DeleteAsync(client, $"/api/people/platforms/{created.Id}", csrf);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
    }

    [Fact]
    public async Task Duplicate_names_return_conflict()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var duplicateCategory = await CapexApi.PostJsonAsync(
            client, "/api/people/categories", new PersonCategoryRequest(" family "), csrf);
        using var duplicatePlatform = await CapexApi.PostJsonAsync(
            client, "/api/people/platforms", new UsernamePlatformRequest(" email "), csrf);

        Assert.Equal(HttpStatusCode.Conflict, duplicateCategory.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicatePlatform.StatusCode);
        var categoryProblem = await duplicateCategory.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        var platformProblem = await duplicatePlatform.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        Assert.Equal("firebird.category.duplicate_name", categoryProblem!.Code);
        Assert.Equal("firebird.platform.duplicate_name", platformProblem!.Code);
    }

    [Fact]
    public async Task The_last_catalogue_rows_cannot_be_deleted()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        var categories = await client.GetFromJsonAsync<PersonCategoryResponse[]>("/api/people/categories", CancellationToken.None)
            ?? throw new InvalidOperationException("Expected person categories.");
        foreach (var category in categories.Take(categories.Length - 1))
        {
            using var deleted = await CapexApi.DeleteAsync(client, $"/api/people/categories/{category.Id}", csrf);
            Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        }

        using var finalCategoryDelete = await CapexApi.DeleteAsync(client, $"/api/people/categories/{categories[^1].Id}", csrf);
        Assert.Equal(HttpStatusCode.Conflict, finalCategoryDelete.StatusCode);
        var categoryProblem = await finalCategoryDelete.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        Assert.Equal("firebird.category.required_not_empty", categoryProblem!.Code);

        var platforms = await client.GetFromJsonAsync<UsernamePlatformResponse[]>("/api/people/platforms", CancellationToken.None)
            ?? throw new InvalidOperationException("Expected username platforms.");
        foreach (var platform in platforms.Take(platforms.Length - 1))
        {
            using var deleted = await CapexApi.DeleteAsync(client, $"/api/people/platforms/{platform.Id}", csrf);
            Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        }

        using var finalPlatformDelete = await CapexApi.DeleteAsync(client, $"/api/people/platforms/{platforms[^1].Id}", csrf);
        Assert.Equal(HttpStatusCode.Conflict, finalPlatformDelete.StatusCode);
        var platformProblem = await finalPlatformDelete.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        Assert.Equal("firebird.platform.required_not_empty", platformProblem!.Code);
    }

    private sealed record ProblemPayload(string? Code);
}
