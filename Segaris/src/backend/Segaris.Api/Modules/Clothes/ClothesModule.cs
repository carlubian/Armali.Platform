using Segaris.Api.Composition;

namespace Segaris.Api.Modules.Clothes;

/// <summary>
/// Business module for household garments and accessories. Wave 0 registers the
/// module and freezes the public contracts; later waves add persistence,
/// catalogues, garment operations, attachments, and Configuration integration.
/// </summary>
internal sealed class ClothesModule : ISegarisModule
{
    public string Name => "Clothes";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapClothesEndpoints();
    }
}
