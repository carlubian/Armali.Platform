namespace Segaris.Api.Modules.Travel.Contracts;

internal sealed record TravelTripTypeRequest(string? Name);

internal sealed record TravelTripTypeResponse(int Id, string Name, int SortOrder);

internal sealed record TravelExpenseCategoryRequest(string? Name);

internal sealed record TravelExpenseCategoryResponse(int Id, string Name, int SortOrder);
