using Segaris.Api.Modules.Configuration.Contracts;
using Segaris.Api.Modules.Inventory;
using Segaris.Api.Modules.Inventory.Contracts;
using Segaris.Api.Modules.Inventory.Domain;
using Segaris.Shared.Api;
using Segaris.Shared.Authorization;

namespace Segaris.UnitTests;

public sealed class InventoryContractTests
{
    [Fact]
    public void Fixed_vocabularies_are_frozen()
    {
        Assert.Equal(["Candidate", "Active", "Deprecated"], Enum.GetNames<InventoryItemStatus>());
        Assert.Equal(
            ["Planning", "Active", "Received", "Cancelled"],
            Enum.GetNames<InventoryOrderStatus>());
        Assert.Equal(["Increase", "Decrease"], Enum.GetNames<InventoryStockAdjustmentDirection>());
    }

    [Fact]
    public void Creation_defaults_are_frozen()
    {
        Assert.Equal(InventoryItemStatus.Candidate, InventoryDefaults.ItemStatus);
        Assert.Equal(InventoryOrderStatus.Planning, InventoryDefaults.OrderStatus);
        Assert.Equal(RecordVisibility.Public, InventoryDefaults.Visibility);
        Assert.Equal(0.00m, InventoryDefaults.CurrentStock);
        Assert.Equal(0.00m, InventoryDefaults.MinimumStock);
        Assert.Equal("Europe/Madrid", InventoryDefaults.HouseholdTimeZoneId);

        var newYearEve = new DateTimeOffset(2025, 12, 31, 23, 30, 0, TimeSpan.Zero);
        Assert.Equal(new DateOnly(2026, 1, 1), InventoryDefaults.OrderDate(newYearEve));
        Assert.Equal(new DateOnly(2026, 1, 8), InventoryDefaults.ExpectedReceiptDate(newYearEve));
    }

    [Fact]
    public void Routes_separate_items_and_orders()
    {
        Assert.Equal("inventory/items", InventoryApiRoutes.Items);
        Assert.Equal("/{itemId:int}", InventoryApiRoutes.ItemById);
        Assert.Equal("/{itemId:int}/stock-adjustments", InventoryApiRoutes.ItemStockAdjustments);
        Assert.Equal("inventory/orders", InventoryApiRoutes.Orders);
        Assert.Equal("/{orderId:int}/receive", InventoryApiRoutes.OrderReceive);
        Assert.Equal("inventory/categories", InventoryApiRoutes.Categories);
        Assert.Equal("inventory/locations", InventoryApiRoutes.Locations);
    }

    [Fact]
    public void Item_sort_and_pagination_contracts_are_frozen()
    {
        Assert.Equal(
            new HashSet<string>(StringComparer.Ordinal)
            {
                "name", "status", "category", "location", "currentStock", "minimumStock",
                "visibility", "id",
            },
            InventoryItemQuery.AllowedSortFields);
        Assert.Equal("name", InventoryItemQuery.SortFields.Default);
        Assert.Equal("id", InventoryItemQuery.SortFields.TieBreaker);
        Assert.Equal("asc", InventoryItemQuery.DefaultSortDirection);
        Assert.Equal([10, 25, 50, 100], InventoryItemQuery.PageSizeOptions);
    }

    [Fact]
    public void Order_sort_and_pagination_contracts_are_frozen()
    {
        Assert.Equal(
            new HashSet<string>(StringComparer.Ordinal)
            {
                "supplier", "status", "orderDate", "expectedReceiptDate", "currency",
                "visibility", "id",
            },
            InventoryOrderQuery.AllowedSortFields);
        Assert.Equal("orderDate", InventoryOrderQuery.SortFields.Default);
        Assert.Equal("id", InventoryOrderQuery.SortFields.TieBreaker);
        Assert.Equal("desc", InventoryOrderQuery.DefaultSortDirection);
        Assert.Equal([10, 25, 50, 100], InventoryOrderQuery.PageSizeOptions);
    }

    [Fact]
    public void Default_item_sort_is_name_ascending()
    {
        var sort = SortRequest.Create(
            null,
            null,
            InventoryItemQuery.AllowedSortFields,
            InventoryItemQuery.SortFields.Default,
            InventoryItemQuery.SortFields.TieBreaker);

        Assert.Equal("name", sort.Field);
        Assert.Equal(SortDirection.Ascending, sort.Direction);
        Assert.Equal("id", sort.TieBreakerField);
    }

    [Fact]
    public void Order_sort_resolves_explicit_descending_default_direction()
    {
        var sort = SortRequest.Create(
            null,
            InventoryOrderQuery.DefaultSortDirection,
            InventoryOrderQuery.AllowedSortFields,
            InventoryOrderQuery.SortFields.Default,
            InventoryOrderQuery.SortFields.TieBreaker);

        Assert.Equal("orderDate", sort.Field);
        Assert.Equal(SortDirection.Descending, sort.Direction);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Pagination_rejects_page_sizes_outside_platform_bounds(int pageSize)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PaginationRequest(1, pageSize));
    }

    [Fact]
    public void Shared_configuration_references_are_explicit()
    {
        Assert.Equal(
            [ConfigurationCatalogKind.Suppliers, ConfigurationCatalogKind.Currencies],
            InventoryConfigurationContracts.SharedReferenceKinds);
    }

    [Fact]
    public void Error_codes_are_namespaced_and_stable()
    {
        Assert.Equal("inventory.item.not_found", InventoryErrorCodes.ItemNotFound.Value);
        Assert.Equal("inventory.item.referenced", InventoryErrorCodes.ItemReferencedByOrder.Value);
        Assert.Equal("inventory.stock.negative_result", InventoryErrorCodes.StockNegativeResult.Value);
        Assert.Equal("inventory.order.not_active", InventoryErrorCodes.OrderNotActive.Value);
        Assert.Equal("inventory.order.received_locked", InventoryErrorCodes.OrderReceivedLocked.Value);
        Assert.Equal(
            "inventory.order.line.supplier_not_allowed",
            InventoryErrorCodes.OrderLineSupplierNotAllowed.Value);
        Assert.Equal("inventory.catalog.unknown_reference", InventoryErrorCodes.UnknownCatalogReference.Value);
        Assert.Equal("inventory.location.not_found", InventoryErrorCodes.LocationNotFound.Value);
    }

    [Fact]
    public void Attachment_owners_distinguish_items_and_orders()
    {
        var item = InventoryAttachments.ItemOwner(12);
        var order = InventoryAttachments.OrderOwner(34);

        Assert.Equal(("Inventory", "Item", "12"), (item.Module, item.EntityType, item.EntityId));
        Assert.Equal(("Inventory", "Order", "34"), (order.Module, order.EntityType, order.EntityId));
    }

    [Fact]
    public void Order_line_response_carries_item_status_for_deprecated_warning()
    {
        var line = new InventoryOrderLineResponse(5, 9, "Olive oil", "Deprecated", 2m, 12.34m);

        Assert.Equal("Deprecated", line.ItemStatus);
        Assert.Equal(12.34m, line.LineTotal);
    }

    [Fact]
    public void Order_request_keeps_lines_subordinate_without_per_line_currency()
    {
        var request = new CreateInventoryOrderRequest(
            SupplierId: 1,
            Status: "Planning",
            CurrencyId: 2,
            OrderDate: new DateOnly(2026, 6, 16),
            ExpectedReceiptDate: new DateOnly(2026, 6, 23),
            Notes: null,
            Visibility: "Public",
            Lines: [new InventoryOrderLineRequest(9, 2m, 12.34m)]);

        Assert.Single(request.Lines);
        Assert.DoesNotContain(
            typeof(InventoryOrderLineRequest).GetProperties(),
            property => property.Name is "CurrencyId" or "Visibility");
    }
}
