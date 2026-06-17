using System.Net;
using System.Net.Http.Json;
using Segaris.Api.IntegrationTests.Capex;
using Segaris.Api.Modules.Configuration;
using Segaris.Api.Modules.Travel.Contracts;
using Segaris.Shared.Authorization;

namespace Segaris.Api.IntegrationTests.Travel;

public sealed class TravelTripDetailTests
{
    [Fact]
    public async Task Detail_returns_itinerary_ordered_by_date_time_sort_order_and_currency_totals()
    {
        using var server = new CapexTestServer();
        using var client = await server.CreateAuthenticatedClientAsync();
        var founderId = await server.GetUserIdAsync(CapexTestServer.AdminUserName);
        var tripId = await TravelTestData.SeedTripAsync(
            server.Services,
            founderId,
            name: "Lisbon",
            itinerary:
            [
                TravelTestData.Entry("Dinner", new DateOnly(2026, 6, 2), new TimeOnly(20, 0)),
                TravelTestData.Entry("Arrival", new DateOnly(2026, 6, 1), new TimeOnly(9, 0)),
                TravelTestData.Entry("Hotel", new DateOnly(2026, 6, 1), new TimeOnly(9, 0)),
            ],
            expenses:
            [
                (ConfigurationCatalog.CurrencyCodes.Euro, 12.50m),
                (ConfigurationCatalog.CurrencyCodes.Euro, 7.25m),
                (ConfigurationCatalog.CurrencyCodes.UsDollar, 20m),
            ]);

        var trip = await client.GetFromJsonAsync<TravelTripResponse>(
            $"/api/travel/trips/{tripId}",
            CancellationToken.None);

        Assert.NotNull(trip);
        Assert.Equal(new[] { "Arrival", "Hotel", "Dinner" }, trip.Itinerary.Select(entry => entry.Title).ToArray());
        Assert.Empty(trip.Attachments);
        Assert.Equal(
            new[] { ("EUR", 19.75m), ("USD", 20m) },
            trip.ExpenseTotals.Select(total => (total.CurrencyCode, total.Amount)).ToArray());
    }

    [Fact]
    public async Task Detail_uses_not_found_for_missing_or_inaccessible_private_trips()
    {
        using var server = new CapexTestServer();
        using var admin = await server.CreateAuthenticatedClientAsync();
        var memberId = await server.CreateUserAsync("member", "MemberPass123!");
        var privateTripId = await TravelTestData.SeedTripAsync(
            server.Services,
            memberId,
            name: "Private",
            visibility: RecordVisibility.Private);

        using var missing = await admin.GetAsync("/api/travel/trips/999999", CancellationToken.None);
        using var inaccessible = await admin.GetAsync($"/api/travel/trips/{privateTripId}", CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, inaccessible.StatusCode);
    }
}
