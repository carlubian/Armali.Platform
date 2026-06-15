using Segaris.Api.Platform.Api;

namespace Segaris.Api.Modules.Configuration;

internal static class ConfigurationProblem
{
    public static ApiProblemException NotFound() => new(StatusCodes.Status404NotFound, ConfigurationErrorCodes.CatalogNotFound, "Catalog value not found.");
    public static ApiProblemException Validation(string field, string message) => new(StatusCodes.Status400BadRequest, ConfigurationErrorCodes.CatalogValidation, "Catalog validation failed.", errors: new Dictionary<string, string[]> { [field] = [message] });
    public static ApiProblemException DuplicateName() => new(StatusCodes.Status409Conflict, ConfigurationErrorCodes.CatalogDuplicateName, "Catalog name already exists.");
    public static ApiProblemException DuplicateCode() => new(StatusCodes.Status409Conflict, ConfigurationErrorCodes.CurrencyDuplicateCode, "Currency code already exists.");
    public static ApiProblemException InvalidCode() => new(StatusCodes.Status400BadRequest, ConfigurationErrorCodes.CurrencyInvalidCode, "Currency code is invalid.", errors: new Dictionary<string, string[]> { ["code"] = ["Code must contain exactly three letters."] });
    public static ApiProblemException RequiredNotEmpty() => new(StatusCodes.Status409Conflict, ConfigurationErrorCodes.CatalogRequiredNotEmpty, "The required catalog cannot be empty.");
    public static ApiProblemException Referenced() => new(StatusCodes.Status409Conflict, ConfigurationErrorCodes.CatalogReferenced, "The catalog value is referenced.");
}
