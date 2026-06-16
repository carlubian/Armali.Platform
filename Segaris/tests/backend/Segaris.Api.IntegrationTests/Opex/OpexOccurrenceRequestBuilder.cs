using Segaris.Api.Modules.Opex.Contracts;

namespace Segaris.Api.IntegrationTests.Opex;

/// <summary>
/// Builds valid Opex occurrence request payloads so the create and update API
/// tests share one setup. Occurrences carry only an effective date, an amount, an
/// optional description, and optional notes; everything else is inherited from the
/// parent contract.
/// </summary>
internal sealed class OpexOccurrenceRequestBuilder
{
    private DateOnly? _effectiveDate = new(2026, 6, 15);
    private decimal _actualAmount = 100m;
    private string? _description;
    private string? _notes;

    public static OpexOccurrenceRequestBuilder Default() => new();

    public OpexOccurrenceRequestBuilder WithEffectiveDate(DateOnly? effectiveDate)
    {
        _effectiveDate = effectiveDate;
        return this;
    }

    public OpexOccurrenceRequestBuilder WithActualAmount(decimal amount)
    {
        _actualAmount = amount;
        return this;
    }

    public OpexOccurrenceRequestBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

    public OpexOccurrenceRequestBuilder WithNotes(string? notes)
    {
        _notes = notes;
        return this;
    }

    public CreateOpexOccurrenceRequest BuildCreate() => new(
        _effectiveDate,
        _actualAmount,
        _description,
        _notes);

    public UpdateOpexOccurrenceRequest BuildUpdate() => new(
        _effectiveDate,
        _actualAmount,
        _description,
        _notes);
}
