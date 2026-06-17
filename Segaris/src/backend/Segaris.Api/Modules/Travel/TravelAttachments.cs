using System.Globalization;
using Segaris.Shared.Attachments;

namespace Segaris.Api.Modules.Travel;

/// <summary>Attachment owner identifiers published by the Travel module.</summary>
internal static class TravelAttachments
{
    public const string Module = "Travel";
    public const string TripEntityType = "Trip";
    public const string ExpenseEntityType = "Expense";

    public static AttachmentOwner TripOwner(int tripId) =>
        new(Module, TripEntityType, tripId.ToString(CultureInfo.InvariantCulture));

    public static AttachmentOwner ExpenseOwner(int expenseId) =>
        new(Module, ExpenseEntityType, expenseId.ToString(CultureInfo.InvariantCulture));
}
