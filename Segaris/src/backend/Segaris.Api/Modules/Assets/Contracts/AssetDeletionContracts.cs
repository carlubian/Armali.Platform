namespace Segaris.Api.Modules.Assets.Contracts;

/// <summary>
/// Privacy-neutral impact for deleting an asset that may be referenced by other
/// modules through Assets-owned deletion contracts.
/// </summary>
internal sealed record AssetDeletionImpactResponse(
    bool IsReferenced,
    int ReferenceCount,
    bool CanDeleteDirectly,
    bool RequiresReassignment,
    bool HasReplacementCandidates);

/// <summary>
/// Request for deleting an asset after atomically reassigning all cross-module
/// references to another compatible asset.
/// </summary>
internal sealed record AssetReassignmentDeletionRequest(int? TargetAssetId);
