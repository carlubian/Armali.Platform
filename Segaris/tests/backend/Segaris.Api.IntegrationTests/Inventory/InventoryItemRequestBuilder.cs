using Segaris.Api.Modules.Inventory.Contracts;

namespace Segaris.Api.IntegrationTests.Inventory;

internal sealed class InventoryItemRequestBuilder
{
    private string? _name = "Test item";
    private string? _status = "Candidate";
    private string? _notes;
    private int _categoryId;
    private int _locationId;
    private decimal _currentStock;
    private decimal _minimumStock;
    private List<int> _supplierIds = [];
    private string? _visibility = "Public";

    public static InventoryItemRequestBuilder Default() => new();

    public InventoryItemRequestBuilder WithName(string? name)
    {
        _name = name;
        return this;
    }

    public InventoryItemRequestBuilder WithStatus(string? status)
    {
        _status = status;
        return this;
    }

    public InventoryItemRequestBuilder WithNotes(string? notes)
    {
        _notes = notes;
        return this;
    }

    public InventoryItemRequestBuilder WithCategory(int categoryId)
    {
        _categoryId = categoryId;
        return this;
    }

    public InventoryItemRequestBuilder WithLocation(int locationId)
    {
        _locationId = locationId;
        return this;
    }

    public InventoryItemRequestBuilder WithStock(decimal currentStock, decimal minimumStock)
    {
        _currentStock = currentStock;
        _minimumStock = minimumStock;
        return this;
    }

    public InventoryItemRequestBuilder WithSuppliers(params int[] supplierIds)
    {
        _supplierIds = [.. supplierIds];
        return this;
    }

    public InventoryItemRequestBuilder WithVisibility(string? visibility)
    {
        _visibility = visibility;
        return this;
    }

    public CreateInventoryItemRequest BuildCreate() => new(
        _name,
        _status,
        _notes,
        _categoryId,
        _locationId,
        _currentStock,
        _minimumStock,
        _supplierIds,
        _visibility);

    public UpdateInventoryItemRequest BuildUpdate() => new(
        _name,
        _status,
        _notes,
        _categoryId,
        _locationId,
        _currentStock,
        _minimumStock,
        _supplierIds,
        _visibility);
}
