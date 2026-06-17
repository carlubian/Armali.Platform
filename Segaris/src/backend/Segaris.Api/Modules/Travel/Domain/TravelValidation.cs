namespace Segaris.Api.Modules.Travel.Domain;

/// <summary>Domain limits shared by the Travel contracts and later waves.</summary>
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
}
