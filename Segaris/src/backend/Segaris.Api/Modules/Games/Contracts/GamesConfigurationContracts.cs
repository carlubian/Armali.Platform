using Segaris.Api.Modules.Configuration.Contracts;

namespace Segaris.Api.Modules.Games.Contracts;

/// <summary>The catalogues Games owns and surfaces through Configuration.</summary>
internal enum GamesCatalogKind
{
    Games,
}

/// <summary>
/// Frozen per-catalogue rules for Games-owned catalogues surfaced through the
/// Configuration presentation boundary. Every playthrough requires a game, so a
/// referenced game may only be replaced, never cleared. The catalogue is optional:
/// it may be empty, so its last remaining row can still be removed when unreferenced.
/// </summary>
internal sealed record GamesCatalogDescriptor(
    GamesCatalogKind Kind,
    string RouteSegment,
    bool IsRequired,
    bool SupportsClearing);

internal static class GamesConfigurationContracts
{
    /// <summary>
    /// Games consumes no shared Configuration catalogue: <c>Game</c> is fully
    /// module-owned and only presented through the Configuration boundary.
    /// </summary>
    public static readonly IReadOnlyList<ConfigurationCatalogKind> SharedReferenceKinds = [];

    public static readonly IReadOnlyList<GamesCatalogDescriptor> OwnedCatalogs =
    [
        new(GamesCatalogKind.Games, "games", IsRequired: false, SupportsClearing: false),
    ];
}
