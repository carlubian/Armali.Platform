using Segaris.Api.Modules.Opex.Contracts;

namespace Segaris.Api.IntegrationTests.Opex;

/// <summary>
/// Builds valid Opex contract request payloads with the documented creation
/// defaults so the create and update API tests share one setup. Catalog
/// references default to <c>0</c> and are normally overridden with the identifiers
/// resolved from the seeded catalog.
/// </summary>
internal sealed class OpexContractRequestBuilder
{
    private string? _name = "Test contract";
    private string _movementType = "Expense";
    private string _status = "Active";
    private DateOnly? _startDate;
    private DateOnly? _closedDate;
    private decimal? _estimatedAnnualAmount;
    private string _expectedFrequency = "Monthly";
    private int _categoryId;
    private int? _supplierId;
    private int? _costCenterId;
    private int _currencyId;
    private string? _notes;
    private string _visibility = "Public";

    public static OpexContractRequestBuilder Default() => new();

    public OpexContractRequestBuilder WithName(string? name)
    {
        _name = name;
        return this;
    }

    public OpexContractRequestBuilder WithMovementType(string movementType)
    {
        _movementType = movementType;
        return this;
    }

    public OpexContractRequestBuilder WithStatus(string status)
    {
        _status = status;
        return this;
    }

    public OpexContractRequestBuilder WithStartDate(DateOnly? startDate)
    {
        _startDate = startDate;
        return this;
    }

    public OpexContractRequestBuilder WithClosedDate(DateOnly? closedDate)
    {
        _closedDate = closedDate;
        return this;
    }

    public OpexContractRequestBuilder WithEstimatedAnnualAmount(decimal? amount)
    {
        _estimatedAnnualAmount = amount;
        return this;
    }

    public OpexContractRequestBuilder WithExpectedFrequency(string frequency)
    {
        _expectedFrequency = frequency;
        return this;
    }

    public OpexContractRequestBuilder WithCategory(int categoryId)
    {
        _categoryId = categoryId;
        return this;
    }

    public OpexContractRequestBuilder WithSupplier(int? supplierId)
    {
        _supplierId = supplierId;
        return this;
    }

    public OpexContractRequestBuilder WithCostCenter(int? costCenterId)
    {
        _costCenterId = costCenterId;
        return this;
    }

    public OpexContractRequestBuilder WithCurrency(int currencyId)
    {
        _currencyId = currencyId;
        return this;
    }

    public OpexContractRequestBuilder WithNotes(string? notes)
    {
        _notes = notes;
        return this;
    }

    public OpexContractRequestBuilder WithVisibility(string visibility)
    {
        _visibility = visibility;
        return this;
    }

    public CreateOpexContractRequest BuildCreate() => new(
        _name,
        _movementType,
        _status,
        _startDate,
        _closedDate,
        _estimatedAnnualAmount,
        _expectedFrequency,
        _categoryId,
        _supplierId,
        _costCenterId,
        _currencyId,
        _notes,
        _visibility);

    public UpdateOpexContractRequest BuildUpdate() => new(
        _name,
        _movementType,
        _status,
        _startDate,
        _closedDate,
        _estimatedAnnualAmount,
        _expectedFrequency,
        _categoryId,
        _supplierId,
        _costCenterId,
        _currencyId,
        _notes,
        _visibility);
}
