using Segaris.Api.Composition;

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
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
    }
}
