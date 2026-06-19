using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Segaris.Api.Modules.Assets.Domain;
using Segaris.Persistence;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.IntegrationTests.Assets;

/// <summary>
/// Seeds assets directly through the database for the read-API and privacy tests,
/// independently of the mutation endpoints. Category and location references are
/// resolved by their seeded display name.
/// </summary>
internal static class AssetsTestData
{
    private static readonly DateTimeOffset SeedNow = new(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);

    public static async Task<int> CategoryIdAsync(IServiceProvider services, string name)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        return await database.Set<AssetCategory>().Where(category => category.Name == name).Select(category => category.Id).SingleAsync();
    }

    public static async Task<int> LocationIdAsync(IServiceProvider services, string name)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        return await database.Set<AssetLocation>().Where(location => location.Name == name).Select(location => location.Id).SingleAsync();
    }

    public static async Task<int> SeedAssetAsync(
        IServiceProvider services,
        int creatorId,
        string name = "Asset",
        string categoryName = "Other",
        string locationName = "Other",
        AssetStatus status = AssetStatus.Active,
        string? code = null,
        string? brandModel = null,
        string? serialNumber = null,
        DateOnly? acquisitionDate = null,
        DateOnly? expectedEndOfLifeDate = null,
        string? notes = null,
        RecordVisibility visibility = RecordVisibility.Public)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();

        var categoryId = await database.Set<AssetCategory>()
            .Where(category => category.Name == categoryName)
            .Select(category => category.Id)
            .SingleAsync();
        var locationId = await database.Set<AssetLocation>()
            .Where(location => location.Name == locationName)
            .Select(location => location.Id)
            .SingleAsync();

        var values = new AssetValues(
            name,
            categoryId,
            locationId,
            status,
            code,
            brandModel,
            serialNumber,
            acquisitionDate,
            expectedEndOfLifeDate,
            notes,
            visibility);

        var asset = Asset.Create(values, new UserId(creatorId), SeedNow);
        database.Add(asset);
        await database.SaveChangesAsync();
        return asset.Id;
    }
}
