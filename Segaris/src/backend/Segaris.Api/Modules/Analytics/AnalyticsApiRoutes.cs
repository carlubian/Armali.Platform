namespace Segaris.Api.Modules.Analytics;

/// <summary>Frozen route shapes for the Analytics HTTP surface.</summary>
internal static class AnalyticsApiRoutes
{
    public const string Tag = "Analytics";
    public const string Analytics = "analytics";

    public const string Overview = "/overview";
    public const string Capex = "/capex";
    public const string Opex = "/opex";
    public const string Inventory = "/inventory";
    public const string Travel = "/travel";
    public const string CrossModule = "/cross-module";

    public static class QueryParameters
    {
        public const string Year = "year";
        public const string Tab = "tab";
    }
}
