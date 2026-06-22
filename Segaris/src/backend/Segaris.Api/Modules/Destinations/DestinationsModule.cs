using Segaris.Api.Composition;

namespace Segaris.Api.Modules.Destinations;

/// <summary>
/// Business module for visited destinations and destination-scoped places. Wave 0
/// registers the module and freezes its public contracts; later waves add
/// persistence, APIs, attachments, Travel integration, and frontend surfaces. The
/// module registers no launcher attention contributor: its launcher card never
/// requests attention.
/// </summary>
internal sealed class DestinationsModule : ISegarisModule
{
    public string Name => "Destinations";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
    }
}
