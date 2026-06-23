using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Segaris.Api.Modules.Health.Domain;
using Segaris.Persistence;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.IntegrationTests.Health;

internal static class HealthTestData
{
    private static readonly DateTimeOffset SeedNow = new(2026, 6, 23, 9, 0, 0, TimeSpan.Zero);

    public static async Task<int> DiseaseCategoryIdAsync(IServiceProvider services, string name)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        return await database.Set<DiseaseCategory>()
            .Where(category => category.Name == name)
            .Select(category => category.Id)
            .SingleAsync();
    }

    public static async Task<bool> DiseaseExistsAsync(IServiceProvider services, int diseaseId)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        return await database.Set<Disease>().AnyAsync(disease => disease.Id == diseaseId);
    }

    public static async Task<int> SeedDiseaseAsync(
        IServiceProvider services,
        int creatorId,
        string name = "Disease",
        string categoryName = "Other",
        string? symptoms = null,
        int? averageDurationDays = null,
        string? notes = null,
        RecordVisibility visibility = RecordVisibility.Public)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();

        var categoryId = await database.Set<DiseaseCategory>()
            .Where(category => category.Name == categoryName)
            .Select(category => category.Id)
            .SingleAsync();

        var disease = Disease.Create(
            new DiseaseValues(
                name,
                categoryId,
                symptoms,
                averageDurationDays,
                notes,
                visibility),
            new UserId(creatorId),
            SeedNow);

        database.Add(disease);
        await database.SaveChangesAsync();
        return disease.Id;
    }
}
