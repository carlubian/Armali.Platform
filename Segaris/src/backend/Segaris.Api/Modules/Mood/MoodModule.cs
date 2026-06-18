using Segaris.Api.Composition;
using Segaris.Api.Modules.Mood.Persistence;
using Segaris.Persistence;

namespace Segaris.Api.Modules.Mood;

/// <summary>
/// Privacy-first business module for the current user's personal mood check-ins.
/// Wave 0 registered the module shell and froze its public contracts. Wave 1 adds
/// the <c>MoodEntry</c> persistence model and code-backed derived-emotion matrix.
/// Later waves add the owner-only entry and weekly-log APIs, and the strict-period
/// dashboard aggregates.
/// </summary>
/// <remarks>
/// Unlike the other business modules, Mood owns its fixed criteria and
/// derived-emotion matrix and must not depend on Configuration catalogs,
/// Attachments, Launcher attention, Analytics, or any other business module.
/// </remarks>
internal sealed class MoodModule : ISegarisModule
{
    public string Name => "Mood";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ISegarisModelContributor, MoodModelContributor>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapMoodEndpoints();
    }
}
