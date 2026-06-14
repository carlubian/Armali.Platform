using Segaris.Api.Modules.Launcher.Contracts;
using Segaris.Api.Platform.Api;

namespace Segaris.Api.Modules.Launcher;

internal static class LauncherEndpoints
{
    public static void MapLauncherEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapSegarisApiGroup("launcher", LauncherApiRoutes.Tag)
            .RequireAuthorization();

        group.MapGet("/attention", GetAttentionAsync)
            .WithName("GetLauncherAttention")
            .WithSummary("Returns the per-module launcher attention state for the current user")
            .Produces<LauncherAttentionResponse>();
    }

    private static async Task<IResult> GetAttentionAsync(
        LauncherAttentionService attention,
        CancellationToken cancellationToken)
    {
        var response = await attention.GetAttentionAsync(cancellationToken);
        return TypedResults.Ok(response);
    }
}
