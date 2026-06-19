using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Assets.Contracts;

/// <summary>
/// The narrow, privacy-respecting projection of an asset published for cross-module
/// references. It carries only the stable identifier, the display name, and the
/// visibility needed to enforce a consumer's visibility rule. It is never an EF Core
/// entity and exposes no other asset detail.
/// </summary>
internal sealed record AssetReference(int AssetId, string Name, RecordVisibility Visibility);

/// <summary>
/// Narrow read contract published by Assets and consumed by modules that hold a live
/// reference to an asset (initially Maintenance). It validates that a referenced
/// asset exists and is accessible to a viewer, resolves its display name, and exposes
/// its visibility so the consumer can apply its own visibility rule. Assets owns and
/// enforces accessibility and visibility here; a consumer may not derive access from
/// an identifier alone.
///
/// Keeping this contract in the Assets namespace preserves the
/// <c>Maintenance -> Assets</c> dependency direction: Maintenance consumes the
/// contract, Assets never references Maintenance.
/// </summary>
internal interface IAssetReferenceReader
{
    /// <summary>
    /// Resolves a single asset reference for <paramref name="viewer"/>, applying the
    /// Assets accessibility rules. Returns <see langword="null"/> when the asset does
    /// not exist or is not accessible to the viewer, matching the platform not-found
    /// behaviour so private assets are not disclosed.
    /// </summary>
    Task<AssetReference?> FindAccessibleAsync(
        int assetId,
        UserId viewer,
        CancellationToken cancellationToken);

    /// <summary>
    /// Resolves the display names of several assets for <paramref name="viewer"/> in
    /// one query, used to project the resolved-asset column of a task table. Assets
    /// that do not exist or are not accessible to the viewer are omitted, so the
    /// consumer renders a neutral placeholder for the missing identifiers.
    /// </summary>
    Task<IReadOnlyDictionary<int, AssetReference>> ResolveAccessibleAsync(
        IReadOnlyCollection<int> assetIds,
        UserId viewer,
        CancellationToken cancellationToken);
}
