namespace Segaris.Api.Modules.Configuration;

/// <summary>
/// Frozen seed definitions for the shared Configuration catalogs.
///
/// Catalog rows use a database-assigned auto-increment <c>Id</c>; the stable
/// identity for idempotent seeding and lookup is the <see cref="CatalogSeed.Code"/>.
/// Display names are the canonical <c>en-GB</c> values and are localizable in the
/// presentation layer; they never serve as database identities or API references.
/// </summary>
internal static class ConfigurationCatalog
{
    public static class SupplierCodes
    {
        public const string Amazon = "AMAZON";
        public const string Ikea = "IKEA";
        public const string Carrefour = "CARREFOUR";
        public const string ElCorteIngles = "EL_CORTE_INGLES";
        public const string LeroyMerlin = "LEROY_MERLIN";
        public const string Other = "OTHER";
    }

    public static class CostCenterCodes
    {
        public const string Household = "HOUSEHOLD";
        public const string Personal = "PERSONAL";
        public const string Work = "WORK";
        public const string Shared = "SHARED";
        public const string Other = "OTHER";
    }

    public static class CurrencyCodes
    {
        public const string Euro = "EUR";
        public const string UsDollar = "USD";
        public const string PoundSterling = "GBP";

        public const string Default = Euro;
    }

    public static readonly IReadOnlyList<CatalogSeed> Suppliers =
    [
        new(SupplierCodes.Amazon, "Amazon"),
        new(SupplierCodes.Ikea, "IKEA"),
        new(SupplierCodes.Carrefour, "Carrefour"),
        new(SupplierCodes.ElCorteIngles, "El Corte Inglés"),
        new(SupplierCodes.LeroyMerlin, "Leroy Merlin"),
        new(SupplierCodes.Other, "Other"),
    ];

    public static readonly IReadOnlyList<CatalogSeed> CostCenters =
    [
        new(CostCenterCodes.Household, "Household"),
        new(CostCenterCodes.Personal, "Personal"),
        new(CostCenterCodes.Work, "Work"),
        new(CostCenterCodes.Shared, "Shared"),
        new(CostCenterCodes.Other, "Other"),
    ];

    public static readonly IReadOnlyList<CatalogSeed> Currencies =
    [
        new(CurrencyCodes.Euro, "Euro"),
        new(CurrencyCodes.UsDollar, "US Dollar"),
        new(CurrencyCodes.PoundSterling, "Pound Sterling"),
    ];
}

/// <summary>
/// A single frozen catalog seed row: its stable <paramref name="Code"/> and the
/// canonical display <paramref name="Name"/>.
/// </summary>
internal sealed record CatalogSeed(string Code, string Name);
