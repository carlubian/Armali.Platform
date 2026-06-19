using Segaris.Api.Composition;
using Segaris.Api.Modules.Assets.Contracts;
using Segaris.Api.Modules.Launcher.Contracts;
using Segaris.Api.Modules.Maintenance.Attention;
using Segaris.Api.Modules.Maintenance.Mutations;
using Segaris.Api.Modules.Maintenance.Persistence;
using Segaris.Api.Modules.Maintenance.Queries;
using Segaris.Api.Modules.Maintenance.Seeding;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Maintenance;

/// <summary>
/// Business module for the repair and maintenance work the household carries out on
/// its physical elements, keeping the history of that work once it is done.
/// Wave 0 registers the module and freezes the public contracts (task routes, the
/// status and priority vocabularies, DTOs, query contracts, stable error codes, the
/// attachment owner kind, the launcher attention key, the Configuration-facing type
/// contracts, and the Assets read and deletion-reference contracts owned by Assets).
/// Later waves add the persistence model and the module-owned <c>MaintenanceType</c>
/// catalogue, the task read and mutation APIs, the optional live Assets reference and
/// its visibility rule, the Assets deletion guard implemented by contract inversion,
/// attachments, launcher attention, and the frontend experience. Maintenance is the
/// first business module that references another business module: it may consume
/// Configuration, Assets, and platform contracts but must not depend on Capex, Opex,
/// Inventory, Travel, Clothes, or Mood, and Assets must never depend on Maintenance.
/// </summary>
internal sealed class MaintenanceModule : ISegarisModule
{
    public string Name => "Maintenance";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ISegarisModelContributor, MaintenanceModelContributor>();
        services.AddScoped<MaintenanceSeeder>();
        services.AddScoped<MaintenanceTypeReadService>();
        services.AddScoped<MaintenanceTypeManagementService>();
        services.AddScoped<MaintenanceTaskReadService>();
        services.AddScoped<MaintenanceTaskWriteService>();
        services.AddScoped<IAssetDeletionReferenceHandler, MaintenanceAssetDeletionReferenceHandler>();
        services.AddScoped<ILauncherAttentionContributor, MaintenanceAttentionContributor>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        // Wave 1 maps the type catalogue routes; Wave 2 onward adds the task and
        // attachment HTTP surface frozen in MaintenanceApiRoutes.
        endpoints.MapMaintenanceEndpoints();
    }
}
