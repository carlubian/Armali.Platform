using System.Net;
using System.Net.Http.Json;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Travel.Contracts;
using Segaris.Api.Modules.Travel.Domain;
using Segaris.Shared.Api;
using Segaris.Shared.Authorization;

namespace Segaris.Api.IntegrationTests.Travel;

public sealed class TravelTripListTests
{
    [Fact]
    public async Task Trips_require_authentication()
    {
        using var server = new CapexTestServer();
        using var client = server.CreateClient();

        using var response = await client.GetAsync("/api/travel/trips", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Trips_paginate_with_total_count()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        for (var index = 0; index < 7; index++)
        {
            await TravelTestData.SeedTripAsync(
                server.Services,
                founderId,
                name: $"Trip {index}",
                startDate: new DateOnly(2026, 1, 1).AddDays(index));
        }

        var firstPage = await GetPageAsync(client, "/api/travel/trips?page=1&pageSize=5");
        var secondPage = await GetPageAsync(client, "/api/travel/trips?page=2&pageSize=5");

        Assert.Equal(7, firstPage.TotalCount);
        Assert.Equal(5, firstPage.Items.Count);
        Assert.Equal(2, secondPage.Items.Count);
    }

    [Theory]
    [InlineData("/api/travel/trips?page=0", "page")]
    [InlineData("/api/travel/trips?pageSize=0", "pageSize")]
    [InlineData("/api/travel/trips?pageSize=101", "pageSize")]
    [InlineData("/api/travel/trips?status=Nope", "status")]
    [InlineData("/api/travel/trips?visibility=Nope", "visibility")]
    [InlineData("/api/travel/trips?sort=unknown", "sort")]
    [InlineData("/api/travel/trips?sortDirection=sideways", "sortDirection")]
    public async Task Trips_reject_invalid_query_values(string route, string field)
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();

        using var response = await client.GetAsync(route, CancellationToken.None);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(problem!.Errors!.ContainsKey(field));
    }

    [Fact]
    public async Task Default_order_is_start_date_then_id_descending()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var first = await TravelTestData.SeedTripAsync(server.Services, founderId, name: "First", startDate: new DateOnly(2026, 5, 1));
        var second = await TravelTestData.SeedTripAsync(server.Services, founderId, name: "Second", startDate: new DateOnly(2026, 5, 1));
        var later = await TravelTestData.SeedTripAsync(server.Services, founderId, name: "Later", startDate: new DateOnly(2026, 6, 1));

        var page = await GetPageAsync(client, "/api/travel/trips");

        Assert.Equal(new[] { later, second, first }, page.Items.Select(trip => trip.Id).ToArray());
    }

    [Fact]
    public async Task Search_matches_name_destination_and_notes()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        await TravelTestData.SeedTripAsync(server.Services, founderId, name: "Lisbon getaway");
        await TravelTestData.SeedTripAsync(server.Services, founderId, name: "Plain", destination: "Lisbon");
        await TravelTestData.SeedTripAsync(server.Services, founderId, name: "Conference", notes: "Train through Lisbon");
        await TravelTestData.SeedTripAsync(server.Services, founderId, name: "Unrelated", destination: "Paris");

        var page = await GetPageAsync(client, "/api/travel/trips?search=LISBON");

        Assert.Equal(3, page.TotalCount);
        Assert.DoesNotContain(page.Items, trip => trip.Name == "Unrelated");
    }

    [Fact]
    public async Task Trip_type_status_visibility_and_creator_filters_are_exact()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var memberId = await server.CreateUserAsync("author", "AuthorPass123!");
        await TravelTestData.SeedTripAsync(server.Services, founderId, name: "Regional planned", tripTypeName: "Regional", status: TravelTripStatus.Planned);
        await TravelTestData.SeedTripAsync(server.Services, founderId, name: "European private", tripTypeName: "European", status: TravelTripStatus.Completed, visibility: RecordVisibility.Private);
        await TravelTestData.SeedTripAsync(server.Services, memberId, name: "By author", tripTypeName: "National", status: TravelTripStatus.Cancelled);
        var europeanId = await TravelTestData.TripTypeIdAsync(server.Services, "European");

        Assert.Equal("European private", Assert.Single((await GetPageAsync(client, $"/api/travel/trips?tripType={europeanId}")).Items).Name);
        Assert.Equal("European private", Assert.Single((await GetPageAsync(client, "/api/travel/trips?status=Completed")).Items).Name);
        Assert.Equal("European private", Assert.Single((await GetPageAsync(client, "/api/travel/trips?visibility=Private")).Items).Name);
        Assert.Equal("By author", Assert.Single((await GetPageAsync(client, $"/api/travel/trips?creator={memberId}")).Items).Name);
    }

    [Fact]
    public async Task Listing_hides_other_users_private_trips_from_everyone_including_admins()
    {
        using var server = new CapexTestServer();
        using var admin = await server.CreateAuthenticatedClientAsync();
        var memberId = await server.CreateUserAsync("member", "MemberPass123!");
        await TravelTestData.SeedTripAsync(server.Services, memberId, name: "Member public");
        await TravelTestData.SeedTripAsync(server.Services, memberId, name: "Member private", visibility: RecordVisibility.Private);

        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, "member", "MemberPass123!");

        var adminView = await GetPageAsync(admin, "/api/travel/trips");
        var memberView = await GetPageAsync(member, "/api/travel/trips");

        Assert.Contains(adminView.Items, trip => trip.Name == "Member public");
        Assert.DoesNotContain(adminView.Items, trip => trip.Name == "Member private");
        Assert.Contains(memberView.Items, trip => trip.Name == "Member private");
    }

    private static async Task<PaginatedResponse<TravelTripSummaryResponse>> GetPageAsync(
        HttpClient client,
        string route)
    {
        var page = await client.GetFromJsonAsync<PaginatedResponse<TravelTripSummaryResponse>>(
            route,
            CancellationToken.None);
        Assert.NotNull(page);
        return page;
    }

    private sealed record ProblemPayload(IReadOnlyDictionary<string, string[]>? Errors);
}
