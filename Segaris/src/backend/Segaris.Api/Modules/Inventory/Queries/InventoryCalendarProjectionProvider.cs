using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Configuration.Persistence;
using Segaris.Api.Modules.Inventory.Contracts;
using Segaris.Api.Modules.Inventory.Domain;
using Segaris.Persistence;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Inventory.Queries;

/// <summary>
/// Publishes expected receipt dates for accessible orders that are
/// <see cref="InventoryOrderStatus.Planning"/> or <see cref="InventoryOrderStatus.Active"/>
/// and carry an expected receipt date inside the requested range. Orders are titled by
/// their supplier, matching how the Inventory order list presents them.
/// </summary>
internal sealed class InventoryCalendarProjectionProvider(SegarisDbContext database)
    : IInventoryCalendarProjectionProvider
{
    public async Task<IReadOnlyList<InventoryOrderExpectedReceiptCalendarProjection>> ListCalendarExpectedReceiptsAsync(
        DateOnly from,
        DateOnly to,
        UserId viewer,
        CancellationToken cancellationToken)
    {
        return await database.Set<InventoryOrder>()
            .AsNoTracking()
            .Where(InventoryOrderPolicies.AccessibleTo(viewer))
            .Where(order => order.Status == InventoryOrderStatus.Planning
                || order.Status == InventoryOrderStatus.Active)
            .Where(order => order.ExpectedReceiptDate != null
                && order.ExpectedReceiptDate >= from
                && order.ExpectedReceiptDate <= to)
            .Select(order => new InventoryOrderExpectedReceiptCalendarProjection(
                order.Id,
                database.Set<SegarisSupplier>()
                    .Where(supplier => supplier.Id == order.SupplierId)
                    .Select(supplier => supplier.Name)
                    .First(),
                order.Status.ToString(),
                order.ExpectedReceiptDate!.Value,
                $"/inventory?orderId={order.Id}"))
            .ToArrayAsync(cancellationToken);
    }
}
