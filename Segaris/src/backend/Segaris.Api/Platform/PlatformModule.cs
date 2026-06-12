using Segaris.Api.Composition;
using Segaris.Api.Platform.Persistence;
using Segaris.Persistence;

namespace Segaris.Api.Platform;

internal sealed class PlatformModule : ISegarisModule
{
    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ISegarisModelContributor, PlatformModelContributor>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
    }
}
