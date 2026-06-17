using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Segaris.Api.Modules.Configuration;
using Segaris.Api.Modules.Configuration.Persistence;
using Segaris.Api.Modules.Travel.Domain;
using Segaris.Persistence;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.IntegrationTests.Travel;

internal static class TravelTestData
{
    private static readonly DateTimeOffset SeedNow = new(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);

    public static async Task<int> TripTypeIdAsync(IServiceProvider services, string name = "Regional")
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        return await database.Set<TravelTripType>()
            .Where(tripType => tripType.Name == name)
            .Select(tripType => tripType.Id)
            .SingleAsync();
    }

    public static async Task<int> CurrencyIdAsync(
        IServiceProvider services,
        string code = ConfigurationCatalog.CurrencyCodes.Default)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        return await database.Set<SegarisCurrency>()
            .Where(currency => currency.Code == code)
            .Select(currency => currency.Id)
            .SingleAsync();
    }

    public static async Task<int> SeedTripAsync(
        IServiceProvider services,
        int creatorId,
        string name = "Trip",
        string tripTypeName = "Regional",
        string? destination = "Madrid",
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        TravelTripStatus status = TravelTripStatus.Planned,
        string? notes = null,
        RecordVisibility visibility = RecordVisibility.Public,
        IReadOnlyList<TravelItineraryEntryValues>? itinerary = null,
        IReadOnlyList<(string CurrencyCode, decimal Amount)>? expenses = null)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();

        var tripTypeId = await database.Set<TravelTripType>()
            .Where(tripType => tripType.Name == tripTypeName)
            .Select(tripType => tripType.Id)
            .SingleAsync();

        var tripStart = startDate ?? new DateOnly(2026, 6, 1);
        var tripEnd = endDate ?? tripStart.AddDays(2);
        var entries = itinerary ?? [Entry("Arrival", tripStart, new TimeOnly(9, 0))];

        var trip = TravelTrip.Create(
            new TravelTripValues(
                name,
                tripTypeId,
                destination,
                tripStart,
                tripEnd,
                status,
                notes,
                visibility,
                entries),
            new UserId(creatorId),
            SeedNow);
        database.Add(trip);
        await database.SaveChangesAsync();

        if (expenses is { Count: > 0 })
        {
            var categoryId = await database.Set<TravelExpenseCategory>()
                .Where(category => category.Name == "Other")
                .Select(category => category.Id)
                .SingleAsync();
            var requestedCodes = expenses.Select(expense => expense.CurrencyCode).Distinct().ToArray();
            var currencies = await database.Set<SegarisCurrency>()
                .Where(currency => requestedCodes.Contains(currency.Code))
                .ToDictionaryAsync(currency => currency.Code, currency => currency.Id);

            foreach (var expense in expenses)
            {
                database.Add(TravelExpense.Create(
                    trip.Id,
                    new TravelExpenseValues(
                        categoryId,
                        $"{expense.CurrencyCode} expense",
                        tripStart,
                        expense.Amount,
                        currencies[expense.CurrencyCode],
                        null,
                        null,
                        null),
                    new UserId(creatorId),
                    SeedNow));
            }

            await database.SaveChangesAsync();
        }

        return trip.Id;
    }

    public static TravelItineraryEntryValues Entry(string title, DateOnly date, TimeOnly? time = null) =>
        new(date, time, title, null, null, null);
}
