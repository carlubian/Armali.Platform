using System.Net;
using System.Net.Http.Json;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Destinations.Contracts;
using Segaris.Shared.Api;
using Segaris.Shared.Authorization;

namespace Segaris.Api.IntegrationTests.Destinations;

public sealed class DestinationEndpointTests
{
    [Fact]
    public async Task Destinations_require_authentication()
    {
        using var server = new CapexTestServer();
        using var client = server.CreateClient();

        using var response = await client.GetAsync("/api/destinations", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Destinations_paginate_and_default_order_by_name_then_id()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var bFirst = await DestinationsTestData.SeedDestinationAsync(server.Services, founderId, name: "Beta");
        var alpha = await DestinationsTestData.SeedDestinationAsync(server.Services, founderId, name: "Alpha");
        var bSecond = await DestinationsTestData.SeedDestinationAsync(server.Services, founderId, name: "Beta");
        for (var index = 0; index < 9; index++)
        {
            await DestinationsTestData.SeedDestinationAsync(server.Services, founderId, name: $"Gamma {index}");
        }

        var firstPage = await GetPageAsync(client, "/api/destinations?page=1&pageSize=10");
        var secondPage = await GetPageAsync(client, "/api/destinations?page=2&pageSize=10");

        Assert.Equal(12, firstPage.TotalCount);
        Assert.Equal(10, firstPage.Items.Count);
        Assert.Equal(2, secondPage.Items.Count);
        Assert.Equal(new[] { alpha, bFirst }, firstPage.Items.Take(2).Select(destination => destination.Id).ToArray());
        Assert.True(bSecond > bFirst);
    }

    [Theory]
    [InlineData("/api/destinations?page=0", "page")]
    [InlineData("/api/destinations?pageSize=0", "pageSize")]
    [InlineData("/api/destinations?pageSize=2", "pageSize")]
    [InlineData("/api/destinations?pageSize=101", "pageSize")]
    [InlineData("/api/destinations?sort=unknown", "sort")]
    [InlineData("/api/destinations?sortDirection=sideways", "sortDirection")]
    public async Task Destinations_reject_invalid_query_values(string route, string field)
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();

        using var response = await client.GetAsync(route, CancellationToken.None);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(problem!.Errors!.ContainsKey(field));
    }

    [Fact]
    public async Task Search_category_schengen_and_category_sorting_are_applied()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var cityId = await DestinationsTestData.DestinationCategoryIdAsync(server.Services, "City");
        var regionId = await DestinationsTestData.DestinationCategoryIdAsync(server.Services, "Region");
        await DestinationsTestData.SeedDestinationAsync(server.Services, founderId, name: "Lisbon", categoryName: "City", isSchengenArea: true);
        await DestinationsTestData.SeedDestinationAsync(server.Services, founderId, name: "Madeira", categoryName: "Region", isSchengenArea: true);
        await DestinationsTestData.SeedDestinationAsync(server.Services, founderId, name: "Tokyo", categoryName: "City", isSchengenArea: false);

        Assert.Equal("Lisbon", Assert.Single((await GetPageAsync(client, "/api/destinations?search=LIS")).Items).Name);
        Assert.Equal(2, (await GetPageAsync(client, $"/api/destinations?category={cityId}")).TotalCount);
        Assert.Equal(2, (await GetPageAsync(client, "/api/destinations?isSchengenArea=true")).TotalCount);

        var sorted = await GetPageAsync(client, "/api/destinations?sort=category");
        Assert.Equal(new[] { cityId, cityId, regionId }, sorted.Items.Select(destination => destination.CategoryId).ToArray());
    }

    [Fact]
    public async Task Visibility_filter_limits_accessible_destination_list()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        await DestinationsTestData.SeedDestinationAsync(server.Services, founderId, name: "Public city");
        await DestinationsTestData.SeedDestinationAsync(
            server.Services,
            founderId,
            name: "Private city",
            visibility: RecordVisibility.Private);

        var publicPage = await GetPageAsync(client, "/api/destinations?visibility=Public");
        var privatePage = await GetPageAsync(client, "/api/destinations?visibility=Private");

        Assert.Contains(publicPage.Items, destination => destination.Name == "Public city");
        Assert.DoesNotContain(publicPage.Items, destination => destination.Name == "Private city");
        Assert.Contains(privatePage.Items, destination => destination.Name == "Private city");
        Assert.DoesNotContain(privatePage.Items, destination => destination.Name == "Public city");
    }

    [Fact]
    public async Task List_and_detail_include_derived_rating_projection()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var destinationId = await DestinationsTestData.SeedDestinationAsync(
            server.Services,
            founderId,
            name: "Rated",
            ratings: [5, 3, null]);

        var page = await GetPageAsync(client, "/api/destinations?search=Rated");
        var summary = Assert.Single(page.Items);
        var detail = await client.GetFromJsonAsync<DestinationResponse>($"/api/destinations/{destinationId}", CancellationToken.None);

        Assert.Equal(4.0m, summary.AveragePlaceRating);
        Assert.Equal(2, summary.RatedPlaceCount);
        Assert.NotNull(detail);
        Assert.Equal(4.0m, detail.AveragePlaceRating);
        Assert.Equal(2, detail.RatedPlaceCount);
        Assert.Empty(detail.Attachments);
        Assert.Equal("placeholder", detail.Thumbnail.Source);
    }

    [Fact]
    public async Task Listing_and_detail_hide_other_users_private_destinations()
    {
        using var server = new CapexTestServer();
        using var admin = await server.CreateAuthenticatedClientAsync();
        var memberId = await server.CreateUserAsync("destination-member", "DestinationMemberPass123!");
        var privateId = await DestinationsTestData.SeedDestinationAsync(
            server.Services,
            memberId,
            name: "Member private",
            visibility: RecordVisibility.Private);
        await DestinationsTestData.SeedDestinationAsync(server.Services, memberId, name: "Member public");

        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, "destination-member", "DestinationMemberPass123!");

        var adminView = await GetPageAsync(admin, "/api/destinations");
        var memberView = await GetPageAsync(member, "/api/destinations");
        using var hiddenDetail = await admin.GetAsync($"/api/destinations/{privateId}", CancellationToken.None);

        Assert.Contains(adminView.Items, destination => destination.Name == "Member public");
        Assert.DoesNotContain(adminView.Items, destination => destination.Name == "Member private");
        Assert.Contains(memberView.Items, destination => destination.Name == "Member private");
        Assert.Equal(HttpStatusCode.NotFound, hiddenDetail.StatusCode);
    }

    [Fact]
    public async Task Create_applies_defaults_and_rejects_invalid_category()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var categoryId = await DestinationsTestData.DestinationCategoryIdAsync(server.Services);
        var request = new CreateDestinationRequest(
            "  Barcelona  ",
            categoryId,
            Country: "  Spain  ",
            EntryRequirements: "",
            IsSchengenArea: false,
            Notes: "",
            Visibility: null);

        using var createdResponse = await CapexApi.PostJsonAsync(client, "/api/destinations", request, csrf);
        var created = await createdResponse.Content.ReadFromJsonAsync<DestinationResponse>(CancellationToken.None);
        using var invalidCategory = await CapexApi.PostJsonAsync(
            client,
            "/api/destinations",
            request with { CategoryId = 999_999 },
            csrf);
        var problem = await invalidCategory.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        Assert.NotNull(created);
        Assert.Equal("Barcelona", created.Name);
        Assert.Equal("Spain", created.Country);
        Assert.Null(created.EntryRequirements);
        Assert.Null(created.Notes);
        Assert.False(created.IsSchengenArea);
        Assert.Equal("Public", created.Visibility);
        Assert.Equal(HttpStatusCode.BadRequest, invalidCategory.StatusCode);
        Assert.Equal("destinations.catalog.unknown_reference", problem!.Code);
    }

    [Fact]
    public async Task Mutations_require_antiforgery_and_validation()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var categoryId = await DestinationsTestData.DestinationCategoryIdAsync(server.Services);
        var request = new CreateDestinationRequest("Valid", categoryId, null, null, false, null, "Public");
        var invalid = request with { Name = " " };

        using var withoutCsrf = await CapexApi.PostJsonAsync(client, "/api/destinations", request, csrf: null);
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        using var invalidResponse = await CapexApi.PostJsonAsync(client, "/api/destinations", invalid, csrf);
        var problem = await invalidResponse.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, withoutCsrf.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);
        Assert.Equal("destinations.destination.validation", problem!.Code);
    }

    [Fact]
    public async Task Update_allows_public_collaboration_but_only_creator_can_change_visibility()
    {
        using var server = new CapexTestServer();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var publicId = await DestinationsTestData.SeedDestinationAsync(server.Services, founderId, name: "Original");
        var privateId = await DestinationsTestData.SeedDestinationAsync(
            server.Services,
            founderId,
            name: "Private",
            visibility: RecordVisibility.Private);
        var memberId = await server.CreateUserAsync("destination-editor", "DestinationEditorPass123!");
        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, "destination-editor", "DestinationEditorPass123!");
        var memberCsrf = await CapexTestServer.GetCsrfTokenAsync(member);
        var categoryId = await DestinationsTestData.DestinationCategoryIdAsync(server.Services, "Region");
        var update = new UpdateDestinationRequest("Updated", categoryId, "Portugal", "Passport", true, "Notes", "Public");
        var privatize = update with { Visibility = "Private" };

        using var updatedResponse = await CapexApi.PutJsonAsync(member, $"/api/destinations/{publicId}", update, memberCsrf);
        var updated = await updatedResponse.Content.ReadFromJsonAsync<DestinationResponse>(CancellationToken.None);
        using var forbidden = await CapexApi.PutJsonAsync(member, $"/api/destinations/{publicId}", privatize, memberCsrf);
        using var hidden = await CapexApi.PutJsonAsync(member, $"/api/destinations/{privateId}", update, memberCsrf);

        Assert.Equal(HttpStatusCode.OK, updatedResponse.StatusCode);
        Assert.NotNull(updated);
        Assert.Equal("Updated", updated.Name);
        Assert.Equal(memberId, updated.UpdatedById);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
    }

    [Fact]
    public async Task Delete_removes_the_destination_and_hides_private_records_from_collaborators()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var destinationId = await DestinationsTestData.SeedDestinationAsync(server.Services, founderId, name: "Disposable", ratings: [5]);

        using var deleted = await CapexApi.DeleteAsync(client, $"/api/destinations/{destinationId}", csrf);
        using var fetched = await client.GetAsync($"/api/destinations/{destinationId}", CancellationToken.None);

        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, fetched.StatusCode);
    }

    private static async Task<PaginatedResponse<DestinationSummaryResponse>> GetPageAsync(
        HttpClient client,
        string route)
    {
        var page = await client.GetFromJsonAsync<PaginatedResponse<DestinationSummaryResponse>>(
            route,
            CancellationToken.None);
        Assert.NotNull(page);
        return page;
    }

    private sealed record ProblemPayload(string? Code, IReadOnlyDictionary<string, string[]>? Errors);
}
