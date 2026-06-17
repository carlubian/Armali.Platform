using Segaris.Api.Composition;
using Segaris.Api.Modules.Launcher.Contracts;
using Segaris.Api.Modules.Travel.Attention;
using Segaris.Api.Modules.Travel.Mutations;
using Segaris.Api.Modules.Travel.Persistence;
using Segaris.Api.Modules.Travel.Queries;
using Segaris.Api.Modules.Travel.Seeding;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Travel;

/// <summary>
/// Business module for household trips, embedded itineraries, and per-trip
/// expenses. Wave 0 registered the module and froze its public contracts; Wave 1
/// adds the persistence model, the one-time trip-type and expense-category
/// initialization, and the module-owned trip-type and expense-category catalog read
/// and administrator management endpoints surfaced through Configuration. Later waves
/// add the trip read, mutation, itinerary, attachment, and expense surfaces, the
/// shared-catalog reference handlers, and launcher attention.
/// </summary>
internal sealed class TravelModule : ISegarisModule
{
    public string Name => "Travel";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ISegarisModelContributor, TravelModelContributor>();
        services.AddScoped<TravelSeeder>();
        services.AddScoped<TravelReadService>();
        services.AddScoped<TravelTripWriteService>();
        services.AddScoped<TravelTripTypeManagementService>();
        services.AddScoped<TravelExpenseCategoryManagementService>();
        services.AddScoped<ILauncherAttentionContributor, TravelAttentionContributor>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapTravelEndpoints();
    }
}
