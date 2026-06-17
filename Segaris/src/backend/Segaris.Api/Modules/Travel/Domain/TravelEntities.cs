using Segaris.Shared.Authorization;
using Segaris.Shared.Identity;

namespace Segaris.Api.Modules.Travel.Domain;

/// <summary>
/// A Travel-owned catalog row (trip type or expense category). It mirrors the
/// shared-catalog shape (display name, normalized name for case-insensitive
/// uniqueness, declaration order, and audit metadata) while remaining owned by
/// Travel and surfaced through Configuration.
/// </summary>
internal sealed class TravelTripType
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

/// <summary>The Travel-owned expense-category catalog row. See <see cref="TravelTripType"/>.</summary>
internal sealed class TravelExpenseCategory
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

/// <summary>A single itinerary entry in a create or update operation, independent of identity.</summary>
internal sealed record TravelItineraryEntryValues(
    DateOnly Date,
    TimeOnly? Time,
    string Title,
    string? Place,
    string? ReservationLocator,
    string? Note);

/// <summary>The editable fields of a trip, independent of audit metadata.</summary>
internal sealed record TravelTripValues(
    string Name,
    int TripTypeId,
    string? Destination,
    DateOnly StartDate,
    DateOnly EndDate,
    TravelTripStatus Status,
    string? Notes,
    RecordVisibility Visibility,
    IReadOnlyList<TravelItineraryEntryValues> Itinerary);

/// <summary>
/// A household trip. The trip is the only top-level Travel entity: it owns a light,
/// embedded itinerary through full-collection replacement and is the privacy and
/// authorization root for its itinerary and its expenses. It stores its type,
/// destination, civil start and end dates, manual status, visibility, and notes.
/// There is no normalized trip total; per-currency totals are computed from the
/// expenses at read time.
/// </summary>
internal sealed class TravelTrip
{
    private readonly List<TravelItineraryEntry> itinerary = [];

    private TravelTrip()
    {
    }

    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int TripTypeId { get; private set; }
    public string? Destination { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public TravelTripStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public RecordVisibility Visibility { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public int CreatedBy { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public int? UpdatedBy { get; private set; }
    public IReadOnlyList<TravelItineraryEntry> Itinerary => itinerary;

    public static TravelTrip Create(TravelTripValues values, UserId creatorId, DateTimeOffset now)
    {
        EnsureUtc(now);
        var trip = new TravelTrip
        {
            CreatedAt = now,
            CreatedBy = creatorId.Value,
        };
        trip.Apply(values);
        return trip;
    }

    public void Update(TravelTripValues values, UserId actorId, DateTimeOffset now)
    {
        EnsureUtc(now);
        Apply(values);
        StampModification(actorId, now);
    }

    /// <summary>
    /// Re-points the trip to <paramref name="tripTypeId"/> during a Travel trip-type
    /// migration. The trip type is required, so it is replaced rather than cleared.
    /// </summary>
    internal void ReplaceTripType(int tripTypeId, UserId actorId, DateTimeOffset now)
    {
        EnsureUtc(now);
        if (tripTypeId <= 0)
        {
            throw new TravelValidationException("Catalog identifiers must be positive.");
        }

        TripTypeId = tripTypeId;
        StampModification(actorId, now);
    }

    private void Apply(TravelTripValues values)
    {
        ArgumentNullException.ThrowIfNull(values);

        var name = TravelValidation.ValidateTripName(values.Name);
        var destination = TravelValidation.ValidateDestination(values.Destination);
        var notes = TravelValidation.ValidateTripNotes(values.Notes);
        if (!Enum.IsDefined(values.Status) || !Enum.IsDefined(values.Visibility))
        {
            throw new TravelValidationException("Status or visibility is invalid.");
        }

        if (values.TripTypeId <= 0)
        {
            throw new TravelValidationException("Catalog identifiers must be positive.");
        }

        if (values.EndDate < values.StartDate)
        {
            throw new TravelValidationException(
                "The end date may not be before the start date.",
                TravelValidationReason.DateRange);
        }

        ReplaceItinerary(values.Itinerary);

        Name = name;
        TripTypeId = values.TripTypeId;
        Destination = destination;
        StartDate = values.StartDate;
        EndDate = values.EndDate;
        Status = values.Status;
        Notes = notes;
        Visibility = values.Visibility;
    }

    /// <summary>
    /// Replaces the whole itinerary. Entries keep the supplied order as a stable
    /// insertion sequence (<see cref="TravelItineraryEntry.SortOrder"/>), which is the
    /// final tie-breaker after civil date and time-of-day when the itinerary is read.
    /// </summary>
    private void ReplaceItinerary(IReadOnlyList<TravelItineraryEntryValues> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        if (entries.Count < TravelValidation.MinimumItineraryEntries
            || entries.Count > TravelValidation.MaximumItineraryEntries)
        {
            throw new TravelValidationException(
                $"A trip must contain between {TravelValidation.MinimumItineraryEntries} and "
                + $"{TravelValidation.MaximumItineraryEntries} itinerary entries.",
                TravelValidationReason.Itinerary);
        }

        itinerary.Clear();
        for (var index = 0; index < entries.Count; index++)
        {
            itinerary.Add(TravelItineraryEntry.Create(entries[index], index));
        }
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
            throw new TravelValidationException("Technical timestamps must use UTC.");
        }
    }
}

/// <summary>
/// A single embedded itinerary entry subordinate to exactly one trip. It carries no
/// independent audit metadata, visibility, or attachments and inherits the
/// authorization of its owning trip. <see cref="SortOrder"/> is the stable insertion
/// sequence that keeps entries deterministic when they share a date and time.
/// </summary>
internal sealed class TravelItineraryEntry
{
    private TravelItineraryEntry()
    {
    }

    public int Id { get; private set; }
    public int TripId { get; private set; }
    public DateOnly Date { get; private set; }
    public TimeOnly? Time { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Place { get; private set; }
    public string? ReservationLocator { get; private set; }
    public string? Note { get; private set; }
    public int SortOrder { get; private set; }

    internal static TravelItineraryEntry Create(TravelItineraryEntryValues values, int sortOrder)
    {
        ArgumentNullException.ThrowIfNull(values);
        return new TravelItineraryEntry
        {
            Date = values.Date,
            Time = values.Time,
            Title = TravelValidation.ValidateItineraryTitle(values.Title),
            Place = TravelValidation.ValidateItineraryPlace(values.Place),
            ReservationLocator = TravelValidation.ValidateItineraryReservationLocator(values.ReservationLocator),
            Note = TravelValidation.ValidateItineraryNote(values.Note),
            SortOrder = sortOrder,
        };
    }
}

/// <summary>The editable fields of an expense, independent of audit metadata.</summary>
internal sealed record TravelExpenseValues(
    int ExpenseCategoryId,
    string Description,
    DateOnly Date,
    decimal Amount,
    int CurrencyId,
    int? SupplierId,
    int? CostCenterId,
    string? Notes);

/// <summary>
/// A travel expense subordinate to exactly one trip. Unlike the embedded itinerary,
/// an expense is created, edited, and deleted individually and may carry its own
/// attachments. Currency is required and belongs to the expense, not the trip;
/// supplier and cost centre are optional shared-catalog references. The expense
/// inherits the visibility and authorization of its owning trip.
/// </summary>
internal sealed class TravelExpense
{
    private TravelExpense()
    {
    }

    public int Id { get; private set; }
    public int TripId { get; private set; }
    public int ExpenseCategoryId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public DateOnly Date { get; private set; }
    public decimal Amount { get; private set; }
    public int CurrencyId { get; private set; }
    public int? SupplierId { get; private set; }
    public int? CostCenterId { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public int CreatedBy { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public int? UpdatedBy { get; private set; }

    public static TravelExpense Create(int tripId, TravelExpenseValues values, UserId creatorId, DateTimeOffset now)
    {
        EnsureUtc(now);
        if (tripId <= 0)
        {
            throw new TravelValidationException("An expense must belong to a trip.");
        }

        var expense = new TravelExpense
        {
            TripId = tripId,
            CreatedAt = now,
            CreatedBy = creatorId.Value,
        };
        expense.Apply(values);
        return expense;
    }

    public void Update(TravelExpenseValues values, UserId actorId, DateTimeOffset now)
    {
        EnsureUtc(now);
        Apply(values);
        StampModification(actorId, now);
    }

    /// <summary>
    /// Re-points the expense to <paramref name="expenseCategoryId"/> during a Travel
    /// expense-category migration. The category is required, so it is replaced rather
    /// than cleared.
    /// </summary>
    internal void ReplaceExpenseCategory(int expenseCategoryId, UserId actorId, DateTimeOffset now)
    {
        EnsureUtc(now);
        if (expenseCategoryId <= 0)
        {
            throw new TravelValidationException("Catalog identifiers must be positive.");
        }

        ExpenseCategoryId = expenseCategoryId;
        StampModification(actorId, now);
    }

    internal void ReplaceSupplier(int? supplierId, UserId actorId, DateTimeOffset now)
    {
        EnsureUtc(now);
        if (supplierId is <= 0)
        {
            throw new TravelValidationException("Catalog identifiers must be positive.");
        }

        SupplierId = supplierId;
        StampModification(actorId, now);
    }

    internal void ReplaceCostCenter(int? costCenterId, UserId actorId, DateTimeOffset now)
    {
        EnsureUtc(now);
        if (costCenterId is <= 0)
        {
            throw new TravelValidationException("Catalog identifiers must be positive.");
        }

        CostCenterId = costCenterId;
        StampModification(actorId, now);
    }

    internal void ConvertCurrency(int targetCurrencyId, decimal exchangeRate, UserId actorId, DateTimeOffset now)
    {
        EnsureUtc(now);
        if (targetCurrencyId <= 0)
        {
            throw new TravelValidationException("Catalog identifiers must be positive.");
        }

        if (exchangeRate <= 0)
        {
            throw new TravelValidationException("The exchange rate must be a positive value.");
        }

        Amount = decimal.Round(Amount * exchangeRate, 2, MidpointRounding.AwayFromZero);
        CurrencyId = targetCurrencyId;
        StampModification(actorId, now);
    }

    private void Apply(TravelExpenseValues values)
    {
        ArgumentNullException.ThrowIfNull(values);

        var description = TravelValidation.ValidateExpenseDescription(values.Description);
        var notes = TravelValidation.ValidateExpenseNotes(values.Notes);
        if (values.ExpenseCategoryId <= 0 || values.CurrencyId <= 0)
        {
            throw new TravelValidationException("Catalog identifiers must be positive.");
        }

        if (values.SupplierId is { } supplierId && supplierId <= 0)
        {
            throw new TravelValidationException("Catalog identifiers must be positive.");
        }

        if (values.CostCenterId is { } costCenterId && costCenterId <= 0)
        {
            throw new TravelValidationException("Catalog identifiers must be positive.");
        }

        var amount = TravelValidation.ValidateExpenseAmount(values.Amount);

        ExpenseCategoryId = values.ExpenseCategoryId;
        Description = description;
        Date = values.Date;
        Amount = amount;
        CurrencyId = values.CurrencyId;
        SupplierId = values.SupplierId;
        CostCenterId = values.CostCenterId;
        Notes = notes;
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
            throw new TravelValidationException("Technical timestamps must use UTC.");
        }
    }
}
