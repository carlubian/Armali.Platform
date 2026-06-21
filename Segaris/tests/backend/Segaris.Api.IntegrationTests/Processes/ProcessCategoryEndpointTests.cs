using System.Net;
using System.Net.Http.Json;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Modules.Processes.Contracts;
using Segaris.Api.Modules.Processes.Domain;

namespace Segaris.Api.IntegrationTests.Processes;

public sealed class ProcessCategoryEndpointTests
{
    [Fact]
    public async Task Categories_require_authentication()
    {
        using var server = new CapexTestServer();
        using var client = server.CreateClient();

        using var response = await client.GetAsync("/api/processes/categories", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Categories_return_the_seeded_catalogue_in_sort_order()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();

        var response = await client.GetFromJsonAsync<ProcessCategoryResponse[]>(
            "/api/processes/categories",
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal(ProcessesDefaults.InitialCategories.Count, response.Length);
        Assert.Equal(ProcessesDefaults.InitialCategories, response.Select(item => item.Name));
        Assert.Equal(Enumerable.Range(0, response.Length), response.Select(item => item.SortOrder));
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
            client, "/api/processes/categories", new ProcessCategoryRequest("Anything"), csrf);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_can_create_move_and_delete_a_category()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var createdResponse = await CapexApi.PostJsonAsync(
            client, "/api/processes/categories", new ProcessCategoryRequest("  Immigration  "), csrf);
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var created = await createdResponse.Content.ReadFromJsonAsync<ProcessCategoryResponse>(CancellationToken.None);
        Assert.Equal("Immigration", created!.Name);

        var beforeMove = await client.GetFromJsonAsync<ProcessCategoryResponse[]>("/api/processes/categories", CancellationToken.None);
        Assert.Equal(created.Id, beforeMove![^1].Id);

        using var moved = await CapexApi.PostJsonAsync(
            client, $"/api/processes/categories/{created.Id}/move", new CatalogMoveRequest("up"), csrf);
        Assert.Equal(HttpStatusCode.NoContent, moved.StatusCode);
        var afterMove = await client.GetFromJsonAsync<ProcessCategoryResponse[]>("/api/processes/categories", CancellationToken.None);
        Assert.Equal(created.Id, afterMove![^2].Id);

        var impact = await client.GetFromJsonAsync<CatalogDeletionImpactResponse>(
            $"/api/processes/categories/{created.Id}/deletion-impact", CancellationToken.None);
        Assert.False(impact!.IsReferenced);
        Assert.True(impact.CanDeleteDirectly);

        using var deleted = await CapexApi.DeleteAsync(client, $"/api/processes/categories/{created.Id}", csrf);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        var afterDelete = await client.GetFromJsonAsync<ProcessCategoryResponse[]>("/api/processes/categories", CancellationToken.None);
        Assert.Equal(ProcessesDefaults.InitialCategories.Count, afterDelete!.Length);
        Assert.Equal(Enumerable.Range(0, afterDelete.Length), afterDelete.Select(item => item.SortOrder));
    }

    [Fact]
    public async Task Duplicate_category_name_returns_conflict()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var duplicate = await CapexApi.PostJsonAsync(
            client, "/api/processes/categories", new ProcessCategoryRequest(" administrative "), csrf);

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var problem = await duplicate.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        Assert.Equal("processes.category.duplicate_name", problem!.Code);
    }

    [Fact]
    public async Task The_last_category_cannot_be_deleted()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        var categories = await client.GetFromJsonAsync<ProcessCategoryResponse[]>("/api/processes/categories", CancellationToken.None);
        Assert.NotNull(categories);

        // Remove every category except the final one; each is unreferenced, so the
        // direct delete path applies.
        foreach (var category in categories.Take(categories.Length - 1))
        {
            using var deleted = await CapexApi.DeleteAsync(client, $"/api/processes/categories/{category.Id}", csrf);
            Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        }

        var last = categories[^1];
        using var finalDelete = await CapexApi.DeleteAsync(client, $"/api/processes/categories/{last.Id}", csrf);
        Assert.Equal(HttpStatusCode.Conflict, finalDelete.StatusCode);
        var problem = await finalDelete.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        Assert.Equal("processes.category.required_not_empty", problem!.Code);
    }

    private sealed record ProblemPayload(string? Code);
}
