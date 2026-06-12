using Segaris.Api.Composition;

namespace Segaris.Api.Modules.Identity;

internal sealed class IdentityModule : ISegarisModule
{
    public string Name => "Identity";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
    }
}
