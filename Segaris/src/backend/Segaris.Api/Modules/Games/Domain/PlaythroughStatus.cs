namespace Segaris.Api.Modules.Games.Domain;

/// <summary>
/// Fixed, manually controlled playthrough status. The system never changes it
/// automatically and never validates it against goal progress: a playthrough may
/// be <see cref="Completed"/> with incomplete goals, and completing every goal
/// does not change the status. New playthroughs default to <see cref="Planning"/>.
/// These are domain values, not an administrator-managed catalogue, persisted as
/// bounded strings using the member names and exchanged on the wire using the same
/// names.
/// </summary>
internal enum PlaythroughStatus
{
    Planning,
    Active,
    Completed,
}
