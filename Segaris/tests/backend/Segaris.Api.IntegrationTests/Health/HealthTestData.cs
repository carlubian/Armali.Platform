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

    public static async Task<int> MedicineCategoryIdAsync(IServiceProvider services, string name)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        return await database.Set<MedicineCategory>()
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

    public static async Task<bool> MedicineExistsAsync(IServiceProvider services, int medicineId)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        return await database.Set<Medicine>().AnyAsync(medicine => medicine.Id == medicineId);
    }

    public static async Task<bool> AssociationExistsAsync(IServiceProvider services, int diseaseId, int medicineId)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        return await database.Set<DiseaseMedicine>()
            .AnyAsync(association => association.DiseaseId == diseaseId && association.MedicineId == medicineId);
    }

    public static async Task<int> AssociationCountAsync(IServiceProvider services, int diseaseId, int medicineId)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        return await database.Set<DiseaseMedicine>()
            .CountAsync(association => association.DiseaseId == diseaseId && association.MedicineId == medicineId);
    }

    public static async Task SeedAssociationAsync(IServiceProvider services, int diseaseId, int medicineId)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();
        var exists = await database.Set<DiseaseMedicine>()
            .AnyAsync(association => association.DiseaseId == diseaseId && association.MedicineId == medicineId);
        if (exists)
        {
            return;
        }

        database.Add(new DiseaseMedicine
        {
            DiseaseId = diseaseId,
            MedicineId = medicineId,
        });
        await database.SaveChangesAsync();
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

    public static async Task<int> SeedMedicineAsync(
        IServiceProvider services,
        int creatorId,
        string name = "Medicine",
        string categoryName = "Other",
        string? posology = null,
        bool requiresPrescription = false,
        string? notes = null,
        RecordVisibility visibility = RecordVisibility.Public)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<SegarisDbContext>();

        var categoryId = await database.Set<MedicineCategory>()
            .Where(category => category.Name == categoryName)
            .Select(category => category.Id)
            .SingleAsync();

        var medicine = Medicine.Create(
            new MedicineValues(
                name,
                categoryId,
                posology,
                requiresPrescription,
                null,
                notes,
                visibility),
            new UserId(creatorId),
            SeedNow);

        database.Add(medicine);
        await database.SaveChangesAsync();
        return medicine.Id;
    }
}
