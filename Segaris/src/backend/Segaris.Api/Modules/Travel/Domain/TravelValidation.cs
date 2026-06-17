namespace Segaris.Api.Modules.Travel.Domain;

/// <summary>Domain limits and validation rules shared by the Travel contracts and waves.</summary>
internal static class TravelValidation
{
    public const int NameMaxLength = 200;
    public const int DestinationMaxLength = 200;
    public const int NotesMaxLength = 4000;
    public const int ItineraryTitleMaxLength = 200;
    public const int ItineraryPlaceMaxLength = 200;
    public const int ItineraryReservationLocatorMaxLength = 200;
    public const int ItineraryNoteMaxLength = 1000;
    public const int MinimumItineraryEntries = 0;
    public const int MaximumItineraryEntries = 100;
    public const int ExpenseDescriptionMaxLength = 200;
    public const int ExpenseNotesMaxLength = 4000;
    public const int CatalogNameMaxLength = 100;

    public static string ValidateTripName(string? value) =>
        ValidateRequiredText(value, NameMaxLength, "The trip name");

    public static string? ValidateDestination(string? value) =>
        ValidateOptionalText(value, DestinationMaxLength, "The destination");

    public static string? ValidateTripNotes(string? value) =>
        ValidateOptionalText(value, NotesMaxLength, "The notes");

    public static string ValidateItineraryTitle(string? value) =>
        ValidateRequiredText(value, ItineraryTitleMaxLength, "The itinerary title", TravelValidationReason.Itinerary);

    public static string? ValidateItineraryPlace(string? value) =>
        ValidateOptionalText(value, ItineraryPlaceMaxLength, "The itinerary place", TravelValidationReason.Itinerary);

    public static string? ValidateItineraryReservationLocator(string? value) =>
        ValidateOptionalText(value, ItineraryReservationLocatorMaxLength, "The reservation locator", TravelValidationReason.Itinerary);

    public static string? ValidateItineraryNote(string? value) =>
        ValidateOptionalText(value, ItineraryNoteMaxLength, "The itinerary note", TravelValidationReason.Itinerary);

    public static string ValidateExpenseDescription(string? value) =>
        ValidateRequiredText(value, ExpenseDescriptionMaxLength, "The expense description");

    public static string? ValidateExpenseNotes(string? value) =>
        ValidateOptionalText(value, ExpenseNotesMaxLength, "The notes");

    /// <summary>
    /// Validates a stored expense amount. The amount is zero or greater and has at
    /// most two decimal places.
    /// </summary>
    public static decimal ValidateExpenseAmount(decimal value)
    {
        if (value < 0 || decimal.Round(value, 2) != value)
        {
            throw new TravelValidationException(
                "The amount must be zero or greater and have at most two decimal places.");
        }

        return value;
    }

    private static string ValidateRequiredText(
        string? value,
        int maxLength,
        string label,
        TravelValidationReason reason = TravelValidationReason.Validation)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.Length > maxLength)
        {
            throw new TravelValidationException(
                $"{label} is required and may contain at most {maxLength} characters.",
                reason);
        }

        return trimmed;
    }

    private static string? ValidateOptionalText(
        string? value,
        int maxLength,
        string label,
        TravelValidationReason reason = TravelValidationReason.Validation)
    {
        if (value is null)
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new TravelValidationException(
                $"{label} may contain at most {maxLength} characters.",
                reason);
        }

        return trimmed.Length == 0 ? null : trimmed;
    }
}

/// <summary>
/// Distinguishes the Travel domain failures so the HTTP surface can map each one to
/// its frozen <see cref="TravelErrorCodes"/> value.
/// </summary>
internal enum TravelValidationReason
{
    /// <summary>A required string, length, enum, amount, or reference rule failed.</summary>
    Validation,

    /// <summary>The trip end date is before its start date.</summary>
    DateRange,

    /// <summary>An itinerary entry or the itinerary collection failed validation.</summary>
    Itinerary,

    /// <summary>A referenced catalog value does not exist.</summary>
    CatalogReference,

    /// <summary>The requested visibility transition is not allowed for the actor.</summary>
    VisibilityForbidden,
}

internal sealed class TravelValidationException(
    string message,
    TravelValidationReason reason = TravelValidationReason.Validation) : Exception(message)
{
    public TravelValidationReason Reason { get; } = reason;
}
