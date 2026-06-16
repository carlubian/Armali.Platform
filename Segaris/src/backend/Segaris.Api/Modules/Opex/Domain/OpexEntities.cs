using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Opex.Domain;

/// <summary>
/// The Opex-owned category catalog row. It mirrors the shared-catalog shape
/// (display name, normalized name for case-insensitive uniqueness, declaration
/// order, and audit metadata) while remaining owned by Opex.
/// </summary>
internal sealed class OpexCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}

/// <summary>The editable fields of an Opex contract, independent of audit metadata.</summary>
internal sealed record OpexContractValues(
    string Name,
    OpexMovementType MovementType,
    OpexContractStatus Status,
    DateOnly? StartDate,
    DateOnly? ClosedDate,
    decimal? EstimatedAnnualAmount,
    OpexExpectedFrequency ExpectedFrequency,
    int CategoryId,
    int? SupplierId,
    int? CostCenterId,
    int CurrencyId,
    string? Notes,
    RecordVisibility Visibility);

/// <summary>
/// A recurrent income or expense contract grouping zero or more effective
/// occurrences. The contract owns its movement type, classification, currency,
/// and visibility; occurrences inherit them. The name is globally unique after
/// trimming and case-insensitive comparison, enforced through
/// <see cref="NormalizedName"/>.
/// </summary>
internal sealed class OpexContract
{
    private readonly List<OpexOccurrence> occurrences = [];

    private OpexContract()
    {
    }

    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string NormalizedName { get; private set; } = string.Empty;
    public OpexMovementType MovementType { get; private set; }
    public OpexContractStatus Status { get; private set; }
    public DateOnly? StartDate { get; private set; }
    public DateOnly? ClosedDate { get; private set; }
    public decimal? EstimatedAnnualAmount { get; private set; }
    public OpexExpectedFrequency ExpectedFrequency { get; private set; }
    public int CategoryId { get; private set; }
    public int? SupplierId { get; private set; }
    public int? CostCenterId { get; private set; }
    public int CurrencyId { get; private set; }
    public string? Notes { get; private set; }
    public RecordVisibility Visibility { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public int CreatedBy { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public int UpdatedBy { get; private set; }
    public IReadOnlyList<OpexOccurrence> Occurrences => occurrences;

    public static OpexContract Create(OpexContractValues values, UserId creatorId, DateTimeOffset now)
    {
        EnsureUtc(now);
        var contract = new OpexContract
        {
            CreatedAt = now,
            CreatedBy = creatorId.Value,
            UpdatedAt = now,
            UpdatedBy = creatorId.Value,
        };
        contract.Apply(values, creatorId, now, isCreation: true);
        return contract;
    }

    public void Update(OpexContractValues values, UserId actorId, DateTimeOffset now)
    {
        Apply(values, actorId, now, isCreation: false);
    }

    internal void ReplaceCategory(int categoryId, UserId actorId, DateTimeOffset now)
    {
        EnsureUtc(now);
        if (categoryId <= 0)
        {
            throw new OpexValidationException("Catalog identifiers must be positive.");
        }

        CategoryId = categoryId;
        StampModification(actorId, now);
    }

    internal void ReplaceSupplier(int? supplierId, UserId actorId, DateTimeOffset now)
    {
        EnsureUtc(now);
        if (supplierId is <= 0)
        {
            throw new OpexValidationException("Catalog identifiers must be positive.");
        }

        SupplierId = supplierId;
        StampModification(actorId, now);
    }

    internal void ReplaceCostCenter(int? costCenterId, UserId actorId, DateTimeOffset now)
    {
        EnsureUtc(now);
        if (costCenterId is <= 0)
        {
            throw new OpexValidationException("Catalog identifiers must be positive.");
        }

        CostCenterId = costCenterId;
        StampModification(actorId, now);
    }

    /// <summary>
    /// Converts the contract to <paramref name="targetCurrencyId"/> using
    /// <paramref name="exchangeRate"/> (<c>1 source = exchangeRate target</c>).
    /// The optional annual estimate and every loaded occurrence amount are rounded
    /// to two decimal places away from zero. The owning Configuration command
    /// guarantees a positive rate with at most eight decimal places.
    /// </summary>
    internal void ConvertCurrency(int targetCurrencyId, decimal exchangeRate, UserId actorId, DateTimeOffset now)
    {
        EnsureUtc(now);
        if (targetCurrencyId <= 0)
        {
            throw new OpexValidationException("Catalog identifiers must be positive.");
        }

        if (exchangeRate <= 0)
        {
            throw new OpexValidationException("The exchange rate must be a positive value.");
        }

        if (EstimatedAnnualAmount is { } estimate)
        {
            EstimatedAnnualAmount = OpexValidation.ConvertAmount(estimate, exchangeRate);
        }

        foreach (var occurrence in occurrences)
        {
            occurrence.ConvertAmount(exchangeRate, actorId, now);
        }

        CurrencyId = targetCurrencyId;
        StampModification(actorId, now);
    }

    private void Apply(OpexContractValues values, UserId actorId, DateTimeOffset now, bool isCreation)
    {
        ArgumentNullException.ThrowIfNull(values);
        EnsureUtc(now);

        var name = OpexValidation.ValidateContractName(values.Name);
        var notes = OpexValidation.ValidateNotes(values.Notes);
        if (!Enum.IsDefined(values.MovementType)
            || !Enum.IsDefined(values.Status)
            || !Enum.IsDefined(values.ExpectedFrequency)
            || !Enum.IsDefined(values.Visibility))
        {
            throw new OpexValidationException(
                "Movement type, status, expected frequency, or visibility is invalid.");
        }

        if (values.CategoryId <= 0 || values.CurrencyId <= 0
            || values.SupplierId is <= 0 || values.CostCenterId is <= 0)
        {
            throw new OpexValidationException("Catalog identifiers must be positive.");
        }

        if (values.EstimatedAnnualAmount is { } estimate)
        {
            OpexValidation.ValidateUserAmount(estimate);
        }

        if (!isCreation && values.Visibility != Visibility && actorId.Value != CreatedBy)
        {
            throw new OpexValidationException(
                "Only the creator may change contract visibility.",
                OpexValidationReason.VisibilityForbidden);
        }

        Name = name;
        NormalizedName = OpexValidation.NormalizeContractName(name);
        MovementType = values.MovementType;
        Status = values.Status;
        StartDate = values.StartDate;
        ClosedDate = values.ClosedDate;
        EstimatedAnnualAmount = values.EstimatedAnnualAmount;
        ExpectedFrequency = values.ExpectedFrequency;
        CategoryId = values.CategoryId;
        SupplierId = values.SupplierId;
        CostCenterId = values.CostCenterId;
        CurrencyId = values.CurrencyId;
        Notes = notes;
        Visibility = values.Visibility;
        StampModification(actorId, now);
    }

    private void StampModification(UserId actorId, DateTimeOffset now)
    {
        UpdatedAt = now;
        UpdatedBy = actorId.Value;
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new OpexValidationException("Technical timestamps must use UTC.");
        }
    }
}

/// <summary>The editable fields of an Opex occurrence, independent of audit metadata.</summary>
internal sealed record OpexOccurrenceValues(
    DateOnly EffectiveDate,
    decimal ActualAmount,
    string? Description,
    string? Notes);

/// <summary>
/// An effective income or expense movement subordinate to exactly one contract.
/// It carries no independent movement type, currency, classification, or
/// visibility; access and inherited properties always follow the parent contract.
/// </summary>
internal sealed class OpexOccurrence
{
    private OpexOccurrence()
    {
    }

    public int Id { get; private set; }
    public int ContractId { get; private set; }
    public DateOnly EffectiveDate { get; private set; }
    public decimal ActualAmount { get; private set; }
    public string? Description { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public int CreatedBy { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public int UpdatedBy { get; private set; }

    public static OpexOccurrence Create(
        int contractId,
        OpexOccurrenceValues values,
        UserId creatorId,
        DateTimeOffset now)
    {
        EnsureUtc(now);
        if (contractId <= 0)
        {
            throw new OpexValidationException("An occurrence must belong to a contract.");
        }

        var occurrence = new OpexOccurrence
        {
            ContractId = contractId,
            CreatedAt = now,
            CreatedBy = creatorId.Value,
            UpdatedAt = now,
            UpdatedBy = creatorId.Value,
        };
        occurrence.Apply(values, creatorId, now);
        return occurrence;
    }

    public void Update(OpexOccurrenceValues values, UserId actorId, DateTimeOffset now)
    {
        Apply(values, actorId, now);
    }

    internal void ConvertAmount(decimal exchangeRate, UserId actorId, DateTimeOffset now)
    {
        EnsureUtc(now);
        ActualAmount = OpexValidation.ConvertAmount(ActualAmount, exchangeRate);
        StampModification(actorId, now);
    }

    private void Apply(OpexOccurrenceValues values, UserId actorId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(values);
        EnsureUtc(now);

        OpexValidation.ValidateUserAmount(values.ActualAmount);
        var description = OpexValidation.ValidateDescription(values.Description);
        var notes = OpexValidation.ValidateNotes(values.Notes);

        EffectiveDate = values.EffectiveDate;
        ActualAmount = values.ActualAmount;
        Description = description;
        Notes = notes;
        StampModification(actorId, now);
    }

    private void StampModification(UserId actorId, DateTimeOffset now)
    {
        UpdatedAt = now;
        UpdatedBy = actorId.Value;
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new OpexValidationException("Technical timestamps must use UTC.");
        }
    }
}
