namespace Segaris.Api.Modules.Configuration;

/// <summary>
/// Normalization rules shared by the Configuration catalogs. Normalization trims
/// exterior whitespace and folds to an invariant upper-case form so uniqueness is
/// case-insensitive. It deliberately does not collapse interior whitespace, so
/// "El  Corte" and "El Corte" remain distinct values.
/// </summary>
internal static class CatalogNormalization
{
    /// <summary>The persisted maximum length of a catalog name.</summary>
    public const int NameMaximumLength = 100;

    /// <summary>The persisted fixed length of a currency code.</summary>
    public const int CurrencyCodeLength = 3;

    /// <summary>The maximum number of decimal places allowed on an exchange rate to EUR.</summary>
    public const int ExchangeRateDecimalPlaces = 8;

    public static string Normalize(string value) =>
        (value ?? string.Empty).Trim().ToUpperInvariant();
}
