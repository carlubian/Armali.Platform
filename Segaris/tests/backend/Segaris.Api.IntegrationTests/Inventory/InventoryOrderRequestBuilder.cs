using Segaris.Api.Modules.Inventory.Contracts;

namespace Segaris.Api.IntegrationTests.Inventory;

internal sealed class InventoryOrderRequestBuilder
{
    private int supplierId;
    private string? status = "Planning";
    private int currencyId;
    private DateOnly? orderDate = new(2026, 6, 16);
    private DateOnly? expectedReceiptDate = new(2026, 6, 23);
    private string? notes;
    private string? visibility = "Public";
    private IReadOnlyList<InventoryOrderLineRequest> lines = [];

    public static InventoryOrderRequestBuilder Default() => new();

    public InventoryOrderRequestBuilder WithSupplier(int value)
    {
        supplierId = value;
        return this;
    }

    public InventoryOrderRequestBuilder WithStatus(string? value)
    {
        status = value;
        return this;
    }

    public InventoryOrderRequestBuilder WithCurrency(int value)
    {
        currencyId = value;
        return this;
    }

    public InventoryOrderRequestBuilder WithDates(DateOnly? order, DateOnly? expected)
    {
        orderDate = order;
        expectedReceiptDate = expected;
        return this;
    }

    public InventoryOrderRequestBuilder WithNotes(string? value)
    {
        notes = value;
        return this;
    }

    public InventoryOrderRequestBuilder WithVisibility(string? value)
    {
        visibility = value;
        return this;
    }

    public InventoryOrderRequestBuilder WithLines(params InventoryOrderLineRequest[] value)
    {
        lines = value;
        return this;
    }

    public CreateInventoryOrderRequest BuildCreate() => new(
        supplierId,
        status,
        currencyId,
        orderDate,
        expectedReceiptDate,
        notes,
        visibility,
        lines);

    public UpdateInventoryOrderRequest BuildUpdate() => new(
        supplierId,
        status,
        currencyId,
        orderDate,
        expectedReceiptDate,
        notes,
        visibility,
        lines);
}
