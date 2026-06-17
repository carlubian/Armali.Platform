using Segaris.Api.Modules.Travel.Domain;
using Segaris.Api.Platform.Api;

namespace Segaris.Api.Modules.Travel;

internal static class TravelExpenseProblem
{
    public static ApiProblemException From(TravelValidationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception.Reason switch
        {
            TravelValidationReason.CatalogReference => new ApiProblemException(
                StatusCodes.Status400BadRequest,
                TravelErrorCodes.UnknownCatalogReference,
                exception.Message),
            _ => new ApiProblemException(
                StatusCodes.Status400BadRequest,
                TravelErrorCodes.ExpenseValidation,
                exception.Message),
        };
    }

    public static ApiProblemException NotFound() => new(
        StatusCodes.Status404NotFound,
        TravelErrorCodes.ExpenseNotFound,
        "Expense not found.");
}
