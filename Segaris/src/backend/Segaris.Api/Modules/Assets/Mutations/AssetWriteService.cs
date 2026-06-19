using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Assets.Contracts;
using Segaris.Api.Modules.Assets.Domain;
using Segaris.Persistence;
using Segaris.Shared.Attachments;
using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;
using Segaris.Shared.Time;

namespace Segaris.Api.Modules.Assets.Mutations;

/// <summary>
/// Write-side operations for assets: create, full replacement update, and physical
/// deletion. Each mutation runs in a single <c>SaveChangesAsync</c> transaction and
/// validates the required category and location references and global case-insensitive
/// code uniqueness before the row is persisted. Authorization mirrors the read side:
/// a public asset is mutable by any user (collaboration) while a private asset is
/// mutable only by its creator, and only the creator may change visibility (enforced
/// in the domain). Deletion is unconditional for accessible assets and reconciles the
/// asset's attachments through the platform service afterwards.
/// </summary>
internal sealed class AssetWriteService(
    SegarisDbContext database,
    AssetCatalogValidator catalogValidator,
    IAttachmentService attachments,
    IEnumerable<IAssetDeletionReferenceHandler> deletionReferenceHandlers,
    IClock clock)
{
    public async Task<int> CreateAsync(
        CreateAssetRequest request,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var values = Map(
            request.Name,
            request.CategoryId,
            request.LocationId,
            request.Status,
            request.Code,
            request.BrandModel,
            request.SerialNumber,
            request.AcquisitionDate,
            request.ExpectedEndOfLifeDate,
            request.Notes,
            request.Visibility);

        // Shape, enum, and code validation happen in the domain factory; catalog
        // references and code uniqueness are checked before the row is saved.
        var asset = Asset.Create(values, actorId, clock.UtcNow);
        await catalogValidator.ValidateAsync(values, cancellationToken);
        await EnsureUniqueCodeAsync(asset.NormalizedCode, excludeId: null, cancellationToken);

        database.Add(asset);
        await SaveAsync(cancellationToken);
        return asset.Id;
    }

    public async Task<bool> UpdateAsync(
        int assetId,
        UpdateAssetRequest request,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var asset = await database.Set<Asset>()
            .Where(AssetPolicies.MutableBy(actorId))
            .Where(candidate => candidate.Id == assetId)
            .FirstOrDefaultAsync(cancellationToken);
        if (asset is null)
        {
            return false;
        }

        var values = Map(
            request.Name,
            request.CategoryId,
            request.LocationId,
            request.Status,
            request.Code,
            request.BrandModel,
            request.SerialNumber,
            request.AcquisitionDate,
            request.ExpectedEndOfLifeDate,
            request.Notes,
            request.Visibility);

        // The domain applies shape validation and the creator-only visibility policy;
        // catalog references and code uniqueness are validated before the single
        // transactional save (last write wins, no concurrency token).
        asset.Update(values, actorId, clock.UtcNow);
        await catalogValidator.ValidateAsync(values, cancellationToken);
        await EnsureUniqueCodeAsync(asset.NormalizedCode, excludeId: assetId, cancellationToken);

        await SaveAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(
        int assetId,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        var asset = await database.Set<Asset>()
            .Where(AssetPolicies.MutableBy(actorId))
            .Where(candidate => candidate.Id == assetId)
            .FirstOrDefaultAsync(cancellationToken);
        if (asset is null)
        {
            return false;
        }

        if (await CountReferencesAsync(assetId, cancellationToken) > 0)
        {
            throw new AssetReassignmentBlockedException(
                AssetsErrorCodes.AssetDeletionReferenced,
                "The asset is referenced and must be reassigned before deletion.");
        }

        database.Remove(asset);
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        // Compensating storage cleanup runs after the asset row is gone. Files are
        // outside the database transaction, so any residue is reconciled later rather
        // than resurrecting the deleted asset.
        await DeleteAttachmentFilesAsync(assetId, cancellationToken);

        return true;
    }

    public async Task<AssetDeletionImpactResponse?> GetDeletionImpactAsync(
        int assetId,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        var asset = await database.Set<Asset>()
            .AsNoTracking()
            .Where(AssetPolicies.MutableBy(actorId))
            .Where(candidate => candidate.Id == assetId)
            .FirstOrDefaultAsync(cancellationToken);
        if (asset is null)
        {
            return null;
        }

        var references = await CountReferencesAsync(assetId, cancellationToken);
        var replacementCandidates = await database.Set<Asset>()
            .AsNoTracking()
            .Where(AssetPolicies.AccessibleTo(actorId))
            .AnyAsync(candidate => candidate.Id != assetId, cancellationToken);
        return new(
            references > 0,
            references,
            references == 0,
            references > 0,
            replacementCandidates);
    }

    public async Task<AssetDeletionOutcome> ReassignAndDeleteAsync(
        int assetId,
        AssetReassignmentDeletionRequest request,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.TargetAssetId is not { } targetAssetId || targetAssetId <= 0 || targetAssetId == assetId)
        {
            return AssetDeletionOutcome.InvalidReassignment;
        }

        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        var source = await database.Set<Asset>()
            .Where(AssetPolicies.MutableBy(actorId))
            .Where(candidate => candidate.Id == assetId)
            .FirstOrDefaultAsync(cancellationToken);
        if (source is null)
        {
            return AssetDeletionOutcome.AssetNotFound;
        }

        if (!await database.Set<Asset>()
            .AsNoTracking()
            .Where(AssetPolicies.AccessibleTo(actorId))
            .AnyAsync(candidate => candidate.Id == targetAssetId, cancellationToken))
        {
            return AssetDeletionOutcome.InvalidReassignment;
        }

        var occurredAt = clock.UtcNow;
        var reassignment = new AssetDeletionReassignment(assetId, targetAssetId, actorId, occurredAt);
        foreach (var handler in deletionReferenceHandlers)
        {
            await handler.ReassignReferencesAsync(reassignment, cancellationToken);
        }

        database.Remove(source);
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await DeleteAttachmentFilesAsync(assetId, cancellationToken);
        return AssetDeletionOutcome.Deleted;
    }

    /// <summary>
    /// Marks one image attachment as the asset primary image. Inaccessible assets are
    /// reported as not found so private records are never disclosed.
    /// </summary>
    public async Task<AssetSetPrimaryResult> SetPrimaryAttachmentAsync(
        int assetId,
        int attachmentId,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        var asset = await database.Set<Asset>()
            .Where(AssetPolicies.MutableBy(actorId))
            .Where(candidate => candidate.Id == assetId)
            .FirstOrDefaultAsync(cancellationToken);
        if (asset is null)
        {
            return new(AssetSetPrimaryOutcome.AssetNotFound, null);
        }

        var owner = AssetsAttachments.AssetOwner(assetId);
        var descriptor = await attachments.FindAsync(new(attachmentId), owner, cancellationToken);
        if (descriptor is null)
        {
            return new(AssetSetPrimaryOutcome.AttachmentNotFound, null);
        }

        if (!AssetsAttachments.IsImageContentType(descriptor.ContentType))
        {
            return new(AssetSetPrimaryOutcome.NotImage, null);
        }

        asset.SetPrimaryAttachment(attachmentId, actorId, clock.UtcNow);
        await database.SaveChangesAsync(cancellationToken);
        return new(AssetSetPrimaryOutcome.Assigned, descriptor);
    }

    /// <summary>
    /// Removes one attachment and clears the primary-image reference when that
    /// attachment was selected.
    /// </summary>
    public async Task<AssetDeleteAttachmentOutcome> DeleteAttachmentAsync(
        int assetId,
        int attachmentId,
        UserId actorId,
        CancellationToken cancellationToken)
    {
        var asset = await database.Set<Asset>()
            .Where(AssetPolicies.MutableBy(actorId))
            .Where(candidate => candidate.Id == assetId)
            .FirstOrDefaultAsync(cancellationToken);
        if (asset is null)
        {
            return AssetDeleteAttachmentOutcome.AssetNotFound;
        }

        var owner = AssetsAttachments.AssetOwner(assetId);
        var removed = await attachments.DeleteAsync(new(attachmentId), owner, cancellationToken);
        if (!removed)
        {
            return AssetDeleteAttachmentOutcome.AttachmentNotFound;
        }

        asset.ClearPrimaryAttachmentIf(attachmentId, actorId, clock.UtcNow);
        await database.SaveChangesAsync(cancellationToken);
        return AssetDeleteAttachmentOutcome.Deleted;
    }

    private async Task EnsureUniqueCodeAsync(string? normalizedCode, int? excludeId, CancellationToken cancellationToken)
    {
        if (normalizedCode is null)
        {
            return;
        }

        var conflict = await database.Set<Asset>()
            .AsNoTracking()
            .AnyAsync(
                asset => asset.NormalizedCode == normalizedCode && asset.Id != excludeId,
                cancellationToken);
        if (conflict)
        {
            throw new AssetValidationException(
                "An asset with the same code already exists.",
                AssetValidationReason.DuplicateCode);
        }
    }

    private async Task<int> CountReferencesAsync(int assetId, CancellationToken cancellationToken)
    {
        var count = 0;
        foreach (var handler in deletionReferenceHandlers)
        {
            count += await handler.CountReferencesAsync(assetId, cancellationToken);
        }

        return count;
    }

    private async Task DeleteAttachmentFilesAsync(int assetId, CancellationToken cancellationToken)
    {
        var owner = AssetsAttachments.AssetOwner(assetId);
        var descriptors = await attachments.ListByOwnerAsync(owner, cancellationToken);
        foreach (var descriptor in descriptors)
        {
            await attachments.DeleteAsync(descriptor.Id, owner, cancellationToken);
        }
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await database.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // The normalized-code unique index is the only constraint reachable after
            // catalog references and uniqueness are pre-validated; surface it as the
            // same duplicate-code conflict if a concurrent writer won the race.
            throw new AssetValidationException(
                "An asset with the same code already exists.",
                AssetValidationReason.DuplicateCode);
        }
    }

    private static AssetValues Map(
        string? name,
        int categoryId,
        int locationId,
        string? status,
        string? code,
        string? brandModel,
        string? serialNumber,
        DateOnly? acquisitionDate,
        DateOnly? expectedEndOfLifeDate,
        string? notes,
        string? visibility) =>
        new(
            name ?? string.Empty,
            categoryId,
            locationId,
            ParseEnum(status, AssetDefaults.Status, "status"),
            code,
            brandModel,
            serialNumber,
            acquisitionDate,
            expectedEndOfLifeDate,
            notes,
            ParseEnum(visibility, AssetDefaults.Visibility, "visibility"));

    private static TEnum ParseEnum<TEnum>(string? value, TEnum fallback, string field)
        where TEnum : struct, Enum
    {
        // An omitted status or visibility falls back to the documented creation
        // default (Active / Public); a present value must be a recognized member.
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        if (!Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed) || !Enum.IsDefined(parsed))
        {
            throw new AssetValidationException($"The {field} is not a recognized value.");
        }

        return parsed;
    }
}

/// <summary>Outcome of a primary-image assignment.</summary>
internal enum AssetSetPrimaryOutcome
{
    AssetNotFound,
    AttachmentNotFound,
    NotImage,
    Assigned,
}

/// <summary>Result of assigning an asset primary image.</summary>
internal sealed record AssetSetPrimaryResult(
    AssetSetPrimaryOutcome Outcome,
    AttachmentDescriptor? Descriptor);

/// <summary>Outcome of deleting an asset attachment.</summary>
internal enum AssetDeleteAttachmentOutcome
{
    AssetNotFound,
    AttachmentNotFound,
    Deleted,
}

internal enum AssetDeletionOutcome
{
    Deleted,
    AssetNotFound,
    InvalidReassignment,
}
