namespace Segaris.Api.Modules.Opex.Contracts;

internal sealed record OpexContractSummaryResponse(
    int Id,
    string Name,
    string MovementType,
    string Status,
    int CategoryId,
    string CategoryName,
    int? SupplierId,
    string? SupplierName,
    int? CostCenterId,
    string? CostCenterName,
    int CurrencyId,
    string CurrencyCode,
    string ExpectedFrequency,
    decimal? EstimatedAnnualAmount,
    decimal RealizedCurrentYearAmount,
    string Visibility,
    int CreatorId,
    string CreatorName);

internal sealed record OpexAttachmentResponse(
    string Id,
    string FileName,
    string ContentType,
    long Size,
    int CreatedById,
    DateTimeOffset CreatedAt);

internal sealed record OpexContractResponse(
    int Id,
    string Name,
    string MovementType,
    string Status,
    DateOnly? StartDate,
    DateOnly? ClosedDate,
    decimal? EstimatedAnnualAmount,
    string ExpectedFrequency,
    int CategoryId,
    string CategoryName,
    int? SupplierId,
    string? SupplierName,
    int? CostCenterId,
    string? CostCenterName,
    int CurrencyId,
    string CurrencyCode,
    string? Notes,
    string Visibility,
    IReadOnlyList<OpexAttachmentResponse> Attachments,
    int CreatedById,
    string CreatedByName,
    DateTimeOffset CreatedAt,
    int? UpdatedById,
    string? UpdatedByName,
    DateTimeOffset UpdatedAt);

internal sealed record OpexOccurrenceSummaryResponse(
    int Id,
    DateOnly EffectiveDate,
    decimal ActualAmount,
    string? Description);

internal sealed record OpexOccurrenceResponse(
    int Id,
    int ContractId,
    DateOnly EffectiveDate,
    decimal ActualAmount,
    string? Description,
    string? Notes,
    IReadOnlyList<OpexAttachmentResponse> Attachments,
    int CreatedById,
    string CreatedByName,
    DateTimeOffset CreatedAt,
    int? UpdatedById,
    string? UpdatedByName,
    DateTimeOffset UpdatedAt);
