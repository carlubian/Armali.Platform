using Segaris.Api.Composition;

namespace Segaris.Api.Modules.Launcher;

/// <summary>
/// Platform module that aggregates per-module launcher attention into a single
/// response for the launcher UI. The aggregation service and
/// <c>GET /api/launcher/attention</c> endpoint are wired here; business modules
/// register their own <c>ILauncherAttentionContributor</c> implementations.
/// </summary>
internal sealed class LauncherModule : ISegarisModule
{
    public string Name => "Launcher";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        // Contributors are registered by their owning business modules; the
        // aggregation service resolves all of them through the contract.
        services.AddScoped<LauncherAttentionService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapLauncherEndpoints();
    }
}
