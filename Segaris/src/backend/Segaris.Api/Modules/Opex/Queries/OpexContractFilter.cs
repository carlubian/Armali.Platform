using Segaris.Api.Modules.Opex.Domain;
using Segaris.Shared.Authorization;

namespace Segaris.Api.Modules.Opex.Queries;

/// <summary>
/// Normalized, validated contracts-list filter. Optional fields are <c>null</c>
/// when the caller did not supply them. Enum-backed filters are parsed against
/// the fixed Opex vocabularies before they reach the database query. Contracts
/// have no single effective date, so the list offers no date-range filter.
/// </summary>
internal sealed record OpexContractFilter(
    string? Search,
    OpexMovementType? MovementType,
    OpexContractStatus? Status,
    int? CategoryId,
    int? SupplierId,
    int? CostCenterId,
    int? CurrencyId,
    OpexExpectedFrequency? Frequency,
    RecordVisibility? Visibility,
    int? CreatorId);
