namespace Segaris.Api.Modules.Configuration;

/// <summary>
/// Frozen route shapes for the read-only Configuration catalog endpoints.
/// Prefixes are relative to <c>/api</c> as required by <c>MapSegarisApiGroup</c>.
/// </summary>
internal static class ConfigurationApiRoutes
{
    public const string Tag = "Configuration";

    public const string Suppliers = "configuration/suppliers";

    public const string CostCenters = "configuration/cost-centers";

    public const string Currencies = "configuration/currencies";
}
