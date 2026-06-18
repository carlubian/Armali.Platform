using Segaris.Api.Composition;
using Segaris.Api.Modules.Clothes.Mutations;
using Segaris.Api.Modules.Clothes.Persistence;
using Segaris.Api.Modules.Clothes.Queries;
using Segaris.Api.Modules.Clothes.Seeding;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Clothes;

/// <summary>
/// Business module for household garments and accessories. Wave 0 registered the
/// module and froze the public contracts; Wave 1 adds the persistence model, the
/// one-time category and colour initialization, and the module-owned category and
/// colour catalog read and administrator management endpoints surfaced through
/// Configuration. Later waves add garment operations, attachments, Configuration
/// reference migration, and the frontend. The module registers no launcher attention
/// contributor.
/// </summary>
internal sealed class ClothesModule : ISegarisModule
{
    public string Name => "Clothes";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ISegarisModelContributor, ClothesModelContributor>();
        services.AddScoped<ClothesSeeder>();
        services.AddScoped<ClothesReadService>();
        services.AddScoped<ClothesGarmentWriteService>();
        services.AddScoped<ClothingCategoryManagementService>();
        services.AddScoped<ClothingColorManagementService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapClothesEndpoints();
    }
}
