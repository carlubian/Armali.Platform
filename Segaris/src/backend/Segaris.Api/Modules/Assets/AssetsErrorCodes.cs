using Segaris.Shared.Api;

namespace Segaris.Api.Modules.Assets;

/// <summary>Stable machine-readable Assets failures.</summary>
internal static class AssetsErrorCodes
{
    public static readonly ErrorCode AssetNotFound = new("assets.asset.not_found");
    public static readonly ErrorCode AssetValidation = new("assets.asset.validation");
    public static readonly ErrorCode AssetVisibilityForbidden = new("assets.asset.visibility_forbidden");
    public static readonly ErrorCode AssetDuplicateCode = new("assets.asset.duplicate_code");

    public static readonly ErrorCode UnknownCatalogReference = new("assets.catalog.unknown_reference");

    public static readonly ErrorCode AttachmentNotFound = new("assets.attachment.not_found");
    public static readonly ErrorCode AttachmentInvalid = new("assets.attachment.invalid");
    public static readonly ErrorCode AttachmentPrimaryInvalid = new("assets.attachment.primary_invalid");

    public static readonly ErrorCode CategoryNotFound = new("assets.category.not_found");
    public static readonly ErrorCode CategoryValidation = new("assets.category.validation");
    public static readonly ErrorCode CategoryDuplicateName = new("assets.category.duplicate_name");
    public static readonly ErrorCode CategoryRequiredNotEmpty = new("assets.category.required_not_empty");
    public static readonly ErrorCode CategoryReferenced = new("assets.category.referenced");
    public static readonly ErrorCode CategoryInvalidReplacement = new("assets.category.invalid_replacement");
    public static readonly ErrorCode CategoryMigrationConflict = new("assets.category.migration_conflict");

    public static readonly ErrorCode LocationNotFound = new("assets.location.not_found");
    public static readonly ErrorCode LocationValidation = new("assets.location.validation");
    public static readonly ErrorCode LocationDuplicateName = new("assets.location.duplicate_name");
    public static readonly ErrorCode LocationRequiredNotEmpty = new("assets.location.required_not_empty");
    public static readonly ErrorCode LocationReferenced = new("assets.location.referenced");
    public static readonly ErrorCode LocationInvalidReplacement = new("assets.location.invalid_replacement");
    public static readonly ErrorCode LocationMigrationConflict = new("assets.location.migration_conflict");
}
