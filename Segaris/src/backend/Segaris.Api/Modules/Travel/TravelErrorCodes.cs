using Segaris.Shared.Api;

namespace Segaris.Api.Modules.Travel;

/// <summary>Stable machine-readable Travel failures.</summary>
internal static class TravelErrorCodes
{
    public static readonly ErrorCode TripNotFound = new("travel.trip.not_found");
    public static readonly ErrorCode TripValidation = new("travel.trip.validation");
    public static readonly ErrorCode TripVisibilityForbidden = new("travel.trip.visibility_forbidden");

    public static readonly ErrorCode ItineraryValidation = new("travel.itinerary.validation");

    public static readonly ErrorCode ExpenseNotFound = new("travel.expense.not_found");
    public static readonly ErrorCode ExpenseValidation = new("travel.expense.validation");

    public static readonly ErrorCode UnknownCatalogReference = new("travel.catalog.unknown_reference");

    public static readonly ErrorCode AttachmentNotFound = new("travel.attachment.not_found");
    public static readonly ErrorCode AttachmentInvalid = new("travel.attachment.invalid");

    public static readonly ErrorCode TripTypeNotFound = new("travel.trip_type.not_found");
    public static readonly ErrorCode TripTypeValidation = new("travel.trip_type.validation");
    public static readonly ErrorCode TripTypeDuplicateName = new("travel.trip_type.duplicate_name");
    public static readonly ErrorCode TripTypeRequiredNotEmpty = new("travel.trip_type.required_not_empty");
    public static readonly ErrorCode TripTypeReferenced = new("travel.trip_type.referenced");
    public static readonly ErrorCode TripTypeInvalidReplacement = new("travel.trip_type.invalid_replacement");
    public static readonly ErrorCode TripTypeMigrationConflict = new("travel.trip_type.migration_conflict");

    public static readonly ErrorCode ExpenseCategoryNotFound = new("travel.expense_category.not_found");
    public static readonly ErrorCode ExpenseCategoryValidation = new("travel.expense_category.validation");
    public static readonly ErrorCode ExpenseCategoryDuplicateName = new("travel.expense_category.duplicate_name");
    public static readonly ErrorCode ExpenseCategoryRequiredNotEmpty = new("travel.expense_category.required_not_empty");
    public static readonly ErrorCode ExpenseCategoryReferenced = new("travel.expense_category.referenced");
    public static readonly ErrorCode ExpenseCategoryInvalidReplacement = new("travel.expense_category.invalid_replacement");
    public static readonly ErrorCode ExpenseCategoryMigrationConflict = new("travel.expense_category.migration_conflict");
}
