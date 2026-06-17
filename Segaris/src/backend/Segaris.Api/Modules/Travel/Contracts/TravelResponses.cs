namespace Segaris.Api.Modules.Travel.Contracts;

internal sealed record TravelAttachmentResponse(
    string Id,
    string FileName,
    string ContentType,
    long Size,
    int CreatedById,
    DateTimeOffset CreatedAt);

internal sealed record TravelTripSummaryResponse(
    int Id,
    string Name,
    int TripTypeId,
    string TripTypeName,
    string? Destination,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status,
    string Visibility,
    int CreatorId,
    string CreatorName);

internal sealed record TravelItineraryEntryResponse(
    int Id,
    DateOnly Date,
    TimeOnly? Time,
    string Title,
    string? Place,
    string? ReservationLocator,
    string? Note,
    int SortOrder);

internal sealed record TravelExpenseTotalResponse(
    int CurrencyId,
    string CurrencyCode,
    decimal Amount);

internal sealed record TravelTripResponse(
    int Id,
    string Name,
    int TripTypeId,
    string TripTypeName,
    string? Destination,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status,
    string? Notes,
    string Visibility,
    IReadOnlyList<TravelItineraryEntryResponse> Itinerary,
    IReadOnlyList<TravelExpenseTotalResponse> ExpenseTotals,
    IReadOnlyList<TravelAttachmentResponse> Attachments,
    int CreatedById,
    string CreatedByName,
    DateTimeOffset CreatedAt,
    int? UpdatedById,
    string? UpdatedByName,
    DateTimeOffset? UpdatedAt);

internal sealed record TravelExpenseSummaryResponse(
    int Id,
    int ExpenseCategoryId,
    string ExpenseCategoryName,
    string Description,
    DateOnly Date,
    decimal Amount,
    int CurrencyId,
    string CurrencyCode,
    int? SupplierId,
    string? SupplierName,
    int? CostCenterId,
    string? CostCenterName);

internal sealed record TravelExpenseResponse(
    int Id,
    int ExpenseCategoryId,
    string ExpenseCategoryName,
    string Description,
    DateOnly Date,
    decimal Amount,
    int CurrencyId,
    string CurrencyCode,
    int? SupplierId,
    string? SupplierName,
    int? CostCenterId,
    string? CostCenterName,
    string? Notes,
    IReadOnlyList<TravelAttachmentResponse> Attachments,
    int CreatedById,
    string CreatedByName,
    DateTimeOffset CreatedAt,
    int? UpdatedById,
    string? UpdatedByName,
    DateTimeOffset? UpdatedAt);
