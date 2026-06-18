namespace Segaris.Api.Modules.Clothes.Contracts;

internal sealed record CreateClothesGarmentRequest(
    string? Name,
    int CategoryId,
    string? Status,
    string? Size,
    IReadOnlyList<int> ColorIds,
    string? WashingCare,
    string? DryingCare,
    string? IroningCare,
    string? DryCleaningCare,
    string? Notes,
    string? Visibility);

internal sealed record UpdateClothesGarmentRequest(
    string? Name,
    int CategoryId,
    string? Status,
    string? Size,
    IReadOnlyList<int> ColorIds,
    string? WashingCare,
    string? DryingCare,
    string? IroningCare,
    string? DryCleaningCare,
    string? Notes,
    string? Visibility);
