using Segaris.Api.Modules.Capex.Contracts;

namespace Segaris.Api.IntegrationTests.Capex;

/// <summary>
/// Builds valid Capex entry request payloads with the documented creation
/// defaults so the create/update API tests in Waves 3-4 share one setup.
/// Catalog references default to <c>0</c> and are normally overridden with the
/// identifiers resolved from the seeded catalog endpoints.
/// </summary>
internal sealed class CapexEntryRequestBuilder
{
    private string? _title = "Test entry";
    private string _movementType = "Expense";
    private string _status = "Planning";
    private DateOnly _dueDate = DateOnly.FromDateTime(DateTime.UtcNow.Date);
    private int _categoryId;
    private int? _supplierId;
    private int? _costCenterId;
    private int _currencyId;
    private string? _notes;
    private string _visibility = "Public";
    private List<CapexItemRequest> _items = [new("Test entry", 1m, 0m)];

    public static CapexEntryRequestBuilder Default() => new();

    public CapexEntryRequestBuilder WithTitle(string? title)
    {
        _title = title;
        return this;
    }

    public CapexEntryRequestBuilder WithMovementType(string movementType)
    {
        _movementType = movementType;
        return this;
    }

    public CapexEntryRequestBuilder WithStatus(string status)
    {
        _status = status;
        return this;
    }

    public CapexEntryRequestBuilder WithDueDate(DateOnly dueDate)
    {
        _dueDate = dueDate;
        return this;
    }

    public CapexEntryRequestBuilder WithCategory(int categoryId)
    {
        _categoryId = categoryId;
        return this;
    }

    public CapexEntryRequestBuilder WithSupplier(int? supplierId)
    {
        _supplierId = supplierId;
        return this;
    }

    public CapexEntryRequestBuilder WithCostCenter(int? costCenterId)
    {
        _costCenterId = costCenterId;
        return this;
    }

    public CapexEntryRequestBuilder WithCurrency(int currencyId)
    {
        _currencyId = currencyId;
        return this;
    }

    public CapexEntryRequestBuilder WithNotes(string? notes)
    {
        _notes = notes;
        return this;
    }

    public CapexEntryRequestBuilder WithVisibility(string visibility)
    {
        _visibility = visibility;
        return this;
    }

    public CapexEntryRequestBuilder WithItems(params CapexItemRequest[] items)
    {
        _items = [.. items];
        return this;
    }

    public CreateCapexEntryRequest BuildCreate() => new(
        _title,
        _movementType,
        _status,
        _dueDate,
        _categoryId,
        _supplierId,
        _costCenterId,
        _currencyId,
        _notes,
        _visibility,
        _items);

    public UpdateCapexEntryRequest BuildUpdate() => new(
        _title,
        _movementType,
        _status,
        _dueDate,
        _categoryId,
        _supplierId,
        _costCenterId,
        _currencyId,
        _notes,
        _visibility,
        _items);
}
