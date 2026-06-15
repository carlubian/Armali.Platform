namespace Segaris.Api.Modules.Opex.Contracts;

internal sealed record CreateOpexOccurrenceRequest(
    DateOnly? EffectiveDate,
    decimal ActualAmount,
    string? Description,
    string? Notes);

internal sealed record UpdateOpexOccurrenceRequest(
    DateOnly? EffectiveDate,
    decimal ActualAmount,
    string? Description,
    string? Notes);
