using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Segaris.Api.Modules.Destinations.Domain;
using Segaris.Persistence;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.IntegrationTests.Destinations;

internal static class DestinationsTestData
{
    private static readonly DateTimeOffset SeedNow = new(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);

    public static async Task<int> DestinationCategoryIdAsync(IServiceProvider services, string name = "City")
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        return await database.Set<DestinationCategory>()
            .Where(category => category.Name == name)
            .Select(category => category.Id)
            .SingleAsync();
    }

    public static async Task<int> PlaceCategoryIdAsync(IServiceProvider services, string name = "Other")
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        return await database.Set<PlaceCategory>()
            .Where(category => category.Name == name)
            .Select(category => category.Id)
            .SingleAsync();
    }

    public static async Task<int> SeedDestinationAsync(
        IServiceProvider services,
        int creatorId,
        string name = "Destination",
        string categoryName = "City",
        string? country = "Spain",
        string? entryRequirements = null,
        bool isSchengenArea = false,
        string? notes = null,
        RecordVisibility visibility = RecordVisibility.Public,
        IReadOnlyList<int?>? ratings = null)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();

        var categoryId = await database.Set<DestinationCategory>()
            .Where(category => category.Name == categoryName)
            .Select(category => category.Id)
            .SingleAsync();
        var placeCategoryId = await database.Set<PlaceCategory>()
            .Where(category => category.Name == "Other")
            .Select(category => category.Id)
            .SingleAsync();

        var destination = Destination.Create(
            new DestinationValues(
                name,
                categoryId,
                country,
                entryRequirements,
                isSchengenArea,
                notes,
                visibility),
            new UserId(creatorId),
            SeedNow);
        database.Add(destination);
        await database.SaveChangesAsync();

        if (ratings is { Count: > 0 })
        {
            for (var index = 0; index < ratings.Count; index++)
            {
                database.Add(Place.Create(
                    destination.Id,
                    new PlaceValues($"Place {index}", placeCategoryId, ratings[index], null, null),
                    new UserId(creatorId),
                    SeedNow));
            }

            await database.SaveChangesAsync();
        }

        return destination.Id;
    }
}
