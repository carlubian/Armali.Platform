using Segaris.Api.Modules.Capex.Domain;
using Segaris.Shared.Authorization;

namespace Segaris.Api.Modules.Capex.Queries;

/// <summary>
/// Normalized, validated Entries-list filter. Optional fields are <c>null</c>
/// when the caller did not supply them. Enum-backed filters are parsed against
/// the fixed Capex vocabularies before they reach the database query.
/// </summary>
internal sealed record CapexEntryFilter(
    string? Search,
    DateOnly? From,
    DateOnly? To,
    CapexMovementType? MovementType,
    CapexEntryStatus? Status,
    int? CategoryId,
    int? SupplierId,
    int? CostCenterId,
    int? CurrencyId,
    RecordVisibility? Visibility,
    int? CreatorId);
