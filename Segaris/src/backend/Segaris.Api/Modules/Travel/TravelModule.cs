using Segaris.Api.Composition;

namespace Segaris.Api.Modules.Travel;

/// <summary>
/// Business module for household trips, embedded itineraries, and per-trip
/// expenses. Wave 0 registers the module and freezes its public contracts; later
/// waves add persistence, reads, mutations, attachments, Configuration reference
/// handlers, and launcher attention.
/// </summary>
internal sealed class TravelModule : ISegarisModule
{
    public string Name => "Travel";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapTravelEndpoints();
    }
}
