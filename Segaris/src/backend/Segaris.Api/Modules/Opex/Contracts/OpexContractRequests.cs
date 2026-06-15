namespace Segaris.Api.Modules.Opex.Contracts;

/// <summary>Body for creating an Opex contract.</summary>
internal sealed record CreateOpexContractRequest(
    string? Name,
    string? MovementType,
    string? Status,
    DateOnly? StartDate,
    DateOnly? ClosedDate,
    decimal? EstimatedAnnualAmount,
    string? ExpectedFrequency,
    int CategoryId,
    int? SupplierId,
    int? CostCenterId,
    int CurrencyId,
    string? Notes,
    string? Visibility);

/// <summary>Body for fully updating an Opex contract.</summary>
internal sealed record UpdateOpexContractRequest(
    string? Name,
    string? MovementType,
    string? Status,
    DateOnly? StartDate,
    DateOnly? ClosedDate,
    decimal? EstimatedAnnualAmount,
    string? ExpectedFrequency,
    int CategoryId,
    int? SupplierId,
    int? CostCenterId,
    int CurrencyId,
    string? Notes,
    string? Visibility);
