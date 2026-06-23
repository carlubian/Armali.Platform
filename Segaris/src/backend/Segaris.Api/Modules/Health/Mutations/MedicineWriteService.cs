using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Health.Contracts;
using Segaris.Api.Modules.Health.Domain;
using Segaris.Persistence;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Health.Mutations;

/// <summary>
/// Write-side operations on Health medicines. Inaccessible medicines are reported as
/// not found so private records are never disclosed.
/// </summary>
internal sealed class MedicineWriteService(SegarisDbContext database, IClock clock)
{
    public async Task<int> CreateAsync(
        CreateMedicineRequest request,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var values = Map(
            request.Name,
            request.CategoryId,
            request.Posology,
            request.RequiresPrescription,
            request.Notes,
            request.Visibility);

        var medicine = Medicine.Create(values, actorId, clock.UtcNow);
        await ValidateCategoryAsync(values.CategoryId, cancellationToken);

        database.Add(medicine);
        await database.SaveChangesAsync(cancellationToken);
        return medicine.Id;
    }

    public async Task<bool> UpdateAsync(
        int medicineId,
        UpdateMedicineRequest request,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var medicine = await database.Set<Medicine>()
            .Where(HealthMedicinePolicies.MutableBy(actorId))
            .Where(candidate => candidate.Id == medicineId)
            .FirstOrDefaultAsync(cancellationToken);
        if (medicine is null)
        {
            return false;
        }

        var values = Map(
            request.Name,
            request.CategoryId,
            request.Posology,
            request.RequiresPrescription,
            request.Notes,
            request.Visibility);

        ValidateVisibilityChange(medicine, values.Visibility, actorId);
        medicine.Update(values, actorId, clock.UtcNow);
        await ValidateCategoryAsync(values.CategoryId, cancellationToken);

        await database.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(
        int medicineId,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        var medicine = await database.Set<Medicine>()
            .Where(HealthMedicinePolicies.MutableBy(actorId))
            .Where(candidate => candidate.Id == medicineId)
            .FirstOrDefaultAsync(cancellationToken);
        if (medicine is null)
        {
            return false;
        }

        database.Remove(medicine);
        await database.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task ValidateCategoryAsync(
        int categoryId,
        CancellationToken cancellationToken)
    {
        var categoryExists = await database.Set<MedicineCategory>()
            .AnyAsync(category => category.Id == categoryId, cancellationToken);

        if (!categoryExists)
        {
            throw new HealthValidationException(
                "The medicine category does not exist.",
                HealthValidationReason.CatalogReference);
        }
    }

    private static void ValidateVisibilityChange(
        Medicine medicine,
        RecordVisibility requestedVisibility,
        UserId actorId)
    {
        if (requestedVisibility != medicine.Visibility
            && !HealthMedicinePolicies.CanChangeVisibility(medicine, actorId))
        {
            throw new HealthValidationException(
                "Only the creator may change medicine visibility.",
                HealthValidationReason.VisibilityForbidden);
        }
    }

    private static MedicineValues Map(
        string? name,
        int categoryId,
        string? posology,
        bool? requiresPrescription,
        string? notes,
        string? visibility) => new(
            name ?? string.Empty,
            categoryId,
            posology,
            requiresPrescription ?? HealthDefaults.RequiresPrescription,
            null,
            notes,
            ParseEnum(visibility, HealthDefaults.Visibility, "visibility"));

    private static TEnum ParseEnum<TEnum>(string? value, TEnum defaultValue, string field)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        if (Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed)
            && Enum.IsDefined(parsed))
        {
            return parsed;
        }

        throw new HealthValidationException($"The {field} is not a recognized value.");
    }
}
