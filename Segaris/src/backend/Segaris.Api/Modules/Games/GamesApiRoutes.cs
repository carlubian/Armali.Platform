namespace Segaris.Api.Modules.Games;

/// <summary>
/// Frozen route shapes for the Games HTTP surface. The prefix is relative to
/// <c>/api</c> as required by <c>MapSegarisApiGroup</c>; the templates document the
/// per-route patterns. Section and goal routes are always scoped through their
/// owning playthrough. Administrative game routes follow the module-owned catalogue
/// management pattern surfaced through Configuration.
/// </summary>
internal static class GamesApiRoutes
{
    public const string Tag = "Games";
    public const string Games = "games";

    // Module-owned game catalogue surfaced through Configuration.
    public const string GameCatalogue = "games/games";
    public const string GameById = "/{gameId:int}";
    public const string GameMove = "/{gameId:int}/move";
    public const string GameDeletionImpact = "/{gameId:int}/deletion-impact";
    public const string GameReplaceAndDelete = "/{gameId:int}/replace-and-delete";

    // Playthrough collection and detail.
    public const string Playthroughs = "games/playthroughs";
    public const string PlaythroughById = "/{playthroughId:int}";

    // Playthrough-scoped sections.
    public const string Sections = "/{playthroughId:int}/sections";
    public const string SectionsOrder = "/{playthroughId:int}/sections/order";
    public const string SectionById = "/{playthroughId:int}/sections/{sectionId:int}";

    // Section-scoped goals.
    public const string Goals = "/{playthroughId:int}/sections/{sectionId:int}/goals";
    public const string GoalById = "/{playthroughId:int}/sections/{sectionId:int}/goals/{goalId:int}";
    public const string GoalCompletion = "/{playthroughId:int}/sections/{sectionId:int}/goals/{goalId:int}/completion";
}
