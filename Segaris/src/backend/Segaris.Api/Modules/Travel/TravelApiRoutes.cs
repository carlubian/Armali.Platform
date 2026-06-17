namespace Segaris.Api.Modules.Travel;

/// <summary>Frozen route shapes for the Travel HTTP surface.</summary>
internal static class TravelApiRoutes
{
    public const string Tag = "Travel";

    public const string Trips = "travel/trips";
    public const string TripById = "/{tripId:int}";
    public const string TripAttachments = "/{tripId:int}/attachments";
    public const string TripAttachmentById = "/{tripId:int}/attachments/{attachmentId}";

    public const string TripExpenses = "/{tripId:int}/expenses";
    public const string TripExpenseById = "/{tripId:int}/expenses/{expenseId:int}";
    public const string TripExpenseAttachments = "/{tripId:int}/expenses/{expenseId:int}/attachments";
    public const string TripExpenseAttachmentById = "/{tripId:int}/expenses/{expenseId:int}/attachments/{attachmentId}";

    public const string TripTypes = "travel/trip-types";
    public const string TripTypeById = "/{tripTypeId:int}";
    public const string TripTypeMove = "/{tripTypeId:int}/move";
    public const string TripTypeDeletionImpact = "/{tripTypeId:int}/deletion-impact";
    public const string TripTypeReplaceAndDelete = "/{tripTypeId:int}/replace-and-delete";

    public const string ExpenseCategories = "travel/expense-categories";
    public const string ExpenseCategoryById = "/{expenseCategoryId:int}";
    public const string ExpenseCategoryMove = "/{expenseCategoryId:int}/move";
    public const string ExpenseCategoryDeletionImpact = "/{expenseCategoryId:int}/deletion-impact";
    public const string ExpenseCategoryReplaceAndDelete = "/{expenseCategoryId:int}/replace-and-delete";
}
