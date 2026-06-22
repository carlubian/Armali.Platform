using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.IntegrationTests.Destinations;
using Segaris.Api.Modules.Destinations.Contracts;
using Segaris.Api.Modules.Travel.Contracts;
using Segaris.Shared.Authorization;

namespace Segaris.Api.IntegrationTests.Travel;

public sealed class TravelDestinationReferenceTests
{
    [Fact]
    public async Task Create_links_accessible_destination_and_resolves_display_fields()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var tripTypeId = await TravelTestData.TripTypeIdAsync(server.Services);
        var destinationId = await DestinationsTestData.SeedDestinationAsync(
            server.Services,
            founderId,
            name: "Barcelona",
            country: "Spain",
            visibility: RecordVisibility.Public);

        using var response = await CapexApi.PostJsonAsync(
            client,
            "/api/travel/trips",
            TravelTripMutationTests.DefaultCreateRequest(tripTypeId) with { DestinationId = destinationId },
            csrf);
        var created = await response.Content.ReadFromJsonAsync<TravelTripResponse>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(created);
        Assert.Equal(destinationId, created.DestinationId);
        Assert.Equal("Barcelona", created.DestinationName);
        Assert.Equal("Spain", created.DestinationCountry);
    }

    [Fact]
    public async Task Public_trip_rejects_private_destination_and_unknown_destination()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var tripTypeId = await TravelTestData.TripTypeIdAsync(server.Services);
        var privateDestinationId = await DestinationsTestData.SeedDestinationAsync(
            server.Services,
            founderId,
            name: "Private destination",
            visibility: RecordVisibility.Private);

        using var privateResponse = await CapexApi.PostJsonAsync(
            client,
            "/api/travel/trips",
            TravelTripMutationTests.DefaultCreateRequest(tripTypeId) with { DestinationId = privateDestinationId },
            csrf);
        using var missingResponse = await CapexApi.PostJsonAsync(
            client,
            "/api/travel/trips",
            TravelTripMutationTests.DefaultCreateRequest(tripTypeId) with { DestinationId = 999_999 },
            csrf);
        var privateProblem = await privateResponse.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        var missingProblem = await missingResponse.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.Forbidden, privateResponse.StatusCode);
        Assert.Equal("travel.trip.visibility_forbidden", privateProblem!.Code);
        Assert.Equal(HttpStatusCode.BadRequest, missingResponse.StatusCode);
        Assert.Equal("travel.catalog.unknown_reference", missingProblem!.Code);
    }

    [Fact]
    public async Task Private_trip_can_reference_private_accessible_destination_but_cannot_be_made_public()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var tripTypeId = await TravelTestData.TripTypeIdAsync(server.Services);
        var destinationId = await DestinationsTestData.SeedDestinationAsync(
            server.Services,
            founderId,
            name: "Hidden coast",
            visibility: RecordVisibility.Private);
        var create = TravelTripMutationTests.DefaultCreateRequest(tripTypeId) with
        {
            DestinationId = destinationId,
            Visibility = "Private",
        };

        using var createdResponse = await CapexApi.PostJsonAsync(client, "/api/travel/trips", create, csrf);
        var created = await createdResponse.Content.ReadFromJsonAsync<TravelTripResponse>(CancellationToken.None);
        var publish = new UpdateTravelTripRequest(
            "Published",
            tripTypeId,
            destinationId,
            created!.StartDate,
            created.EndDate,
            created.Status,
            null,
            "Public",
            []);
        using var publishResponse = await CapexApi.PutJsonAsync(client, $"/api/travel/trips/{created.Id}", publish, csrf);
        var problem = await publishResponse.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        Assert.Equal(destinationId, created.DestinationId);
        Assert.Equal(HttpStatusCode.Forbidden, publishResponse.StatusCode);
        Assert.Equal("travel.trip.visibility_forbidden", problem!.Code);
    }

    [Fact]
    public async Task Destination_deletion_impact_counts_and_delete_clears_mixed_ownership_trip_links()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var memberId = await server.CreateUserAsync("travel-destination-member", "TravelDestinationMember123!");
        var destinationId = await DestinationsTestData.SeedDestinationAsync(server.Services, founderId, name: "Seville");
        var publicTripId = await TravelTestData.SeedTripAsync(server.Services, founderId, name: "Public trip", destinationId: destinationId);
        var privateTripId = await TravelTestData.SeedTripAsync(
            server.Services,
            memberId,
            name: "Member private trip",
            destinationId: destinationId,
            visibility: RecordVisibility.Private);

        using var impactResponse = await client.GetAsync($"/api/destinations/{destinationId}/deletion-impact", CancellationToken.None);
        var impact = await impactResponse.Content.ReadFromJsonAsync<DestinationDeletionImpactResponse>(CancellationToken.None);
        using var deleteResponse = await CapexApi.DeleteAsync(client, $"/api/destinations/{destinationId}", csrf);

        Assert.Equal(HttpStatusCode.OK, impactResponse.StatusCode);
        Assert.NotNull(impact);
        Assert.True(impact.IsReferenced);
        Assert.Equal(2, impact.ReferenceCount);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Null(await TravelTestData.TripDestinationIdAsync(server.Services, publicTripId));
        Assert.Null(await TravelTestData.TripDestinationIdAsync(server.Services, privateTripId));
    }

    [Fact]
    public async Task Destination_delete_rolls_back_destination_and_trip_links_when_a_later_handler_fails()
    {
        using var server = new CapexTestServer(configureServices: services =>
            services.AddScoped<IDestinationDeletionReferenceHandler, FailingDestinationDeletionReferenceHandler>());
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var destinationId = await DestinationsTestData.SeedDestinationAsync(server.Services, founderId, name: "Rollback");
        var tripId = await TravelTestData.SeedTripAsync(server.Services, founderId, destinationId: destinationId);

        using var response = await CapexApi.DeleteAsync(client, $"/api/destinations/{destinationId}", csrf);
        using var fetched = await client.GetAsync($"/api/destinations/{destinationId}", CancellationToken.None);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, fetched.StatusCode);
        Assert.Equal(destinationId, await TravelTestData.TripDestinationIdAsync(server.Services, tripId));
    }

    private sealed class FailingDestinationDeletionReferenceHandler : IDestinationDeletionReferenceHandler
    {
        public Task<int> CountReferencesAsync(int destinationId, CancellationToken cancellationToken) =>
            Task.FromResult(0);

        public Task ClearReferencesAsync(
            DestinationDeletionClearing clearing,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Injected test failure after earlier handlers run.");
    }

    private sealed record ProblemPayload(string? Code);
}
