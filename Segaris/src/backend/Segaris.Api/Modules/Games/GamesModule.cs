using Segaris.Api.Composition;

namespace Segaris.Api.Modules.Games;

/// <summary>
/// Independent business module recording household progress through video games,
/// board games, tabletop campaigns, and similar entertainment. It owns the
/// administrator-managed <c>Game</c> catalogue surfaced through Configuration and
/// the user-owned <c>Playthrough</c>, <c>Section</c>, and <c>Goal</c> entities.
/// Wave 0 registers the shell and freezes the public contracts; later waves add
/// persistence, the game catalogue, playthrough, section, and goal behavior, and
/// the frontend. Games consumes only Configuration, Launcher, Identity, and
/// platform contracts, depends on no other business module, and its launcher card
/// never requests attention.
/// </summary>
internal sealed class GamesModule : ISegarisModule
{
    public string Name => "Games";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGamesEndpoints();
    }
}
