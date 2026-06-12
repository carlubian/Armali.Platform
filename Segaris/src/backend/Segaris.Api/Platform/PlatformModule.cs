using Segaris.Api.Composition;
using Segaris.Api.Platform.Api;
using Segaris.Api.Platform.Attachments;
using Segaris.Api.Platform.Persistence;
using Segaris.Persistence;

namespace Segaris.Api.Platform;

internal sealed class PlatformModule : ISegarisModule
{
    public string Name => "Platform";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ISegarisModelContributor, PlatformModelContributor>();
        services.AddSegarisAttachments(configuration);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var environment = endpoints.ServiceProvider.GetRequiredService<IHostEnvironment>();
        if (environment.IsEnvironment("Testing"))
        {
            endpoints.MapApiConventionProbes();
            endpoints.MapAttachmentProbes();
        }
    }
}
