namespace Segaris.Api.Modules.Destinations.Contracts;

internal sealed record CreateDestinationRequest(
    string? Name,
    int CategoryId,
    string? Country,
    string? EntryRequirements,
    bool IsSchengenArea,
    string? Notes,
    string? Visibility);

internal sealed record UpdateDestinationRequest(
    string? Name,
    int CategoryId,
    string? Country,
    string? EntryRequirements,
    bool IsSchengenArea,
    string? Notes,
    string? Visibility);

internal sealed record CreatePlaceRequest(
    string Name,
    int CategoryId,
    int? Rating,
    string? Review,
    string? Address);

internal sealed record DestinationCategoryRequest(string Name);

internal sealed record PlaceCategoryRequest(string Name);
