using Segaris.Api.Composition;

namespace Segaris.Api.Modules.Assets;

/// <summary>
/// Business module for the household's individually identified durable objects.
/// Wave 0 registers the module and freezes the public contracts; later waves add
/// persistence, the required category and location catalogues, asset operations,
/// attachments with a primary image, launcher attention, and Configuration
/// reference migration. Assets does not depend on any other business module.
/// </summary>
internal sealed class AssetsModule : ISegarisModule
{
    public string Name => "Assets";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapAssetsEndpoints();
    }
}
