using Segaris.Api.Platform.Api;

namespace Segaris.Api.Modules.Configuration;

internal static class ConfigurationProblem
{
    public static ApiProblemException NotFound() => new(StatusCodes.Status404NotFound, ConfigurationErrorCodes.CatalogNotFound, "Catalog value not found.");
    public static ApiProblemException Validation(string field, string message) => new(StatusCodes.Status400BadRequest, ConfigurationErrorCodes.CatalogValidation, "Catalog validation failed.", errors: new Dictionary<string, string[]> { [field] = [message] });
    public static ApiProblemException DuplicateName() => new(StatusCodes.Status409Conflict, ConfigurationErrorCodes.CatalogDuplicateName, "Catalog name already exists.");
    public static ApiProblemException DuplicateCode() => new(StatusCodes.Status409Conflict, ConfigurationErrorCodes.CurrencyDuplicateCode, "Currency code already exists.");
    public static ApiProblemException InvalidCode() => new(StatusCodes.Status400BadRequest, ConfigurationErrorCodes.CurrencyInvalidCode, "Currency code is invalid.", errors: new Dictionary<string, string[]> { ["code"] = ["Code must contain exactly three letters."] });
    public static ApiProblemException CurrencyExchangeRateRequired() => new(StatusCodes.Status400BadRequest, ConfigurationErrorCodes.CurrencyExchangeRateRequired, "A non-EUR currency requires an exchange rate to EUR.", errors: new Dictionary<string, string[]> { ["exchangeRateToEur"] = ["An exchange rate to EUR is required."] });
    public static ApiProblemException CurrencyExchangeRateInvalid() => new(StatusCodes.Status400BadRequest, ConfigurationErrorCodes.CurrencyExchangeRateInvalid, "The exchange rate to EUR is invalid.", errors: new Dictionary<string, string[]> { ["exchangeRateToEur"] = ["The exchange rate must be positive with at most eight decimal places."] });
    public static ApiProblemException CurrencyExchangeRateNotOne() => new(StatusCodes.Status400BadRequest, ConfigurationErrorCodes.CurrencyExchangeRateNotOne, "The EUR exchange rate must be 1.", errors: new Dictionary<string, string[]> { ["exchangeRateToEur"] = ["The EUR exchange rate is fixed at 1."] });
    public static ApiProblemException RequiredNotEmpty() => new(StatusCodes.Status409Conflict, ConfigurationErrorCodes.CatalogRequiredNotEmpty, "The required catalog cannot be empty.");
    public static ApiProblemException Referenced() => new(StatusCodes.Status409Conflict, ConfigurationErrorCodes.CatalogReferenced, "The catalog value is referenced.");
    public static ApiProblemException InvalidReplacement() => new(StatusCodes.Status400BadRequest, ConfigurationErrorCodes.CatalogInvalidReplacement, "The replacement request is invalid.");
    public static ApiProblemException ExchangeRateRequired() => new(StatusCodes.Status400BadRequest, ConfigurationErrorCodes.CatalogExchangeRateRequired, "Deleting a referenced currency requires an exchange rate.", errors: new Dictionary<string, string[]> { ["exchangeRate"] = ["An exchange rate is required to convert existing entries."] });
    public static ApiProblemException ExchangeRateInvalid() => new(StatusCodes.Status400BadRequest, ConfigurationErrorCodes.CatalogExchangeRateInvalid, "The exchange rate is invalid.", errors: new Dictionary<string, string[]> { ["exchangeRate"] = ["The exchange rate must be positive with at most eight decimal places."] });
    public static ApiProblemException MigrationConflict() => new(StatusCodes.Status409Conflict, ConfigurationErrorCodes.CatalogMigrationConflict, "The catalog migration conflicted with a concurrent change.");
    public static ApiProblemException MigrationFailed() => new(StatusCodes.Status409Conflict, ConfigurationErrorCodes.CatalogMigrationFailed, "A catalog consumer could not migrate its references.");
    public static ApiProblemException ReplacementRequired() => new(StatusCodes.Status409Conflict, ConfigurationErrorCodes.CatalogReplacementRequired, "A catalog consumer requires a replacement value because its references cannot be cleared.");
}
