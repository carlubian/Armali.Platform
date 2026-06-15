namespace Segaris.Api.Modules.Configuration.Contracts;

/// <summary>
/// Frozen response contract for
/// <c>GET /api/configuration/{catalog}/{id}/deletion-impact</c> and the equivalent
/// Capex category route. The shape is deliberately privacy-neutral: it never
/// exposes record counts, titles, owners, or per-module totals. It states only
/// whether the value is in use and which removal paths are currently available.
///
/// The response is advisory. The deletion and replace-and-delete commands
/// re-evaluate every condition inside their own transaction, so a direct delete
/// fails rather than cascading when a concurrent reference appears.
/// </summary>
/// <param name="IsReferenced">Any business record currently references the value.</param>
/// <param name="CanDeleteDirectly">
/// The value is unreferenced and the catalog's minimum-cardinality rule permits
/// removal.
/// </param>
/// <param name="CanClearReferences">
/// References may be cleared to <c>null</c> because the catalog's consumer
/// references are optional.
/// </param>
/// <param name="RequiresExchangeRate">
/// Removal requires a target value and an explicit exchange rate (referenced
/// currency conversion).
/// </param>
/// <param name="HasReplacementCandidates">
/// At least one other value in the catalog can serve as a replacement target.
/// </param>
internal sealed record CatalogDeletionImpactResponse(
    bool IsReferenced,
    bool CanDeleteDirectly,
    bool CanClearReferences,
    bool RequiresExchangeRate,
    bool HasReplacementCandidates);
