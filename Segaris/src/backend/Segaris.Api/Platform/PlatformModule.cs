using Segaris.Api.Composition;
using Segaris.Api.Platform.Api;
using Segaris.Api.Platform.Attachments;
using Segaris.Api.Platform.Backup;
using Segaris.Api.Platform.Jobs;
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
        services.AddSegarisJobs();
        services.AddSegarisBackup();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapBackupEndpoints();

        var environment = endpoints.ServiceProvider.GetRequiredService<IHostEnvironment>();
        if (environment.IsEnvironment("Testing"))
        {
            endpoints.MapApiConventionProbes();
            endpoints.MapAttachmentProbes();
            endpoints.MapJobProbes();
        }
    }
}
