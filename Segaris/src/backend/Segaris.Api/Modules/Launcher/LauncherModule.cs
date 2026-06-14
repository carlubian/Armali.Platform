using Segaris.Api.Composition;

namespace Segaris.Api.Modules.Launcher;

/// <summary>
/// Platform module that aggregates per-module launcher attention into a single
/// response for the launcher UI. Wave 0 only registers the module shell and
/// freezes the contributor contract and aggregated response shape; the
/// aggregation service and <c>GET /api/launcher/attention</c> endpoint are added
/// in Wave 3, and business modules register their own contributors.
/// </summary>
internal sealed class LauncherModule : ISegarisModule
{
    public string Name => "Launcher";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        // Wave 3 registers the attention aggregation service here. Contributors
        // are registered by their owning business modules.
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        // Wave 3 maps the attention endpoint described by LauncherApiRoutes here.
    }
}
