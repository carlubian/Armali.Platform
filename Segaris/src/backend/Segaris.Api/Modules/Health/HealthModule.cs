using Segaris.Api.Composition;
using Segaris.Api.Modules.Health.Mutations;
using Segaris.Api.Modules.Health.Persistence;
using Segaris.Api.Modules.Health.Queries;
using Segaris.Api.Modules.Health.Seeding;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Health;

/// <summary>
/// Business module for diseases, medicines, their symmetric association, medicine
/// attachments, and medicine-to-Inventory item references. Wave 0 registered the
/// shell and froze the public contracts; Wave 1 adds the persistence model, the
/// one-time category initialization, and the module-owned disease and medicine
/// category catalogue read and administrator management endpoints surfaced through
/// Configuration; later waves add disease, medicine, association, attachment, and the
/// Inventory link behavior. Health contributes no launcher attention.
/// </summary>
internal sealed class HealthModule : ISegarisModule
{
    public string Name => "Health";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ISegarisModelContributor, HealthModelContributor>();
        services.AddScoped<HealthSeeder>();
        services.AddScoped<HealthCatalogReadService>();
        services.AddScoped<DiseaseReadService>();
        services.AddScoped<DiseaseWriteService>();
        services.AddScoped<MedicineReadService>();
        services.AddScoped<MedicineWriteService>();
        services.AddScoped<DiseaseCategoryManagementService>();
        services.AddScoped<MedicineCategoryManagementService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthEndpoints();
    }
}
