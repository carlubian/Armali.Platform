using Segaris.Api.Composition;

namespace Segaris.Api.Modules.Mood;

/// <summary>
/// Privacy-first business module for the current user's personal mood check-ins.
/// Wave 0 registers the module shell and freezes its public contracts (entry and
/// dashboard routes, criteria enums, request/response DTOs, the strict
/// dashboard-period contract, and stable error codes) without adding persistence
/// or endpoints. Later waves add the <c>MoodEntry</c> model and code-backed
/// derived-emotion matrix, the owner-only entry and weekly-log APIs, and the
/// strict-period dashboard aggregates.
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
        // Wave 0 registers no services. Persistence, the derived-emotion matrix,
        // the entry/log services, and the dashboard aggregates arrive in later waves.
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapMoodEndpoints();
    }
}
