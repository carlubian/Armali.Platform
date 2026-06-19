using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Assets.Contracts;

/// <summary>
/// Deletion-reference contract published by Assets and implemented by every module
/// that references an asset (initially Maintenance). When an asset is deleted, the
/// Assets deletion command resolves every registered handler, evaluates the combined
/// impact, and drives reassignment inside the single <c>SegarisDbContext</c>
/// transaction it started. Assets enumerates handlers; it never queries the
/// consuming module's entities.
///
/// Transaction ownership is fixed: a handler mutates tracked entities and updates
/// their audit metadata, but it must never call <c>SaveChanges</c> or commit. The
/// owner performs one final save and commit only after every handler succeeds; if any
/// handler or validation step fails, the whole transaction rolls back and no
/// reference, audit field, or asset row changes.
///
/// Keeping the interface in the Assets namespace preserves the
/// <c>Maintenance -> Assets</c> dependency direction: Maintenance implements and
/// registers handlers, Assets never references Maintenance.
/// </summary>
internal interface IAssetDeletionReferenceHandler
{
    /// <summary>
    /// Reports how many records reference the asset without disclosing identities or
    /// private records of other users. The count includes public and private records
    /// so the owner can present a privacy-neutral deletion impact.
    /// </summary>
    Task<int> CountReferencesAsync(int assetId, CancellationToken cancellationToken);

    /// <summary>
    /// Reassigns every reference from the deleted asset to the target asset and
    /// updates the affected records' modification metadata. The handler validates
    /// that the target satisfies its own per-record rules (for Maintenance, the
    /// visibility rule for every affected task) and throws
    /// <see cref="AssetReassignmentBlockedException"/> when no compatible target
    /// exists. The reference is never cleared. Does not save or commit.
    /// </summary>
    Task ReassignReferencesAsync(
        AssetDeletionReassignment reassignment,
        CancellationToken cancellationToken);
}

/// <summary>
/// The validated reassignment a handler must apply when a referenced asset is
/// deleted. Every reference moves from <see cref="SourceAssetId"/> to
/// <see cref="TargetAssetId"/> regardless of record status or ownership; the
/// reference is never cleared.
/// </summary>
/// <param name="SourceAssetId">The asset being deleted.</param>
/// <param name="TargetAssetId">The asset that receives the references.</param>
/// <param name="Actor">The user performing the deletion, for audit.</param>
/// <param name="OccurredAt">The UTC modification time stamped on affected records.</param>
internal sealed record AssetDeletionReassignment(
    int SourceAssetId,
    int TargetAssetId,
    UserId Actor,
    DateTimeOffset OccurredAt);

/// <summary>
/// Raised by a handler when the requested target cannot receive the references
/// because no compatible target satisfies every affected record's rules (for
/// Maintenance, a public task may reference only a public asset). The owning Assets
/// deletion command catches it and surfaces a stable, privacy-neutral block so the
/// user is told to reassign or delete the affected records manually first. It never
/// leaves a partial reassignment behind because the whole transaction rolls back.
/// </summary>
internal sealed class AssetReassignmentBlockedException(string message) : Exception(message);
