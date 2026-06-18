using Segaris.Shared.Api;

namespace Segaris.Api.Modules.Clothes;

/// <summary>Stable machine-readable Clothes failures.</summary>
internal static class ClothesErrorCodes
{
    public static readonly ErrorCode GarmentNotFound = new("clothes.garment.not_found");
    public static readonly ErrorCode GarmentValidation = new("clothes.garment.validation");
    public static readonly ErrorCode GarmentVisibilityForbidden = new("clothes.garment.visibility_forbidden");

    public static readonly ErrorCode UnknownCatalogReference = new("clothes.catalog.unknown_reference");

    public static readonly ErrorCode AttachmentNotFound = new("clothes.attachment.not_found");
    public static readonly ErrorCode AttachmentInvalid = new("clothes.attachment.invalid");
    public static readonly ErrorCode AttachmentPrimaryInvalid = new("clothes.attachment.primary_invalid");

    public static readonly ErrorCode CategoryNotFound = new("clothes.category.not_found");
    public static readonly ErrorCode CategoryValidation = new("clothes.category.validation");
    public static readonly ErrorCode CategoryDuplicateName = new("clothes.category.duplicate_name");
    public static readonly ErrorCode CategoryRequiredNotEmpty = new("clothes.category.required_not_empty");
    public static readonly ErrorCode CategoryReferenced = new("clothes.category.referenced");
    public static readonly ErrorCode CategoryInvalidReplacement = new("clothes.category.invalid_replacement");
    public static readonly ErrorCode CategoryMigrationConflict = new("clothes.category.migration_conflict");

    public static readonly ErrorCode ColorNotFound = new("clothes.color.not_found");
    public static readonly ErrorCode ColorValidation = new("clothes.color.validation");
    public static readonly ErrorCode ColorDuplicateName = new("clothes.color.duplicate_name");
    public static readonly ErrorCode ColorRequiredNotEmpty = new("clothes.color.required_not_empty");
    public static readonly ErrorCode ColorReferenced = new("clothes.color.referenced");
    public static readonly ErrorCode ColorInvalidReplacement = new("clothes.color.invalid_replacement");
    public static readonly ErrorCode ColorMigrationConflict = new("clothes.color.migration_conflict");
}
