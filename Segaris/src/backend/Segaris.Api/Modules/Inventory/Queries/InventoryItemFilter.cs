using Segaris.Api.Modules.Inventory.Domain;
using Segaris.Shared.Authorization;

namespace Segaris.Api.Modules.Inventory.Queries;

/// <summary>
/// Normalized, validated items-list filter. Optional fields are <c>null</c> when
/// the caller did not supply them. Enum-backed filters are parsed against the
/// fixed Inventory vocabularies before they reach the database query. The supplier
/// filter matches items whose allowed-supplier set contains the given supplier.
/// </summary>
internal sealed record InventoryItemFilter(
    string? Search,
    InventoryItemStatus? Status,
    int? CategoryId,
    int? LocationId,
    int? SupplierId,
    RecordVisibility? Visibility,
    int? CreatorId);
