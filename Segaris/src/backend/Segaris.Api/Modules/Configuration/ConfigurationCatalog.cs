namespace Segaris.Api.Modules.Configuration;

/// <summary>
/// Frozen initial values for the shared Configuration catalogs, inserted only once
/// by the one-time initialization service. Rows are assigned a database <c>Id</c>
/// and a <c>SortOrder</c> following their declaration order here; display names are
/// the canonical <c>en-GB</c> values and are localizable in the presentation layer.
///
/// Non-currency catalogs no longer carry a stable code: their identity is the
/// generated <c>Id</c>. Currencies keep an editable three-letter display code.
/// </summary>
internal static class ConfigurationCatalog
{
    public static class CurrencyCodes
    {
        public const string Euro = "EUR";
        public const string UsDollar = "USD";
        public const string PoundSterling = "GBP";

        public const string Default = Euro;
    }

    public static readonly IReadOnlyList<CatalogSeed> Suppliers =
    [
        new("Amazon"),
        new("IKEA"),
        new("Carrefour"),
        new("El Corte Inglés"),
        new("Leroy Merlin"),
        new("Other"),
    ];

    public static readonly IReadOnlyList<CatalogSeed> CostCenters =
    [
        new("Household"),
        new("Personal"),
        new("Work"),
        new("Shared"),
        new("Other"),
    ];

    // Non-EUR seed rates are explicit development placeholders, not authoritative
    // economic data. Administrators are expected to maintain the live rates through
    // Configuration. EUR is fixed at 1.
    public static readonly IReadOnlyList<CurrencySeed> Currencies =
    [
        new(CurrencyCodes.Euro, "Euro", 1m),
        new(CurrencyCodes.UsDollar, "US Dollar", 0.92m),
        new(CurrencyCodes.PoundSterling, "Pound Sterling", 1.17m),
    ];
}

/// <summary>
/// A single frozen catalog seed row identified only by its canonical display
/// <paramref name="Name"/>; ordering follows declaration order.
/// </summary>
internal sealed record CatalogSeed(string Name);

/// <summary>
/// A single frozen currency seed row: its editable display <paramref name="Code"/>,
/// canonical display <paramref name="Name"/>, and seeded current
/// <paramref name="ExchangeRateToEur"/> (a development placeholder for non-EUR).
/// </summary>
internal sealed record CurrencySeed(string Code, string Name, decimal ExchangeRateToEur);
