using Segaris.Shared.Api;

namespace Segaris.Api.Modules.Health;

/// <summary>Stable machine-readable Health failures.</summary>
internal static class HealthErrorCodes
{
    public static readonly ErrorCode DiseaseNotFound = new("health.disease.not_found");
    public static readonly ErrorCode DiseaseValidation = new("health.disease.validation");
    public static readonly ErrorCode DiseaseVisibilityForbidden = new("health.disease.visibility_forbidden");

    public static readonly ErrorCode MedicineNotFound = new("health.medicine.not_found");
    public static readonly ErrorCode MedicineValidation = new("health.medicine.validation");
    public static readonly ErrorCode MedicineVisibilityForbidden = new("health.medicine.visibility_forbidden");
    public static readonly ErrorCode MedicineItemNotAccessible = new("health.medicine.item_not_accessible");
    public static readonly ErrorCode MedicineItemVisibilityForbidden = new("health.medicine.item_visibility_forbidden");

    public static readonly ErrorCode AssociationNotAccessible = new("health.association.not_accessible");
    public static readonly ErrorCode AssociationVisibilityForbidden = new("health.association.visibility_forbidden");
    public static readonly ErrorCode AssociationPublishBlocked = new("health.association.publish_blocked");

    public static readonly ErrorCode UnknownCatalogReference = new("health.catalog.unknown_reference");
    public static readonly ErrorCode AttachmentNotFound = new("health.attachment.not_found");
    public static readonly ErrorCode AttachmentInvalid = new("health.attachment.invalid");
    public static readonly ErrorCode AttachmentPrimaryInvalid = new("health.attachment.primary_invalid");

    public static readonly ErrorCode DiseaseCategoryNotFound = new("health.disease_category.not_found");
    public static readonly ErrorCode DiseaseCategoryValidation = new("health.disease_category.validation");
    public static readonly ErrorCode DiseaseCategoryDuplicateName = new("health.disease_category.duplicate_name");
    public static readonly ErrorCode DiseaseCategoryRequiredNotEmpty = new("health.disease_category.required_not_empty");
    public static readonly ErrorCode DiseaseCategoryReferenced = new("health.disease_category.referenced");
    public static readonly ErrorCode DiseaseCategoryInvalidReplacement = new("health.disease_category.invalid_replacement");
    public static readonly ErrorCode DiseaseCategoryMigrationConflict = new("health.disease_category.migration_conflict");

    public static readonly ErrorCode MedicineCategoryNotFound = new("health.medicine_category.not_found");
    public static readonly ErrorCode MedicineCategoryValidation = new("health.medicine_category.validation");
    public static readonly ErrorCode MedicineCategoryDuplicateName = new("health.medicine_category.duplicate_name");
    public static readonly ErrorCode MedicineCategoryRequiredNotEmpty = new("health.medicine_category.required_not_empty");
    public static readonly ErrorCode MedicineCategoryReferenced = new("health.medicine_category.referenced");
    public static readonly ErrorCode MedicineCategoryInvalidReplacement = new("health.medicine_category.invalid_replacement");
    public static readonly ErrorCode MedicineCategoryMigrationConflict = new("health.medicine_category.migration_conflict");
}
