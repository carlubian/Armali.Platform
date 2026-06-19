namespace Segaris.Api.Modules.Assets;

/// <summary>
/// Frozen identity used by the Assets launcher attention contributor (Wave 4).
/// The key is the stable module identifier reported in the aggregated launcher
/// attention response. Attention becomes true when the current user can access a
/// non-<c>Retired</c> asset whose expected end of life falls within the inclusive
/// window from today to today plus 30 natural days in <c>Europe/Madrid</c>.
/// </summary>
internal static class AssetsLauncherCard
{
    public const string ModuleKey = "assets";
}
