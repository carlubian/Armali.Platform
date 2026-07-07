namespace Segaris.Api.Modules.Games.Domain;

/// <summary>
/// Fixed platform vocabulary for a catalogue <c>Game</c>. The platform set is not
/// administrator-configurable in the initial release. These are domain values,
/// persisted as bounded strings using the member names and exchanged on the wire
/// using the same names.
/// </summary>
internal enum GamePlatform
{
    PC,
    Console,
    Mobile,
    BoardGame,
    TabletopRpg,
    Other,
}
