namespace Segaris.Api.Modules.Health.Contracts;

internal sealed record CreateDiseaseRequest(
    string? Name,
    int CategoryId,
    string? Symptoms,
    int? AverageDurationDays,
    string? Notes,
    string? Visibility);

internal sealed record UpdateDiseaseRequest(
    string? Name,
    int CategoryId,
    string? Symptoms,
    int? AverageDurationDays,
    string? Notes,
    string? Visibility);

internal sealed record CreateMedicineRequest(
    string? Name,
    int CategoryId,
    string? Posology,
    bool? RequiresPrescription,
    int? InventoryItemId,
    string? Notes,
    string? Visibility);

internal sealed record UpdateMedicineRequest(
    string? Name,
    int CategoryId,
    string? Posology,
    bool? RequiresPrescription,
    int? InventoryItemId,
    string? Notes,
    string? Visibility);
