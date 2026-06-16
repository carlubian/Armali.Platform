using Segaris.Api.Modules.Inventory.Domain;
using Segaris.Shared.Authorization;

namespace Segaris.Api.Modules.Inventory.Queries;

/// <summary>
/// Normalized, validated orders-list filter. Optional fields are <c>null</c> when
/// omitted by the caller.
/// </summary>
internal sealed record InventoryOrderFilter(
    string? Search,
    int? SupplierId,
    InventoryOrderStatus? Status,
    int? CurrencyId,
    RecordVisibility? Visibility,
    int? CreatorId);
