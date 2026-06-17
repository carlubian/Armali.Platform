using System.Net;
using System.Net.Http.Json;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Modules.Travel;
using Segaris.Api.Modules.Travel.Contracts;

namespace Segaris.Api.IntegrationTests.Travel;

public sealed class TravelCatalogEndpointTests
{
    [Theory]
    [InlineData("/api/travel/trip-types")]
    [InlineData("/api/travel/expense-categories")]
    public async Task Catalogs_require_authentication(string route)
    {
        using var server = new CapexTestServer();
        using var client = server.CreateClient();

        using var response = await client.GetAsync(route, CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Trip_types_return_the_seeded_catalog_in_sort_order()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();

        var response = await client.GetFromJsonAsync<TravelTripTypeResponse[]>(
            "/api/travel/trip-types",
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal(TravelCatalog.TripTypes.Count, response.Length);
        Assert.Equal(
            TravelCatalog.TripTypes.Select(seed => seed.Name),
            response.Select(item => item.Name));
        Assert.Equal(
            Enumerable.Range(0, response.Length),
            response.Select(item => item.SortOrder));
        Assert.All(response, item => Assert.True(item.Id > 0));
    }

    [Fact]
    public async Task Expense_categories_return_the_seeded_catalog_in_sort_order()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();

        var response = await client.GetFromJsonAsync<TravelExpenseCategoryResponse[]>(
            "/api/travel/expense-categories",
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal(TravelCatalog.ExpenseCategories.Count, response.Length);
        Assert.Equal(
            TravelCatalog.ExpenseCategories.Select(seed => seed.Name),
            response.Select(item => item.Name));
    }

    [Theory]
    [InlineData("/api/travel/trip-types")]
    [InlineData("/api/travel/expense-categories")]
    public async Task Management_routes_reject_normal_users(string route)
    {
        using var server = new CapexTestServer();
        await server.CreateUserAsync("member", "MemberPass123!");
        using var client = await server.CreateAuthenticatedClientAsync("member", "MemberPass123!");
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var response = await CapexApi.PostJsonAsync(
            client, route, new TravelTripTypeRequest("Anything"), csrf);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_can_create_move_and_delete_a_trip_type()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var createdResponse = await CapexApi.PostJsonAsync(
            client, "/api/travel/trip-types", new TravelTripTypeRequest("  Intercontinental  "), csrf);
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var created = await createdResponse.Content.ReadFromJsonAsync<TravelTripTypeResponse>(CancellationToken.None);
        Assert.Equal("Intercontinental", created!.Name);

        var beforeMove = await client.GetFromJsonAsync<TravelTripTypeResponse[]>("/api/travel/trip-types", CancellationToken.None);
        Assert.Equal(created.Id, beforeMove![^1].Id);

        using var moved = await CapexApi.PostJsonAsync(
            client, $"/api/travel/trip-types/{created.Id}/move", new CatalogMoveRequest("up"), csrf);
        Assert.Equal(HttpStatusCode.NoContent, moved.StatusCode);
        var afterMove = await client.GetFromJsonAsync<TravelTripTypeResponse[]>("/api/travel/trip-types", CancellationToken.None);
        Assert.Equal(created.Id, afterMove![^2].Id);

        var impact = await client.GetFromJsonAsync<CatalogDeletionImpactResponse>(
            $"/api/travel/trip-types/{created.Id}/deletion-impact", CancellationToken.None);
        Assert.False(impact!.IsReferenced);
        Assert.True(impact.CanDeleteDirectly);

        using var deleted = await CapexApi.DeleteAsync(client, $"/api/travel/trip-types/{created.Id}", csrf);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        var afterDelete = await client.GetFromJsonAsync<TravelTripTypeResponse[]>("/api/travel/trip-types", CancellationToken.None);
        Assert.Equal(TravelCatalog.TripTypes.Count, afterDelete!.Length);
    }

    [Fact]
    public async Task Admin_can_create_and_delete_an_expense_category()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var createdResponse = await CapexApi.PostJsonAsync(
            client, "/api/travel/expense-categories", new TravelExpenseCategoryRequest("  Insurance  "), csrf);
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var created = await createdResponse.Content.ReadFromJsonAsync<TravelExpenseCategoryResponse>(CancellationToken.None);
        Assert.Equal("Insurance", created!.Name);

        using var deleted = await CapexApi.DeleteAsync(client, $"/api/travel/expense-categories/{created.Id}", csrf);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        var afterDelete = await client.GetFromJsonAsync<TravelExpenseCategoryResponse[]>("/api/travel/expense-categories", CancellationToken.None);
        Assert.Equal(TravelCatalog.ExpenseCategories.Count, afterDelete!.Length);
    }

    [Fact]
    public async Task Duplicate_trip_type_name_returns_conflict()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var duplicate = await CapexApi.PostJsonAsync(
            client, "/api/travel/trip-types", new TravelTripTypeRequest(" regional "), csrf);

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var problem = await duplicate.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        Assert.Equal("travel.trip_type.duplicate_name", problem!.Code);
    }

    [Fact]
    public async Task Duplicate_expense_category_name_returns_conflict()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);

        using var duplicate = await CapexApi.PostJsonAsync(
            client, "/api/travel/expense-categories", new TravelExpenseCategoryRequest(" flight "), csrf);

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var problem = await duplicate.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        Assert.Equal("travel.expense_category.duplicate_name", problem!.Code);
    }

    private sealed record ProblemPayload(string? Code);
}
