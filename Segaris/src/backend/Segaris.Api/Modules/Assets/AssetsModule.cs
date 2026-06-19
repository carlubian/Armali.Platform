using Segaris.Api.Composition;
using Segaris.Api.Modules.Assets.Attention;
using Segaris.Api.Modules.Assets.Contracts;
using Segaris.Api.Modules.Assets.Domain;
using Segaris.Api.Modules.Assets.Mutations;
using Segaris.Api.Modules.Assets.Persistence;
using Segaris.Api.Modules.Assets.Queries;
using Segaris.Api.Modules.Assets.Seeding;
using Segaris.Api.Modules.Launcher.Contracts;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Assets;

/// <summary>
/// Business module for the household's individually identified durable objects.
/// Wave 0 registered the module and froze the public contracts; Wave 1 adds the
/// persistence model, the one-time category and location initialization, and the
/// module-owned category and location catalog read and administrator management
/// endpoints surfaced through Configuration; later waves add asset operations,
/// attachments with a primary image, launcher attention, and Configuration
/// reference migration. Assets does not depend on any other business module.
/// </summary>
internal sealed class AssetsModule : ISegarisModule
{
    public string Name => "Assets";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ISegarisModelContributor, AssetsModelContributor>();
        services.AddScoped<AssetsSeeder>();
        services.AddScoped<AssetReadService>();
        services.AddScoped<IAssetReferenceReader, AssetReferenceReader>();
        services.AddScoped<AssetWriteService>();
        services.AddScoped<AssetCatalogValidator>();
        services.AddScoped<AssetCategoryManagementService>();
        services.AddScoped<AssetLocationManagementService>();
        services.AddScoped<ILauncherAttentionContributor, AssetsAttentionContributor>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapAssetsEndpoints();
    }
}
