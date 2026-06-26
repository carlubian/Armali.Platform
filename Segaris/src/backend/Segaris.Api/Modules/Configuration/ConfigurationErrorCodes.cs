using Segaris.Shared.Api;

namespace Segaris.Api.Modules.Configuration;

/// <summary>
/// Stable, Configuration-specific <see cref="ErrorCode"/> values returned through
/// <c>ApiProblemException</c> by the shared-catalog management endpoints. Generic
/// authorization, antiforgery, and transport failures continue to use the platform
/// <c>ApiErrorCodes</c>. Capex category management uses the parallel Capex-owned
/// codes in <c>CapexErrorCodes</c>.
/// </summary>
internal static class ConfigurationErrorCodes
{
    /// <summary>The addressed catalog value does not exist.</summary>
    public static readonly ErrorCode CatalogNotFound = new("configuration.catalog.not_found");

    /// <summary>The request failed catalog validation; may carry field errors.</summary>
    public static readonly ErrorCode CatalogValidation = new("configuration.catalog.validation");

    /// <summary>Another value in the catalog already uses the name (case-insensitive).</summary>
    public static readonly ErrorCode CatalogDuplicateName = new("configuration.catalog.duplicate_name");

    /// <summary>Another currency already uses the code (case-insensitive).</summary>
    public static readonly ErrorCode CurrencyDuplicateCode = new("configuration.currency.duplicate_code");

    /// <summary>The currency code is not exactly three letters.</summary>
    public static readonly ErrorCode CurrencyInvalidCode = new("configuration.currency.invalid_code");

    /// <summary>A non-EUR currency was created or updated without an exchange rate to EUR.</summary>
    public static readonly ErrorCode CurrencyExchangeRateRequired = new("configuration.currency.exchange_rate_required");

    /// <summary>The supplied exchange rate to EUR is not positive or exceeds eight decimal places.</summary>
    public static readonly ErrorCode CurrencyExchangeRateInvalid = new("configuration.currency.exchange_rate_invalid");

    /// <summary>The EUR currency was given an exchange rate other than the fixed value <c>1</c>.</summary>
    public static readonly ErrorCode CurrencyExchangeRateNotOne = new("configuration.currency.exchange_rate_not_one");

    /// <summary>The last row of a required catalog cannot be removed.</summary>
    public static readonly ErrorCode CatalogRequiredNotEmpty = new("configuration.catalog.required_not_empty");

    /// <summary>A direct delete was attempted on a value that is still referenced.</summary>
    public static readonly ErrorCode CatalogReferenced = new("configuration.catalog.referenced");

    /// <summary>The requested replacement target is missing, equal to the source, or otherwise invalid.</summary>
    public static readonly ErrorCode CatalogInvalidReplacement = new("configuration.catalog.invalid_replacement");

    /// <summary>Deleting a referenced currency requires an exchange rate that was not supplied.</summary>
    public static readonly ErrorCode CatalogExchangeRateRequired = new("configuration.catalog.exchange_rate_required");

    /// <summary>The supplied exchange rate is not positive or exceeds eight decimal places.</summary>
    public static readonly ErrorCode CatalogExchangeRateInvalid = new("configuration.catalog.exchange_rate_invalid");

    /// <summary>A concurrent change invalidated the source, replacement, or references.</summary>
    public static readonly ErrorCode CatalogMigrationConflict = new("configuration.catalog.migration_conflict");

    /// <summary>A consumer could not complete its reference migration; the transaction rolled back.</summary>
    public static readonly ErrorCode CatalogMigrationFailed = new("configuration.catalog.migration_failed");

    /// <summary>A consumer requires a replacement value because its references are mandatory and cannot be cleared.</summary>
    public static readonly ErrorCode CatalogReplacementRequired = new("configuration.catalog.replacement_required");
}
