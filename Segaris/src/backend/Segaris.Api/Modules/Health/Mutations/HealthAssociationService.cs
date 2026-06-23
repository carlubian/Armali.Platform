using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Health.Contracts;
using Segaris.Api.Modules.Health.Domain;
using Segaris.Api.Modules.Identity;
using Segaris.Persistence;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Health.Mutations;

/// <summary>
/// Coordinates the symmetric disease-to-medicine association. Reads are viewer
/// filtered, creation requires access to both endpoints, and delete is no-op safe.
/// </summary>
internal sealed class HealthAssociationService(SegarisDbContext database)
{
    public async Task<IReadOnlyList<MedicineSummaryResponse>?> ListMedicinesForDiseaseAsync(
        int diseaseId,
        UserId viewerId,
        CancellationToken cancellationToken)
    {
        var diseaseExists = await database.Set<Disease>()
            .AsNoTracking()
            .Where(HealthDiseasePolicies.AccessibleTo(viewerId))
            .AnyAsync(disease => disease.Id == diseaseId, cancellationToken);
        if (!diseaseExists)
        {
            return null;
        }

        return await database.Set<DiseaseMedicine>()
            .AsNoTracking()
            .Where(association => association.DiseaseId == diseaseId)
            .Join(
                database.Set<Medicine>().AsNoTracking().Where(HealthMedicinePolicies.AccessibleTo(viewerId)),
                association => association.MedicineId,
                medicine => medicine.Id,
                (_, medicine) => medicine)
            .OrderBy(medicine => medicine.Name)
            .ThenBy(medicine => medicine.Id)
            .Select(medicine => new MedicineSummaryResponse(
                medicine.Id,
                medicine.Name,
                medicine.CategoryId,
                database.Set<MedicineCategory>()
                    .Where(category => category.Id == medicine.CategoryId).Select(category => category.Name).First(),
                medicine.RequiresPrescription,
                null,
                null,
                medicine.Visibility.ToString(),
                new MedicineThumbnailResponse(null, null, "Placeholder"),
                medicine.CreatedBy,
                database.Set<SegarisUser>()
                    .Where(user => user.Id == medicine.CreatedBy).Select(user => user.DisplayName).First()))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DiseaseSummaryResponse>?> ListDiseasesForMedicineAsync(
        int medicineId,
        UserId viewerId,
        CancellationToken cancellationToken)
    {
        var medicineExists = await database.Set<Medicine>()
            .AsNoTracking()
            .Where(HealthMedicinePolicies.AccessibleTo(viewerId))
            .AnyAsync(medicine => medicine.Id == medicineId, cancellationToken);
        if (!medicineExists)
        {
            return null;
        }

        return await database.Set<DiseaseMedicine>()
            .AsNoTracking()
            .Where(association => association.MedicineId == medicineId)
            .Join(
                database.Set<Disease>().AsNoTracking().Where(HealthDiseasePolicies.AccessibleTo(viewerId)),
                association => association.DiseaseId,
                disease => disease.Id,
                (_, disease) => disease)
            .OrderBy(disease => disease.Name)
            .ThenBy(disease => disease.Id)
            .Select(disease => new DiseaseSummaryResponse(
                disease.Id,
                disease.Name,
                disease.CategoryId,
                database.Set<DiseaseCategory>()
                    .Where(category => category.Id == disease.CategoryId).Select(category => category.Name).First(),
                disease.Visibility.ToString(),
                database.Set<DiseaseMedicine>()
                    .Where(association => association.DiseaseId == disease.Id)
                    .Join(
                        database.Set<Medicine>().Where(HealthMedicinePolicies.AccessibleTo(viewerId)),
                        association => association.MedicineId,
                        medicine => medicine.Id,
                        (association, _) => association)
                    .Count(),
                disease.CreatedBy,
                database.Set<SegarisUser>()
                    .Where(user => user.Id == disease.CreatedBy).Select(user => user.DisplayName).First()))
            .ToArrayAsync(cancellationToken);
    }

    public async Task AddAsync(
        int diseaseId,
        int medicineId,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        var disease = await database.Set<Disease>()
            .AsNoTracking()
            .Where(disease => disease.Id == diseaseId)
            .Select(disease => new AssociationEndpoint(disease.Visibility, disease.CreatedBy))
            .FirstOrDefaultAsync(cancellationToken);
        var medicine = await database.Set<Medicine>()
            .AsNoTracking()
            .Where(medicine => medicine.Id == medicineId)
            .Select(medicine => new AssociationEndpoint(medicine.Visibility, medicine.CreatedBy))
            .FirstOrDefaultAsync(cancellationToken);

        ValidateCreationEndpoint(disease, actorId, "disease");
        ValidateCreationEndpoint(medicine, actorId, "medicine");

        var exists = await database.Set<DiseaseMedicine>()
            .AnyAsync(
                association => association.DiseaseId == diseaseId && association.MedicineId == medicineId,
                cancellationToken);
        if (exists)
        {
            return;
        }

        database.Add(new DiseaseMedicine
        {
            DiseaseId = diseaseId,
            MedicineId = medicineId,
        });
        await database.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(
        int diseaseId,
        int medicineId,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        var canAccessDisease = await database.Set<Disease>()
            .AsNoTracking()
            .Where(HealthDiseasePolicies.AccessibleTo(actorId))
            .AnyAsync(disease => disease.Id == diseaseId, cancellationToken);
        var canAccessMedicine = await database.Set<Medicine>()
            .AsNoTracking()
            .Where(HealthMedicinePolicies.AccessibleTo(actorId))
            .AnyAsync(medicine => medicine.Id == medicineId, cancellationToken);
        if (!canAccessDisease || !canAccessMedicine)
        {
            return;
        }

        await database.Set<DiseaseMedicine>()
            .Where(association => association.DiseaseId == diseaseId && association.MedicineId == medicineId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private static void ValidateCreationEndpoint(AssociationEndpoint? endpoint, UserId actorId, string endpointName)
    {
        if (endpoint is null || !endpoint.IsAccessibleTo(actorId))
        {
            throw new HealthValidationException(
                $"The associated {endpointName} is not accessible.",
                HealthValidationReason.AssociationNotAccessible);
        }

        if (endpoint.Visibility == RecordVisibility.Private && endpoint.CreatedBy != actorId.Value)
        {
            throw new HealthValidationException(
                $"The associated {endpointName} visibility does not allow this association.",
                HealthValidationReason.AssociationVisibilityForbidden);
        }
    }

    private sealed record AssociationEndpoint(RecordVisibility Visibility, int CreatedBy)
    {
        public bool IsAccessibleTo(UserId viewerId) =>
            Visibility == RecordVisibility.Public || CreatedBy == viewerId.Value;
    }
}
