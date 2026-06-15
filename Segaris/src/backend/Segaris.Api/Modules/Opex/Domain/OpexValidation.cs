namespace Segaris.Api.Modules.Opex.Domain;

/// <summary>Domain limits and normalization rules shared by later Opex Waves.</summary>
internal static class OpexValidation
{
    public const int ContractNameMaximumLength = 200;
    public const int CategoryNameMaximumLength = 100;
    public const int DescriptionMaximumLength = 300;
    public const int NotesMaximumLength = 4000;

    public static string NormalizeContractName(string? value) =>
        (value ?? string.Empty).Trim().ToUpperInvariant();

    public static string ValidateContractName(string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.Length > ContractNameMaximumLength)
        {
            throw new OpexValidationException(
                $"Name is required and may contain at most {ContractNameMaximumLength} characters.");
        }

        return trimmed;
    }

    public static string? ValidateDescription(string? value)
    {
        if (value is null)
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > DescriptionMaximumLength)
        {
            throw new OpexValidationException(
                $"Description may contain at most {DescriptionMaximumLength} characters.");
        }

        return trimmed.Length == 0 ? null : trimmed;
    }

    public static string? ValidateNotes(string? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value.Length > NotesMaximumLength)
        {
            throw new OpexValidationException(
                $"Notes may contain at most {NotesMaximumLength} characters.");
        }

        return value.Length == 0 ? null : value;
    }

    public static decimal ValidateUserAmount(decimal value)
    {
        if (value < 0 || decimal.Round(value, 2) != value)
        {
            throw new OpexValidationException(
                "Amounts must be nonnegative and have at most two decimal places.");
        }

        return value;
    }

    public static decimal ConvertAmount(decimal value, decimal exchangeRate)
    {
        ValidateUserAmount(value);
        if (exchangeRate <= 0 || decimal.Round(exchangeRate, 8) != exchangeRate)
        {
            throw new OpexValidationException(
                "Exchange rates must be positive and have at most eight decimal places.");
        }

        return decimal.Round(value * exchangeRate, 2, MidpointRounding.AwayFromZero);
    }
}

/// <summary>
/// Distinguishes the Opex mutation failures so the HTTP surface can map each one
/// to its frozen <see cref="OpexErrorCodes"/> value.
/// </summary>
internal enum OpexValidationReason
{
    /// <summary>A required string, length, enum, or amount rule failed.</summary>
    Validation,

    /// <summary>A referenced category, supplier, cost center, or currency does not exist.</summary>
    CatalogReference,

    /// <summary>A non-creator attempted to change a contract's visibility.</summary>
    VisibilityForbidden,
}

internal sealed class OpexValidationException(
    string message,
    OpexValidationReason reason = OpexValidationReason.Validation) : Exception(message)
{
    public OpexValidationReason Reason { get; } = reason;
}
