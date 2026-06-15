namespace Segaris.Api.Modules.Configuration;

/// <summary>
/// Frozen route shapes for the Configuration catalog endpoints. Prefixes are
/// relative to <c>/api</c> as required by <c>MapSegarisApiGroup</c>. The read
/// collection paths remain authenticated for business forms; the per-row
/// management templates below are mapped onto each catalog collection in later
/// waves and require the <c>Admin</c> role plus antiforgery on writes.
/// </summary>
internal static class ConfigurationApiRoutes
{
    public const string Tag = "Configuration";

    public const string Suppliers = "configuration/suppliers";

    public const string CostCenters = "configuration/cost-centers";

    public const string Currencies = "configuration/currencies";

    /// <summary>Update path relative to a catalog collection.</summary>
    public const string ById = "/{id:int}";

    /// <summary>Move-up/move-down path relative to a catalog collection.</summary>
    public const string Move = "/{id:int}/move";

    /// <summary>Privacy-neutral deletion-impact path relative to a catalog collection.</summary>
    public const string DeletionImpact = "/{id:int}/deletion-impact";

    /// <summary>Replace-and-delete path relative to a catalog collection.</summary>
    public const string ReplaceAndDelete = "/{id:int}/replace-and-delete";
}
