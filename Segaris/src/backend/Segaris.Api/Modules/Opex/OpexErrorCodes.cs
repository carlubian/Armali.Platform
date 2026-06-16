using Segaris.Shared.Api;

namespace Segaris.Api.Modules.Opex;

/// <summary>Stable machine-readable Opex failures.</summary>
internal static class OpexErrorCodes
{
    public static readonly ErrorCode ContractNotFound = new("opex.contract.not_found");
    public static readonly ErrorCode ContractValidation = new("opex.contract.validation");
    public static readonly ErrorCode ContractDuplicateName = new("opex.contract.duplicate_name");
    public static readonly ErrorCode OccurrenceNotFound = new("opex.occurrence.not_found");
    public static readonly ErrorCode OccurrenceValidation = new("opex.occurrence.validation");
    public static readonly ErrorCode UnknownCatalogReference = new("opex.catalog.unknown_reference");
    public static readonly ErrorCode VisibilityForbidden = new("opex.contract.visibility_forbidden");
    public static readonly ErrorCode AttachmentNotFound = new("opex.attachment.not_found");
    public static readonly ErrorCode AttachmentInvalid = new("opex.attachment.invalid");
    public static readonly ErrorCode CategoryNotFound = new("opex.category.not_found");
    public static readonly ErrorCode CategoryValidation = new("opex.category.validation");
    public static readonly ErrorCode CategoryDuplicateName = new("opex.category.duplicate_name");
    public static readonly ErrorCode CategoryRequiredNotEmpty = new("opex.category.required_not_empty");
    public static readonly ErrorCode CategoryReferenced = new("opex.category.referenced");
    public static readonly ErrorCode CategoryInvalidReplacement = new("opex.category.invalid_replacement");
    public static readonly ErrorCode CategoryMigrationConflict = new("opex.category.migration_conflict");
}
