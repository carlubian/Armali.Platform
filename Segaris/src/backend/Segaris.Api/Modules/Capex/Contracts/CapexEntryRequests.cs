namespace Segaris.Api.Modules.Capex.Contracts;

/// <summary>
/// Frozen request item line. The item's persisted position is the index of the
/// line within <see cref="CreateCapexEntryRequest.Items"/> /
/// <see cref="UpdateCapexEntryRequest.Items"/>, so reordering is expressed by
/// resubmitting the collection in the desired order. The server is authoritative
/// for the calculated line amount and never trusts a client-supplied total.
/// </summary>
internal sealed record CapexItemRequest(
    string? Description,
    decimal Quantity,
    decimal UnitAmount);

/// <summary>
/// Frozen request contract for <c>POST /api/capex/entries</c>.
/// <see cref="MovementType"/>, <see cref="Status"/>, and <see cref="Visibility"/>
/// are the fixed string vocabularies (the enum member names and the platform
/// visibility names). <see cref="DueDate"/> is a civil date with no past/future
/// boundary. Catalog references are the database-assigned integer identifiers.
/// </summary>
internal sealed record CreateCapexEntryRequest(
    string? Title,
    string? MovementType,
    string? Status,
    DateOnly DueDate,
    int CategoryId,
    int? SupplierId,
    int? CostCenterId,
    int CurrencyId,
    string? Notes,
    string? Visibility,
    IReadOnlyList<CapexItemRequest> Items);

/// <summary>
/// Frozen request contract for <c>PUT /api/capex/entries/{entryId}</c>. The
/// update fully replaces the ordered item collection; the server recalculates
/// every line and the persisted total in one transaction.
/// </summary>
internal sealed record UpdateCapexEntryRequest(
    string? Title,
    string? MovementType,
    string? Status,
    DateOnly DueDate,
    int CategoryId,
    int? SupplierId,
    int? CostCenterId,
    int CurrencyId,
    string? Notes,
    string? Visibility,
    IReadOnlyList<CapexItemRequest> Items);
