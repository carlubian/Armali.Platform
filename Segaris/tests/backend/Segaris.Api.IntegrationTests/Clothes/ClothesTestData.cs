using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Segaris.Api.Modules.Clothes.Domain;
using Segaris.Persistence;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.IntegrationTests.Clothes;

internal static class ClothesTestData
{
    private static readonly DateTimeOffset SeedNow = new(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);

    public static async Task<int> CategoryIdAsync(IServiceProvider services, string name)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        return await database.Set<ClothingCategory>().Where(category => category.Name == name).Select(category => category.Id).SingleAsync();
    }

    public static async Task<int> ColorIdAsync(IServiceProvider services, string name)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        return await database.Set<ClothingColor>().Where(color => color.Name == name).Select(color => color.Id).SingleAsync();
    }

    public static async Task<IReadOnlyList<int>> GarmentColorIdsAsync(IServiceProvider services, int garmentId)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        return await database.Set<ClothesGarmentColor>()
            .Where(color => color.GarmentId == garmentId)
            .OrderBy(color => color.ColorId)
            .Select(color => color.ColorId)
            .ToListAsync();
    }

    public static async Task<bool> GarmentExistsAsync(IServiceProvider services, int garmentId)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        return await database.Set<ClothesGarment>().AnyAsync(garment => garment.Id == garmentId);
    }

    public static async Task<int> SeedGarmentAsync(
        IServiceProvider services,
        int creatorId,
        string name = "Garment",
        string categoryName = "Other",
        ClothesGarmentStatus status = ClothesGarmentStatus.Active,
        string? size = null,
        IReadOnlyList<string>? colorNames = null,
        WashingCare? washingCare = null,
        DryingCare? dryingCare = null,
        IroningCare? ironingCare = null,
        DryCleaningCare? dryCleaningCare = null,
        string? notes = null,
        RecordVisibility visibility = RecordVisibility.Public)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();

        var categoryId = await database.Set<ClothingCategory>()
            .Where(category => category.Name == categoryName)
            .Select(category => category.Id)
            .SingleAsync();

        var names = colorNames ?? [];
        var colorIds = await database.Set<ClothingColor>()
            .Where(color => names.Contains(color.Name))
            .Select(color => color.Id)
            .ToListAsync();

        var garment = ClothesGarment.Create(
            new ClothesGarmentValues(
                name,
                categoryId,
                status,
                size,
                colorIds,
                washingCare,
                dryingCare,
                ironingCare,
                dryCleaningCare,
                notes,
                visibility),
            new UserId(creatorId),
            SeedNow);

        database.Add(garment);
        await database.SaveChangesAsync();
        return garment.Id;
    }
}
