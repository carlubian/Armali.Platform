using System.Net;
using System.Net.Http.Json;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Destinations.Contracts;
using Segaris.Shared.Api;
using Segaris.Shared.Authorization;

namespace Segaris.Api.IntegrationTests.Destinations;

public sealed class PlaceEndpointTests
{
    [Fact]
    public async Task Places_require_authentication()
    {
        using var server = new CapexTestServer();
        using var client = server.CreateClient();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var destinationId = await DestinationsTestData.SeedDestinationAsync(server.Services, founderId);

        using var response = await client.GetAsync($"/api/destinations/{destinationId}/places", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Places_are_scoped_paginated_and_ordered_by_name_then_id()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var destinationId = await DestinationsTestData.SeedDestinationAsync(server.Services, founderId, name: "Owner");
        var other = await DestinationsTestData.SeedDestinationAsync(server.Services, founderId, name: "Other");
        var bFirst = await DestinationsTestData.SeedPlaceAsync(server.Services, destinationId, founderId, name: "Beta");
        var alpha = await DestinationsTestData.SeedPlaceAsync(server.Services, destinationId, founderId, name: "Alpha");
        var bSecond = await DestinationsTestData.SeedPlaceAsync(server.Services, destinationId, founderId, name: "Beta");
        for (var index = 0; index < 9; index++)
        {
            await DestinationsTestData.SeedPlaceAsync(server.Services, destinationId, founderId, name: $"Gamma {index}");
        }

        await DestinationsTestData.SeedPlaceAsync(server.Services, other, founderId, name: "Foreign");

        var firstPage = await GetPageAsync(client, $"/api/destinations/{destinationId}/places?page=1&pageSize=10");
        var secondPage = await GetPageAsync(client, $"/api/destinations/{destinationId}/places?page=2&pageSize=10");

        Assert.Equal(12, firstPage.TotalCount);
        Assert.Equal(10, firstPage.Items.Count);
        Assert.Equal(2, secondPage.Items.Count);
        Assert.Equal(new[] { alpha, bFirst }, firstPage.Items.Take(2).Select(place => place.Id).ToArray());
        Assert.True(bSecond > bFirst);
        Assert.All(firstPage.Items, place => Assert.Equal(destinationId, place.DestinationId));
        Assert.DoesNotContain(
            firstPage.Items.Concat(secondPage.Items),
            place => place.Name == "Foreign");
    }

    [Theory]
    [InlineData("?page=0", "page")]
    [InlineData("?pageSize=0", "pageSize")]
    [InlineData("?pageSize=2", "pageSize")]
    [InlineData("?pageSize=101", "pageSize")]
    [InlineData("?sort=unknown", "sort")]
    [InlineData("?sortDirection=sideways", "sortDirection")]
    public async Task Places_reject_invalid_query_values(string queryString, string field)
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var destinationId = await DestinationsTestData.SeedDestinationAsync(server.Services, founderId);

        using var response = await client.GetAsync(
            $"/api/destinations/{destinationId}/places{queryString}",
            CancellationToken.None);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(problem!.Errors!.ContainsKey(field));
    }

    [Fact]
    public async Task Search_category_rating_filters_and_sorting_are_applied()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var destinationId = await DestinationsTestData.SeedDestinationAsync(server.Services, founderId);
        var hotelId = await DestinationsTestData.PlaceCategoryIdAsync(server.Services, "Hotel");
        var museumId = await DestinationsTestData.PlaceCategoryIdAsync(server.Services, "Museum");
        await DestinationsTestData.SeedPlaceAsync(server.Services, destinationId, founderId, name: "Ritz", categoryName: "Hotel", rating: 5);
        await DestinationsTestData.SeedPlaceAsync(server.Services, destinationId, founderId, name: "Hostel", categoryName: "Hotel", rating: 3);
        await DestinationsTestData.SeedPlaceAsync(server.Services, destinationId, founderId, name: "Prado", categoryName: "Museum", rating: 5);

        Assert.Equal("Ritz", Assert.Single((await GetPageAsync(client, $"/api/destinations/{destinationId}/places?search=RIT")).Items).Name);
        Assert.Equal(2, (await GetPageAsync(client, $"/api/destinations/{destinationId}/places?category={hotelId}")).TotalCount);
        Assert.Equal(2, (await GetPageAsync(client, $"/api/destinations/{destinationId}/places?rating=5")).TotalCount);

        var byCategory = await GetPageAsync(client, $"/api/destinations/{destinationId}/places?sort=category");
        Assert.Equal(new[] { hotelId, hotelId, museumId }, byCategory.Items.Select(place => place.CategoryId).ToArray());

        var byRating = await GetPageAsync(client, $"/api/destinations/{destinationId}/places?sort=rating&sortDirection=desc");
        Assert.Equal(new int?[] { 5, 5, 3 }, byRating.Items.Select(place => place.Rating).ToArray());
    }

    [Fact]
    public async Task Create_applies_defaults_and_rejects_invalid_category()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var destinationId = await DestinationsTestData.SeedDestinationAsync(server.Services, founderId);
        var categoryId = await DestinationsTestData.PlaceCategoryIdAsync(server.Services, "Hotel");
        var request = new CreatePlaceRequest("  Ritz  ", categoryId, Rating: null, Review: "", Address: "  Main street  ");

        using var createdResponse = await CapexApi.PostJsonAsync(
            client,
            $"/api/destinations/{destinationId}/places",
            request,
            csrf);
        var created = await createdResponse.Content.ReadFromJsonAsync<PlaceSummaryResponse>(CancellationToken.None);
        using var invalidCategory = await CapexApi.PostJsonAsync(
            client,
            $"/api/destinations/{destinationId}/places",
            request with { CategoryId = 999_999 },
            csrf);
        var problem = await invalidCategory.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        Assert.NotNull(created);
        Assert.Equal("Ritz", created.Name);
        Assert.Equal(destinationId, created.DestinationId);
        Assert.Null(created.Rating);
        Assert.Null(created.Review);
        Assert.Equal("Main street", created.Address);
        Assert.Equal(HttpStatusCode.BadRequest, invalidCategory.StatusCode);
        Assert.Equal("destinations.catalog.unknown_reference", problem!.Code);
    }

    [Fact]
    public async Task Mutations_require_antiforgery_and_validation()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var destinationId = await DestinationsTestData.SeedDestinationAsync(server.Services, founderId);
        var categoryId = await DestinationsTestData.PlaceCategoryIdAsync(server.Services, "Hotel");
        var request = new CreatePlaceRequest("Valid", categoryId, null, null, null);
        var invalid = request with { Name = " " };

        using var withoutCsrf = await CapexApi.PostJsonAsync(client, $"/api/destinations/{destinationId}/places", request, csrf: null);
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        using var invalidResponse = await CapexApi.PostJsonAsync(client, $"/api/destinations/{destinationId}/places", invalid, csrf);
        var problem = await invalidResponse.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, withoutCsrf.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);
        Assert.Equal("destinations.place.validation", problem!.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public async Task Create_rejects_ratings_outside_the_one_to_five_scale(int rating)
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var destinationId = await DestinationsTestData.SeedDestinationAsync(server.Services, founderId);
        var categoryId = await DestinationsTestData.PlaceCategoryIdAsync(server.Services, "Hotel");

        using var response = await CapexApi.PostJsonAsync(
            client,
            $"/api/destinations/{destinationId}/places",
            new CreatePlaceRequest("Edge", categoryId, rating, null, null),
            csrf);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("destinations.place.validation", problem!.Code);
    }

    [Fact]
    public async Task Update_and_delete_round_trip_within_destination_scope()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var destinationId = await DestinationsTestData.SeedDestinationAsync(server.Services, founderId);
        var placeId = await DestinationsTestData.SeedPlaceAsync(server.Services, destinationId, founderId, name: "Hostel");
        var museumId = await DestinationsTestData.PlaceCategoryIdAsync(server.Services, "Museum");
        var update = new UpdatePlaceRequest("Prado", museumId, 4, "Great", "Centre");

        using var updatedResponse = await CapexApi.PutJsonAsync(client, $"/api/destinations/{destinationId}/places/{placeId}", update, csrf);
        var updated = await updatedResponse.Content.ReadFromJsonAsync<PlaceSummaryResponse>(CancellationToken.None);
        using var deleted = await CapexApi.DeleteAsync(client, $"/api/destinations/{destinationId}/places/{placeId}", csrf);
        using var fetched = await client.GetAsync($"/api/destinations/{destinationId}/places/{placeId}", CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, updatedResponse.StatusCode);
        Assert.NotNull(updated);
        Assert.Equal("Prado", updated.Name);
        Assert.Equal(museumId, updated.CategoryId);
        Assert.Equal(4, updated.Rating);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, fetched.StatusCode);
    }

    [Fact]
    public async Task Place_routes_never_cross_destination_boundaries()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var ownerId = await DestinationsTestData.SeedDestinationAsync(server.Services, founderId, name: "Owner");
        var otherId = await DestinationsTestData.SeedDestinationAsync(server.Services, founderId, name: "Other");
        var placeId = await DestinationsTestData.SeedPlaceAsync(server.Services, ownerId, founderId, name: "Belongs");
        var museumId = await DestinationsTestData.PlaceCategoryIdAsync(server.Services, "Museum");

        using var crossGet = await client.GetAsync($"/api/destinations/{otherId}/places/{placeId}", CancellationToken.None);
        using var crossUpdate = await CapexApi.PutJsonAsync(
            client,
            $"/api/destinations/{otherId}/places/{placeId}",
            new UpdatePlaceRequest("Hijack", museumId, null, null, null),
            csrf);
        using var crossDelete = await CapexApi.DeleteAsync(client, $"/api/destinations/{otherId}/places/{placeId}", csrf);
        using var ownerGet = await client.GetAsync($"/api/destinations/{ownerId}/places/{placeId}", CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, crossGet.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, crossUpdate.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, crossDelete.StatusCode);
        Assert.Equal(HttpStatusCode.OK, ownerGet.StatusCode);
    }

    [Fact]
    public async Task Places_inherit_destination_visibility_and_return_not_found_when_inaccessible()
    {
        using var server = new CapexTestServer();
        using var admin = await server.CreateAuthenticatedClientAsync();
        var memberId = await server.CreateUserAsync("place-member", "PlaceMemberPass123!");
        var privateId = await DestinationsTestData.SeedDestinationAsync(
            server.Services,
            memberId,
            name: "Member private",
            visibility: RecordVisibility.Private);
        var placeId = await DestinationsTestData.SeedPlaceAsync(server.Services, privateId, memberId, name: "Hidden");
        var categoryId = await DestinationsTestData.PlaceCategoryIdAsync(server.Services, "Hotel");
        var adminCsrf = await CapexTestServer.GetCsrfTokenAsync(admin);

        using var list = await admin.GetAsync($"/api/destinations/{privateId}/places", CancellationToken.None);
        using var detail = await admin.GetAsync($"/api/destinations/{privateId}/places/{placeId}", CancellationToken.None);
        using var create = await CapexApi.PostJsonAsync(
            admin,
            $"/api/destinations/{privateId}/places",
            new CreatePlaceRequest("Intruder", categoryId, null, null, null),
            adminCsrf);

        Assert.Equal(HttpStatusCode.NotFound, list.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, detail.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, create.StatusCode);

        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, "place-member", "PlaceMemberPass123!");
        var memberView = await GetPageAsync(member, $"/api/destinations/{privateId}/places");
        Assert.Equal("Hidden", Assert.Single(memberView.Items).Name);
    }

    [Fact]
    public async Task Derived_average_rating_reflects_place_mutations()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var destinationId = await DestinationsTestData.SeedDestinationAsync(server.Services, founderId, name: "Rated");
        var categoryId = await DestinationsTestData.PlaceCategoryIdAsync(server.Services, "Hotel");

        var first = await CreatePlaceAsync(client, destinationId, new CreatePlaceRequest("A", categoryId, 5, null, null), csrf);
        await CreatePlaceAsync(client, destinationId, new CreatePlaceRequest("B", categoryId, 3, null, null), csrf);
        var afterCreate = await GetDestinationAsync(client, destinationId);

        using var removed = await CapexApi.DeleteAsync(client, $"/api/destinations/{destinationId}/places/{first}", csrf);
        var afterDelete = await GetDestinationAsync(client, destinationId);

        Assert.Equal(4.0m, afterCreate.AveragePlaceRating);
        Assert.Equal(2, afterCreate.RatedPlaceCount);
        Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);
        Assert.Equal(3.0m, afterDelete.AveragePlaceRating);
        Assert.Equal(1, afterDelete.RatedPlaceCount);
    }

    private static async Task<int> CreatePlaceAsync(
        HttpClient client,
        int destinationId,
        CreatePlaceRequest request,
        string? csrf)
    {
        using var response = await CapexApi.PostJsonAsync(client, $"/api/destinations/{destinationId}/places", request, csrf);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var place = await response.Content.ReadFromJsonAsync<PlaceSummaryResponse>(CancellationToken.None);
        Assert.NotNull(place);
        return place.Id;
    }

    private static async Task<DestinationResponse> GetDestinationAsync(HttpClient client, int destinationId)
    {
        var destination = await client.GetFromJsonAsync<DestinationResponse>(
            $"/api/destinations/{destinationId}",
            CancellationToken.None);
        Assert.NotNull(destination);
        return destination;
    }

    private static async Task<PaginatedResponse<PlaceSummaryResponse>> GetPageAsync(HttpClient client, string route)
    {
        var page = await client.GetFromJsonAsync<PaginatedResponse<PlaceSummaryResponse>>(route, CancellationToken.None);
        Assert.NotNull(page);
        return page;
    }

    private sealed record ProblemPayload(string? Code, IReadOnlyDictionary<string, string[]>? Errors);
}
