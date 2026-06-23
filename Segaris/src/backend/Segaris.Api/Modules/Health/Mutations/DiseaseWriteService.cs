using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Health.Contracts;
using Segaris.Api.Modules.Health.Domain;
using Segaris.Persistence;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Health.Mutations;

/// <summary>
/// Write-side operations on Health diseases. Inaccessible diseases are reported as
/// not found so private records are never disclosed.
/// </summary>
internal sealed class DiseaseWriteService(SegarisDbContext database, IClock clock)
{
    public async Task<int> CreateAsync(
        CreateDiseaseRequest request,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var values = Map(
            request.Name,
            request.CategoryId,
            request.Symptoms,
            request.AverageDurationDays,
            request.Notes,
            request.Visibility);

        var disease = Disease.Create(values, actorId, clock.UtcNow);
        await ValidateCategoryAsync(values.CategoryId, cancellationToken);

        database.Add(disease);
        await database.SaveChangesAsync(cancellationToken);
        return disease.Id;
    }

    public async Task<bool> UpdateAsync(
        int diseaseId,
        UpdateDiseaseRequest request,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var disease = await database.Set<Disease>()
            .Where(HealthDiseasePolicies.MutableBy(actorId))
            .Where(candidate => candidate.Id == diseaseId)
            .FirstOrDefaultAsync(cancellationToken);
        if (disease is null)
        {
            return false;
        }

        var values = Map(
            request.Name,
            request.CategoryId,
            request.Symptoms,
            request.AverageDurationDays,
            request.Notes,
            request.Visibility);

        ValidateVisibilityChange(disease, values.Visibility, actorId);
        disease.Update(values, actorId, clock.UtcNow);
        await ValidateCategoryAsync(values.CategoryId, cancellationToken);

        await database.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(
        int diseaseId,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        var disease = await database.Set<Disease>()
            .Where(HealthDiseasePolicies.MutableBy(actorId))
            .Where(candidate => candidate.Id == diseaseId)
            .FirstOrDefaultAsync(cancellationToken);
        if (disease is null)
        {
            return false;
        }

        database.Remove(disease);
        await database.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task ValidateCategoryAsync(
        int categoryId,
        CancellationToken cancellationToken)
    {
        var categoryExists = await database.Set<DiseaseCategory>()
            .AnyAsync(category => category.Id == categoryId, cancellationToken);

        if (!categoryExists)
        {
            throw new HealthValidationException(
                "The disease category does not exist.",
                HealthValidationReason.CatalogReference);
        }
    }

    private static void ValidateVisibilityChange(
        Disease disease,
        RecordVisibility requestedVisibility,
        UserId actorId)
    {
        if (requestedVisibility != disease.Visibility
            && !HealthDiseasePolicies.CanChangeVisibility(disease, actorId))
        {
            throw new HealthValidationException(
                "Only the creator may change disease visibility.",
                HealthValidationReason.VisibilityForbidden);
        }
    }

    private static DiseaseValues Map(
        string? name,
        int categoryId,
        string? symptoms,
        int? averageDurationDays,
        string? notes,
        string? visibility) => new(
            name ?? string.Empty,
            categoryId,
            symptoms,
            averageDurationDays,
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
