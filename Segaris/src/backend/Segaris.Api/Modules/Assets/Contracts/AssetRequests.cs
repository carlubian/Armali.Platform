namespace Segaris.Api.Modules.Assets.Contracts;

/// <summary>
/// Frozen request contract for <c>POST /api/assets/items</c>. <see cref="Status"/>
/// and <see cref="Visibility"/> are the fixed string vocabularies (the enum member
/// names and the platform visibility names). <see cref="Code"/>, when present, is a
/// case-insensitively unique household reference. <see cref="AcquisitionDate"/> and
/// <see cref="ExpectedEndOfLifeDate"/> are optional civil dates with no past/future
/// boundary. Catalog references are the database-assigned integer identifiers.
/// </summary>
internal sealed record CreateAssetRequest(
    string? Name,
    int CategoryId,
    int LocationId,
    string? Status,
    string? Code,
    string? BrandModel,
    string? SerialNumber,
    DateOnly? AcquisitionDate,
    DateOnly? ExpectedEndOfLifeDate,
    string? Notes,
    string? Visibility);

/// <summary>
/// Frozen request contract for <c>PUT /api/assets/items/{assetId}</c>. The update
/// fully replaces the asset's editable fields in one transaction.
/// </summary>
internal sealed record UpdateAssetRequest(
    string? Name,
    int CategoryId,
    int LocationId,
    string? Status,
    string? Code,
    string? BrandModel,
    string? SerialNumber,
    DateOnly? AcquisitionDate,
    DateOnly? ExpectedEndOfLifeDate,
    string? Notes,
    string? Visibility);
