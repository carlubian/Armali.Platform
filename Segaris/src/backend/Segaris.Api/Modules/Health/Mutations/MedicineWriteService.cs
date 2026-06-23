using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Health.Contracts;
using Segaris.Api.Modules.Health.Domain;
using Segaris.Api.Modules.Inventory.Contracts;
using Segaris.Persistence;
using Segaris.Shared.Attachments;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Health.Mutations;

/// <summary>
/// Write-side operations on Health medicines. Inaccessible medicines are reported as
/// not found so private records are never disclosed.
/// </summary>
internal sealed class MedicineWriteService(
    SegarisDbContext database,
    IInventoryItemReferenceReader itemReferences,
    IAttachmentService attachments,
    IClock clock)
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
            request.InventoryItemId,
            request.Notes,
            request.Visibility);

        var medicine = Medicine.Create(values, actorId, clock.UtcNow);
        await ValidateReferencesAsync(values, actorId, cancellationToken);

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
            request.InventoryItemId,
            request.Notes,
            request.Visibility);

        ValidateVisibilityChange(medicine, values.Visibility, actorId);
        await ValidatePublishAsync(medicine, values.Visibility, cancellationToken);
        medicine.Update(values, actorId, clock.UtcNow);
        await ValidateReferencesAsync(values, actorId, cancellationToken);

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

        var owner = HealthAttachments.MedicineOwner(medicineId);
        var descriptors = await attachments.ListByOwnerAsync(owner, cancellationToken);
        foreach (var descriptor in descriptors)
        {
            await attachments.DeleteAsync(descriptor.Id, owner, cancellationToken);
        }

        return true;
    }

    /// <summary>
    /// Marks one image attachment as the medicine's primary image. Inaccessible
    /// medicines are reported as not found so private records are never disclosed; a
    /// missing or non-image attachment is rejected without mutating the medicine.
    /// </summary>
    public async Task<MedicineSetPrimaryResult> SetPrimaryAttachmentAsync(
        int medicineId,
        int attachmentId,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        var medicine = await database.Set<Medicine>()
            .Where(HealthMedicinePolicies.MutableBy(actorId))
            .Where(candidate => candidate.Id == medicineId)
            .FirstOrDefaultAsync(cancellationToken);
        if (medicine is null)
        {
            return new(MedicineSetPrimaryOutcome.MedicineNotFound, null);
        }

        var owner = HealthAttachments.MedicineOwner(medicineId);
        var descriptor = await attachments.FindAsync(new(attachmentId), owner, cancellationToken);
        if (descriptor is null)
        {
            return new(MedicineSetPrimaryOutcome.AttachmentNotFound, null);
        }

        if (!HealthAttachments.IsImageContentType(descriptor.ContentType))
        {
            return new(MedicineSetPrimaryOutcome.NotImage, null);
        }

        medicine.SetPrimaryAttachment(attachmentId, actorId, clock.UtcNow);
        await database.SaveChangesAsync(cancellationToken);
        return new(MedicineSetPrimaryOutcome.Assigned, descriptor);
    }

    /// <summary>
    /// Removes one attachment from a medicine, clearing the primary-image reference when
    /// the removed attachment was the primary. Inaccessible medicines are reported as not
    /// found so private records are never disclosed.
    /// </summary>
    public async Task<MedicineDeleteAttachmentOutcome> DeleteAttachmentAsync(
        int medicineId,
        int attachmentId,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        var medicine = await database.Set<Medicine>()
            .Where(HealthMedicinePolicies.MutableBy(actorId))
            .Where(candidate => candidate.Id == medicineId)
            .FirstOrDefaultAsync(cancellationToken);
        if (medicine is null)
        {
            return MedicineDeleteAttachmentOutcome.MedicineNotFound;
        }

        var owner = HealthAttachments.MedicineOwner(medicineId);
        var removed = await attachments.DeleteAsync(new(attachmentId), owner, cancellationToken);
        if (!removed)
        {
            return MedicineDeleteAttachmentOutcome.AttachmentNotFound;
        }

        if (medicine.PrimaryAttachmentId == attachmentId)
        {
            medicine.ClearPrimaryAttachment(actorId, clock.UtcNow);
            await database.SaveChangesAsync(cancellationToken);
        }

        return MedicineDeleteAttachmentOutcome.Deleted;
    }

    private async Task ValidateReferencesAsync(
        MedicineValues values,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        var categoryExists = await database.Set<MedicineCategory>()
            .AnyAsync(category => category.Id == values.CategoryId, cancellationToken);

        if (!categoryExists)
        {
            throw new HealthValidationException(
                "The medicine category does not exist.",
                HealthValidationReason.CatalogReference);
        }

        if (values.InventoryItemId is not { } itemId)
        {
            return;
        }

        var item = await itemReferences.FindAccessibleAsync(itemId, actorId, cancellationToken);
        if (item is null)
        {
            throw new HealthValidationException(
                "The referenced Inventory item was not found.",
                HealthValidationReason.ItemNotAccessible);
        }

        if (values.Visibility == RecordVisibility.Public && item.Visibility != RecordVisibility.Public)
        {
            throw new HealthValidationException(
                "A public medicine may reference only public Inventory items.",
                HealthValidationReason.ItemVisibilityForbidden);
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

    private async Task ValidatePublishAsync(
        Medicine medicine,
        RecordVisibility requestedVisibility,
        CancellationToken cancellationToken)
    {
        if (medicine.Visibility != RecordVisibility.Private || requestedVisibility != RecordVisibility.Public)
        {
            return;
        }

        var blockingCount = await database.Set<DiseaseMedicine>()
            .Where(association => association.MedicineId == medicine.Id)
            .Join(
                database.Set<Disease>().Where(disease => disease.Visibility != RecordVisibility.Public),
                association => association.DiseaseId,
                disease => disease.Id,
                (association, _) => association)
            .CountAsync(cancellationToken);
        if (blockingCount > 0)
        {
            throw new HealthValidationException(
                $"The medicine cannot be published while {blockingCount} associated disease record(s) are not public.",
                HealthValidationReason.AssociationPublishBlocked);
        }
    }

    private static MedicineValues Map(
        string? name,
        int categoryId,
        string? posology,
        bool? requiresPrescription,
        int? inventoryItemId,
        string? notes,
        string? visibility) => new(
            name ?? string.Empty,
            categoryId,
            posology,
            requiresPrescription ?? HealthDefaults.RequiresPrescription,
            inventoryItemId,
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

/// <summary>Outcome of a primary-image assignment.</summary>
internal enum MedicineSetPrimaryOutcome
{
    MedicineNotFound,
    AttachmentNotFound,
    NotImage,
    Assigned,
}

/// <summary>
/// Result of <see cref="MedicineWriteService.SetPrimaryAttachmentAsync"/>, carrying the
/// assigned attachment descriptor when the assignment succeeded.
/// </summary>
internal sealed record MedicineSetPrimaryResult(
    MedicineSetPrimaryOutcome Outcome,
    AttachmentDescriptor? Descriptor);

/// <summary>Outcome of an attachment deletion.</summary>
internal enum MedicineDeleteAttachmentOutcome
{
    MedicineNotFound,
    AttachmentNotFound,
    Deleted,
}
