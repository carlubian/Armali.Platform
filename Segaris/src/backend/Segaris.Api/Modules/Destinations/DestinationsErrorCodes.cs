using Segaris.Shared.Api;

namespace Segaris.Api.Modules.Destinations;

/// <summary>Stable machine-readable Destinations failures.</summary>
internal static class DestinationsErrorCodes
{
    public static readonly ErrorCode DestinationNotFound = new("destinations.destination.not_found");
    public static readonly ErrorCode DestinationValidation = new("destinations.destination.validation");
    public static readonly ErrorCode DestinationVisibilityForbidden = new("destinations.destination.visibility_forbidden");

    public static readonly ErrorCode PlaceNotFound = new("destinations.place.not_found");
    public static readonly ErrorCode PlaceValidation = new("destinations.place.validation");

    public static readonly ErrorCode UnknownCatalogReference = new("destinations.catalog.unknown_reference");

    public static readonly ErrorCode AttachmentNotFound = new("destinations.attachment.not_found");
    public static readonly ErrorCode AttachmentInvalid = new("destinations.attachment.invalid");
    public static readonly ErrorCode AttachmentPrimaryInvalid = new("destinations.attachment.primary_invalid");

    public static readonly ErrorCode CategoryNotFound = new("destinations.category.not_found");
    public static readonly ErrorCode CategoryValidation = new("destinations.category.validation");
    public static readonly ErrorCode CategoryDuplicateName = new("destinations.category.duplicate_name");
    public static readonly ErrorCode CategoryRequiredNotEmpty = new("destinations.category.required_not_empty");
    public static readonly ErrorCode CategoryReferenced = new("destinations.category.referenced");
    public static readonly ErrorCode CategoryInvalidReplacement = new("destinations.category.invalid_replacement");
    public static readonly ErrorCode CategoryMigrationConflict = new("destinations.category.migration_conflict");

    public static readonly ErrorCode PlaceCategoryNotFound = new("destinations.place_category.not_found");
    public static readonly ErrorCode PlaceCategoryValidation = new("destinations.place_category.validation");
    public static readonly ErrorCode PlaceCategoryDuplicateName = new("destinations.place_category.duplicate_name");
    public static readonly ErrorCode PlaceCategoryRequiredNotEmpty = new("destinations.place_category.required_not_empty");
    public static readonly ErrorCode PlaceCategoryReferenced = new("destinations.place_category.referenced");
    public static readonly ErrorCode PlaceCategoryInvalidReplacement = new("destinations.place_category.invalid_replacement");
    public static readonly ErrorCode PlaceCategoryMigrationConflict = new("destinations.place_category.migration_conflict");
}
