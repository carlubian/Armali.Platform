namespace Segaris.Api.Modules.Mood;

/// <summary>
/// HTTP surface seam for the Mood module. Wave 0 freezes the route shapes in
/// <see cref="MoodApiRoutes"/> but maps no endpoints yet; the owner-only entry,
/// weekly-log, options, and dashboard endpoints are added in later waves.
/// </summary>
internal static class MoodEndpoints
{
    public static IEndpointRouteBuilder MapMoodEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Intentionally empty in Wave 0. Entry CRUD and the weekly log arrive in
        // Wave 2; the dashboard aggregates arrive in Wave 3.
        return endpoints;
    }
}
