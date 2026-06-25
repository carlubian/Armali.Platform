using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Inventory.Contracts;

internal sealed record InventoryOrderExpectedReceiptCalendarProjection(
    int OrderId,
    string Title,
    string Status,
    DateOnly ExpectedReceiptDate,
    string? TargetRoute);

internal interface IInventoryCalendarProjectionProvider
{
    Task<IReadOnlyList<InventoryOrderExpectedReceiptCalendarProjection>> ListCalendarExpectedReceiptsAsync(
        DateOnly from,
        DateOnly to,
        UserId viewer,
        CancellationToken cancellationToken);
}
