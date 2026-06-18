using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Clothes.Contracts;
using Segaris.Api.Modules.Clothes.Domain;
using Segaris.Persistence;
using Segaris.Shared.Attachments;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Clothes.Mutations;

/// <summary>
/// Write-side operations on Clothes garments. Authorization follows garment
/// visibility rules: inaccessible garments are reported as not found so private
/// records are never disclosed.
/// </summary>
internal sealed class ClothesGarmentWriteService(
    SegarisDbContext database,
    IAttachmentService attachments,
    IClock clock)
{
    public async Task<int> CreateAsync(
        CreateClothesGarmentRequest request,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var values = Map(
            request.Name,
            request.CategoryId,
            request.Status,
            request.Size,
            request.ColorIds,
            request.WashingCare,
            request.DryingCare,
            request.IroningCare,
            request.DryCleaningCare,
            request.Notes,
            request.Visibility);

        var garment = ClothesGarment.Create(values, actorId, clock.UtcNow);
        await ValidateReferencesAsync(values, cancellationToken);

        database.Add(garment);
        await database.SaveChangesAsync(cancellationToken);
        return garment.Id;
    }

    public async Task<bool> UpdateAsync(
        int garmentId,
        UpdateClothesGarmentRequest request,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var garment = await database.Set<ClothesGarment>()
            .Where(ClothesGarmentPolicies.MutableBy(actorId))
            .Where(candidate => candidate.Id == garmentId)
            .Include(candidate => candidate.Colors)
            .FirstOrDefaultAsync(cancellationToken);
        if (garment is null)
        {
            return false;
        }

        var values = Map(
            request.Name,
            request.CategoryId,
            request.Status,
            request.Size,
            request.ColorIds,
            request.WashingCare,
            request.DryingCare,
            request.IroningCare,
            request.DryCleaningCare,
            request.Notes,
            request.Visibility);

        ValidateVisibilityChange(garment, values.Visibility, actorId);
        garment.Update(values, actorId, clock.UtcNow);
        await ValidateReferencesAsync(values, cancellationToken);

        await database.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(
        int garmentId,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        var garment = await database.Set<ClothesGarment>()
            .Where(ClothesGarmentPolicies.MutableBy(actorId))
            .Where(candidate => candidate.Id == garmentId)
            .FirstOrDefaultAsync(cancellationToken);
        if (garment is null)
        {
            return false;
        }

        database.Remove(garment);
        await database.SaveChangesAsync(cancellationToken);

        var owner = ClothesAttachments.GarmentOwner(garmentId);
        var descriptors = await attachments.ListByOwnerAsync(owner, cancellationToken);
        foreach (var descriptor in descriptors)
        {
            await attachments.DeleteAsync(descriptor.Id, owner, cancellationToken);
        }

        return true;
    }

    /// <summary>
    /// Marks one image attachment as the garment's primary image. Inaccessible
    /// garments are reported as not found so private records are never disclosed; a
    /// missing or non-image attachment is rejected without mutating the garment.
    /// </summary>
    public async Task<ClothesSetPrimaryResult> SetPrimaryAttachmentAsync(
        int garmentId,
        int attachmentId,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        var garment = await database.Set<ClothesGarment>()
            .Where(ClothesGarmentPolicies.MutableBy(actorId))
            .Where(candidate => candidate.Id == garmentId)
            .FirstOrDefaultAsync(cancellationToken);
        if (garment is null)
        {
            return new(ClothesSetPrimaryOutcome.GarmentNotFound, null);
        }

        var owner = ClothesAttachments.GarmentOwner(garmentId);
        var descriptor = await attachments.FindAsync(new(attachmentId), owner, cancellationToken);
        if (descriptor is null)
        {
            return new(ClothesSetPrimaryOutcome.AttachmentNotFound, null);
        }

        if (!ClothesAttachments.IsImageContentType(descriptor.ContentType))
        {
            return new(ClothesSetPrimaryOutcome.NotImage, null);
        }

        garment.SetPrimaryAttachment(attachmentId, actorId, clock.UtcNow);
        await database.SaveChangesAsync(cancellationToken);
        return new(ClothesSetPrimaryOutcome.Assigned, descriptor);
    }

    /// <summary>
    /// Removes one attachment from a garment, clearing the primary-image reference when
    /// the removed attachment was the primary. Inaccessible garments are reported as not
    /// found so private records are never disclosed.
    /// </summary>
    public async Task<ClothesDeleteAttachmentOutcome> DeleteAttachmentAsync(
        int garmentId,
        int attachmentId,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        var garment = await database.Set<ClothesGarment>()
            .Where(ClothesGarmentPolicies.MutableBy(actorId))
            .Where(candidate => candidate.Id == garmentId)
            .FirstOrDefaultAsync(cancellationToken);
        if (garment is null)
        {
            return ClothesDeleteAttachmentOutcome.GarmentNotFound;
        }

        var owner = ClothesAttachments.GarmentOwner(garmentId);
        var removed = await attachments.DeleteAsync(new(attachmentId), owner, cancellationToken);
        if (!removed)
        {
            return ClothesDeleteAttachmentOutcome.AttachmentNotFound;
        }

        if (garment.PrimaryAttachmentId == attachmentId)
        {
            garment.ClearPrimaryAttachment(actorId, clock.UtcNow);
            await database.SaveChangesAsync(cancellationToken);
        }

        return ClothesDeleteAttachmentOutcome.Deleted;
    }

    private async Task ValidateReferencesAsync(
        ClothesGarmentValues values,
        CancellationToken cancellationToken)
    {
        var categoryExists = await database.Set<ClothingCategory>()
            .AnyAsync(category => category.Id == values.CategoryId, cancellationToken);
        var distinctColorIds = values.ColorIds.Distinct().ToArray();
        var colorCount = distinctColorIds.Length == 0
            ? 0
            : await database.Set<ClothingColor>()
                .CountAsync(color => distinctColorIds.Contains(color.Id), cancellationToken);

        if (!categoryExists || colorCount != distinctColorIds.Length)
        {
            throw new ClothesValidationException(
                "One or more Clothes catalog references do not exist.",
                ClothesValidationReason.CatalogReference);
        }
    }

    private static void ValidateVisibilityChange(
        ClothesGarment garment,
        RecordVisibility requestedVisibility,
        UserId actorId)
    {
        if (requestedVisibility != garment.Visibility
            && !ClothesGarmentPolicies.CanChangeVisibility(garment, actorId))
        {
            throw new ClothesValidationException(
                "Only the creator may change garment visibility.",
                ClothesValidationReason.VisibilityForbidden);
        }
    }

    private static ClothesGarmentValues Map(
        string? name,
        int categoryId,
        string? status,
        string? size,
        IReadOnlyList<int>? colorIds,
        string? washingCare,
        string? dryingCare,
        string? ironingCare,
        string? dryCleaningCare,
        string? notes,
        string? visibility) => new(
            name ?? string.Empty,
            categoryId,
            ParseEnum(status, ClothesDefaults.GarmentStatus, "status"),
            size,
            colorIds ?? [],
            ParseNullableEnum<WashingCare>(washingCare, "washingCare"),
            ParseNullableEnum<DryingCare>(dryingCare, "dryingCare"),
            ParseNullableEnum<IroningCare>(ironingCare, "ironingCare"),
            ParseNullableEnum<DryCleaningCare>(dryCleaningCare, "dryCleaningCare"),
            notes,
            ParseEnum(visibility, ClothesDefaults.Visibility, "visibility"));

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

        throw new ClothesValidationException($"The {field} is not a recognized value.");
    }

    private static TEnum? ParseNullableEnum<TEnum>(string? value, string field)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed)
            && Enum.IsDefined(parsed))
        {
            return parsed;
        }

        throw new ClothesValidationException($"The {field} is not a recognized value.");
    }
}

/// <summary>Outcome of a primary-image assignment.</summary>
internal enum ClothesSetPrimaryOutcome
{
    GarmentNotFound,
    AttachmentNotFound,
    NotImage,
    Assigned,
}

/// <summary>
/// Result of <see cref="ClothesGarmentWriteService.SetPrimaryAttachmentAsync"/>, carrying
/// the assigned attachment descriptor when the assignment succeeded.
/// </summary>
internal sealed record ClothesSetPrimaryResult(
    ClothesSetPrimaryOutcome Outcome,
    AttachmentDescriptor? Descriptor);

/// <summary>Outcome of an attachment deletion.</summary>
internal enum ClothesDeleteAttachmentOutcome
{
    GarmentNotFound,
    AttachmentNotFound,
    Deleted,
}
