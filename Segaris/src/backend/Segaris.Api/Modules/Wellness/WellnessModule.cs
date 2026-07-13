using Segaris.Api.Composition;

namespace Segaris.Api.Modules.Wellness;

/// <summary>
/// Independent business module delivering a daily set of healthy-habit tasks that
/// each user completes, producing a persisted per-day score that is also visualized
/// inside the Mood weekly log. It owns the administrator-managed <c>WellnessTask</c>
/// catalogue surfaced through Configuration, the user-owned <c>WellnessDay</c> and
/// <c>WellnessDayTask</c> entities, the fixed <c>WellnessCategory</c> enum, the daily
/// selection algorithm, and the per-day score projection. Wave 0 registers the shell
/// and freezes the public contracts; later waves add persistence, the task catalogue,
/// daily generation, scoring, the day APIs, and the frontend. Wellness consumes only
/// Configuration, Launcher, Identity, and platform contracts, depends on no other
/// business module (Mood in particular gains no dependency on Wellness), and its
/// launcher card never requests attention.
/// </summary>
internal sealed class WellnessModule : ISegarisModule
{
    public string Name => "Wellness";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapWellnessEndpoints();
    }
}
