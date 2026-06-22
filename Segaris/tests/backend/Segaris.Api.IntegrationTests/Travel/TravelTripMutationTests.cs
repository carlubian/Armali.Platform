using System.Net;
using System.Net.Http.Json;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Travel.Contracts;
using Segaris.Shared.Authorization;

namespace Segaris.Api.IntegrationTests.Travel;

public sealed class TravelTripMutationTests
{
    [Fact]
    public async Task Create_applies_defaults_and_persists_an_empty_itinerary()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var defaultTripTypeId = await TravelTestData.TripTypeIdAsync(server.Services);
        var request = new CreateTravelTripRequest(
            "  Madrid weekend  ",
            TripTypeId: 0,
            DestinationId: null,
            StartDate: default,
            EndDate: default,
            Status: null,
            Notes: null,
            Visibility: null,
            Itinerary: []);

        using var response = await CapexApi.PostJsonAsync(client, "/api/travel/trips", request, csrf);
        var created = await response.Content.ReadFromJsonAsync<TravelTripResponse>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(created);
        Assert.Equal("Madrid weekend", created.Name);
        Assert.Equal(defaultTripTypeId, created.TripTypeId);
        Assert.Equal(created.StartDate, created.EndDate);
        Assert.Equal("Planned", created.Status);
        Assert.Equal("Public", created.Visibility);
        Assert.Empty(created.Itinerary);
        Assert.Empty(created.Attachments);
    }

    [Fact]
    public async Task Create_requires_antiforgery_and_rejects_unknown_trip_type()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var request = DefaultCreateRequest(999_999);

        using var withoutCsrf = await CapexApi.PostJsonAsync(client, "/api/travel/trips", request, csrf: null);
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        using var unknownType = await CapexApi.PostJsonAsync(client, "/api/travel/trips", request, csrf);
        var problem = await unknownType.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, withoutCsrf.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, unknownType.StatusCode);
        Assert.Equal("travel.catalog.unknown_reference", problem!.Code);
    }

    [Fact]
    public async Task Create_rejects_date_invariant_and_itinerary_validation_failures()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var tripTypeId = await TravelTestData.TripTypeIdAsync(server.Services);
        var badDates = DefaultCreateRequest(
            tripTypeId,
            startDate: new DateOnly(2026, 7, 2),
            endDate: new DateOnly(2026, 7, 1));
        var badItinerary = DefaultCreateRequest(
            tripTypeId,
            itinerary: [new(new DateOnly(2026, 7, 1), null, "   ", null, null, null)]);

        using var datesResponse = await CapexApi.PostJsonAsync(client, "/api/travel/trips", badDates, csrf);
        using var itineraryResponse = await CapexApi.PostJsonAsync(client, "/api/travel/trips", badItinerary, csrf);
        var datesProblem = await datesResponse.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        var itineraryProblem = await itineraryResponse.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, datesResponse.StatusCode);
        Assert.Equal("travel.trip.validation", datesProblem!.Code);
        Assert.Equal(HttpStatusCode.BadRequest, itineraryResponse.StatusCode);
        Assert.Equal("travel.itinerary.validation", itineraryProblem!.Code);
    }

    [Fact]
    public async Task Update_replaces_the_itinerary_and_allows_public_collaboration()
    {
        using var server = new CapexTestServer();
        using var founder = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var tripId = await TravelTestData.SeedTripAsync(server.Services, founderId, name: "Original");
        var memberId = await server.CreateUserAsync("travel-editor", "TravelEditorPass123!");
        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, "travel-editor", "TravelEditorPass123!");
        var memberCsrf = await CapexTestServer.GetCsrfTokenAsync(member);
        var tripTypeId = await TravelTestData.TripTypeIdAsync(server.Services, "European");
        var update = new UpdateTravelTripRequest(
            "Updated",
            tripTypeId,
            DestinationId: null,
            new DateOnly(2026, 8, 1),
            new DateOnly(2026, 8, 3),
            "Ongoing",
            "Shared notes",
            "Public",
            [
                new(new DateOnly(2026, 8, 2), new TimeOnly(20, 0), "Dinner", "Alfama", "DIN123", null),
                new(new DateOnly(2026, 8, 1), null, "Arrival", "Airport", "FLY456", "Window seat"),
            ]);

        using var response = await CapexApi.PutJsonAsync(member, $"/api/travel/trips/{tripId}", update, memberCsrf);
        var updated = await response.Content.ReadFromJsonAsync<TravelTripResponse>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(updated);
        Assert.Equal("Updated", updated.Name);
        Assert.Equal(memberId, updated.UpdatedById);
        Assert.Equal(new[] { "Arrival", "Dinner" }, updated.Itinerary.Select(entry => entry.Title).ToArray());
        Assert.Equal(new[] { "FLY456", "DIN123" }, updated.Itinerary.Select(entry => entry.ReservationLocator).ToArray());
        Assert.Equal([1, 0], updated.Itinerary.Select(entry => entry.SortOrder).ToArray());
    }

    [Fact]
    public async Task Non_creator_cannot_make_a_public_trip_private_and_private_trips_are_hidden()
    {
        using var server = new CapexTestServer();
        using var founder = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var publicTripId = await TravelTestData.SeedTripAsync(server.Services, founderId, name: "Public");
        var privateTripId = await TravelTestData.SeedTripAsync(
            server.Services,
            founderId,
            name: "Private",
            visibility: RecordVisibility.Private);
        await server.CreateUserAsync("travel-member", "TravelMemberPass123!");
        using var member = server.CreateClient();
        await CapexTestServer.LoginAsync(member, "travel-member", "TravelMemberPass123!");
        var memberCsrf = await CapexTestServer.GetCsrfTokenAsync(member);
        var tripTypeId = await TravelTestData.TripTypeIdAsync(server.Services);
        var privatize = DefaultUpdateRequest(tripTypeId, visibility: "Private");
        var updatePrivate = DefaultUpdateRequest(tripTypeId);

        using var forbidden = await CapexApi.PutJsonAsync(member, $"/api/travel/trips/{publicTripId}", privatize, memberCsrf);
        using var hidden = await CapexApi.PutJsonAsync(member, $"/api/travel/trips/{privateTripId}", updatePrivate, memberCsrf);
        var forbiddenProblem = await forbidden.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);
        var hiddenProblem = await hidden.Content.ReadFromJsonAsync<ProblemPayload>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal("travel.trip.visibility_forbidden", forbiddenProblem!.Code);
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
        Assert.Equal("travel.trip.not_found", hiddenProblem!.Code);
    }

    [Fact]
    public async Task Delete_removes_the_trip_and_its_itinerary()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var csrf = await CapexTestServer.GetCsrfTokenAsync(client);
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var tripId = await TravelTestData.SeedTripAsync(
            server.Services,
            founderId,
            name: "Disposable",
            itinerary:
            [
                TravelTestData.Entry("One", new DateOnly(2026, 6, 1)),
                TravelTestData.Entry("Two", new DateOnly(2026, 6, 2)),
            ]);

        using var deleted = await CapexApi.DeleteAsync(client, $"/api/travel/trips/{tripId}", csrf);
        using var fetched = await client.GetAsync($"/api/travel/trips/{tripId}", CancellationToken.None);

        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, fetched.StatusCode);
    }

    internal static CreateTravelTripRequest DefaultCreateRequest(
        int tripTypeId,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        IReadOnlyList<TravelItineraryEntryRequest>? itinerary = null) =>
        new(
            "Trip",
            tripTypeId,
            DestinationId: null,
            startDate ?? new DateOnly(2026, 7, 1),
            endDate ?? new DateOnly(2026, 7, 2),
            "Planned",
            null,
            "Public",
            itinerary ?? [new(new DateOnly(2026, 7, 1), null, "Arrival", null, "ABC123", null)]);

    private static UpdateTravelTripRequest DefaultUpdateRequest(
        int tripTypeId,
        string visibility = "Public") =>
        new(
            "Updated",
            tripTypeId,
            DestinationId: null,
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 7, 2),
            "Planned",
            null,
            visibility,
            []);

    private sealed record ProblemPayload(string? Code);
}
