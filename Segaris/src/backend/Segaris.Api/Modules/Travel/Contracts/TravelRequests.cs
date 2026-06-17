namespace Segaris.Api.Modules.Travel.Contracts;

internal sealed record TravelItineraryEntryRequest(
    DateOnly Date,
    TimeOnly? Time,
    string? Title,
    string? Place,
    string? ReservationLocator,
    string? Note);

internal sealed record CreateTravelTripRequest(
    string? Name,
    int TripTypeId,
    string? Destination,
    DateOnly StartDate,
    DateOnly EndDate,
    string? Status,
    string? Notes,
    string? Visibility,
    IReadOnlyList<TravelItineraryEntryRequest> Itinerary);

internal sealed record UpdateTravelTripRequest(
    string? Name,
    int TripTypeId,
    string? Destination,
    DateOnly StartDate,
    DateOnly EndDate,
    string? Status,
    string? Notes,
    string? Visibility,
    IReadOnlyList<TravelItineraryEntryRequest> Itinerary);

internal sealed record CreateTravelExpenseRequest(
    int ExpenseCategoryId,
    string? Description,
    DateOnly Date,
    decimal Amount,
    int CurrencyId,
    int? SupplierId,
    int? CostCenterId,
    string? Notes);

internal sealed record UpdateTravelExpenseRequest(
    int ExpenseCategoryId,
    string? Description,
    DateOnly Date,
    decimal Amount,
    int CurrencyId,
    int? SupplierId,
    int? CostCenterId,
    string? Notes);
