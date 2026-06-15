using Segaris.Api.Composition;

namespace Segaris.Api.Modules.Opex;

/// <summary>
/// Business module for recurrent income and expenses. Wave 0 registers the
/// module and freezes its public contracts; persistence and endpoints are added
/// by later Waves.
/// </summary>
internal sealed class OpexModule : ISegarisModule
{
    public string Name => "Opex";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
    }
}
