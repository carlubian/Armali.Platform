using Segaris.Api.Modules.Assets.Domain;
using Segaris.Shared.Authorization;

namespace Segaris.Api.Modules.Assets.Queries;

/// <summary>
/// Normalized, validated assets-list filter. Optional fields are <c>null</c> when
/// the caller did not supply them. Enum-backed filters are parsed against the fixed
/// asset vocabularies before they reach the database query. The free-text search
/// matches name, code, brand/model, serial number, and notes.
/// </summary>
internal sealed record AssetFilter(
    string? Search,
    int? CategoryId,
    int? LocationId,
    AssetStatus? Status,
    RecordVisibility? Visibility,
    int? CreatorId);
