using Segaris.Api.Composition;
using Segaris.Api.Modules.Analytics.Projection;
using Segaris.Api.Modules.Analytics.Queries;

namespace Segaris.Api.Modules.Analytics;

/// <summary>
/// Cross-domain read module for financial Analytics. Wave 0 registers the shell
/// and freezes route, chart, source-projection, and currency-read contracts.
/// Later waves add aggregation endpoints and UI-backed behavior.
/// </summary>
internal sealed class AnalyticsModule : ISegarisModule
{
    public string Name => "Analytics";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<AnalyticsOverviewService>();
        services.AddScoped<AnalyticsModuleGroupingService>();
        services.AddScoped<IAnalyticsFinancialProjectionProvider, CapexAnalyticsFinancialProjectionAdapter>();
        services.AddScoped<IAnalyticsFinancialProjectionProvider, OpexAnalyticsFinancialProjectionAdapter>();
        services.AddScoped<IAnalyticsFinancialProjectionProvider, InventoryAnalyticsFinancialProjectionAdapter>();
        services.AddScoped<IAnalyticsFinancialProjectionProvider, TravelAnalyticsFinancialProjectionAdapter>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapAnalyticsEndpoints();
    }
}
