using Microsoft.EntityFrameworkCore;
using Segaris.Api.Modules.Configuration.Persistence;
using Segaris.Api.Modules.Inventory.Contracts;
using Segaris.Api.Modules.Inventory.Domain;
using Segaris.Persistence;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Inventory.Queries;

/// <summary>
/// Publishes accessible Inventory order spending for Analytics. Orders contribute when
/// their status is neither <see cref="InventoryOrderStatus.Planning"/> nor
/// <see cref="InventoryOrderStatus.Cancelled"/> (that is, <c>Active</c> or
/// <c>Received</c>). The Inventory model does not persist a distinct actual receipt
/// date, so the accounting date is the order's expected receipt date and falls back to
/// its order date; orders that carry neither inside the requested range are excluded.
///
/// Each projection is one order line: the amount is the line total in the order currency
/// and the projection carries the line item and its category so Analytics can group by
/// item category and rank top items. The supplier label is the order supplier and the
/// stable <c>inventory:{orderId}:{lineId}</c> source identifier lets Analytics regroup
/// lines into orders for the average-order-by-supplier chart. Inventory orders carry no
/// order-level category, cost centre, or destination, so those labels are absent.
/// </summary>
internal sealed class InventoryFinancialProjectionProvider(SegarisDbContext database)
    : IInventoryFinancialProjectionProvider
{
    public async Task<IReadOnlyList<InventoryFinancialProjection>> ListFinancialProjectionsAsync(
        DateOnly from,
        DateOnly to,
        UserId viewer,
        CancellationToken cancellationToken)
    {
        var accessibleOrders = database.Set<InventoryOrder>()
            .AsNoTracking()
            .Where(InventoryOrderPolicies.AccessibleTo(viewer))
            .Where(order => order.Status != InventoryOrderStatus.Planning
                && order.Status != InventoryOrderStatus.Cancelled)
            .Where(order => (order.ExpectedReceiptDate ?? order.OrderDate) >= from
                && (order.ExpectedReceiptDate ?? order.OrderDate) <= to);

        var rows = await database.Set<InventoryOrderLine>()
            .AsNoTracking()
            .Join(
                accessibleOrders,
                line => line.OrderId,
                order => order.Id,
                (line, order) => new { line, order })
            .OrderBy(row => row.order.ExpectedReceiptDate ?? row.order.OrderDate)
            .ThenBy(row => row.order.Id)
            .ThenBy(row => row.line.Id)
            .Select(row => new ProjectionRow(
                row.order.Id,
                row.line.Id,
                row.order.ExpectedReceiptDate ?? row.order.OrderDate,
                row.line.LineTotal,
                database.Set<SegarisCurrency>()
                    .Where(currency => currency.Id == row.order.CurrencyId)
                    .Select(currency => currency.Code)
                    .First(),
                database.Set<SegarisSupplier>()
                    .Where(supplier => supplier.Id == row.order.SupplierId)
                    .Select(supplier => supplier.Name)
                    .First(),
                database.Set<InventoryItem>()
                    .Where(item => item.Id == row.line.ItemId)
                    .Select(item => item.Name)
                    .First(),
                database.Set<InventoryItem>()
                    .Where(item => item.Id == row.line.ItemId)
                    .Join(
                        database.Set<InventoryCategory>(),
                        item => item.CategoryId,
                        category => category.Id,
                        (item, category) => category.Name)
                    .First()))
            .ToArrayAsync(cancellationToken);

        return rows
            .Select(row => new InventoryFinancialProjection(
                $"inventory:{row.OrderId}:{row.LineId}",
                "inventory",
                "orderLine",
                row.AccountingDate!.Value,
                "Expense",
                row.Amount,
                row.CurrencyCode,
                null,
                row.SupplierLabel,
                null,
                row.ItemCategoryLabel,
                row.ItemLabel,
                null))
            .ToArray();
    }

    private sealed record ProjectionRow(
        int OrderId,
        int LineId,
        DateOnly? AccountingDate,
        decimal Amount,
        string CurrencyCode,
        string SupplierLabel,
        string ItemLabel,
        string ItemCategoryLabel);
}
